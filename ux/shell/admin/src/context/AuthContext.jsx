import { createContext, useContext, useState, useEffect } from 'react';
import { useStytchUser, useStytch } from '@stytch/react';
import axios from 'axios';

const AuthContext = createContext(null);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const { user } = useStytchUser();
  const stytch = useStytch();
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [loading, setLoading] = useState(true);
  const [syncedUser, setSyncedUser] = useState(null);
  const [permissions, setPermissions] = useState([]);

  // Sync user with backend when Stytch user changes
  useEffect(() => {
    const syncUserWithBackend = async () => {
      if (user) {
        try {
          // Extract user info from Stytch user object
          const email = user.emails?.[0]?.email || '';
          const stytchUserId = user.user_id || '';
          const firstName = user.name?.first_name || '';
          const lastName = user.name?.last_name || '';

          // Call the sync endpoint
          const response = await axios.post('http://localhost:3600/api/auth/sync', {
            email,
            stytchUserId,
            firstName,
            lastName,
            displayName: `${firstName} ${lastName}`.trim() || email
          });

          setSyncedUser(response.data);
          setPermissions(response.data.permissions || []);
          setIsAuthenticated(true);

          console.log('User synced successfully:', response.data);
        } catch (error) {
          console.error('Error syncing user with backend:', error);
          // Still set authenticated even if sync fails
          setIsAuthenticated(true);
        }
      } else {
        setIsAuthenticated(false);
        setSyncedUser(null);
        setPermissions([]);
      }
      setLoading(false);
    };

    syncUserWithBackend();
  }, [user]);

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
      const response = await axios.get(`http://localhost:3200/api/users/${syncedUser.userId}`);
      setSyncedUser(prev => ({
        ...prev,
        ...response.data,
        // Preserve permissions from the original sync
        permissions: prev?.permissions || []
      }));
      console.log('User refreshed successfully:', response.data);
    } catch (error) {
      console.error('Error refreshing user:', error);
    }
  };

  const value = {
    user, // Stytch user object
    syncedUser, // Synced user from our backend with roles and permissions
    permissions, // User's permissions
    isAuthenticated,
    loading,
    login,
    logout,
    refreshUser, // Call this after profile updates to refresh the header
    // Helper function to check if user has a specific permission
    hasPermission: (permission) => permissions.includes(permission),
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
