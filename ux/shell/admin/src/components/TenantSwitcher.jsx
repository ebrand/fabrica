import { Popover, PopoverButton, PopoverPanel } from '@headlessui/react';
import { ChevronDownIcon } from '@heroicons/react/20/solid';
import { useTenant } from '../context/TenantContext';

const TenantSwitcher = () => {
  const { currentTenant, availableTenants, selectTenant, hasMultipleTenants, openSelector } = useTenant();

  console.log('🏢 TenantSwitcher render - currentTenant:', currentTenant, 'hasMultipleTenants:', hasMultipleTenants);

  if (!currentTenant) {
    console.log('🏢 TenantSwitcher: No current tenant, returning null');
    return null;
  }

  // If only one tenant, show a static badge
  if (!hasMultipleTenants) {
    return (
      <div className="flex items-center px-3 py-1.5 bg-gray-100 rounded-lg">
        <div className={`w-6 h-6 rounded flex items-center justify-center mr-2 ${
          currentTenant.isPersonal
            ? 'bg-gradient-to-br from-purple-400 to-purple-600'
            : 'bg-gradient-to-br from-blue-400 to-blue-600'
        }`}>
          {currentTenant.isPersonal ? (
            <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
          ) : (
            <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
          )}
        </div>
        <span className="text-sm font-medium text-gray-700">{currentTenant.name}</span>
      </div>
    );
  }

  // Multiple tenants - show dropdown
  return (
    <Popover className="relative">
      {({ open, close }) => (
        <>
          <PopoverButton className="flex items-center px-3 py-1.5 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors focus:outline-none cursor-pointer">
            <div className={`w-6 h-6 rounded flex items-center justify-center mr-2 ${
              currentTenant.isPersonal
                ? 'bg-gradient-to-br from-purple-400 to-purple-600'
                : 'bg-gradient-to-br from-blue-400 to-blue-600'
            }`}>
              {currentTenant.isPersonal ? (
                <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              ) : (
                <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                </svg>
              )}
            </div>
            <span className="text-sm font-medium text-gray-700 mr-1">{currentTenant.name}</span>
            <ChevronDownIcon
              className={`w-4 h-4 text-gray-500 transition-transform ${open ? 'rotate-180' : ''}`}
            />
          </PopoverButton>

          <PopoverPanel
            transition
            className="absolute left-0 z-10 mt-2 w-64 transition data-closed:translate-y-1 data-closed:opacity-0 data-enter:duration-200 data-enter:ease-out data-leave:duration-150 data-leave:ease-in"
          >
            <div className="overflow-hidden rounded-lg shadow-lg ring-1 ring-gray-200 ring-opacity-50 bg-white">
              <div className="p-2">
                <div className="px-3 py-2 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                  Switch Workspace
                </div>
                {availableTenants.map((tenant) => (
                  <button
                    key={tenant.tenantId}
                    onClick={() => {
                      selectTenant(tenant);
                      close();
                    }}
                    className={`w-full group flex items-center gap-x-3 rounded-lg p-3 transition-colors ${
                      currentTenant.tenantId === tenant.tenantId
                        ? 'bg-blue-50 text-blue-700'
                        : 'text-gray-700 hover:bg-gray-50'
                    }`}
                  >
                    <div className={`flex-none w-8 h-8 rounded-lg flex items-center justify-center ${
                      tenant.isPersonal
                        ? 'bg-gradient-to-br from-purple-400 to-purple-600'
                        : 'bg-gradient-to-br from-blue-400 to-blue-600'
                    }`}>
                      {tenant.isPersonal ? (
                        <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                        </svg>
                      ) : (
                        <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                        </svg>
                      )}
                    </div>
                    <div className="flex-auto text-left">
                      <div className="font-semibold text-sm">{tenant.name}</div>
                      <div className="text-xs text-gray-500">{tenant.role}</div>
                    </div>
                    {currentTenant.tenantId === tenant.tenantId && (
                      <svg className="w-4 h-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    )}
                  </button>
                ))}
              </div>
            </div>
          </PopoverPanel>
        </>
      )}
    </Popover>
  );
};

export default TenantSwitcher;
