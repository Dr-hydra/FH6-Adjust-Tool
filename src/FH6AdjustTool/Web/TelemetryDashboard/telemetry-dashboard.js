const state = {
  pending: new Map(),
  requestId: 1,
  snapshot: null,
  samples: [],
  laps: [],
  comparisons: [],
  issues: [],
  sessions: [],
  replayIndex: null,
  replayPlaying: false,
  playTimer: null
};

const $ = (id) => document.getElementById(id);

window.telemetryBridge = {
  receive(response) {
    const item = typeof response === "string" ? JSON.parse(response) : response;
    const pending = state.pending.get(item.id);
    if (!pending) return;
    state.pending.delete(item.id);
    if (item.ok) pending.resolve(item.payload);
    else pending.reject(new Error(item.error || "Bridge request failed"));
  }
};

function bridge(action, payload = {}) {
  return new Promise((resolve, reject) => {
    if (!window.chrome || !window.chrome.webview) {
      reject(new Error("WebView2 bridge is unavailable"));
      return;
    }
    const id = `req-${state.requestId++}`;
    state.pending.set(id, { resolve, reject });
    window.chrome.webview.postMessage(JSON.stringify({ id, action, payload }));
  });
}

function handleHostEvent(message) {
  const event = typeof message === "string" ? JSON.parse(message) : message;
  if (!event || !event.type) return;

  if (event.type === "telemetry-live") {
    applyDashboardPayload(event.payload, false);
  } else if (event.type === "lap-completed") {
    state.laps = [...state.laps, event.payload].slice(-80);
    renderLaps();
  } else if (event.type === "issue-marked") {
    state.issues = [...state.issues, event.payload].slice(-120);
    renderIssues();
  } else if (event.type === "status") {
    $("statusText").textContent = String(event.payload || "");
  }
}

function applyDashboardPayload(payload, includeTables = true) {
  if (!payload) return;
  const snapshot = payload.snapshot || payload;
  state.snapshot = snapshot;
  state.samples = snapshot.recentSamples || state.samples || [];
  state.issues = snapshot.recentIssues || state.issues || [];
  state.laps = snapshot.recentLaps || state.laps || [];

  if (includeTables) {
    state.sessions = payload.sessions || state.sessions || [];
    state.comparisons = payload.comparisons || state.comparisons || [];
    if (payload.settings) {
      $("hostInput").value = payload.settings.host || "127.0.0.1";
      $("portInput").value = payload.settings.port || 5400;
    }
  }

  renderSnapshot();
  renderRoute();
  renderLaps();
  renderComparisons();
  renderIssues();
}

function renderSnapshot() {
  const snapshot = state.snapshot || {};
  const sample = snapshot.currentSample || {};
  const context = snapshot.context || {};
  $("statusText").textContent = snapshot.status || (snapshot.isRunning ? "监听中" : "未启动");
  $("startBtn").disabled = Boolean(snapshot.isRunning);
  $("stopBtn").disabled = !snapshot.isRunning;
  $("speedValue").textContent = number(sample.speedKmh, 0);
  $("rpmValue").textContent = number(sample.currentRpm, 0);
  $("gearValue").textContent = gearName(sample.gear);
  $("inputValue").textContent = `油 ${percent(sample.throttle)} / 刹 ${percent(sample.brake)}`;
  $("tuneValue").textContent = context.tuneName || "未命名调校";
  $("carValue").textContent = context.carName || sample.carClassName || "未选择车辆";
  $("currentLapValue").textContent = sample.currentLapSeconds ? formatSeconds(sample.currentLapSeconds) : "--";
  $("gameLapValue").textContent = sample.lapNumber ? `Lap ${sample.lapNumber}` : "游戏计时";
  renderDataPanel(sample);
  renderDataChart();
}

