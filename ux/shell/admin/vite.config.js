import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

// Only the ACL Admin URL is configured here - all other URLs are fetched from it
const ACL_ADMIN_URL = process.env.VITE_ACL_ADMIN_URL || 'http://localhost:3600'

/**
 * Fetch ALL service URLs from acl-admin service (which gets them from Vault/Consul)
 * Falls back to localhost defaults if fetch fails
 */
async function getServiceUrls() {
  const fallbackUrls = {
    adminMfeUrl: 'http://localhost:3100',
    productMfeUrl: 'http://localhost:3110',
    contentMfeUrl: 'http://localhost:3180',
    customerMfeUrl: 'http://localhost:3170',
    commonMfeUrl: 'http://localhost:3099'
  }

  try {
    const response = await fetch(`${ACL_ADMIN_URL}/api/config/services`)
    if (response.ok) {
      const config = await response.json()
      console.log('✅ Fetched service URLs from acl-admin:', {
        adminMfe: config.adminMfeUrl,
        productMfe: config.productMfeUrl,
        contentMfe: config.contentMfeUrl,
        customerMfe: config.customerMfeUrl,
        commonMfe: config.commonMfeUrl
      })
      return {
        adminMfeUrl: config.adminMfeUrl || fallbackUrls.adminMfeUrl,
        productMfeUrl: config.productMfeUrl || fallbackUrls.productMfeUrl,
        contentMfeUrl: config.contentMfeUrl || fallbackUrls.contentMfeUrl,
        customerMfeUrl: config.customerMfeUrl || fallbackUrls.customerMfeUrl,
        commonMfeUrl: config.commonMfeUrl || fallbackUrls.commonMfeUrl
      }
    }
  } catch (error) {
    console.warn('⚠️  Could not fetch from acl-admin, using fallback URLs')
  }

  console.log('Using fallback service URLs:', fallbackUrls)
  return fallbackUrls
}

// https://vite.dev/config/
export default defineConfig(async () => {
  const urls = await getServiceUrls()

  return {
    plugins: [
      react(),
      federation({
        name: 'adminShell',
        remotes: {
          adminMfe: `${urls.adminMfeUrl}/assets/remoteEntry.js`,
          productMfe: `${urls.productMfeUrl}/assets/remoteEntry.js`,
          commonMfe: `${urls.commonMfeUrl}/assets/remoteEntry.js`,
          contentMfe: `${urls.contentMfeUrl}/assets/remoteEntry.js`,
          customerMfe: `${urls.customerMfeUrl}/assets/remoteEntry.js`
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
