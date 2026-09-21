import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Forward API calls to RailWeaver.Api during local development (see its launchSettings.json).
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
})
