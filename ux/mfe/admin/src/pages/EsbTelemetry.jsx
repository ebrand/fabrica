import { useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import configService from '../services/config';
import Select from 'commonMfe/Select';

const MAX_EVENTS = 100;

const DOMAIN_OPTIONS = [
  { id: '', name: 'All Domains' },
  { id: 'admin', name: 'Admin' },
  { id: 'content', name: 'Content' },
  { id: 'product', name: 'Product' },
];

const SERVICE_OPTIONS = [
  { id: '', name: 'All Services' },
  { id: 'OutboxPublisher', name: 'Publisher' },
  { id: 'CacheSubscriber', name: 'Consumer' },
];

const SERVICE_TYPE_ICONS = {
  outboxPublisher: 'PUB',
  cacheSubscriber: 'SUB',
};

function EsbTelemetry({
  events: propsEvents,
  connectionStatus: propsConnectionStatus,
  clearEvents: propsClearEvents
}) {
  // Use props if provided (shell context), otherwise manage local state
  const usePropsState = propsEvents !== undefined;

  const [localEvents, setLocalEvents] = useState([]);
  const [localConnectionStatus, setLocalConnectionStatus] = useState('disconnected');
  const [filter, setFilter] = useState({
    domain: DOMAIN_OPTIONS[0],
    serviceType: SERVICE_OPTIONS[0],
    showErrors: false,
    hideAdmin: false
  });
  const [selectedEvent, setSelectedEvent] = useState(null);
  const [highlightedAggregateId, setHighlightedAggregateId] = useState(null);
  const connectionRef = useRef(null);
  const eventsEndRef = useRef(null);

  // Use props or local state
  const events = usePropsState ? propsEvents : localEvents;
  const connectionStatus = usePropsState ? propsConnectionStatus : localConnectionStatus;

  const addEvent = useCallback((event) => {
    setLocalEvents((prev) => {
      const newEvents = [event, ...prev].slice(0, MAX_EVENTS);
      return newEvents;
    });
  }, []);

  const clearEvents = useCallback(() => {
    if (usePropsState && propsClearEvents) {
      propsClearEvents();
    } else {
      setLocalEvents([]);
    }
  }, [usePropsState, propsClearEvents]);

  // Only create local connection if not using props
  useEffect(() => {
    if (usePropsState) return;

    let connection = null;

    const connect = async () => {
      try {
        const bffUrl = await configService.getBffAdminUrl();
        const hubUrl = `${bffUrl}/hubs/telemetry`;

        connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl)
          .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
          .configureLogging(signalR.LogLevel.Information)
          .build();

        connection.on('TelemetryEvent', (event) => {
          addEvent(event);
        });

        connection.onreconnecting(() => {
          setLocalConnectionStatus('reconnecting');
        });

        connection.onreconnected(() => {
          setLocalConnectionStatus('connected');
        });

        connection.onclose(() => {
          setLocalConnectionStatus('disconnected');
        });

        await connection.start();
        connectionRef.current = connection;
        setLocalConnectionStatus('connected');
      } catch (err) {
        console.error('Failed to connect to telemetry hub:', err);
        setLocalConnectionStatus('error');
      }
    };

    connect();

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [usePropsState, addEvent]);

  const filteredEvents = events.filter((event) => {
    if (event.eventType === 'subscriptionUpdated') return false;
    if (filter.hideAdmin && event.domain === 'admin') return false;
    if (filter.domain?.id && event.domain !== filter.domain.id) return false;
    if (filter.serviceType?.id && event.serviceType !== filter.serviceType.id) return false;
    if (filter.showErrors && event.success !== false) return false;
    return true;
  });

  const formatTimestamp = (timestamp) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString('en-US', {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      fractionalSecondDigits: 3,
    });
  };

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="mb-4 flex justify-between items-center px-6 py-4">
        <div>
          <p className="text-sm text-gray-500 mt-1">
            Real-time event streaming from producers and consumers
          </p>
        </div>
        <div className="flex items-center gap-4">
          <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-sm ${
            connectionStatus === 'connected' ? 'bg-green-100 text-green-800' :
            connectionStatus === 'reconnecting' ? 'bg-yellow-100 text-yellow-800' :
            connectionStatus === 'error' ? 'bg-red-100 text-red-800' :
            'bg-gray-100 text-gray-800'
          }`}>
            <span className={`w-2 h-2 rounded-full ${
              connectionStatus === 'connected' ? 'bg-green-500 animate-pulse' :
              connectionStatus === 'reconnecting' ? 'bg-yellow-500 animate-pulse' :
              connectionStatus === 'error' ? 'bg-red-500' :
              'bg-gray-400'
            }`} />
            {connectionStatus}
          </div>
          <button
            onClick={clearEvents}
            className="px-3 py-1.5 text-sm text-gray-600 hover:text-gray-900 border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Clear
          </button>
        </div>
      </div>

      {/* Filters */}
      <div className="px-6 mb-4 space-y-2">
        <Select
          options={DOMAIN_OPTIONS}
          value={filter.domain}
          onChange={(option) => setFilter((f) => ({ ...f, domain: option }))}
          displayKey="name"
          valueKey="id"
        />
        <Select
          options={SERVICE_OPTIONS}
          value={filter.serviceType}
          onChange={(option) => setFilter((f) => ({ ...f, serviceType: option }))}
          displayKey="name"
          valueKey="id"
        />
        <div className="flex items-center gap-4">
          <label className="flex items-center gap-2 text-sm text-gray-600">
            <input
              type="checkbox"
              checked={filter.showErrors}
              onChange={(e) => setFilter((f) => ({ ...f, showErrors: e.target.checked }))}
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            Errors only
          </label>
          <label className="flex items-center gap-2 text-sm text-gray-600">
            <input
              type="checkbox"
              checked={filter.hideAdmin}
              onChange={(e) => setFilter((f) => ({ ...f, hideAdmin: e.target.checked }))}
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            Hide admin
          </label>
          <span className="text-sm text-gray-500 ml-auto">
            {filteredEvents.length} events
          </span>
        </div>
      </div>

      {/* Event Stream */}
      <div className="flex-1 overflow-hidden px-6 pb-6">
        <div className="h-full border border-gray-200 rounded-lg bg-gray-900 overflow-y-auto font-mono text-xs">
          {filteredEvents.length === 0 ? (
            <div className="flex items-center justify-center h-full text-gray-500">
              {connectionStatus === 'connected'
                ? 'Waiting for events... (try updating a product)'
                : 'Connecting to telemetry hub...'}
            </div>
          ) : (
            <div className="p-2">
              {filteredEvents.map((event) => {
                const isHighlighted = highlightedAggregateId && event.aggregateId === highlightedAggregateId;
                return (
                <div
                  key={event.id}
                  onClick={() => {
                    if (event.aggregateId) {
                      setHighlightedAggregateId(
                        highlightedAggregateId === event.aggregateId ? null : event.aggregateId
                      );
                    }
                  }}
                  onDoubleClick={() => setSelectedEvent(event)}
                  className={`flex items-center gap-2 py-0.5 px-2 rounded cursor-pointer leading-tight ${
                    isHighlighted ? 'bg-purple-900/40 ring-1 ring-purple-500/50' :
                    event.success === false ? 'bg-red-900/20' : 'hover:bg-gray-800/50'
                  }`}
                >
                  <span className="text-gray-500 whitespace-nowrap">
                    {formatTimestamp(event.timestamp)}
                  </span>
                  <span className={`px-1.5 py-0.5 rounded text-xs font-medium ${
                    event.serviceType === 'OutboxPublisher'
                      ? 'bg-green-900/50 text-green-400'
                      : 'bg-blue-900/50 text-blue-400'
                  }`}>
                    {SERVICE_TYPE_ICONS[event.serviceType] || '???'}
                  </span>
                  <span className="text-gray-200 font-medium w-16">
                    {event.domain}
                  </span>
                  <span className={`text-lg ${
                    event.eventType === 'eventPublished' ? 'text-green-400' :
                    event.eventType === 'eventProcessed' ? 'text-blue-400' :
                    event.eventType?.includes('Failed') ? 'text-red-400' :
                    'text-gray-400'
                  }`}>
                    {event.eventType === 'eventPublished' ? '→' :
                     event.eventType === 'eventProcessed' ? '←' :
                     event.eventType?.includes('Failed') ? '✕' : '•'}
                  </span>
                  {event.eventType === 'serviceStarted' && (
                    <span className="text-green-400">
                      {event.serviceType} <span className="text-gray-400">started</span>
                    </span>
                  )}
                  {event.eventType === 'serviceStopped' && (
                    <span className="text-red-400">
                      {event.serviceType} <span className="text-gray-400">stopped</span>
                    </span>
                  )}
                  {event.eventType === 'subscriptionUpdated' && (
                    <span className="text-purple-400">
                      subscription updated <span className="text-gray-400">{event.topic}</span>
                    </span>
                  )}
                  {event.aggregateType && (
                    <span className="text-cyan-400">
                      {event.aggregateType}
                    </span>
                  )}
                  {event.action && (
                    <span className="text-yellow-400">
                      {event.action}
                    </span>
                  )}
                  {event.durationMs !== null && (
                    <span className="text-gray-400">
                      {event.durationMs}ms
                    </span>
                  )}
                  {event.success === false && event.errorMessage && (
                    <span className="text-red-400 truncate" title={event.errorMessage}>
                      {event.errorMessage}
                    </span>
                  )}
                </div>
              );
              })}
              <div ref={eventsEndRef} />
            </div>
          )}
        </div>
      </div>

      {/* Event Detail Modal */}
      {selectedEvent && (
        <div
          className="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
          onClick={() => setSelectedEvent(null)}
        >
          <div
            className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[80vh] overflow-hidden"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex justify-between items-center px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-semibold text-gray-900">Event Details</h3>
              <button
                onClick={() => setSelectedEvent(null)}
                className="text-gray-400 hover:text-gray-600"
              >
                <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <div className="px-6 py-4 overflow-y-auto max-h-[calc(80vh-120px)]">
              <dl className="space-y-3">
                {Object.entries(selectedEvent).map(([key, value]) => (
                  <div key={key} className="grid grid-cols-3 gap-4">
                    <dt className="text-sm font-medium text-gray-500">{key}</dt>
                    <dd className="text-sm text-gray-900 col-span-2 font-mono break-all">
                      {value === null ? (
                        <span className="text-gray-400 italic">null</span>
                      ) : value === true ? (
                        <span className="text-green-600">true</span>
                      ) : value === false ? (
                        <span className="text-red-600">false</span>
                      ) : typeof value === 'object' ? (
                        <pre className="text-xs bg-gray-100 p-2 rounded overflow-x-auto">
                          {JSON.stringify(value, null, 2)}
                        </pre>
                      ) : (
                        String(value)
                      )}
                    </dd>
                  </div>
                ))}
              </dl>
            </div>
            <div className="px-6 py-4 border-t border-gray-200 flex justify-end">
              <button
                onClick={() => setSelectedEvent(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default EsbTelemetry;
