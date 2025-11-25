import { lazy, Suspense } from 'react';
import Layout from '../components/Layout';

// Lazy load the Product MFE component
const ProductManagement = lazy(() => import('productMfe/ProductManagement'));

function Products() {
  return (
    <Layout>
      {/* Embedded Product MFE via Module Federation */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <Suspense fallback={
          <div className="flex items-center justify-center p-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            <p className="ml-4 text-gray-600">Loading product management...</p>
          </div>
        }>
          <ProductManagement />
        </Suspense>
      </div>
    </Layout>
  );
}

export default Products;
