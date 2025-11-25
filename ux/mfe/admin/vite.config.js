import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

// Common MFE URL (shared UI components)
const COMMON_MFE_URL = process.env.VITE_COMMON_MFE_URL || 'http://localhost:3099'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'adminMfe',
      filename: 'remoteEntry.js',
      // Expose the UserManagement, ApiDocumentation, and UserProfile components
      exposes: {
        './UserManagement': './src/pages/UserManagement.jsx',
        './ApiDocumentation': './src/pages/ApiDocumentation.jsx',
        './UserProfile': './src/pages/UserProfile.jsx',
        './UserEditor': './src/components/UserEditor.jsx',
        './RecentUsers': './src/components/RecentUsers.jsx'
      },
      remotes: {
        commonMfe: `${COMMON_MFE_URL}/assets/remoteEntry.js`
      },
      shared: ['react', 'react-dom', '@headlessui/react', '@heroicons/react']
    })
  ],
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false
  }
})
