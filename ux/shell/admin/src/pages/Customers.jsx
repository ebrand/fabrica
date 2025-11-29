import { useState, lazy, Suspense } from 'react';
import Layout from '../components/Layout';
import TenantFilterDropdown from '../components/TenantFilterDropdown';

// Lazy load the Customer MFE component
const CustomerManagement = lazy(() => import('customerMfe/CustomerManagement'));

function Customers() {
  const [filterTenantId, setFilterTenantId] = useState(null);

  const handleTenantFilterChange = (tenantId) => {
    setFilterTenantId(tenantId);
  };

  return (
    <Layout>
      <div className="space-y-4">
        {/* Header with tenant filter */}
        <div className="flex flex-col gap-4">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Customers</h1>
              <p className="mt-1 text-sm text-gray-500">
                Manage your customer records
              </p>
            </div>
          </div>
          <TenantFilterDropdown
            selectedTenantId={filterTenantId}
            onTenantChange={handleTenantFilterChange}
          />
        </div>

        {/* Embedded Customer MFE via Module Federation */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <Suspense fallback={
            <div className="flex items-center justify-center p-12">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
              <p className="ml-4 text-gray-600">Loading customer management...</p>
            </div>
          }>
            <CustomerManagement filterTenantId={filterTenantId} />
          </Suspense>
        </div>
      </div>
    </Layout>
  );
}

export default Customers;
