import { useState, useEffect } from 'react';
import axios from 'axios';
import configService from '../services/config';

// HTTP Method Badge Component
const MethodBadge = ({ method }) => {
  const colors = {
    get: 'bg-blue-100 text-blue-800',
    post: 'bg-green-100 text-green-800',
    put: 'bg-yellow-100 text-yellow-800',
    delete: 'bg-red-100 text-red-800',
    patch: 'bg-purple-100 text-purple-800',
  };

  return (
    <span className={`px-2 py-1 rounded text-xs font-semibold uppercase ${colors[method.toLowerCase()] || 'bg-gray-100 text-gray-800'}`}>
      {method}
    </span>
  );
};

function ApiDocumentation() {
  const [services, setServices] = useState([]);
  const [expandedServices, setExpandedServices] = useState({});
  const [servicesData, setServicesData] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [bffUrl, setBffUrl] = useState(null);

  useEffect(() => {
    initializeAndFetchServices();
  }, []);

  const initializeAndFetchServices = async () => {
    try {
      // Fetch BFF URL from acl-admin
      const url = await configService.getBffAdminUrl();
      setBffUrl(url);

      // Fetch services
      await fetchServices(url);
    } catch (err) {
      setError('Failed to initialize configuration');
      setLoading(false);
    }
  };

  const fetchServices = async (url) => {
    try {
      setLoading(true);
      const response = await axios.get(`${url}/api/docs/services`);
      setServices(response.data);

      // Fetch swagger data for all services
      const dataPromises = response.data.map(service =>
        fetchSwaggerData(url, service.id).then(data => ({ id: service.id, data }))
      );

      const results = await Promise.all(dataPromises);
      const dataMap = {};
      results.forEach(result => {
        dataMap[result.id] = result.data;
      });
      setServicesData(dataMap);
    } catch (err) {
      setError('Failed to load service registry');
      console.error('Error fetching services:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchSwaggerData = async (url, serviceId) => {
    try {
      const response = await axios.get(`${url}/api/docs/swagger/${serviceId}`);
      return response.data;
    } catch (err) {
      console.error(`Failed to load swagger for ${serviceId}:`, err);
      return null;
    }
  };

  const toggleService = (serviceId) => {
    setExpandedServices(prev => ({
      ...prev,
      [serviceId]: !prev[serviceId]
    }));
  };

  // Parse endpoints from swagger data
  const getEndpoints = (swaggerData) => {
    if (!swaggerData || !swaggerData.paths) return [];

    const endpoints = [];
    Object.entries(swaggerData.paths).forEach(([path, methods]) => {
      Object.entries(methods).forEach(([method, details]) => {
        if (['get', 'post', 'put', 'delete', 'patch'].includes(method.toLowerCase())) {
          // Extract controller name from tags or operationId
          const controller = details.tags?.[0] ||
                           details.operationId?.split('_')[0] ||
                           'Controller';

          endpoints.push({
            method: method.toUpperCase(),
            path,
            controller,
            summary: details.summary,
            description: details.description,
          });
        }
      });
    });

    // Sort by controller name, then by path
    return endpoints.sort((a, b) => {
      if (a.controller !== b.controller) {
        return a.controller.localeCompare(b.controller);
      }
      return a.path.localeCompare(b.path);
    });
  };

  // Calculate controller rowspans for merged cells
  const getControllerRowspans = (endpoints) => {
    const rowspans = new Map();
    let currentController = null;
    let count = 0;
    let startIndex = 0;

    endpoints.forEach((endpoint, index) => {
      if (endpoint.controller !== currentController) {
        if (currentController !== null) {
          rowspans.set(startIndex, count);
        }
        currentController = endpoint.controller;
        startIndex = index;
        count = 1;
      } else {
        count++;
      }
    });

    // Set the last controller's rowspan
    if (currentController !== null) {
      rowspans.set(startIndex, count);
    }

    return rowspans;
  };

  const getBaseUrl = (service) => {
    return `http://localhost:${service.port}`;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
        {error}
      </div>
    );
  }

  if (services.length === 0) {
    return (
      <div className="bg-yellow-50 border border-yellow-200 text-yellow-700 px-4 py-3 rounded">
        No domain services available for documentation.
      </div>
    );
  }

  return (
    <div>
      {/* Services Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <table className="min-w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-8"></th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Service Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Base URI</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Port</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Endpoints</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {services.map((service) => {
              const isExpanded = expandedServices[service.id];
              const swaggerData = servicesData[service.id];
              const endpoints = swaggerData ? getEndpoints(swaggerData) : [];

              return (
                <>
                  {/* Service Row */}
                  <tr
                    key={service.id}
                    className="hover:bg-gray-50 cursor-pointer transition-colors"
                    onClick={() => toggleService(service.id)}
                  >
                    <td className="px-6 py-4">
                      <svg
                        className={`w-5 h-5 text-gray-400 transition-transform ${isExpanded ? 'rotate-90' : ''}`}
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                      </svg>
                    </td>
                    <td className="px-6 py-4">
                      <div className="text-sm font-medium text-gray-900">{service.name}</div>
                      {service.description && (
                        <div className="text-xs text-gray-500 mt-1">{service.description}</div>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <code className="text-sm text-gray-600">{getBaseUrl(service)}</code>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-gray-900">{service.port}</span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-gray-600">{endpoints.length} routes</span>
                    </td>
                  </tr>

                  {/* Expanded Endpoints */}
                  {isExpanded && endpoints.length > 0 && (
                    <tr>
                      <td colSpan="5" className="px-6 py-0 bg-gray-50">
                        <div className="py-4">
                          <table className="min-w-full">
                            <thead>
                              <tr className="border-b border-gray-200">
                                <th className="px-6 py-2 text-left text-xs font-medium text-gray-500 uppercase">Controller</th>
                                <th className="px-6 py-2 text-left text-xs font-medium text-gray-500 uppercase">Method</th>
                                <th className="px-6 py-2 text-left text-xs font-medium text-gray-500 uppercase">Route</th>
                                <th className="px-6 py-2 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                              </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-200">
                              {(() => {
                                const rowspans = getControllerRowspans(endpoints);
                                return endpoints.map((endpoint, idx) => {
                                  const rowspan = rowspans.get(idx);
                                  const showController = rowspan !== undefined;

                                  return (
                                    <tr key={idx} className="hover:bg-white transition-colors">
                                      {showController && (
                                        <td
                                          rowSpan={rowspan}
                                          className="px-6 py-3 bg-gray-100 border-r border-gray-200 align-top"
                                        >
                                          <span className="text-sm font-semibold text-gray-900">{endpoint.controller}</span>
                                        </td>
                                      )}
                                      <td className="px-6 py-3">
                                        <MethodBadge method={endpoint.method} />
                                      </td>
                                      <td className="px-6 py-3">
                                        <code className="text-sm text-gray-900">{endpoint.path}</code>
                                      </td>
                                      <td className="px-6 py-3">
                                        <span className="text-sm text-gray-600">{endpoint.summary || endpoint.description || '-'}</span>
                                      </td>
                                    </tr>
                                  );
                                });
                              })()}
                            </tbody>
                          </table>
                        </div>
                      </td>
                    </tr>
                  )}

                  {/* No Endpoints Message */}
                  {isExpanded && endpoints.length === 0 && (
                    <tr>
                      <td colSpan="5" className="px-6 py-4 bg-gray-50">
                        <div className="text-center text-sm text-gray-500">
                          No endpoints found for this service
                        </div>
                      </td>
                    </tr>
                  )}
                </>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default ApiDocumentation;
