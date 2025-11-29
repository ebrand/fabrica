import { useState, lazy, Suspense } from 'react';
import Layout from '../components/Layout';
import { useAuth } from '../context/AuthContext';
import { useTenant } from '../context/TenantContext';
import TenantFilterDropdown from '../components/TenantFilterDropdown';

// Dynamically import the User Management component from the Admin MFE
const UserManagement = lazy(() => import('adminMfe/UserManagement'));

const Users = () => {
  const { isSystemAdmin } = useAuth();
  const { isTenantOwner } = useTenant();
  const [filterTenantId, setFilterTenantId] = useState(null);

  // Can manage users if System Admin OR tenant owner
  const canManageUsers = isSystemAdmin || isTenantOwner;

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
              <h1 className="text-2xl font-bold text-gray-900">Users</h1>
              <p className="mt-1 text-sm text-gray-500">
                Manage users and permissions
              </p>
            </div>
          </div>
          <TenantFilterDropdown
            selectedTenantId={filterTenantId}
            onTenantChange={handleTenantFilterChange}
          />
        </div>

        {/* Embedded Admin MFE via Module Federation */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <Suspense fallback={
            <div className="flex items-center justify-center p-12">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
              <p className="ml-4 text-gray-600">Loading user management...</p>
            </div>
          }>
            <UserManagement
              isCurrentUserSystemAdmin={isSystemAdmin}
              canManageUsers={canManageUsers}
              filterTenantId={filterTenantId}
            />
          </Suspense>
        </div>
      </div>
    </Layout>
  );
};

export default Users;
