import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

// Common MFE URL (shared UI components)
const COMMON_MFE_URL = process.env.VITE_COMMON_MFE_URL || 'http://localhost:3099'

import { rm, readdir } from 'fs/promises'
import { join } from 'path'

// Plugin to remove CSS from production build (shell provides Tailwind)
const removeCssPlugin = () => ({
  name: 'remove-css',
  async closeBundle() {
    try {
      const assetsDir = 'dist/assets'
      const files = await readdir(assetsDir)
      for (const file of files) {
        if (file.endsWith('.css')) {
          await rm(join(assetsDir, file), { force: true })
          console.log(`Removed CSS: ${file}`)
        }
      }
    } catch (err) {
      // Ignore if dist doesn't exist yet
    }
  }
})

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'adminMfe',
      filename: 'remoteEntry.js',
      // Expose the UserManagement, ApiDocumentation, UserProfile, and EsbTelemetry components
      exposes: {
        './UserManagement': './src/pages/UserManagement.jsx',
        './ApiDocumentation': './src/pages/ApiDocumentation.jsx',
        './UserProfile': './src/pages/UserProfile.jsx',
        './UserEditor': './src/components/UserEditor.jsx',
        './RecentUsers': './src/components/RecentUsers.jsx',
        './EsbTelemetry': './src/pages/EsbTelemetry.jsx'
      },
      remotes: {
        commonMfe: `${COMMON_MFE_URL}/assets/remoteEntry.js`
      },
      shared: ['react', 'react-dom', '@headlessui/react', '@heroicons/react']
    }),
    removeCssPlugin()
  ],
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false
  }
})