function renderRoute() {
  const canvas = $("routeCanvas");
  const rect = canvas.getBoundingClientRect();
  const scale = window.devicePixelRatio || 1;
  const width = Math.max(320, Math.floor(rect.width * scale));
  const height = Math.max(260, Math.floor(rect.height * scale));
  if (canvas.width !== width || canvas.height !== height) {
    canvas.width = width;
    canvas.height = height;
  }

  const ctx = canvas.getContext("2d");
  ctx.clearRect(0, 0, width, height);
  drawGrid(ctx, width, height);

  const samples = (state.samples || []).filter((s) => Number.isFinite(s.x) && Number.isFinite(s.z));
  if (samples.length < 2) {
    drawEmptyRoute(ctx, width, height);
    updateReplaySlider(samples.length);
    return;
  }

  const bounds = getBounds(samples);
  const project = (sample) => {
    const pad = 32 * scale;
    const xRange = Math.max(1, bounds.maxX - bounds.minX);
    const zRange = Math.max(1, bounds.maxZ - bounds.minZ);
    const fit = Math.min((width - pad * 2) / xRange, (height - pad * 2) / zRange);
    return {
      x: pad + (sample.x - bounds.minX) * fit,
      y: height - pad - (sample.z - bounds.minZ) * fit
    };
  };

  const maxSpeed = Math.max(80, ...samples.map((s) => Number(s.speedKmh || 0)));
  ctx.lineWidth = 2.4 * scale;
  ctx.lineCap = "round";
  for (let i = 1; i < samples.length; i++) {
    const a = project(samples[i - 1]);
    const b = project(samples[i]);
    const speedRatio = clamp(Number(samples[i].speedKmh || 0) / maxSpeed, 0, 1);
    ctx.strokeStyle = speedColor(speedRatio);
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.stroke();
  }

  const live = state.replayIndex === null ? samples[samples.length - 1] : samples[Math.min(state.replayIndex, samples.length - 1)];
  const livePoint = project(live);
  ctx.fillStyle = "#9d3f53";
  ctx.beginPath();
  ctx.arc(livePoint.x, livePoint.y, 6 * scale, 0, Math.PI * 2);
  ctx.fill();

  updateReplaySlider(samples.length);
}

function renderDataPanel(sample) {
  const samples = state.samples || [];
  const frontTemp = avg(sample.tireTempFrontLeft, sample.tireTempFrontRight);
  const rearTemp = avg(sample.tireTempRearLeft, sample.tireTempRearRight);
  const suspensionMax = maxAbs(
    sample.suspensionTravelFrontLeft,
    sample.suspensionTravelFrontRight,
    sample.suspensionTravelRearLeft,
    sample.suspensionTravelRearRight
  );
  const peakSlip = Number(sample.peakCombinedSlip || maxAbs(
    sample.tireCombinedSlipFrontLeft,
    sample.tireCombinedSlipFrontRight,
    sample.tireCombinedSlipRearLeft,
    sample.tireCombinedSlipRearRight
  ));

  $("sampleCountValue").textContent = `${samples.length} 样本`;
  $("powerValue").textContent = number(sample.powerKw, 0);
  $("torqueValue").textContent = number(sample.torqueNm, 0);
  $("boostValue").textContent = number(sample.boost, 1);
  $("fuelValue").textContent = number(Number(sample.fuel || 0) * 100, 0);
  $("latGValue").textContent = signed(sample.accelLatG, 2);
  $("longGValue").textContent = signed(sample.accelLongG, 2);
  $("frontTempValue").textContent = number(frontTemp, 0);
  $("rearTempValue").textContent = number(rearTemp, 0);

  setBar("throttle", Number(sample.throttle || 0) / 255);
  setBar("brake", Number(sample.brake || 0) / 255);
  setSignedBar("steer", normalizeSteer(sample.steer));
  setBar("slip", clamp(peakSlip / 1.6, 0, 1), number(peakSlip, 2));
  setBar("suspension", clamp(suspensionMax, 0, 1));
}

