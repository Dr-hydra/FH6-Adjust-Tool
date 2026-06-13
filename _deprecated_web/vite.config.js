import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
export default defineConfig({
  base: './',
  plugins: [react()],
  build: {
    // Capacitor needs relative paths
    outDir:    'dist',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        // Keep chunks manageable for WebView
        manualChunks: undefined,
      }
    }
  },
  // Needed for Capacitor local server
  server: {
    port: 5173,
    strictPort: true,
  }
})
