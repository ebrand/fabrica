import { createContext, useContext, useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

const MAX_EVENTS = 200;

const TelemetryContext = createContext(null);

export function TelemetryProvider({ children, bffUrl }) {
  const [events, setEvents] = useState([]);
  const [connectionStatus, setConnectionStatus] = useState('disconnected');
  const connectionRef = useRef(null);
  const isConnectingRef = useRef(false);

  const addEvent = useCallback((event) => {
    setEvents((prev) => {
      const newEvents = [event, ...prev].slice(0, MAX_EVENTS);
      return newEvents;
    });
  }, []);

  const clearEvents = useCallback(() => {
    setEvents([]);
  }, []);

  useEffect(() => {
    if (!bffUrl || isConnectingRef.current || connectionRef.current) return;

    isConnectingRef.current = true;

    const connect = async () => {
      try {
        const hubUrl = `${bffUrl}/hubs/telemetry`;

        const connection = new signalR.HubConnectionBuilder()
          .withUrl(hubUrl)
          .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
          .configureLogging(signalR.LogLevel.Warning)
          .build();

        connection.on('TelemetryEvent', (event) => {
          addEvent(event);
        });

        connection.onreconnecting(() => {
          setConnectionStatus('reconnecting');
        });

        connection.onreconnected(() => {
          setConnectionStatus('connected');
        });

        connection.onclose(() => {
          setConnectionStatus('disconnected');
          connectionRef.current = null;
          isConnectingRef.current = false;
        });

        await connection.start();
        connectionRef.current = connection;
        setConnectionStatus('connected');
        isConnectingRef.current = false;
      } catch (err) {
        console.error('Failed to connect to telemetry hub:', err);
        setConnectionStatus('error');
        isConnectingRef.current = false;
      }
    };

    connect();

    return () => {
      // Don't disconnect on unmount - keep connection alive
    };
  }, [bffUrl, addEvent]);

  const value = {
    events,
    connectionStatus,
    clearEvents,
    addEvent,
  };

  return (
    <TelemetryContext.Provider value={value}>
      {children}
    </TelemetryContext.Provider>
  );
}

export function useTelemetry() {
  const context = useContext(TelemetryContext);
  if (!context) {
    throw new Error('useTelemetry must be used within a TelemetryProvider');
  }
  return context;
}

export default TelemetryContext;
