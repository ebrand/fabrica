/**
 * Configuration service
 * Fetches service URLs from acl-admin (which reads from Consul)
 */

const ACL_ADMIN_URL = import.meta.env.VITE_ACL_ADMIN_URL || 'http://localhost:3600';

class ConfigService {
  constructor() {
    this.config = null;
  }

  /**
   * Fetch service configuration from acl-admin
   */
  async fetchConfig() {
    if (this.config) {
      return this.config;
    }

    try {
      console.log('Fetching configuration from acl-admin...');
      const response = await fetch(`${ACL_ADMIN_URL}/api/config/services`);
      if (!response.ok) {
        throw new Error('Failed to fetch service config');
      }

      this.config = await response.json();
      console.log('✅ Loaded service configuration from acl-admin:', this.config);
      return this.config;
    } catch (error) {
      console.error('❌ Failed to load config from acl-admin:', error);
      // Fallback to env vars if acl-admin is unavailable
      console.warn('Falling back to environment variables');
      this.config = {
        bffAdminUrl: import.meta.env.VITE_BFF_URL || 'http://localhost:3200',
        aclAdminUrl: ACL_ADMIN_URL
      };
      return this.config;
    }
  }

  /**
   * Get BFF Admin URL
   */
  async getBffAdminUrl() {
    const config = await this.fetchConfig();
    return config.bffAdminUrl;
  }

  /**
   * Get ACL Admin URL
   */
  async getAclAdminUrl() {
    const config = await this.fetchConfig();
    return config.aclAdminUrl;
  }
}

export default new ConfigService();
