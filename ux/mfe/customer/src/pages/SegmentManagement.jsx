import { useState, useEffect, Suspense, lazy } from 'react';
import axios from 'axios';
import configService from '../services/config';

// Configure axios to send credentials (cookies) with all requests
axios.defaults.withCredentials = true;

// Import common components from commonMfe
const Toast = lazy(() => import('commonMfe/Toast'));

function SegmentManagement({ filterTenantId = null }) {
  const [segments, setSegments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [bffUrl, setBffUrl] = useState(null);

  // Toast notification state
  const [toast, setToast] = useState({ show: false, title: '', message: '', type: 'success' });

  const showToast = (title, message, type = 'success') => {
    setToast({ show: true, title, message, type });
  };

  useEffect(() => {
    initializeAndFetchSegments();
  }, [filterTenantId]);

  const initializeAndFetchSegments = async () => {
    try {
      const url = await configService.getBffCustomerUrl();
      setBffUrl(url);
      await fetchSegments(url);
    } catch (err) {
      setError('Failed to initialize configuration');
      setLoading(false);
    }
  };

  const fetchSegments = async (url) => {
    try {
      setLoading(true);
      const tenantParam = filterTenantId ? `?tenantId=${filterTenantId}` : '';
      const response = await axios.get(`${url}/api/customers/segments${tenantParam}`);
      setSegments(response.data);
    } catch (err) {
      setError('Failed to load segments');
      console.error('Error fetching segments:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div>
      {/* Toast Notification */}
      {toast.show && (
        <Suspense fallback={null}>
          <Toast
            title={toast.title}
            message={toast.message}
            type={toast.type}
            show={toast.show}
            onClose={() => setToast(prev => ({ ...prev, show: false }))}
            autoHide={true}
            autoHideDelay={4000}
          />
        </Suspense>
      )}

      <div className="mb-4 flex justify-between items-center px-6 py-4">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Customer Segments</h2>
          <p className="text-sm text-gray-500 mt-1">View and manage customer segmentation</p>
        </div>
      </div>

      <div className="overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Segment Name
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Type
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Members
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Created
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {segments.length === 0 ? (
              <tr>
                <td colSpan="5" className="px-6 py-4 text-center text-sm text-gray-500">
                  No segments found.
                </td>
              </tr>
            ) : (
              segments.map((segment) => (
                <tr key={segment.segmentId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">{segment.name}</div>
                    {segment.description && (
                      <div className="text-sm text-gray-500">{segment.description}</div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      segment.segmentType === 'Dynamic' ? 'bg-purple-100 text-purple-800' :
                      'bg-blue-100 text-blue-800'
                    }`}>
                      {segment.segmentType}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {segment.memberCount || 0}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      segment.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                    }`}>
                      {segment.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {new Date(segment.createdAt).toLocaleDateString()}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="px-6 py-4 bg-gray-50 border-t border-gray-200 text-sm text-gray-500">
        Total segments: {segments.length}
      </div>
    </div>
  );
}

export default SegmentManagement;
