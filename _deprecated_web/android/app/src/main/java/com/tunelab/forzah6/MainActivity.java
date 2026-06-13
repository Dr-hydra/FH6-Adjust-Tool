// android/app/src/main/java/com/tunelab/forzah6/MainActivity.java
package com.tunelab.forzah6;

import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.view.WindowInsetsController;
import android.content.pm.ActivityInfo;

import androidx.core.view.WindowCompat;
import androidx.core.view.WindowInsetsCompat;
import androidx.core.view.WindowInsetsControllerCompat;

import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // ── Edge-to-edge ────────────────────────────────────────────────────
        // Let our app draw under status bar and navigation bar.
        // The CSS env(safe-area-inset-*) values then give us the exact padding.
        WindowCompat.setDecorFitsSystemWindows(getWindow(), false);

        // ── Status bar & nav bar appearance ─────────────────────────────────
        Window window = getWindow();
        window.setStatusBarColor(android.graphics.Color.TRANSPARENT);
        window.setNavigationBarColor(android.graphics.Color.TRANSPARENT);

        // Light icons on dark background
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            WindowInsetsController controller = window.getInsetsController();
            if (controller != null) {
                // We have a dark background, so we want light status bar icons
                controller.setSystemBarsAppearance(0,
                    WindowInsetsController.APPEARANCE_LIGHT_STATUS_BARS);
                controller.setSystemBarsAppearance(0,
                    WindowInsetsController.APPEARANCE_LIGHT_NAVIGATION_BARS);
            }
        }

        // ── 120Hz / High Refresh Rate ────────────────────────────────────────
        // Request the highest available refresh rate for this window.
        // On S25 Ultra (Snapdragon 8 Elite) this enables 120Hz for smooth
        // animations. Falls back gracefully on 60Hz devices.
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            // Android 11+ — use preferred display mode
            WindowManager.LayoutParams params = window.getAttributes();
            params.preferredRefreshRate = 120f;
            window.setAttributes(params);
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            // Android 12+ — opt into minimal post-processing for lower latency
            window.setPreferMinimalPostProcessing(true);
        }
    }

    @Override
    public void onBackPressed() {
        // Let the WebView/JS handle back navigation first.
        // Capacitor will call window.history.back() which our app handles.
        // If JS says nothing to go back to, super will exit the activity.
        if (getBridge() != null && getBridge().getWebView().canGoBack()) {
            getBridge().getWebView().goBack();
        } else {
            // Post a message to JS so our app can close overlays first
            getBridge().getWebView().evaluateJavascript(
                "window.dispatchEvent(new Event('androidBackButton'))", null
            );
        }
    }
}
