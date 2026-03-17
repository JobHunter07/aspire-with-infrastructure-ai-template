import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Proxy API calls to the app service
      '/api': {
            // Prefer Aspire-injected env vars GATEWAYHOST_HTTPS / GATEWAYHOST_HTTP
            target:
                process.env.GATEWAYHOST_HTTPS ??
                process.env.GATEWAYHOST_HTTP ??
                // fallback for local development
                'http://localhost:5430',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
