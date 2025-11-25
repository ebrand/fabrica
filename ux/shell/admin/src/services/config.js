/**
 * Configuration service
 * Fetches auth configuration from acl-admin service (which reads from Vault)
 */

// Get acl-admin URL from environment, with fallback to localhost
const CONFIG_API_URL = import.meta.env.VITE_ACL_ADMIN_URL || 'http://localhost:3600';

class ConfigService {
  constructor() {
    this.config = null;
  }

  /**
   * Fetch authentication configuration from Vault via acl-admin
   */
  async fetchAuthConfig() {
    if (this.config) {
      return this.config;
    }

    try {
      const response = await fetch(`${CONFIG_API_URL}/api/vault/auth`);
      if (!response.ok) {
        throw new Error('Failed to fetch auth config');
      }

      this.config = await response.json();
      console.log('✅ Loaded auth configuration from Vault');
      return this.config;
    } catch (error) {
      console.error('❌ Failed to load config from Vault:', error);
      // Fallback to env vars if Vault is unavailable
      console.warn('Falling back to environment variables');
      this.config = {
        stytch: {
          publicToken: import.meta.env.VITE_STYTCH_PUBLIC_TOKEN || ''
        }
      };
      return this.config;
    }
  }

  /**
   * Get Stytch public token
   */
  async getStytchPublicToken() {
    const config = await this.fetchAuthConfig();
    return config.stytch.publicToken;
  }

  /**
   * Get Google OAuth config
   */
  async getGoogleConfig() {
    const config = await this.fetchAuthConfig();
    return config.google;
  }

  /**
   * Get service URLs (BFF, MFE, etc.)
   */
  async fetchServiceUrls() {
    try {
      const response = await fetch(`${CONFIG_API_URL}/api/config/services`);
      if (!response.ok) {
        throw new Error('Failed to fetch service URLs');
      }

      const urls = await response.json();
      console.log('✅ Loaded service URLs from acl-admin');
      return urls;
    } catch (error) {
      console.error('❌ Failed to load service URLs:', error);
      // Fallback to env vars
      return {
        bffAdminUrl: import.meta.env.VITE_BFF_URL || 'http://localhost:3200',
        adminMfeUrl: import.meta.env.VITE_ADMIN_MFE_URL || 'http://localhost:3100',
        aclAdminUrl: CONFIG_API_URL
      };
    }
  }

  /**
   * Get BFF Admin URL
   */
  async getBffAdminUrl() {
    const urls = await this.fetchServiceUrls();
    return urls.bffAdminUrl;
  }

  /**
   * Get Admin MFE URL
   */
  async getAdminMfeUrl() {
    const urls = await this.fetchServiceUrls();
    return urls.adminMfeUrl;
  }
}

export default new ConfigService();
