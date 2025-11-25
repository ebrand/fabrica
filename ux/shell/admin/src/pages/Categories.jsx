import { lazy, Suspense } from 'react';
import Layout from '../components/Layout';

// Lazy load the Category MFE component
const CategoryManagement = lazy(() => import('productMfe/CategoryManagement'));

function Categories() {
  return (
    <Layout>
      {/* Embedded Category MFE via Module Federation */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <Suspense fallback={
          <div className="flex items-center justify-center p-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            <p className="ml-4 text-gray-600">Loading category management...</p>
          </div>
        }>
          <CategoryManagement />
        </Suspense>
      </div>
    </Layout>
  );
}

export default Categories;
