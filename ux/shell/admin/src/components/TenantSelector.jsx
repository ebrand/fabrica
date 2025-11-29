import { useState } from 'react';
import { useTenant } from '../context/TenantContext';

const TenantSelector = () => {
  const { availableTenants, selectTenant, showSelector, closeSelector, currentTenant } = useTenant();
  const [selecting, setSelecting] = useState(null);

  if (!showSelector || availableTenants.length === 0) {
    return null;
  }

  const handleSelect = async (tenant) => {
    setSelecting(tenant.tenantId);
    const success = await selectTenant(tenant);
    if (!success) {
      setSelecting(null);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/50 transition-opacity" />

      {/* Modal Content */}
      <div className="flex min-h-full items-center justify-center p-4">
        <div className="relative bg-white rounded-lg shadow-xl w-full max-w-lg">
          {/* Header */}
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">Select Workspace</h3>
            <p className="mt-1 text-sm text-gray-500">
              Choose which workspace you'd like to work in
            </p>
          </div>

          {/* Tenant List */}
          <div className="px-6 py-4 max-h-96 overflow-y-auto">
            <div className="space-y-2">
              {availableTenants.map((tenant) => (
                <button
                  key={tenant.tenantId}
                  onClick={() => handleSelect(tenant)}
                  disabled={selecting !== null}
                  className={`w-full text-left p-4 rounded-lg border-2 transition-all ${
                    currentTenant?.tenantId === tenant.tenantId
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-blue-300 hover:bg-gray-50'
                  } ${selecting !== null && selecting !== tenant.tenantId ? 'opacity-50' : ''}`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      {/* Tenant Icon */}
                      <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${
                        tenant.slug === 'all'
                          ? 'bg-gradient-to-br from-amber-400 to-amber-600'
                          : tenant.isPersonal
                          ? 'bg-gradient-to-br from-purple-400 to-purple-600'
                          : 'bg-gradient-to-br from-blue-400 to-blue-600'
                      }`}>
                        {tenant.slug === 'all' ? (
                          <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                        ) : tenant.isPersonal ? (
                          <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                          </svg>
                        ) : (
                          <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                          </svg>
                        )}
                      </div>

                      {/* Tenant Info */}
                      <div>
                        <div className="font-medium text-gray-900">{tenant.name}</div>
                        <div className="text-sm text-gray-500 flex items-center space-x-2">
                          <span>{tenant.role}</span>
                          {tenant.slug === 'all' && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-amber-100 text-amber-800">
                              System Admin
                            </span>
                          )}
                          {tenant.isPersonal && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-800">
                              Personal
                            </span>
                          )}
                        </div>
                      </div>
                    </div>

                    {/* Loading/Selected indicator */}
                    {selecting === tenant.tenantId && (
                      <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600" />
                    )}
                    {currentTenant?.tenantId === tenant.tenantId && selecting !== tenant.tenantId && (
                      <svg className="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    )}
                  </div>
                </button>
              ))}
            </div>
          </div>

          {/* Footer */}
          {currentTenant && (
            <div className="px-6 py-4 border-t border-gray-200 bg-gray-50 rounded-b-lg">
              <button
                onClick={closeSelector}
                className="w-full px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                Cancel
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default TenantSelector;
