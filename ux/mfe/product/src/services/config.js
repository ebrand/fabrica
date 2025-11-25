// Configuration service to fetch BFF URLs from ACL Admin
class ConfigService {
  constructor() {
    this.config = null;
    this.aclAdminUrl = import.meta.env.VITE_ACL_ADMIN_URL || 'http://localhost:3600';
  }

  async fetchConfig() {
    if (this.config) {
      return this.config;
    }

    try {
      const response = await fetch(`${this.aclAdminUrl}/api/config/services`);
      if (!response.ok) {
        throw new Error('Failed to fetch configuration');
      }
      this.config = await response.json();
      return this.config;
    } catch (error) {
      console.error('Error fetching config:', error);
      // Fallback configuration
      this.config = {
        bffProductUrl: 'http://localhost:3220',
        bffAdminUrl: 'http://localhost:3200',
        aclAdminUrl: this.aclAdminUrl
      };
      return this.config;
    }
  }

  async getBffProductUrl() {
    const config = await this.fetchConfig();
    return config.bffProductUrl || 'http://localhost:3220';
  }
}

const configService = new ConfigService();
export default configService;