function renderDataChart() {
  const canvas = $("dataCanvas");
  if (!canvas) return;
  const rect = canvas.getBoundingClientRect();
  const scale = window.devicePixelRatio || 1;
  const width = Math.max(320, Math.floor(rect.width * scale));
  const height = Math.max(160, Math.floor(rect.height * scale));
  if (canvas.width !== width || canvas.height !== height) {
    canvas.width = width;
    canvas.height = height;
  }

  const ctx = canvas.getContext("2d");
  ctx.clearRect(0, 0, width, height);
  drawChartGrid(ctx, width, height, scale);

  const samples = (state.samples || []).slice(-180);
  if (samples.length < 2) {
    drawEmptyChart(ctx, width, height);
    return;
  }

  drawSeries(ctx, samples, width, height, scale, {
    label: "速度",
    color: "#146c74",
    value: (s) => clamp(Number(s.speedKmh || 0) / 360, 0, 1)
  });
  drawSeries(ctx, samples, width, height, scale, {
    label: "转速",
    color: "#9d3f53",
    value: (s) => clamp(Number(s.currentRpm || 0) / Math.max(1000, Number(s.engineMaxRpm || 8000)), 0, 1)
  });
  drawSeries(ctx, samples, width, height, scale, {
    label: "油门",
    color: "#2c8b57",
    value: (s) => clamp(Number(s.throttle || 0) / 255, 0, 1)
  });
  drawSeries(ctx, samples, width, height, scale, {
    label: "刹车",
    color: "#a66a12",
    value: (s) => clamp(Number(s.brake || 0) / 255, 0, 1)
  });
  drawSeries(ctx, samples, width, height, scale, {
    label: "滑移",
    color: "#253041",
    value: (s) => clamp(Number(s.peakCombinedSlip || maxAbs(
      s.tireCombinedSlipFrontLeft,
      s.tireCombinedSlipFrontRight,
      s.tireCombinedSlipRearLeft,
      s.tireCombinedSlipRearRight
    )) / 1.6, 0, 1)
  });

  drawChartLegend(ctx, scale);
}

function drawGrid(ctx, width, height) {
  ctx.fillStyle = "rgba(255,255,255,0.62)";
  ctx.fillRect(0, 0, width, height);
  ctx.strokeStyle = "rgba(38,48,65,0.08)";
  ctx.lineWidth = 1;
  const step = 40 * (window.devicePixelRatio || 1);
  ctx.beginPath();
  for (let x = 0; x <= width; x += step) {
    ctx.moveTo(x, 0);
    ctx.lineTo(x, height);
  }
  for (let y = 0; y <= height; y += step) {
    ctx.moveTo(0, y);
    ctx.lineTo(width, y);
  }
  ctx.stroke();
}

function drawEmptyRoute(ctx, width, height) {
  ctx.fillStyle = "#687386";
  ctx.font = `${13 * (window.devicePixelRatio || 1)}px Segoe UI`;
  ctx.textAlign = "center";
  ctx.fillText("等待遥测轨迹", width / 2, height / 2);
}

function drawChartGrid(ctx, width, height, scale) {
  ctx.fillStyle = "rgba(255,255,255,0.52)";
  ctx.fillRect(0, 0, width, height);
  ctx.strokeStyle = "rgba(38,48,65,0.08)";
  ctx.lineWidth = 1;
  ctx.beginPath();
  const rows = 4;
  for (let i = 1; i < rows; i++) {
    const y = (height / rows) * i;
    ctx.moveTo(0, y);
    ctx.lineTo(width, y);
  }
  for (let x = 0; x <= width; x += 80 * scale) {
    ctx.moveTo(x, 0);
    ctx.lineTo(x, height);
  }
  ctx.stroke();
}

function drawEmptyChart(ctx, width, height) {
  ctx.fillStyle = "#687386";
  ctx.font = `${12 * (window.devicePixelRatio || 1)}px Segoe UI`;
  ctx.textAlign = "center";
  ctx.fillText("等待速度、转速、输入和滑移曲线", width / 2, height / 2);
}

