import { lazy, Suspense } from 'react';
import Layout from '../components/Layout';

// Dynamically import the User Management component from the Admin MFE
const UserManagement = lazy(() => import('adminMfe/UserManagement'));

const Users = () => {
  return (
    <Layout>
      

      {/* Embedded Admin MFE via Module Federation */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <Suspense fallback={
          <div className="flex items-center justify-center p-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            <p className="ml-4 text-gray-600">Loading user management...</p>
          </div>
        }>
          <UserManagement />
        </Suspense>
      </div>
    </Layout>
  );
};

export default Users;
