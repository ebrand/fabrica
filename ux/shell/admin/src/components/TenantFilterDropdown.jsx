import { useState, useEffect } from 'react';
import { useTenant } from '../context/TenantContext';
import { useAuth } from '../context/AuthContext';
import configService from '../services/config';

/**
 * Tenant filter dropdown for System Admins in "All Tenants" mode.
 * Allows filtering data by a specific tenant.
 *
 * @param {function} onTenantChange - Callback when tenant selection changes. Receives tenantId (or null for all).
 * @param {string} selectedTenantId - Currently selected tenant ID (controlled component)
 * @param {string} className - Additional CSS classes
 */
const TenantFilterDropdown = ({ onTenantChange, selectedTenantId, className = '' }) => {
  const { isAllTenantsMode } = useTenant();
  const { syncedUser } = useAuth();
  const [allTenants, setAllTenants] = useState([]);
  const [loading, setLoading] = useState(true);

  const isSystemAdmin = syncedUser?.isSystemAdmin ?? false;

  // Fetch all tenants when in All Tenants mode
  useEffect(() => {
    const fetchAllTenants = async () => {
      if (!isAllTenantsMode || !isSystemAdmin) {
        setAllTenants([]);
        setLoading(false);
        return;
      }

      try {
        const bffUrl = await configService.getBffAdminUrl();
        const response = await fetch(`${bffUrl}/api/tenants`, {
          credentials: 'include'
        });

        if (response.ok) {
          const tenants = await response.json();
          setAllTenants(tenants);
        }
      } catch (error) {
        console.error('Error fetching tenants:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchAllTenants();
  }, [isAllTenantsMode, isSystemAdmin]);

  // Don't render if not in All Tenants mode or not a system admin
  if (!isAllTenantsMode || !isSystemAdmin) {
    return null;
  }

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <label htmlFor="tenant-filter" className="text-sm font-medium text-gray-700">
        Filter by Tenant:
      </label>
      <select
        id="tenant-filter"
        value={selectedTenantId || ''}
        onChange={(e) => onTenantChange(e.target.value || null)}
        disabled={loading}
        className="block w-64 rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 text-sm py-1.5 px-3 border bg-white"
      >
        <option value="">All Tenants</option>
        {allTenants.map((tenant) => (
          <option key={tenant.tenantId} value={tenant.tenantId}>
            {tenant.name}
          </option>
        ))}
      </select>
      {loading && (
        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
      )}
    </div>
  );
};

export default TenantFilterDropdown;
