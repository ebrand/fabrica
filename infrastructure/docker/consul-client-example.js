/**
 * Consul Client for Fabrica Services
 *
 * Access port registry and service configuration
 */

const axios = require('axios');

class ConsulClient {
  constructor(consulAddr = 'http://consul:8500') {
    this.consulAddr = consulAddr;
  }

  /**
   * Get port for a specific service
   *
   * @param {string} serviceName - Service name
   * @param {string} type - Service type (shell, mfe, bff, domain, etc.)
   * @returns {Object} Port information
   */
  async getPort(serviceName, type) {
    try {
      const response = await axios.get(
        `${this.consulAddr}/v1/kv/fabrica/ports/${type}/${serviceName}?raw=true`
      );

      return JSON.parse(response.data);
    } catch (error) {
      console.error(`Failed to get port for ${serviceName}:`, error.message);
      throw error;
    }
  }

  /**
   * Get all ports of a specific type
   *
   * @param {string} type - Service type
   * @returns {Array} List of services and their ports
   */
  async getPortsByType(type) {
    try {
      const response = await axios.get(
        `${this.consulAddr}/v1/kv/fabrica/ports/${type}?recurse=true`
      );

      return response.data.map(item => ({
        service: item.Key.split('/').pop(),
        ...JSON.parse(Buffer.from(item.Value, 'base64').toString())
      }));
    } catch (error) {
      console.error(`Failed to get ports for type ${type}:`, error.message);
      return [];
    }
  }

  /**
   * Get all registered ports
   *
   * @returns {Object} All ports grouped by type
   */
  async getAllPorts() {
    try {
      const response = await axios.get(
        `${this.consulAddr}/v1/kv/fabrica/ports?recurse=true`
      );

      const ports = {};
      response.data.forEach(item => {
        const parts = item.Key.split('/');
        const type = parts[2];
        const service = parts[3];

        if (!ports[type]) ports[type] = [];

        ports[type].push({
          service,
          ...JSON.parse(Buffer.from(item.Value, 'base64').toString())
        });
      });

      return ports;
    } catch (error) {
      console.error('Failed to get all ports:', error.message);
      return {};
    }
  }

  /**
   * Register or update a port
   *
   * @param {string} serviceName - Service name
   * @param {number} port - Port number
   * @param {string} type - Service type
   * @param {string} description - Service description
   */
  async registerPort(serviceName, port, type, description) {
    const data = {
      port,
      service: serviceName,
      type,
      description
    };

    try {
      await axios.put(
        `${this.consulAddr}/v1/kv/fabrica/ports/${type}/${serviceName}`,
        JSON.stringify(data)
      );

      console.log(`✓ Registered ${serviceName} on port ${port}`);
    } catch (error) {
      console.error(`Failed to register port for ${serviceName}:`, error.message);
      throw error;
    }
  }

  /**
   * Check if a port is already in use
   *
   * @param {number} port - Port number to check
   * @returns {Object|null} Service using the port, or null if available
   */
  async isPortInUse(port) {
    try {
      const allPorts = await this.getAllPorts();

      for (const type in allPorts) {
        const service = allPorts[type].find(s => s.port === port);
        if (service) return service;
      }

      return null;
    } catch (error) {
      console.error('Failed to check port:', error.message);
      return null;
    }
  }

  /**
   * Get next available port in a range
   *
   * @param {number} startPort - Start of range
   * @param {number} endPort - End of range
   * @returns {number|null} Next available port or null
   */
  async getNextAvailablePort(startPort, endPort) {
    const allPorts = await this.getAllPorts();
    const usedPorts = new Set();

    for (const type in allPorts) {
      allPorts[type].forEach(s => usedPorts.add(s.port));
    }

    for (let port = startPort; port <= endPort; port++) {
      if (!usedPorts.has(port)) {
        return port;
      }
    }

    return null;
  }

  /**
   * Get service configuration
   *
   * @param {string} serviceName - Service name
   * @returns {Object} Service configuration
   */
  async getServiceConfig(serviceName) {
    try {
      const response = await axios.get(
        `${this.consulAddr}/v1/kv/fabrica/config/${serviceName}?raw=true`
      );

      return JSON.parse(response.data);
    } catch (error) {
      console.error(`Failed to get config for ${serviceName}:`, error.message);
      throw error;
    }
  }

  /**
   * Build service URL from registry
   *
   * @param {string} serviceName - Service name
   * @param {string} type - Service type
   * @param {string} protocol - http or https
   * @returns {string} Full service URL
   */
  async getServiceUrl(serviceName, type, protocol = 'http') {
    const portInfo = await this.getPort(serviceName, type);
    return `${protocol}://localhost:${portInfo.port}`;
  }
}

// ============================================
// Usage Examples
// ============================================

async function exampleUsage() {
  const consul = new ConsulClient();

  // 1. Get port for admin service
  const adminPort = await consul.getPort('auth-iam', 'shared');
  console.log('Admin service:', adminPort);
  // → { port: 3600, service: 'auth-iam', type: 'shared', ... }

  // 2. Get all domain services
  const domainServices = await consul.getPortsByType('domain');
  console.log('Domain services:', domainServices);

  // 3. Check if port is in use
  const inUse = await consul.isPortInUse(3600);
  console.log('Port 3600 in use by:', inUse);
  // → { port: 3600, service: 'auth-iam', ... }

  // 4. Find next available port in range
  const nextPort = await consul.getNextAvailablePort(3400, 3499);
  console.log('Next available port:', nextPort);

  // 5. Register a new service
  await consul.registerPort('new-service', 3460, 'domain', 'New service description');

  // 6. Get service URL
  const adminUrl = await consul.getServiceUrl('auth-iam', 'shared');
  console.log('Admin URL:', adminUrl);
  // → http://localhost:3600

  // 7. Get service configuration
  const config = await consul.getServiceConfig('admin-service');
  console.log('Service config:', config);

  // 8. Get all ports organized by type
  const allPorts = await consul.getAllPorts();
  console.log('All ports:', JSON.stringify(allPorts, null, 2));
}

// Export for use in services
module.exports = ConsulClient;

// Run example if executed directly
if (require.main === module) {
  exampleUsage().catch(console.error);
}
