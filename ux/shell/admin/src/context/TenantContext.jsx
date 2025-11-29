import { createContext, useContext, useState, useEffect, useCallback, useRef } from 'react';
import axios from 'axios';
import configService from '../services/config';
import { useAuth } from './AuthContext';

const TenantContext = createContext(null);

export const useTenant = () => {
  const context = useContext(TenantContext);
  if (!context) {
    throw new Error('useTenant must be used within TenantProvider');
  }
  return context;
};

export const TenantProvider = ({ children }) => {
  const { tenants: authTenants, isAuthenticated } = useAuth();
  const [currentTenant, setCurrentTenant] = useState(null);
  const [availableTenants, setAvailableTenants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showSelector, setShowSelector] = useState(false);

  // Track if we've checked for saved tenant from BFF (use state so it triggers re-render)
  const [checkedSavedTenant, setCheckedSavedTenant] = useState(false);
  // Track the saved tenant ID from BFF (before we have tenant details)
  const savedTenantIdRef = useRef(null);

  // Reset when user logs out
  useEffect(() => {
    if (!isAuthenticated) {
      setCurrentTenant(null);
      setAvailableTenants([]);
      setShowSelector(false);
      setCheckedSavedTenant(false);
      savedTenantIdRef.current = null;
      setLoading(true);
    }
  }, [isAuthenticated]);

  // Fetch current tenant from BFF FIRST (in case of page refresh)
  // This must run before we decide whether to show the selector
  useEffect(() => {
    const fetchCurrentTenant = async () => {
      try {
        const bffUrl = await configService.getBffAdminUrl();
        const response = await axios.get(`${bffUrl}/api/tenants/current`, {
          withCredentials: true
        });

        if (response.data.selected && response.data.tenantId) {
          console.log('TenantContext: Found saved tenant selection from BFF:', response.data.tenantId);
          savedTenantIdRef.current = response.data.tenantId;
          // Store just the ID for now - will be enriched when authTenants arrive
          setCurrentTenant({ tenantId: response.data.tenantId });
        } else {
          console.log('TenantContext: No saved tenant selection');
        }
      } catch (error) {
        console.error('Error fetching current tenant:', error);
      } finally {
        setCheckedSavedTenant(true);
      }
    };

    if (isAuthenticated && !checkedSavedTenant) {
      fetchCurrentTenant();
    }
  }, [isAuthenticated, checkedSavedTenant]);

  // Sync available tenants from AuthContext and decide whether to show selector
  useEffect(() => {
    if (authTenants && authTenants.length > 0 && checkedSavedTenant) {
      console.log('TenantContext: Processing tenants from auth:', authTenants.length);
      setAvailableTenants(authTenants);

      // If we have a saved tenant ID, find and set the full tenant object
      if (savedTenantIdRef.current) {
        const savedTenant = authTenants.find(t => t.tenantId === savedTenantIdRef.current);
        if (savedTenant) {
          console.log('TenantContext: Restored saved tenant:', savedTenant.name);
          setCurrentTenant(savedTenant);
          setLoading(false);
          return;
        }
        // Saved tenant not in user's list anymore (maybe removed access)
        console.log('TenantContext: Saved tenant not found in available tenants');
        savedTenantIdRef.current = null;
      }

      // No saved tenant - decide what to do
      if (authTenants.length === 1) {
        // Auto-select the only tenant
        setCurrentTenant(authTenants[0]);
        console.log('TenantContext: Auto-selected single tenant:', authTenants[0].name);
        // Also save it to BFF so it persists
        (async () => {
          try {
            const bffUrl = await configService.getBffAdminUrl();
            await axios.post(`${bffUrl}/api/tenants/select`,
              { tenantId: authTenants[0].tenantId },
              { withCredentials: true }
            );
          } catch (e) {
            console.error('Error auto-saving tenant selection:', e);
          }
        })();
      } else if (authTenants.length > 1 && !currentTenant?.tenantId) {
        // Multiple tenants and no selection - show selector
        console.log('TenantContext: Multiple tenants, showing selector');
        setShowSelector(true);
      }

      setLoading(false);
    }
  }, [authTenants, checkedSavedTenant]);

  // Update current tenant details when available tenants change
  useEffect(() => {
    if (currentTenant?.tenantId && availableTenants.length > 0) {
      const tenant = availableTenants.find(t => t.tenantId === currentTenant.tenantId);
      if (tenant && tenant.name && tenant.name !== currentTenant.name) {
        setCurrentTenant(tenant);
      }
    }
  }, [availableTenants, currentTenant?.tenantId]);

  // Select a tenant
  const selectTenant = useCallback(async (tenant) => {
    try {
      const bffUrl = await configService.getBffAdminUrl();
      await axios.post(`${bffUrl}/api/tenants/select`,
        { tenantId: tenant.tenantId },
        { withCredentials: true }
      );

      setCurrentTenant(tenant);
      setShowSelector(false);

      console.log('Tenant selected:', tenant.name);
      return true;
    } catch (error) {
      console.error('Error selecting tenant:', error);
      return false;
    }
  }, []);

  // Clear tenant selection
  const clearTenant = useCallback(async () => {
    try {
      const bffUrl = await configService.getBffAdminUrl();
      await axios.post(`${bffUrl}/api/tenants/clear`, {}, { withCredentials: true });

      setCurrentTenant(null);

      // Show selector if multiple tenants available
      if (availableTenants.length > 1) {
        setShowSelector(true);
      }

      return true;
    } catch (error) {
      console.error('Error clearing tenant:', error);
      return false;
    }
  }, [availableTenants.length]);

  // Open tenant selector modal
  const openSelector = useCallback(() => {
    if (availableTenants.length > 1) {
      setShowSelector(true);
    }
  }, [availableTenants.length]);

  // Close tenant selector modal
  const closeSelector = useCallback(() => {
    setShowSelector(false);
  }, []);

  // Check if "All Tenants" mode is active (tenantId is empty GUID string)
  const isAllTenantsMode = currentTenant?.tenantId === '00000000-0000-0000-0000-000000000000' ||
                           currentTenant?.slug === 'all';

  // Check if user is the owner of the current tenant
  const isTenantOwner = currentTenant?.role === 'owner';

  const value = {
    currentTenant,
    availableTenants,
    loading,
    showSelector,
    selectTenant,
    clearTenant,
    openSelector,
    closeSelector,
    hasTenant: !!currentTenant,
    hasMultipleTenants: availableTenants.length > 1,
    isAllTenantsMode, // True when System Admin has selected "All Tenants"
    isTenantOwner, // True when user is the owner of the current tenant
  };

  return (
    <TenantContext.Provider value={value}>
      {children}
    </TenantContext.Provider>
  );
};
