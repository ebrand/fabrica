import { lazy, Suspense } from 'react';
import { useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { useAuth } from '../context/AuthContext';

// Lazy load the RecentUsers component from the Admin MFE
const RecentUsers = lazy(() => import('adminMfe/RecentUsers'));

const Dashboard = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  const stats = [
    { name: 'Total Users', value: '2,651', change: '+12%', changeType: 'positive' },
    { name: 'Active Roles', value: '24', change: '+2', changeType: 'positive' },
    { name: 'Permissions', value: '156', change: '+8', changeType: 'positive' },
    { name: 'Active Sessions', value: '423', change: '-3%', changeType: 'negative' },
  ];

  return (
    <Layout>
      {/* Page Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
        <p className="mt-2 text-gray-600">Welcome back, {user?.name?.first_name || 'Admin'}</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4 mb-8">
        {stats.map((stat) => (
          <div key={stat.name} className="bg-white rounded-xl shadow-sm p-6 border border-gray-200 hover:shadow-md transition-shadow">
            <p className="text-sm font-medium text-gray-600">{stat.name}</p>
            <div className="mt-2 flex items-baseline justify-between">
              <p className="text-3xl font-semibold text-gray-900">{stat.value}</p>
              <span
                className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                  stat.changeType === 'positive'
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'
                }`}
              >
                {stat.change}
              </span>
            </div>
          </div>
        ))}
      </div>

      {/* Recent Users - loaded from Admin MFE */}
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

      {/* Quick Actions */}
      <div className="mt-8 grid grid-cols-1 gap-6 sm:grid-cols-3">
        <button className="bg-blue-600 text-white rounded-xl shadow-sm p-6 hover:bg-blue-700 transition-colors text-left">
          <svg className="w-8 h-8 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
          </svg>
          <h3 className="text-lg font-semibold">Add New User</h3>
          <p className="mt-1 text-sm text-blue-100">Create a new user account</p>
        </button>

        <button className="bg-white border-2 border-gray-300 rounded-xl shadow-sm p-6 hover:border-gray-400 hover:shadow-md transition-all text-left">
          <svg className="w-8 h-8 mb-3 text-gray-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
          </svg>
          <h3 className="text-lg font-semibold text-gray-900">Create Role</h3>
          <p className="mt-1 text-sm text-gray-600">Define a new user role</p>
        </button>

        <button className="bg-white border-2 border-gray-300 rounded-xl shadow-sm p-6 hover:border-gray-400 hover:shadow-md transition-all text-left">
          <svg className="w-8 h-8 mb-3 text-gray-700" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <h3 className="text-lg font-semibold text-gray-900">View Reports</h3>
          <p className="mt-1 text-sm text-gray-600">Access system reports</p>
        </button>
      </div>
    </Layout>
  );
};

export default Dashboard;
