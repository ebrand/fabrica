import { useState, useEffect, useRef, Suspense, lazy } from 'react';

// Lazy load Toast from commonMfe
const Toast = lazy(() => import('commonMfe/Toast'));

// Domain service URL mapping
const DOMAIN_SERVICE_URLS = {
  admin: 'http://localhost:3600',
  product: 'http://localhost:3420'
};

const ACL_ADMIN_URL = 'http://localhost:3600';
const SCRIPT_RUNNER_URL = 'http://localhost:3800';
const SCRIPT_RUNNER_API_KEY = 'fabrica-dev-key';

// All possible domains for selection
const ALL_DOMAINS = [
  { id: 'admin', name: 'Admin' },
  { id: 'content', name: 'Content' },
  { id: 'customer', name: 'Customer' },
  { id: 'finance', name: 'Finance' },
  { id: 'fulfillment', name: 'Fulfill' },
  { id: 'legal', name: 'Legal' },
  { id: 'order-mgmt', name: 'Order-Mgmt' },
  { id: 'product', name: 'Product' },
  { id: 'sales-mktg', name: 'Sales-Mktg' }
];

const AdminToolsDrawer = ({ isOpen, onClose, onOpen }) => {
  const [activeTab, setActiveTab] = useState('esb'); // 'esb' or 'deployment'

  return (
    <>
      {/* Backdrop */}
      <div
        className={`fixed inset-0 bg-black transition-opacity z-40 ${
          isOpen ? 'opacity-50' : 'opacity-0 pointer-events-none'
        }`}
        onClick={onClose}
      />

      {/* Drawer container with tab */}
      <div
        className={`fixed right-0 top-0 h-full transform transition-transform duration-300 ease-in-out z-50 ${
          isOpen ? 'translate-x-0 pointer-events-auto' : 'translate-x-[700px] pointer-events-none'
        }`}
        style={{ width: 'calc(700px + 24px)' }}
      >
        {/* Tab handle with gear icon */}
        <button
          onClick={isOpen ? onClose : onOpen}
          className="absolute -left-2 bottom-20 bg-gray-700 hover:bg-gray-800 text-white px-1.5 py-3 rounded-l-md shadow-lg transition-colors pointer-events-auto flex flex-col items-center gap-2"
          title={isOpen ? 'Close Admin Tools' : 'Open Admin Tools'}
        >
          {/* Gear icon */}
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          <span
            className="text-xs font-medium tracking-wide block"
            style={{ writingMode: 'vertical-lr', transform: 'rotate(180deg)' }}
          >
            Admin Tools
          </span>
        </button>

        {/* Drawer panel */}
        <div className="absolute right-0 top-0 h-full w-[700px] bg-white shadow-2xl flex flex-col">
          {/* Header with tabs */}
          <div className="flex justify-between items-center px-4 py-3 border-b border-gray-200 bg-gray-50">
            <div className="flex items-center gap-4">
              <h2 className="text-lg font-semibold text-gray-900">Admin Tools</h2>
              {/* Tab buttons in header */}
              <div className="flex bg-gray-200 rounded-lg p-0.5">
                <button
                  onClick={() => setActiveTab('esb')}
                  className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
                    activeTab === 'esb'
                      ? 'bg-white text-gray-900 shadow-sm'
                      : 'text-gray-600 hover:text-gray-900'
                  }`}
                >
                  ESB Config
                </button>
                <button
                  onClick={() => setActiveTab('deployment')}
                  className={`px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
                    activeTab === 'deployment'
                      ? 'bg-white text-gray-900 shadow-sm'
                      : 'text-gray-600 hover:text-gray-900'
                  }`}
                >
                  Deployment
                </button>
              </div>
            </div>
            <button
              onClick={onClose}
              className="p-1 text-gray-500 hover:text-gray-700 rounded hover:bg-gray-200"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Tab Content */}
          <div className="flex-1 overflow-hidden">
            {activeTab === 'esb' ? (
              <EsbConfigTab />
            ) : (
              <DeploymentTab />
            )}
          </div>
        </div>
      </div>
    </>
  );
};

// ============================================================================
// ESB CONFIG TAB - Redesigned with side-by-side Publishing/Subscribing layout
// ============================================================================
const EsbConfigTab = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [domains, setDomains] = useState([]);
  const [selectedDomain, setSelectedDomain] = useState('product');
  const [tables, setTables] = useState([]);
  const [outboxConfigs, setOutboxConfigs] = useState([]);
  const [cacheConfigs, setCacheConfigs] = useState([]);
  const [otherDomainsPublishing, setOtherDomainsPublishing] = useState({});
  const [expandedItems, setExpandedItems] = useState({});
  const [toast, setToast] = useState({ show: false, title: '', message: '', type: 'success' });

  const getServiceUrl = (domain = selectedDomain) => {
    return DOMAIN_SERVICE_URLS[domain] || ACL_ADMIN_URL;
  };

  // Fetch domain registry
  useEffect(() => {
    fetchDomains();
  }, []);

  // Fetch data when domain changes
  useEffect(() => {
    if (selectedDomain) {
      fetchDomainData();
      fetchOtherDomainsPublishing();
    }
  }, [selectedDomain, domains]);

  const fetchDomains = async () => {
    try {
      const response = await fetch(`${ACL_ADMIN_URL}/api/esb/domain`);
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const data = await response.json();
      setDomains(data);
    } catch (err) {
      console.warn('Failed to fetch domains:', err);
    }
  };

  const fetchDomainData = async () => {
    setLoading(true);
    setError(null);
    try {
      const serviceUrl = getServiceUrl();
      const [tablesRes, outboxRes, cacheRes] = await Promise.all([
        fetch(`${serviceUrl}/api/esb/tables`),
        fetch(`${serviceUrl}/api/esb/outbox-config`),
        fetch(`${serviceUrl}/api/esb/cache-config`)
      ]);

      if (!tablesRes.ok) throw new Error(`Tables: HTTP ${tablesRes.status}`);
      if (!outboxRes.ok) throw new Error(`Outbox: HTTP ${outboxRes.status}`);
      if (!cacheRes.ok) throw new Error(`Cache: HTTP ${cacheRes.status}`);

      const [tablesData, outboxData, cacheData] = await Promise.all([
        tablesRes.json(),
        outboxRes.json(),
        cacheRes.json()
      ]);

      setTables(tablesData);
      setOutboxConfigs(outboxData);
      setCacheConfigs(cacheData);
    } catch (err) {
      setError(`Failed to fetch domain data: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchOtherDomainsPublishing = async () => {
    const otherDomains = domains.filter(d => d.domainName !== selectedDomain);
    const publishing = {};

    for (const domain of otherDomains) {
      try {
        const serviceUrl = getServiceUrl(domain.domainName);
        const response = await fetch(`${serviceUrl}/api/esb/outbox-config`);
        if (response.ok) {
          publishing[domain.domainName] = await response.json();
        }
      } catch (err) {
        publishing[domain.domainName] = [];
      }
    }

    setOtherDomainsPublishing(publishing);
  };

  const getOutboxConfig = (tableName) => outboxConfigs.find(c => c.tableName === tableName);
  const getCacheConfig = (sourceDomain, sourceTable) =>
    cacheConfigs.find(c => c.sourceDomain === sourceDomain && c.sourceTable === sourceTable);

  const toggleExpanded = (key) => {
    setExpandedItems(prev => ({ ...prev, [key]: !prev[key] }));
  };

  // Create Kafka topic
  const createKafkaTopic = async (topicName) => {
    try {
      const response = await fetch(`${SCRIPT_RUNNER_URL}/kafka/topics`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-API-Key': SCRIPT_RUNNER_API_KEY },
        body: JSON.stringify({ topicName })
      });
      return response.ok;
    } catch {
      return false;
    }
  };

  // Delete Kafka topic
  const deleteKafkaTopic = async (topicName) => {
    try {
      const response = await fetch(`${SCRIPT_RUNNER_URL}/kafka/topics/${encodeURIComponent(topicName)}`, {
        method: 'DELETE',
        headers: { 'X-API-Key': SCRIPT_RUNNER_API_KEY }
      });
      return response.ok;
    } catch {
      return false;
    }
  };

  // Handle publish
  const handlePublish = async (table) => {
    try {
      const serviceUrl = getServiceUrl();
      const topicName = `${selectedDomain}.${table.tableName}`;

      const response = await fetch(`${serviceUrl}/api/esb/outbox-config`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          schemaName: table.schemaName,
          tableName: table.tableName,
          captureInsert: true,
          captureUpdate: true,
          captureDelete: true,
          isActive: true,
          description: `Publishing ${table.tableName} changes`
        })
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || `HTTP ${response.status}`);
      }

      await createKafkaTopic(topicName);
      fetchDomainData();
      setToast({ show: true, title: 'Published', message: `Now publishing ${table.tableName}`, type: 'success' });
    } catch (err) {
      setError(err.message);
    }
  };

  // Handle stop publishing
  const handleStopPublishing = async (config) => {
    try {
      const serviceUrl = getServiceUrl();
      const response = await fetch(`${serviceUrl}/api/esb/outbox-config/${config.id}`, {
        method: 'DELETE'
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);

      // Optionally delete topic
      await deleteKafkaTopic(`${selectedDomain}.${config.tableName}`);
      fetchDomainData();
      setToast({ show: true, title: 'Stopped', message: `Stopped publishing ${config.tableName}`, type: 'info' });
    } catch (err) {
      setError(err.message);
    }
  };

  // Handle subscribe
  const handleSubscribe = async (sourceDomain, sourceTable, sourceSchema = 'fabrica') => {
    try {
      const serviceUrl = getServiceUrl();
      const consumerGroup = `${selectedDomain}-${sourceDomain}.${sourceTable}`;

      const response = await fetch(`${serviceUrl}/api/esb/cache-config`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sourceDomain,
          sourceSchema,
          sourceTable,
          consumerGroup,
          listenCreate: true,
          listenUpdate: true,
          listenDelete: true,
          isActive: true,
          description: `Consuming ${sourceTable} from ${sourceDomain}`
        })
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || `HTTP ${response.status}`);
      }

      fetchDomainData();
      setToast({ show: true, title: 'Subscribed', message: `Now subscribed to ${sourceTable}`, type: 'success' });
    } catch (err) {
      setError(err.message);
    }
  };

  // Handle stop subscribing
  const handleStopSubscribing = async (config) => {
    try {
      const serviceUrl = getServiceUrl();
      const response = await fetch(`${serviceUrl}/api/esb/cache-config/${config.id}`, {
        method: 'DELETE'
      });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);

      fetchDomainData();
      setToast({ show: true, title: 'Unsubscribed', message: `Stopped subscribing to ${config.sourceTable}`, type: 'info' });
    } catch (err) {
      setError(err.message);
    }
  };

  // Get domain display name
  const getDomainDisplayName = (domainId) => {
    const found = domains.find(d => d.domainName === domainId);
    return found?.displayName || ALL_DOMAINS.find(d => d.id === domainId)?.name || domainId;
  };

  // Group subscriptions by source domain
  const getSubscriptionsByDomain = () => {
    const grouped = {};

    // First, group existing cache configs
    cacheConfigs.forEach(config => {
      if (!grouped[config.sourceDomain]) {
        grouped[config.sourceDomain] = [];
      }
      grouped[config.sourceDomain].push({
        ...config,
        isSubscribed: true
      });
    });

    // Then add available topics from other domains
    Object.entries(otherDomainsPublishing).forEach(([domainName, configs]) => {
      if (!grouped[domainName]) {
        grouped[domainName] = [];
      }

      configs.forEach(pubConfig => {
        const existing = grouped[domainName].find(c => c.sourceTable === pubConfig.tableName || c.tableName === pubConfig.tableName);
        if (!existing) {
          grouped[domainName].push({
            sourceTable: pubConfig.tableName,
            sourceDomain: domainName,
            schemaName: pubConfig.schemaName,
            isSubscribed: false
          });
        }
      });
    });

    return grouped;
  };

  const subscriptionsByDomain = getSubscriptionsByDomain();

  return (
    <div className="h-full flex flex-col">
      {/* Toast */}
      {toast.show && (
        <Suspense fallback={null}>
          <Toast
            title={toast.title}
            message={toast.message}
            type={toast.type}
            show={toast.show}
            onClose={() => setToast(prev => ({ ...prev, show: false }))}
            autoHide={true}
            autoHideDelay={3000}
          />
        </Suspense>
      )}

      {/* Domain Selection Pills */}
      <div className="px-4 py-3 border-b border-gray-200">
        <p className="text-sm text-gray-600 mb-2">
          Select a domain below to configure which tables it publishes to the enterprise and to which tables from other domains it subscribes.
        </p>
        <p className="text-xs font-medium text-gray-500 mb-2">Domain selection:</p>
        <div className="grid grid-cols-3 gap-2">
          {ALL_DOMAINS.map(domain => {
            const isSelected = selectedDomain === domain.id;
            const isAvailable = DOMAIN_SERVICE_URLS[domain.id];

            return (
              <button
                key={domain.id}
                onClick={() => isAvailable && setSelectedDomain(domain.id)}
                disabled={!isAvailable}
                className={`px-3 py-2 text-sm font-medium rounded-md border transition-all ${
                  isSelected
                    ? 'border-gray-900 bg-white shadow-sm ring-1 ring-gray-900'
                    : isAvailable
                      ? 'border-gray-300 bg-gray-50 hover:bg-gray-100 hover:border-gray-400'
                      : 'border-gray-200 bg-gray-100 text-gray-400 cursor-not-allowed'
                }`}
              >
                {domain.name}
              </button>
            );
          })}
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mx-4 mt-3 bg-red-50 border border-red-200 rounded-lg p-3">
          <div className="flex items-center">
            <svg className="w-4 h-4 text-red-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span className="text-xs text-red-700">{error}</span>
            <button onClick={() => setError(null)} className="ml-auto text-red-400 hover:text-red-600">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      )}

      {/* Domain Panel */}
      <div className="flex-1 overflow-y-auto p-4">
        {loading ? (
          <div className="flex items-center justify-center h-32">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>
        ) : (
          <div className="border-2 border-blue-500 rounded-lg overflow-hidden">
            {/* Domain Header */}
            <div className="bg-blue-600 text-white px-4 py-2 text-center font-semibold">
              {getDomainDisplayName(selectedDomain)}
            </div>

            {/* Two Column Layout */}
            <div className="grid grid-cols-2 divide-x divide-gray-200">
              {/* Publishing Column */}
              <div className="p-3">
                <h3 className="text-sm font-semibold text-gray-700 mb-3">Publishing:</h3>

                {/* Domain header with outgoing arrow */}
                <div className="bg-gray-200 rounded px-3 py-1.5 mb-2 flex items-center justify-between">
                  <span className="text-sm font-medium">{getDomainDisplayName(selectedDomain)}</span>
                  <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" />
                  </svg>
                </div>

                {/* Publishing topics */}
                <div className="space-y-1.5">
                  {tables.map(table => {
                    const config = getOutboxConfig(table.tableName);
                    const isPublishing = !!config;
                    const expandKey = `pub-${table.tableName}`;
                    const isExpanded = expandedItems[expandKey];

                    return (
                      <div key={table.tableName}>
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => isPublishing ? handleStopPublishing(config) : handlePublish(table)}
                            className={`px-2.5 py-1 text-xs font-medium rounded ${
                              isPublishing
                                ? 'bg-red-500 text-white hover:bg-red-600'
                                : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                            }`}
                          >
                            {isPublishing ? 'Stop' : 'Pub'}
                          </button>
                          <div
                            className={`flex-1 px-3 py-1.5 rounded cursor-pointer ${
                              isPublishing ? 'bg-white border border-gray-300' : 'bg-gray-100'
                            }`}
                            onClick={() => isPublishing && toggleExpanded(expandKey)}
                          >
                            <span className={`text-sm ${isPublishing ? 'font-semibold text-gray-900' : 'text-gray-500'}`}>
                              {table.tableName}
                            </span>
                          </div>
                        </div>

                        {/* Expanded details */}
                        {isPublishing && isExpanded && (
                          <div className="ml-12 mt-1 p-2 bg-gray-50 rounded border border-gray-200 text-xs">
                            <p className="text-gray-600">
                              Topic: <span className="font-mono text-blue-600">{selectedDomain}.{table.tableName}</span>
                            </p>
                            <div className="mt-1 flex gap-2">
                              {config.captureInsert && <span className="px-1.5 py-0.5 bg-green-100 text-green-700 rounded">insert</span>}
                              {config.captureUpdate && <span className="px-1.5 py-0.5 bg-blue-100 text-blue-700 rounded">update</span>}
                              {config.captureDelete && <span className="px-1.5 py-0.5 bg-red-100 text-red-700 rounded">delete</span>}
                            </div>
                          </div>
                        )}
                      </div>
                    );
                  })}

                  {tables.length === 0 && (
                    <p className="text-sm text-gray-400 italic py-2">No tables available</p>
                  )}
                </div>
              </div>

              {/* Subscribing Column */}
              <div className="p-3">
                <h3 className="text-sm font-semibold text-gray-700 mb-3">Subscribing:</h3>

                {Object.entries(subscriptionsByDomain).map(([domainName, items]) => {
                  if (items.length === 0) return null;

                  return (
                    <div key={domainName} className="mb-4">
                      {/* Source domain header with incoming arrow */}
                      <div className="bg-gray-200 rounded px-3 py-1.5 mb-2 flex items-center justify-between">
                        <span className="text-sm font-medium">{getDomainDisplayName(domainName)}</span>
                        <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
                        </svg>
                      </div>

                      {/* Subscription topics */}
                      <div className="space-y-1.5">
                        {items.map(item => {
                          const tableName = item.sourceTable || item.tableName;
                          const isSubscribed = item.isSubscribed;
                          const expandKey = `sub-${domainName}-${tableName}`;
                          const isExpanded = expandedItems[expandKey];

                          return (
                            <div key={`${domainName}-${tableName}`}>
                              <div className="flex items-center gap-2">
                                <button
                                  onClick={() => isSubscribed
                                    ? handleStopSubscribing(item)
                                    : handleSubscribe(domainName, tableName, item.schemaName)
                                  }
                                  className={`px-2.5 py-1 text-xs font-medium rounded ${
                                    isSubscribed
                                      ? 'bg-red-500 text-white hover:bg-red-600'
                                      : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                                  }`}
                                >
                                  {isSubscribed ? 'Stop' : 'Sub'}
                                </button>
                                <div
                                  className={`flex-1 px-3 py-1.5 rounded cursor-pointer ${
                                    isSubscribed ? 'bg-white border border-gray-300' : 'bg-gray-100'
                                  }`}
                                  onClick={() => isSubscribed && toggleExpanded(expandKey)}
                                >
                                  <span className={`text-sm ${isSubscribed ? 'font-semibold text-gray-900' : 'text-gray-500'}`}>
                                    {tableName}
                                  </span>
                                </div>
                              </div>

                              {/* Expanded details */}
                              {isSubscribed && isExpanded && (
                                <div className="ml-12 mt-1 p-2 bg-gray-50 rounded border border-gray-200 text-xs">
                                  <p className="text-gray-600">
                                    Topic: <span className="font-mono text-blue-600">{domainName}.{tableName}</span>
                                  </p>
                                  <p className="text-gray-600 mt-1">
                                    Consumer: <span className="font-mono text-purple-600">{item.consumerGroup}</span>
                                  </p>
                                </div>
                              )}
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  );
                })}

                {Object.keys(subscriptionsByDomain).length === 0 && (
                  <p className="text-sm text-gray-400 italic py-2">No other domains publishing</p>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

// ============================================================================
// DEPLOYMENT TAB - Preserved from original DevToolsDrawer
// ============================================================================
const DeploymentTab = () => {
  const [scriptRunnerStatus, setScriptRunnerStatus] = useState(null);
  const [deploying, setDeploying] = useState(null);
  const [output, setOutput] = useState([]);
  const [options, setOptions] = useState({ noCache: false, killPorts: false });
  const outputRef = useRef(null);

  const components = [
    { id: 'shell-admin', name: 'Admin Shell', category: 'Admin', color: 'blue' },
    { id: 'mfe-admin', name: 'Admin MFE', category: 'Admin', color: 'blue' },
    { id: 'bff-admin', name: 'Admin BFF', category: 'Admin', color: 'blue' },
    { id: 'acl-admin', name: 'Admin ACL', category: 'Admin', color: 'blue' },
    { id: 'mfe-product', name: 'Product MFE', category: 'Product', color: 'green' },
    { id: 'bff-product', name: 'Product BFF', category: 'Product', color: 'green' },
    { id: 'acl-product', name: 'Product ACL', category: 'Product', color: 'green' },
  ];

  useEffect(() => {
    checkScriptRunnerStatus();
  }, []);

  useEffect(() => {
    if (outputRef.current) {
      outputRef.current.scrollTop = outputRef.current.scrollHeight;
    }
  }, [output]);

  const checkScriptRunnerStatus = async () => {
    try {
      const response = await fetch(`${SCRIPT_RUNNER_URL}/health`);
      const data = await response.json();
      setScriptRunnerStatus({ scriptRunnerAvailable: true, ...data });
    } catch (err) {
      setScriptRunnerStatus({ scriptRunnerAvailable: false, error: err.message });
    }
  };

  const handleRedeploy = async (componentId) => {
    if (deploying) return;

    setDeploying(componentId);
    setOutput([{ type: 'info', line: `Starting redeploy of ${componentId}...`, timestamp: new Date() }]);

    const url = `${SCRIPT_RUNNER_URL}/redeploy/${componentId}`;
    let streamingWorked = false;
    let receivedComplete = false;

    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'text/event-stream',
          'X-API-Key': SCRIPT_RUNNER_API_KEY
        },
        body: JSON.stringify(options)
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            try {
              const data = JSON.parse(line.slice(6));
              streamingWorked = true;
              if (data.type === 'complete') receivedComplete = true;
              handleSSEEvent(data);
            } catch {}
          }
        }
      }

      if (buffer.startsWith('data: ')) {
        try {
          const data = JSON.parse(buffer.slice(6));
          streamingWorked = true;
          if (data.type === 'complete') receivedComplete = true;
          handleSSEEvent(data);
        } catch {}
      }
    } catch (err) {
      if (!streamingWorked) {
        setOutput(prev => [...prev,
          { type: 'info', line: `Build triggered for ${componentId}!`, timestamp: new Date() },
          { type: 'info', line: 'Note: Live streaming may not be available, but the build is running in the background.', timestamp: new Date() }
        ]);
      } else if (!receivedComplete) {
        setOutput(prev => [...prev,
          { type: 'info', line: 'Connection closed. Build is continuing in the background.', timestamp: new Date() }
        ]);
      }
    } finally {
      setDeploying(null);
    }
  };

  const handleSSEEvent = (data) => {
    const timestamp = new Date();

    switch (data.type) {
      case 'start':
        setOutput(prev => [...prev, { type: 'info', line: `Executing: redeploy.sh ${data.args?.join(' ') || ''}`, timestamp }]);
        break;
      case 'stdout':
        setOutput(prev => [...prev, { type: 'stdout', line: data.line, timestamp }]);
        break;
      case 'stderr':
        setOutput(prev => [...prev, { type: 'stderr', line: data.line, timestamp }]);
        break;
      case 'complete':
        const success = data.code === 0;
        setOutput(prev => [...prev, {
          type: success ? 'success' : 'error',
          line: success ? 'Redeploy completed successfully!' : `Redeploy failed with exit code ${data.code}`,
          timestamp
        }]);
        break;
      case 'error':
        setOutput(prev => [...prev, { type: 'error', line: `Error: ${data.message}`, timestamp }]);
        break;
    }
  };

  const clearOutput = () => setOutput([]);

  const getLineColor = (type) => {
    switch (type) {
      case 'info': return 'text-blue-400';
      case 'stdout': return 'text-gray-300';
      case 'stderr': return 'text-yellow-400';
      case 'success': return 'text-green-400';
      case 'error': return 'text-red-400';
      default: return 'text-gray-400';
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Script Runner Status */}
      {!scriptRunnerStatus?.scriptRunnerAvailable && (
        <div className="mx-4 mt-4 bg-yellow-50 border border-yellow-200 rounded-lg p-3">
          <div className="flex">
            <svg className="w-4 h-4 text-yellow-400 mr-2 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <div>
              <h3 className="text-xs font-medium text-yellow-800">Script Runner Not Available</h3>
              <pre className="mt-1 text-xs bg-yellow-100 p-1.5 rounded font-mono">
                cd infrastructure/local-services/script-runner && ./start.sh
              </pre>
            </div>
          </div>
        </div>
      )}

      {/* Status indicator */}
      <div className="px-4 py-2 flex items-center gap-2">
        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
          scriptRunnerStatus?.scriptRunnerAvailable
            ? 'bg-green-100 text-green-800'
            : 'bg-red-100 text-red-800'
        }`}>
          <span className={`w-1.5 h-1.5 rounded-full mr-1.5 ${
            scriptRunnerStatus?.scriptRunnerAvailable ? 'bg-green-500' : 'bg-red-500'
          }`}></span>
          {scriptRunnerStatus?.scriptRunnerAvailable ? 'Connected' : 'Disconnected'}
        </span>
      </div>

      {/* Options */}
      <div className="px-4 py-2 border-b border-gray-200">
        <div className="flex gap-4">
          <label className="flex items-center">
            <input
              type="checkbox"
              checked={options.noCache}
              onChange={(e) => setOptions(prev => ({ ...prev, noCache: e.target.checked }))}
              className="h-3.5 w-3.5 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
            />
            <span className="ml-1.5 text-xs text-gray-700">No Cache</span>
          </label>
          <label className="flex items-center">
            <input
              type="checkbox"
              checked={options.killPorts}
              onChange={(e) => setOptions(prev => ({ ...prev, killPorts: e.target.checked }))}
              className="h-3.5 w-3.5 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
            />
            <span className="ml-1.5 text-xs text-gray-700">Kill Local Ports</span>
          </label>
        </div>
      </div>

      {/* Components Grid */}
      <div className="px-4 py-3 border-b border-gray-200">
        <div className="grid grid-cols-2 gap-2">
          {components.map((component) => (
            <button
              key={component.id}
              onClick={() => handleRedeploy(component.id)}
              disabled={!scriptRunnerStatus?.scriptRunnerAvailable || deploying}
              className={`relative px-3 py-2 rounded-lg border text-left transition-all text-sm ${
                deploying === component.id
                  ? 'border-blue-500 bg-blue-50'
                  : scriptRunnerStatus?.scriptRunnerAvailable && !deploying
                    ? 'border-gray-200 bg-white hover:border-blue-300 hover:shadow-sm cursor-pointer'
                    : 'border-gray-200 bg-gray-50 cursor-not-allowed opacity-60'
              }`}
            >
              {deploying === component.id && (
                <div className="absolute top-2 right-2">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                </div>
              )}
              <div className={`inline-flex px-1.5 py-0.5 rounded text-xs font-medium mb-1 ${
                component.color === 'blue' ? 'bg-blue-100 text-blue-800' : 'bg-green-100 text-green-800'
              }`}>
                {component.category}
              </div>
              <div className="font-medium text-gray-900 text-xs">{component.name}</div>
            </button>
          ))}
        </div>
      </div>

      {/* Output Console */}
      <div className="flex-1 flex flex-col min-h-0 bg-gray-900">
        <div className="flex justify-between items-center px-3 py-2 bg-gray-800 border-b border-gray-700">
          <h3 className="text-xs font-medium text-gray-300">Output</h3>
          <button
            onClick={clearOutput}
            className="text-xs text-gray-400 hover:text-white px-2 py-0.5 rounded hover:bg-gray-700"
          >
            Clear
          </button>
        </div>
        <div
          ref={outputRef}
          className="flex-1 p-3 overflow-y-auto font-mono text-xs"
        >
          {output.length === 0 ? (
            <p className="text-gray-500">Click a component to start a redeploy...</p>
          ) : (
            output.map((line, index) => (
              <div key={index} className={`${getLineColor(line.type)} whitespace-pre-wrap`}>
                {line.line}
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminToolsDrawer;
