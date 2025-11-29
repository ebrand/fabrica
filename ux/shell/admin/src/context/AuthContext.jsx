import { createContext, useContext, useState, useEffect, useRef } from 'react';
import { useStytchUser, useStytch } from '@stytch/react';
import axios from 'axios';
import configService from '../services/config';

const AuthContext = createContext(null);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const { user, isInitialized } = useStytchUser();
  const stytch = useStytch();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [loading, setLoading] = useState(true);
  const [syncedUser, setSyncedUser] = useState(null);
  const [permissions, setPermissions] = useState([]);
  const [tenants, setTenants] = useState([]);
  const [requiresOnboarding, setRequiresOnboarding] = useState(false);

  // Track the last synced user ID to prevent duplicate syncs
  const lastSyncedUserId = useRef(null);

  console.log('🔵 AuthProvider render - isInitialized:', isInitialized, 'user:', !!user, 'loading:', loading, 'isAuthenticated:', isAuthenticated);

  // Sync user with backend when Stytch user changes
  useEffect(() => {
    // Wait for Stytch to initialize before making auth decisions
    if (!isInitialized) {
      console.log('🟡 Stytch not initialized yet, waiting...');
      return;
    }

    const currentUserId = user?.user_id || null;

    // Skip sync if we've already synced this user
    if (currentUserId && currentUserId === lastSyncedUserId.current) {
      console.log('🟡 User already synced, skipping duplicate sync');
      return;
    }

    const syncUserWithBackend = async () => {
      console.log('🟢 syncUserWithBackend called, user:', !!user);
      if (user) {
        try {
          // Extract user info from Stytch user object
          const email = user.emails?.[0]?.email || '';
          const stytchUserId = user.user_id || '';
          const firstName = user.name?.first_name || '';
          const lastName = user.name?.last_name || '';

          // Get BFF URL from config
          const bffUrl = await configService.getBffAdminUrl();

          // Call the sync endpoint
          const response = await axios.post(`${bffUrl}/api/auth/sync`, {
            email,
            stytchUserId,
            firstName,
            lastName,
            displayName: `${firstName} ${lastName}`.trim() || email
          }, { withCredentials: true });

          setSyncedUser(response.data);
          setPermissions(response.data.permissions || []);
          setTenants(response.data.tenants || []);
          setRequiresOnboarding(response.data.requiresOnboarding || false);
          setIsAuthenticated(true);
          lastSyncedUserId.current = stytchUserId;

          console.log('User synced successfully:', response.data);
          if (response.data.requiresOnboarding) {
            console.log('🚀 User requires onboarding');
          }
        } catch (error) {
          console.error('Error syncing user with backend:', error);
          // Still set authenticated even if sync fails
          setIsAuthenticated(true);
        }
      } else {
        setIsAuthenticated(false);
        setSyncedUser(null);
        setPermissions([]);
        setTenants([]);
        setRequiresOnboarding(false);
        lastSyncedUserId.current = null;
      }
      setLoading(false);
    };

    syncUserWithBackend();
  }, [user?.user_id, isInitialized]); // Use user_id instead of user object to prevent unnecessary re-syncs

  const login = async (token) => {
    try {
      await stytch.session.authenticate({ session_token: token });
      setIsAuthenticated(true);
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  };

  const logout = async () => {
    try {
      await stytch.session.revoke();
      setIsAuthenticated(false);
      setTenants([]);
      lastSyncedUserId.current = null;
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  // Refresh user data from backend (call after profile updates)
  const refreshUser = async () => {
    if (!syncedUser?.userId) {
      console.warn('Cannot refresh user: no userId available');
      return;
    }

    try {
      const bffUrl = await configService.getBffAdminUrl();
      const response = await axios.get(`${bffUrl}/api/users/${syncedUser.userId}`);
      setSyncedUser(prev => ({
        ...prev,
        ...response.data,
        // Preserve permissions and tenants from the original sync
        permissions: prev?.permissions || [],
        tenants: prev?.tenants || []
      }));
      console.log('User refreshed successfully:', response.data);
    } catch (error) {
      console.error('Error refreshing user:', error);
    }
  };

  // Call after onboarding completes to update tenants and clear onboarding flag
  const completeOnboarding = async (newTenantId) => {
    try {
      const bffUrl = await configService.getBffAdminUrl();
      // Re-fetch user data to get updated tenants
      const email = user?.emails?.[0]?.email || '';
      const stytchUserId = user?.user_id || '';
      const firstName = user?.name?.first_name || '';
      const lastName = user?.name?.last_name || '';

      const response = await axios.post(`${bffUrl}/api/auth/sync`, {
        email,
        stytchUserId,
        firstName,
        lastName,
        displayName: `${firstName} ${lastName}`.trim() || email
      }, { withCredentials: true });

      setSyncedUser(response.data);
      setPermissions(response.data.permissions || []);
      setTenants(response.data.tenants || []);
      setRequiresOnboarding(false);
      console.log('Onboarding completed, user data refreshed');
    } catch (error) {
      console.error('Error completing onboarding:', error);
      // Still clear onboarding flag to allow user to continue
      setRequiresOnboarding(false);
    }
  };

  const value = {
    user, // Stytch user object
    syncedUser, // Synced user from our backend with roles and permissions
    permissions, // User's permissions
    tenants, // User's available tenants
    isAuthenticated,
    loading,
    login,
    logout,
    refreshUser, // Call this after profile updates to refresh the header
    completeOnboarding, // Call after onboarding flow completes
    requiresOnboarding, // Flag indicating user needs to complete onboarding
    // Helper function to check if user has a specific permission
    hasPermission: (permission) => permissions.includes(permission),
    // System admin flag for cross-tenant access
    isSystemAdmin: syncedUser?.isSystemAdmin || false,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
