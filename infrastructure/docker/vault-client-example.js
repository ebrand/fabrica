/**
 * Vault Client Example for Fabrica Services
 *
 * This shows how to authenticate with Vault using AppRole
 * and retrieve secrets from your Node.js services.
 */

const axios = require('axios');

class VaultClient {
  constructor() {
    this.vaultAddr = process.env.VAULT_ADDR || 'http://vault:8200';
    this.roleId = process.env.VAULT_ROLE_ID;
    this.secretId = process.env.VAULT_SECRET_ID;
    this.token = null;
  }

  /**
   * Authenticate with Vault using AppRole
   */
  async authenticate() {
    try {
      const response = await axios.post(
        `${this.vaultAddr}/v1/auth/approle/login`,
        {
          role_id: this.roleId,
          secret_id: this.secretId
        }
      );

      this.token = response.data.auth.client_token;
      console.log('✅ Successfully authenticated with Vault');

      // Set up token renewal
      this.scheduleTokenRenewal(response.data.auth.lease_duration);

      return this.token;
    } catch (error) {
      console.error('❌ Vault authentication failed:', error.message);
      throw error;
    }
  }

  /**
   * Schedule automatic token renewal
   */
  scheduleTokenRenewal(leaseDuration) {
    // Renew at 80% of lease duration
    const renewTime = leaseDuration * 0.8 * 1000;

    setTimeout(async () => {
      try {
        await this.renewToken();
      } catch (error) {
        console.error('Token renewal failed, re-authenticating...');
        await this.authenticate();
      }
    }, renewTime);
  }

  /**
   * Renew the current token
   */
  async renewToken() {
    const response = await axios.post(
      `${this.vaultAddr}/v1/auth/token/renew-self`,
      {},
      {
        headers: { 'X-Vault-Token': this.token }
      }
    );

    console.log('✅ Token renewed');
    this.scheduleTokenRenewal(response.data.auth.lease_duration);
  }

  /**
   * Read a secret from Vault
   *
   * @param {string} path - Secret path (e.g., 'fabrica/admin/database')
   * @returns {Object} Secret data
   */
  async getSecret(path) {
    if (!this.token) {
      await this.authenticate();
    }

    try {
      const response = await axios.get(
        `${this.vaultAddr}/v1/${path}`,
        {
          headers: { 'X-Vault-Token': this.token }
        }
      );

      return response.data.data.data; // KV v2 returns data in data.data.data
    } catch (error) {
      console.error(`❌ Failed to read secret at ${path}:`, error.message);
      throw error;
    }
  }

  /**
   * Write a secret to Vault
   *
   * @param {string} path - Secret path
   * @param {Object} data - Secret data to write
   */
  async putSecret(path, data) {
    if (!this.token) {
      await this.authenticate();
    }

    try {
      await axios.post(
        `${this.vaultAddr}/v1/${path}`,
        { data },
        {
          headers: { 'X-Vault-Token': this.token }
        }
      );

      console.log(`✅ Secret written to ${path}`);
    } catch (error) {
      console.error(`❌ Failed to write secret at ${path}:`, error.message);
      throw error;
    }
  }

  /**
   * Get database credentials
   * Combines shared config with service-specific credentials
   */
  async getDatabaseConfig(serviceName) {
    const [sharedConfig, serviceConfig] = await Promise.all([
      this.getSecret('fabrica/data/shared/database'),
      this.getSecret(`fabrica/data/${serviceName}/database`)
    ]);

    return {
      host: sharedConfig.host,
      port: sharedConfig.port,
      database: serviceConfig.name,
      username: serviceConfig.username,
      password: serviceConfig.password,
      ssl: { rejectUnauthorized: sharedConfig.ssl_mode !== 'disable' }
    };
  }

  /**
   * Get all shared infrastructure config
   */
  async getSharedConfig() {
    const [database, rabbitmq, redis, consul] = await Promise.all([
      this.getSecret('fabrica/data/shared/database'),
      this.getSecret('fabrica/data/shared/rabbitmq'),
      this.getSecret('fabrica/data/shared/redis'),
      this.getSecret('fabrica/data/shared/consul')
    ]);

    return { database, rabbitmq, redis, consul };
  }
}

// ============================================
// Usage Examples
// ============================================

async function exampleUsage() {
  const vault = new VaultClient();

  // 1. Get database configuration for admin service
  const dbConfig = await vault.getDatabaseConfig('admin');
  console.log('Database config:', dbConfig);
  // Output: { host: 'postgres', port: 5432, database: 'fabrica-admin-db', ... }

  // 2. Get a specific secret
  const jwtConfig = await vault.getSecret('fabrica/data/admin/jwt');
  console.log('JWT secret:', jwtConfig.secret);

  // 3. Get all shared infrastructure config
  const sharedConfig = await vault.getSharedConfig();
  console.log('Shared config:', sharedConfig);

  // 4. Write a new secret
  await vault.putSecret('fabrica/data/admin/oauth', {
    google_client_id: 'your-client-id',
    google_client_secret: 'your-client-secret'
  });

  // 5. Read the secret back
  const oauthConfig = await vault.getSecret('fabrica/data/admin/oauth');
  console.log('OAuth config:', oauthConfig);
}

// Export for use in services
module.exports = VaultClient;

// Run example if executed directly
if (require.main === module) {
  exampleUsage().catch(console.error);
}
