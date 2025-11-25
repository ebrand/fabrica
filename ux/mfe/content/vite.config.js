import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

// Common MFE URL (shared UI components)
const COMMON_MFE_URL = process.env.VITE_COMMON_MFE_URL || 'http://localhost:3099'

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'contentMfe',
      filename: 'remoteEntry.js',
      exposes: {
        './ContentBlock': './src/components/ContentBlockWrapper.jsx'
      },
      remotes: {
        commonMfe: `${COMMON_MFE_URL}/assets/remoteEntry.js`
      },
      shared: ['react', 'react-dom', '@headlessui/react', '@heroicons/react']
    })
  ],
  css: {
    postcss: './postcss.config.js'
  },
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false
  }
})
