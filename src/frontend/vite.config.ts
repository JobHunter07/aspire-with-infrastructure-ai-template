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
                // fallback for local development (backend in this template runs with HTTPS on 54955)
                'https://localhost:7415',
        changeOrigin: true,
        secure: false
      }
      ,
      // Proxy BFF endpoints (login/callback/user/logout) to the backend during dev
      '/bff': {
        target:
            process.env.GATEWAYHOST_HTTPS ??
            process.env.GATEWAYHOST_HTTP ??
            'http://localhost:7415',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
