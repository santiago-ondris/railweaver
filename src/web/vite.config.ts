import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { viteStaticCopy } from 'vite-plugin-static-copy'

const cesiumSource = 'node_modules/cesium/Build/Cesium'
const cesiumBaseUrl = 'cesiumStatic'
const cesiumSourceDepth = cesiumSource.split('/').length

// https://vite.dev/config/
export default defineConfig({
  define: {
    CESIUM_BASE_URL: JSON.stringify(`/${cesiumBaseUrl}/`),
  },
  plugins: [
    react(),
    viteStaticCopy({
      targets: [
        {
          src: `${cesiumSource}/{Workers,ThirdParty,Assets,Widgets}/**/*`,
          dest: cesiumBaseUrl,
          rename: { stripBase: cesiumSourceDepth },
        },
      ],
    }),
  ],
  server: {
    // Forward API calls to RailWeaver.Api during local development (see its launchSettings.json).
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
})
