import { lazy, Suspense, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import ErrorBoundary from '../components/ErrorBoundary';
import { useAuth } from '../context/AuthContext';
import { useTenant } from '../context/TenantContext';

// Lazy load MFE components
const RecentUsers = lazy(() => import('adminMfe/RecentUsers'));

// BFF URLs
const BFF_ADMIN_URL = import.meta.env.VITE_BFF_URL || 'http://localhost:3200';
const BFF_PRODUCT_URL = import.meta.env.VITE_BFF_PRODUCT_URL || 'http://localhost:3220';

const Dashboard = () => {
  const { user } = useAuth();
  const { currentTenant } = useTenant();
  const navigate = useNavigate();

  const [stats, setStats] = useState({
    products: { count: null, loading: true, error: null },
    categories: { count: null, loading: true, error: null },
    users: { count: null, loading: true, error: null }
  });

  useEffect(() => {
    if (!currentTenant?.tenantId) return;

    const fetchStats = async () => {
      const headers = {
        'Content-Type': 'application/json',
        'X-Tenant-ID': currentTenant.tenantId
      };

      // Fetch products count
      fetch(`${BFF_PRODUCT_URL}/api/products`, {
        credentials: 'include',
        headers
      })
        .then(res => res.ok ? res.json() : Promise.reject('Failed'))
        .then(data => {
          setStats(prev => ({
            ...prev,
            products: { count: Array.isArray(data) ? data.length : 0, loading: false, error: null }
          }));
        })
        .catch(() => {
          setStats(prev => ({
            ...prev,
            products: { count: null, loading: false, error: 'Failed to load' }
          }));
        });

      // Fetch categories count
      fetch(`${BFF_PRODUCT_URL}/api/categories`, {
        credentials: 'include',
        headers
      })
        .then(res => res.ok ? res.json() : Promise.reject('Failed'))
        .then(data => {
          setStats(prev => ({
            ...prev,
            categories: { count: Array.isArray(data) ? data.length : 0, loading: false, error: null }
          }));
        })
        .catch(() => {
          setStats(prev => ({
            ...prev,
            categories: { count: null, loading: false, error: 'Failed to load' }
          }));
        });

      // Fetch users count
      fetch(`${BFF_ADMIN_URL}/api/users`, {
        credentials: 'include',
        headers
      })
        .then(res => res.ok ? res.json() : Promise.reject('Failed'))
        .then(data => {
          setStats(prev => ({
            ...prev,
            users: { count: Array.isArray(data) ? data.length : 0, loading: false, error: null }
          }));
        })
        .catch(() => {
          setStats(prev => ({
            ...prev,
            users: { count: null, loading: false, error: 'Failed to load' }
          }));
        });
    };

    fetchStats();
  }, [currentTenant?.tenantId]);

  const statCards = [
    {
      name: 'Products',
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
        </svg>
      ),
      ...stats.products,
      href: '/products',
      color: 'blue'
    },
    {
      name: 'Categories',
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
        </svg>
      ),
      ...stats.categories,
      href: '/categories',
      color: 'purple'
    },
    {
      name: 'Users',
      icon: (
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
        </svg>
      ),
      ...stats.users,
      href: '/users',
      color: 'green'
    }
  ];

  const colorClasses = {
    blue: {
      bg: 'bg-blue-50',
      icon: 'text-blue-600',
      hover: 'hover:bg-blue-100'
    },
    purple: {
      bg: 'bg-purple-50',
      icon: 'text-purple-600',
      hover: 'hover:bg-purple-100'
    },
    green: {
      bg: 'bg-green-50',
      icon: 'text-green-600',
      hover: 'hover:bg-green-100'
    }
  };

  return (
    <Layout>

      {/* Page Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
        <p className="mt-2 text-gray-600">Welcome back, {user?.name?.first_name || 'Admin'}</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-3 mb-8">
        {statCards.map((stat) => {
          const colors = colorClasses[stat.color];
          return (
            <button
              key={stat.name}
              onClick={() => navigate(stat.href)}
              className={`bg-white rounded-xl shadow-sm p-6 border border-gray-200 hover:shadow-md transition-all text-left cursor-pointer ${colors.hover}`}
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">{stat.name}</p>
                  <div className="mt-2">
                    {stat.loading ? (
                      <div className="h-9 w-16 bg-gray-200 animate-pulse rounded"></div>
                    ) : stat.error ? (
                      <p className="text-sm text-red-500">{stat.error}</p>
                    ) : (
                      <p className="text-3xl font-semibold text-gray-900">{stat.count}</p>
                    )}
                  </div>
                </div>
                <div className={`p-3 rounded-lg ${colors.bg}`}>
                  <span className={colors.icon}>{stat.icon}</span>
                </div>
              </div>
            </button>
          );
        })}
      </div>

      {/* Content Cards Row - using Content factory */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">

      </div>

      {/* Recent Users - loaded from Admin MFE */}
      <ErrorBoundary fallback={
        <div className="bg-white rounded-xl shadow-sm border border-gray-200">
          <div className="px-6 py-5 border-b border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900">Users</h2>
          </div>
          <div className="flex items-center justify-center p-8 text-gray-400 text-sm">
            Unable to load recent users
          </div>
        </div>
      }>
        <Suspense
          fallback={
            <div className="bg-white rounded-xl shadow-sm border border-gray-200">
              <div className="px-6 py-5 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">Recent Users</h2>
              </div>
              <div className="flex items-center justify-center p-8">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              </div>
            </div>
          }
        >
          <RecentUsers limit={5} onViewAll={() => navigate('/users')} />
        </Suspense>
      </ErrorBoundary>

      {/* Quick Actions */}

    </Layout>
  );
};

export default Dashboard;
