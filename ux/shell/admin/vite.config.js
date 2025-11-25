import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

// Get acl-admin URL from environment
const ACL_ADMIN_URL = process.env.VITE_ACL_ADMIN_URL || 'http://localhost:3600'

/**
 * Fetch MFE URLs from acl-admin service
 * Falls back to environment variable or localhost if fetch fails
 */
async function getMfeUrls() {
  try {
    // Try to fetch from acl-admin
    const response = await fetch(`${ACL_ADMIN_URL}/api/config/services`)
    if (response.ok) {
      const config = await response.json()
      console.log('✅ Fetched MFE URLs from acl-admin:', {
        adminMfe: config.adminMfeUrl,
        productMfe: config.productMfeUrl
      })
      return {
        adminMfeUrl: config.adminMfeUrl,
        productMfeUrl: config.productMfeUrl
      }
    }
  } catch (error) {
    console.warn('⚠️  Could not fetch from acl-admin, using fallback')
  }

  // Fallback to environment variables or localhost
  const fallbackUrls = {
    adminMfeUrl: process.env.VITE_ADMIN_MFE_URL || 'http://localhost:3100',
    productMfeUrl: process.env.VITE_PRODUCT_MFE_URL || 'http://localhost:3110'
  }
  console.log('Using fallback MFE URLs:', fallbackUrls)
  return fallbackUrls
}

// Common MFE URL (shared UI components)
const COMMON_MFE_URL = process.env.VITE_COMMON_MFE_URL || 'http://localhost:3099'

// https://vite.dev/config/
export default defineConfig(async () => {
  const { adminMfeUrl, productMfeUrl } = await getMfeUrls()

  return {
    plugins: [
      react(),
      federation({
        name: 'adminShell',
        remotes: {
          adminMfe: `${adminMfeUrl}/assets/remoteEntry.js`,
          productMfe: `${productMfeUrl}/assets/remoteEntry.js`,
          commonMfe: `${COMMON_MFE_URL}/assets/remoteEntry.js`
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
  }
})
