import { lazy, Suspense } from 'react';
import { useTelemetry } from '../context/TelemetryContext';

// Lazy load the EsbTelemetry component from adminMfe
const EsbTelemetryMfe = lazy(() => import('adminMfe/EsbTelemetry'));

const EsbPanel = ({ isOpen, onClose, onOpen }) => {
  const { events, connectionStatus, clearEvents } = useTelemetry();

  return (
    <>
      {/* Tab handle - positioned on the right edge */}
      <button
        onClick={isOpen ? onClose : onOpen}
        className={`fixed right-0 bottom-[830px] bg-blue-600 hover:bg-blue-700 text-white px-1.5 py-3 rounded-l-md shadow-lg transition-all duration-300 z-50 flex flex-col items-center gap-2 ${
          isOpen ? 'translate-x-0' : 'translate-x-0'
        }`}
        style={{ transform: isOpen ? `translateX(-500px)` : 'translateX(0)' }}
        title={isOpen ? 'Close ESB Panel' : 'Open ESB Panel'}
      >
        {/* Activity/Signal icon */}
        <svg
          className="w-5 h-5"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
          />
        </svg>
        <span
          className="text-xs font-medium tracking-wide block"
          style={{
            writingMode: 'vertical-lr',
            transform: 'rotate(180deg)'
          }}
        >
          ESB
        </span>
      </button>

      {/* Panel - slides in from the right */}
      <div
        className={`fixed right-0 top-0 h-full bg-white shadow-2xl flex flex-col transition-transform duration-300 ease-in-out z-40 ${
          isOpen ? 'translate-x-0' : 'translate-x-full'
        }`}
        style={{ width: '500px' }}
      >
        {/* Header */}
        <div className="flex justify-between items-center px-4 py-3 border-b border-gray-200 bg-blue-50">
          <div className="flex items-center gap-2">
            <svg
              className="w-5 h-5 text-blue-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
              />
            </svg>
            <h2 className="text-lg font-semibold text-gray-900">ESB Telemetry</h2>
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

        {/* Content */}
        <div className="flex-1 overflow-hidden">
          <Suspense
            fallback={
              <div className="flex items-center justify-center h-full">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
                  <p className="mt-4 text-gray-600">Loading ESB Telemetry...</p>
                </div>
              </div>
            }
          >
            <EsbTelemetryMfe
              events={events}
              connectionStatus={connectionStatus}
              clearEvents={clearEvents}
            />
          </Suspense>
        </div>
      </div>
    </>
  );
};

export default EsbPanel;
