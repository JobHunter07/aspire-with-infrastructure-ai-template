import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Proxy API calls to the API Gateway, which handles auth and routes upstream.
      '/api': {
            target: process.env.GATEWAY_HTTPS,
        changeOrigin: true,
        secure: false  // accept self-signed cert in development
      }
    }
  }
})
