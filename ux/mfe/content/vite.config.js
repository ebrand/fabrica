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

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'contentMfe',
      filename: 'remoteEntry.js',
      exposes: {
        './Content': './src/components/ContentWrapper.jsx',
        './ContentBlock': './src/components/ContentBlockWrapper.jsx',
        './CardHero': './src/components/CardHeroWrapper.jsx',
        './CardMinimal': './src/components/CardMinimalWrapper.jsx',
        './CardDark': './src/components/CardDarkWrapper.jsx',
        './BlockContentEditor': './src/components/BlockContentEditorWrapper.jsx'
      },
      remotes: {
        commonMfe: `${COMMON_MFE_URL}/assets/remoteEntry.js`
      },
      shared: ['react', 'react-dom', '@headlessui/react', '@heroicons/react']
    }),
    removeCssPlugin()
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
