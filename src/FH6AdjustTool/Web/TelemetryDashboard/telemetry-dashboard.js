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

  ctx.lineWidth = 2 * scale;
  ctx.strokeStyle = "#253041";
  ctx.beginPath();
  samples.forEach((sample, index) => {
    const point = project(sample);
    if (index === 0) ctx.moveTo(point.x, point.y);
    else ctx.lineTo(point.x, point.y);
  });
  ctx.stroke();

  const live = state.replayIndex === null ? samples[samples.length - 1] : samples[Math.min(state.replayIndex, samples.length - 1)];
  const livePoint = project(live);
  ctx.fillStyle = "#9d3f53";
  ctx.beginPath();
  ctx.arc(livePoint.x, livePoint.y, 6 * scale, 0, Math.PI * 2);
  ctx.fill();

  updateReplaySlider(samples.length);
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
  window.addEventListener("resize", () => renderRoute());
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener("message", (event) => handleHostEvent(event.data));
  }
}

wireUi();
refreshAll();
