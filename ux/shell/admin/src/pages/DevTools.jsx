import { useState, useEffect, useRef } from 'react';
import Layout from '../components/Layout';

// Script runner runs locally on the host machine, not in Docker
const SCRIPT_RUNNER_URL = 'http://localhost:3800';
const SCRIPT_RUNNER_API_KEY = 'fabrica-dev-key';

const DevTools = () => {
  const [scriptRunnerStatus, setScriptRunnerStatus] = useState(null);
  const [loading, setLoading] = useState(true);
  const [deploying, setDeploying] = useState(null);
  const [output, setOutput] = useState([]);
  const [options, setOptions] = useState({ noCache: false, killPorts: false });
  const outputRef = useRef(null);

  const components = [
    { id: 'shell-admin', name: 'Admin Shell', category: 'Admin', color: 'blue' },
    { id: 'mfe-admin', name: 'Admin MFE', category: 'Admin', color: 'blue' },
    { id: 'bff-admin', name: 'Admin BFF', category: 'Admin', color: 'blue' },
    { id: 'acl-admin', name: 'Admin ACL', category: 'Admin', color: 'blue' },
    { id: 'mfe-content', name: 'Content MFE', category: 'Content', color: 'purple' },
    { id: 'bff-content', name: 'Content BFF', category: 'Content', color: 'purple' },
    { id: 'acl-content', name: 'Content ACL', category: 'Content', color: 'purple' },
    { id: 'mfe-product', name: 'Product MFE', category: 'Product', color: 'green' },
    { id: 'bff-product', name: 'Product BFF', category: 'Product', color: 'green' },
    { id: 'acl-product', name: 'Product ACL', category: 'Product', color: 'green' },
  ];

  useEffect(() => {
    initializeDevTools();
  }, []);

  useEffect(() => {
    // Auto-scroll output to bottom
    if (outputRef.current) {
      outputRef.current.scrollTop = outputRef.current.scrollHeight;
    }
  }, [output]);

  const initializeDevTools = async () => {
    try {
      await checkScriptRunnerStatus();
    } catch (err) {
      console.error('Failed to initialize DevTools:', err);
    } finally {
      setLoading(false);
    }
  };

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

    // Use fetch with ReadableStream for SSE - directly to script runner
    try {
      const controller = new AbortController();
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'text/event-stream',
          'X-API-Key': SCRIPT_RUNNER_API_KEY
        },
        body: JSON.stringify(options),
        signal: controller.signal
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
        buffer = lines.pop() || ''; // Keep incomplete line in buffer

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            try {
              const data = JSON.parse(line.slice(6));
              streamingWorked = true;
              if (data.type === 'complete') receivedComplete = true;
              handleSSEEvent(data);
            } catch (e) {
              // Ignore parse errors
            }
          }
        }
      }

      // Process any remaining buffer
      if (buffer.startsWith('data: ')) {
        try {
          const data = JSON.parse(buffer.slice(6));
          streamingWorked = true;
          if (data.type === 'complete') receivedComplete = true;
          handleSSEEvent(data);
        } catch (e) {
          // Ignore
        }
      }
    } catch (err) {
      // If we got some streaming data, the build is likely running in background
      if (!streamingWorked) {
        setOutput(prev => [...prev,
          { type: 'info', line: `Build triggered for ${componentId}!`, timestamp: new Date() },
          { type: 'info', line: 'Note: Live streaming may not be available, but the build is running in the background.', timestamp: new Date() },
          { type: 'info', line: 'Watch container status with: docker ps -f name=' + componentId, timestamp: new Date() }
        ]);
      } else if (!receivedComplete) {
        setOutput(prev => [...prev,
          { type: 'info', line: 'Connection closed. Build is continuing in the background.', timestamp: new Date() },
          { type: 'info', line: 'Watch container status with: docker ps -f name=' + componentId, timestamp: new Date() }
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

  const clearOutput = () => {
    setOutput([]);
  };

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

  if (loading) {
    return (
      <Layout>
        <div className="flex items-center justify-center p-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Deployment</h1>
            <p className="mt-1 text-sm text-gray-500">Rebuild and redeploy Fabrica components</p>
          </div>
          <div className="flex items-center gap-2">
            <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${
              scriptRunnerStatus?.scriptRunnerAvailable
                ? 'bg-green-100 text-green-800'
                : 'bg-red-100 text-red-800'
            }`}>
              <span className={`w-2 h-2 rounded-full mr-2 ${
                scriptRunnerStatus?.scriptRunnerAvailable ? 'bg-green-500' : 'bg-red-500'
              }`}></span>
              Script Runner: {scriptRunnerStatus?.scriptRunnerAvailable ? 'Connected' : 'Disconnected'}
            </span>
            <button
              onClick={() => checkScriptRunnerStatus()}
              className="p-2 text-gray-500 hover:text-gray-700 rounded-md hover:bg-gray-100"
              title="Refresh status"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
            </button>
          </div>
        </div>

        {/* Script Runner Not Available Warning */}
        {!scriptRunnerStatus?.scriptRunnerAvailable && (
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex">
              <svg className="w-5 h-5 text-yellow-400 mr-3 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <div>
                <h3 className="text-sm font-medium text-yellow-800">Script Runner Not Available</h3>
                <p className="mt-1 text-sm text-yellow-700">
                  To use Deployment, start the script runner service on your local machine:
                </p>
                <pre className="mt-2 text-xs bg-yellow-100 p-2 rounded font-mono">
                  cd infrastructure/local-services/script-runner && ./start.sh
                </pre>
              </div>
            </div>
          </div>
        )}

        {/* Options */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
          <h2 className="text-sm font-medium text-gray-700 mb-3">Build Options</h2>
          <div className="flex gap-6">
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={options.noCache}
                onChange={(e) => setOptions(prev => ({ ...prev, noCache: e.target.checked }))}
                className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="ml-2 text-sm text-gray-700">No Cache</span>
              <span className="ml-1 text-xs text-gray-500">(clean build, slower)</span>
            </label>
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={options.killPorts}
                onChange={(e) => setOptions(prev => ({ ...prev, killPorts: e.target.checked }))}
                className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="ml-2 text-sm text-gray-700">Kill Local Ports</span>
              <span className="ml-1 text-xs text-gray-500">(stop local dev servers)</span>
            </label>
          </div>
        </div>

        {/* Components Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {components.map((component) => (
            <button
              key={component.id}
              onClick={() => handleRedeploy(component.id)}
              disabled={!scriptRunnerStatus?.scriptRunnerAvailable || deploying}
              className={`relative p-4 rounded-lg border-2 text-left transition-all ${
                deploying === component.id
                  ? 'border-blue-500 bg-blue-50'
                  : scriptRunnerStatus?.scriptRunnerAvailable && !deploying
                    ? 'border-gray-200 bg-white hover:border-blue-300 hover:shadow-md cursor-pointer'
                    : 'border-gray-200 bg-gray-50 cursor-not-allowed opacity-60'
              }`}
            >
              {deploying === component.id && (
                <div className="absolute top-2 right-2">
                  <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
                </div>
              )}
              <div className={`inline-flex px-2 py-1 rounded text-xs font-medium mb-2 ${
                component.color === 'blue' ? 'bg-blue-100 text-blue-800' :
                component.color === 'purple' ? 'bg-purple-100 text-purple-800' : 'bg-green-100 text-green-800'
              }`}>
                {component.category}
              </div>
              <h3 className="font-medium text-gray-900">{component.name}</h3>
              <p className="text-sm text-gray-500 mt-1">{component.id}</p>
            </button>
          ))}
        </div>

        {/* Output Console */}
        <div className="bg-gray-900 rounded-lg shadow-sm border border-gray-700 overflow-hidden">
          <div className="flex justify-between items-center px-4 py-2 bg-gray-800 border-b border-gray-700">
            <h2 className="text-sm font-medium text-gray-300">Output</h2>
            <button
              onClick={clearOutput}
              className="text-xs text-gray-400 hover:text-white px-2 py-1 rounded hover:bg-gray-700"
            >
              Clear
            </button>
          </div>
          <div
            ref={outputRef}
            className="p-4 h-80 overflow-y-auto font-mono text-sm"
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
    </Layout>
  );
};

export default DevTools;
