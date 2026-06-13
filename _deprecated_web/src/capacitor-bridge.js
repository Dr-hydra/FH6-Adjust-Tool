/**
 * capacitor-bridge.js
 * Import this in App.jsx to wire up Android-native features.
 * All calls are no-ops in browser/web context — safe to include always.
 *
 * Usage in App.jsx:
 *   import { haptic, checkNetwork, useAndroidBack } from './capacitor-bridge.js'
 */

// ── Detect if running inside Capacitor (Android) ─────────────────────────────
const isNative = () => typeof window !== 'undefined' && !!window.Capacitor?.isNative;

// ── Haptic feedback ──────────────────────────────────────────────────────────
// Types: 'light' | 'medium' | 'heavy' | 'select' | 'success' | 'warning' | 'error'
export async function haptic(type = 'light') {
  if (!isNative()) {
    // Web fallback — vibration API
    if (navigator.vibrate) {
      const durations = { light: 10, medium: 30, heavy: 60, select: 8, success: [10, 50, 10], warning: [30, 30, 30], error: [50, 30, 50] };
      navigator.vibrate(durations[type] || 10);
    }
    return;
  }
  try {
    const { Haptics, ImpactStyle, NotificationType } = await import('@capacitor/haptics');
    switch (type) {
      case 'light':   await Haptics.impact({ style: ImpactStyle.Light });   break;
      case 'medium':  await Haptics.impact({ style: ImpactStyle.Medium });  break;
      case 'heavy':   await Haptics.impact({ style: ImpactStyle.Heavy });   break;
      case 'select':  await Haptics.selectionStart();                       break;
      case 'success': await Haptics.notification({ type: NotificationType.Success }); break;
      case 'warning': await Haptics.notification({ type: NotificationType.Warning }); break;
      case 'error':   await Haptics.notification({ type: NotificationType.Error });   break;
      default:        await Haptics.impact({ style: ImpactStyle.Light });
    }
  } catch {
    // Haptics not available — fail silently
  }
}

// ── Network check ─────────────────────────────────────────────────────────────
// Returns true if connected. Call before AI requests.
export async function checkNetwork() {
  if (!isNative()) return navigator.onLine ?? true;
  try {
    const { Network } = await import('@capacitor/network');
    const status = await Network.getStatus();
    return status.connected;
  } catch {
    return navigator.onLine ?? true;
  }
}

// ── Android back button handler ──────────────────────────────────────────────
// Call this in your main component with a callback that closes the top overlay.
// Returns a cleanup function to remove the listener.
export function useAndroidBack(handler) {
  if (typeof window === 'undefined') return () => {};
  const listener = () => handler();
  window.addEventListener('androidBackButton', listener);
  return () => window.removeEventListener('androidBackButton', listener);
}

// ── Status bar ───────────────────────────────────────────────────────────────
export async function setStatusBarColor(color = '#080a0f') {
  if (!isNative()) return;
  try {
    const { StatusBar, Style } = await import('@capacitor/status-bar');
    await StatusBar.setStyle({ style: Style.Dark });
    await StatusBar.setBackgroundColor({ color });
  } catch {}
}

// ── In-app review prompt ──────────────────────────────────────────────────────
// Call after 3rd successful tune generation.
// Uses Play Store native review dialog — no redirect.
export async function requestReview() {
  if (!isNative()) return;
  try {
    const { RateApp } = await import('@capacitor-community/rate-app');
    await RateApp.requestReview();
  } catch {}
}

// ── 120Hz CSS performance hint ────────────────────────────────────────────────
// Call once on app init. Marks key animated elements for compositor promotion.
export function apply120HzHints() {
  const style = document.createElement('style');
  style.textContent = `
    /* Compositor layer hints for 120Hz smoothness */
    button, [role="button"] {
      will-change: transform;
      transform: translateZ(0);
    }
    /* Bottom sheets and overlays */
    [data-overlay] {
      will-change: transform, opacity;
      transform: translateZ(0);
    }
    /* Scrollable content */
    [data-scroll] {
      will-change: scroll-position;
      -webkit-overflow-scrolling: touch;
    }
  `;
  document.head.appendChild(style);
}
