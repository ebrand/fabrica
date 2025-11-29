import { useState, useEffect } from 'react';
import axios from 'axios';
import configService from '../services/config';

function RecentUsers({ limit = 5, onViewAll }) {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      setLoading(true);
      const url = await configService.getBffAdminUrl();
      const response = await axios.get(`${url}/api/users`, { withCredentials: true });
      // Sort by lastLoginAt descending and take the limit
      const sorted = response.data
        .sort((a, b) => {
          if (!a.lastLoginAt) return 1;
          if (!b.lastLoginAt) return -1;
          return new Date(b.lastLoginAt) - new Date(a.lastLoginAt);
        })
        .slice(0, limit);
      setUsers(sorted);
    } catch (err) {
      setError('Failed to load users');
      console.error('Error fetching users:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatTimeAgo = (dateString) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)} min ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)} hours ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)} days ago`;
    return date.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="bg-white rounded-xl shadow-sm border border-gray-200">
        <div className="px-6 py-5 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">Recent Users</h2>
        </div>
        <div className="flex items-center justify-center p-8">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-xl shadow-sm border border-gray-200">
        <div className="px-6 py-5 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">Recent Users</h2>
        </div>
        <div className="p-6">
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded text-sm">
            {error}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200">
      <div className="px-6 py-5 border-b border-gray-200 flex justify-between items-center">
        <h2 className="text-lg font-semibold text-gray-900">Recent Users</h2>
        {onViewAll && (
          <button
            onClick={onViewAll}
            className="text-sm text-blue-600 hover:text-blue-800 font-medium"
          >
            View All
          </button>
        )}
      </div>
      {/* Column Headers */}
      <div className="px-6 py-3 bg-gray-50 border-b border-gray-200">
        <div className="grid grid-cols-12 gap-4 items-center">
          <div className="col-span-5 text-xs font-medium text-gray-500 uppercase tracking-wider">User</div>
          <div className="col-span-2 text-xs font-medium text-gray-500 uppercase tracking-wider text-center">Role</div>
          <div className="col-span-2 text-xs font-medium text-gray-500 uppercase tracking-wider text-center">Status</div>
          <div className="col-span-3 text-xs font-medium text-gray-500 uppercase tracking-wider text-right">Last Login</div>
        </div>
      </div>

      <div className="divide-y divide-gray-200">
        {users.length === 0 ? (
          <div className="px-6 py-8 text-center text-sm text-gray-500">
            No users found
          </div>
        ) : (
          users.map((user) => (
            <div key={user.userId} className="px-6 py-3 hover:bg-gray-50 transition-colors">
              <div className="grid grid-cols-12 gap-4 items-center">
                {/* User info - 5 cols */}
                <div className="col-span-5 flex items-center gap-3 min-w-0">
                  <div className="w-10 h-10 bg-gradient-to-br from-blue-400 to-blue-600 rounded-full flex items-center justify-center flex-shrink-0">
                    <span className="text-white font-semibold text-sm">
                      {(user.firstName?.[0] || user.email?.[0] || 'U').toUpperCase()}
                    </span>
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-gray-900 truncate">
                      {user.displayName || `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'Unknown'}
                    </p>
                    <p className="text-sm text-gray-500 truncate">{user.email}</p>
                  </div>
                </div>
                {/* Role - 2 cols */}
                <div className="col-span-2 text-center">
                  {user.isSystemAdmin && (
                    <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-purple-100 text-purple-800">
                      Admin
                    </span>
                  )}
                </div>
                {/* Status - 2 cols */}
                <div className="col-span-2 text-center">
                  <span className={`px-2 py-0.5 text-xs font-medium rounded-full ${
                    user.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'
                  }`}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                {/* Last login - 3 cols */}
                <div className="col-span-3 text-right">
                  <span className="text-xs text-gray-400 whitespace-nowrap">
                    {formatTimeAgo(user.lastLoginAt)}
                  </span>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

export default RecentUsers;
