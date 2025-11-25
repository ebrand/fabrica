import { lazy, Suspense } from 'react';
import Layout from '../components/Layout';

// Dynamically import the API Documentation component from the Admin MFE
const ApiDocumentation = lazy(() => import('adminMfe/ApiDocumentation'));

const ApiDocs = () => {
  return (
    <Layout>
      {/* Page Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900">API Documentation</h1>
        <p className="mt-2 text-gray-600">Browse all domain service APIs</p>
      </div>

      {/* Embedded Admin MFE via Module Federation */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <Suspense fallback={
          <div className="flex items-center justify-center p-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            <p className="ml-4 text-gray-600">Loading API documentation...</p>
          </div>
        }>
          <ApiDocumentation />
        </Suspense>
      </div>
    </Layout>
  );
};

export default ApiDocs;