function drawSeries(ctx, samples, width, height, scale, series) {
  const padX = 18 * scale;
  const padY = 18 * scale;
  const usableW = Math.max(1, width - padX * 2);
  const usableH = Math.max(1, height - padY * 2);
  ctx.strokeStyle = series.color;
  ctx.lineWidth = 1.8 * scale;
  ctx.lineJoin = "round";
  ctx.lineCap = "round";
  ctx.beginPath();
  samples.forEach((sample, index) => {
    const x = padX + (index / Math.max(1, samples.length - 1)) * usableW;
    const y = height - padY - clamp(series.value(sample), 0, 1) * usableH;
    if (index === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });
  ctx.stroke();
}

function drawChartLegend(ctx, scale) {
  const items = [
    ["速度", "#146c74"],
    ["转速", "#9d3f53"],
    ["油门", "#2c8b57"],
    ["刹车", "#a66a12"],
    ["滑移", "#253041"]
  ];
  let x = 14 * scale;
  const y = 16 * scale;
  ctx.font = `${11 * scale}px Segoe UI`;
  ctx.textAlign = "left";
  for (const [label, color] of items) {
    ctx.fillStyle = color;
    ctx.fillRect(x, y - 7 * scale, 16 * scale, 3 * scale);
    ctx.fillStyle = "#687386";
    ctx.fillText(label, x + 22 * scale, y - 3 * scale);
    x += 68 * scale;
  }
}

function speedColor(ratio) {
  const t = clamp(ratio, 0, 1);
  const slow = [20, 108, 116];
  const fast = [157, 63, 83];
  const rgb = slow.map((value, index) => Math.round(value + (fast[index] - value) * t));
  return `rgb(${rgb[0]}, ${rgb[1]}, ${rgb[2]})`;
}

function getBounds(samples) {
  return samples.reduce((bounds, sample) => ({
    minX: Math.min(bounds.minX, sample.x),
    maxX: Math.max(bounds.maxX, sample.x),
    minZ: Math.min(bounds.minZ, sample.z),
    maxZ: Math.max(bounds.maxZ, sample.z)
  }), {
    minX: samples[0].x,
    maxX: samples[0].x,
    minZ: samples[0].z,
    maxZ: samples[0].z
  });
}

function updateReplaySlider(length) {
  const slider = $("replaySlider");
  slider.max = Math.max(0, length - 1);
  if (state.replayIndex === null || state.replayIndex >= length) {
    slider.value = Math.max(0, length - 1);
  } else {
    slider.value = state.replayIndex;
  }
}

function renderLaps() {
  const rows = (state.laps || []).slice().reverse().slice(0, 80);
  renderRows("lapsBody", rows, (lap) => [
    lap.mode || "--",
    lap.approximate ? "估" : String(lap.lapNumber || "--"),
    { text: formatMs(lap.lapTimeMs), className: "time" },
    lap.tuneName || "--"
  ], 4);
}

function renderComparisons() {
  renderRows("compareBody", state.comparisons || [], (item) => [
    item.tuneName || "--",
    { text: formatMs(item.bestLapMs), className: "time" },
    String(item.lapCount || 0)
  ], 3);
}

function renderIssues() {
  const list = $("issuesList");
  list.innerHTML = "";
  const issues = (state.issues || []).slice().reverse().slice(0, 20);
  if (!issues.length) {
    const li = document.createElement("li");
    li.className = "empty";
    li.textContent = "暂无标记";
    list.appendChild(li);
    return;
  }
  for (const issue of issues) {
    const li = document.createElement("li");
    const title = document.createElement("strong");
    title.textContent = `${issue.issueType || "marker"} · ${issue.severity || "info"}`;
    const body = document.createElement("span");
    body.textContent = issue.message || "";
    li.append(title, body);
    list.appendChild(li);
  }
}

function renderRows(bodyId, rows, map, colCount) {
  const body = $(bodyId);
  body.innerHTML = "";
  if (!rows.length) {
    const tr = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = colCount;
    td.className = "empty";
    td.textContent = "暂无数据";
    tr.appendChild(td);
    body.appendChild(tr);
    return;
  }
  for (const row of rows) {
    const tr = document.createElement("tr");
    for (const cell of map(row)) {
      const td = document.createElement("td");
      if (typeof cell === "object") {
        td.textContent = cell.text;
        if (cell.className) td.className = cell.className;
      } else {
        td.textContent = cell;
      }
      tr.appendChild(td);
    }
    body.appendChild(tr);
  }
}

function number(value, digits = 1) {
  const n = Number(value || 0);
  return Number.isFinite(n) ? n.toFixed(digits) : "0";
}

function percent(value) {
  return `${Math.round((Number(value || 0) / 255) * 100)}%`;
}

function gearName(value) {
  const gear = Number(value || 0);
  return gear <= 0 ? "N" : String(gear);
}

function formatSeconds(seconds) {
  return formatMs(Math.round(Number(seconds || 0) * 1000));
}

function formatMs(ms) {
  const value = Number(ms || 0);
  if (!value) return "--";
  const minutes = Math.floor(value / 60000);
  const seconds = Math.floor((value % 60000) / 1000);
  const millis = Math.floor(value % 1000);
  return `${minutes}:${String(seconds).padStart(2, "0")}.${String(millis).padStart(3, "0")}`;
}

function signed(value, digits = 1) {
  const n = Number(value || 0);
  if (!Number.isFinite(n)) return "0";
  return `${n > 0 ? "+" : ""}${n.toFixed(digits)}`;
}

function avg(...values) {
  const nums = values.map((v) => Number(v)).filter((v) => Number.isFinite(v));
  if (!nums.length) return 0;
  return nums.reduce((sum, value) => sum + value, 0) / nums.length;
}

function maxAbs(...values) {
  const nums = values.map((v) => Math.abs(Number(v || 0))).filter((v) => Number.isFinite(v));
  return nums.length ? Math.max(...nums) : 0;
}

function clamp(value, min, max) {
  const n = Number(value);
  if (!Number.isFinite(n)) return min;
  return Math.min(max, Math.max(min, n));
}

function normalizeSteer(value) {
  const n = Number(value || 0);
  if (Math.abs(n) <= 1) return clamp(n, -1, 1);
  return clamp(n / 127, -1, 1);
}

function setBar(name, ratio, text = null) {
  const bar = $(`${name}Bar`);
  const label = $(`${name}Text`);
  const value = clamp(ratio, 0, 1);
  if (bar) bar.style.width = `${Math.round(value * 100)}%`;
  if (label) label.textContent = text || `${Math.round(value * 100)}%`;
}

function setSignedBar(name, ratio) {
  const bar = $(`${name}Bar`);
  const label = $(`${name}Text`);
  const value = clamp(ratio, -1, 1);
  if (bar) {
    bar.style.width = `${Math.abs(value) * 50}%`;
    bar.style.marginLeft = value < 0 ? `${50 - Math.abs(value) * 50}%` : "50%";
  }
  if (label) label.textContent = `${Math.round(value * 100)}%`;
}

async function refreshAll() {
  try {
    const payload = await bridge("getSnapshot");
    applyDashboardPayload(payload, true);
  } catch (error) {
    $("statusText").textContent = error.message;
  }
}

async function start() {
  const host = $("hostInput").value.trim() || "127.0.0.1";
  const port = Number.parseInt($("portInput").value, 10) || 5400;
  const payload = await bridge("start", { host, port });
  applyDashboardPayload(payload, true);
}

async function stop() {
  const payload = await bridge("stop");
  applyDashboardPayload(payload, true);
}

async function markGate() {
  const payload = await bridge("markGate", { trackName: "手动路线" });
  applyDashboardPayload(payload, true);
}

async function syncContext() {
  const payload = await bridge("refreshContext");
  applyDashboardPayload(payload, true);
}

async function saveIssue() {
  const message = window.prompt("问题备注", "手动标记");
  if (!message) return;
  await bridge("saveIssue", { issueType: "manual", severity: "info", message });
  await refreshAll();
}

function toggleReplay() {
  state.replayPlaying = !state.replayPlaying;
  $("playReplayBtn").textContent = state.replayPlaying ? "暂停" : "回放";
  if (state.playTimer) {
    window.clearInterval(state.playTimer);
    state.playTimer = null;
  }
  if (!state.replayPlaying) return;
  state.replayIndex = Number($("replaySlider").value || 0);
  state.playTimer = window.setInterval(() => {
    const count = state.samples.length;
    if (!count) return;
    state.replayIndex = (state.replayIndex + 1) % count;
    renderRoute();
  }, 80);
}

function wireUi() {
  $("startBtn").addEventListener("click", () => start().catch((e) => $("statusText").textContent = e.message));
  $("stopBtn").addEventListener("click", () => stop().catch((e) => $("statusText").textContent = e.message));
  $("gateBtn").addEventListener("click", () => markGate().catch((e) => $("statusText").textContent = e.message));
  $("contextBtn").addEventListener("click", () => syncContext().catch((e) => $("statusText").textContent = e.message));
  $("refreshBtn").addEventListener("click", () => refreshAll());
  $("issueBtn").addEventListener("click", () => saveIssue().catch((e) => $("statusText").textContent = e.message));
  $("playReplayBtn").addEventListener("click", toggleReplay);
  $("replaySlider").addEventListener("input", (event) => {
    state.replayIndex = Number(event.target.value || 0);
    renderRoute();
  });
  window.addEventListener("resize", () => {
    renderRoute();
    renderDataChart();
  });
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener("message", (event) => handleHostEvent(event.data));
  }
}

wireUi();
refreshAll();
