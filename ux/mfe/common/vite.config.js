import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'commonMfe',
      filename: 'remoteEntry.js',
      // Expose common UI components
      exposes: {
        './Combobox': './src/components/Combobox.jsx',
        './Select': './src/components/Select.jsx',
        './RadioCards': './src/components/RadioCards.jsx',
        './Toast': './src/components/Toast.jsx'
      },
      shared: {
        'react': { singleton: true, requiredVersion: '^19.2.0' },
        'react-dom': { singleton: true, requiredVersion: '^19.2.0' }
      }
    })
  ],
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false
  }
})
