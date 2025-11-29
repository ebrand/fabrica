import { useState, useEffect, Suspense, lazy } from 'react';
import axios from 'axios';
import configService from '../services/config';

// Configure axios to send credentials (cookies) with all requests
axios.defaults.withCredentials = true;

// Import common components from commonMfe
const Select = lazy(() => import('commonMfe/Select'));
const Toast = lazy(() => import('commonMfe/Toast'));
const ConfirmModal = lazy(() => import('commonMfe/ConfirmModal'));

// Status options for the product status dropdown
const statusOptions = [
  { id: 'draft', name: 'Draft' },
  { id: 'active', name: 'Active' },
  { id: 'archived', name: 'Archived' }
];

function ProductManagement({ filterTenantId = null }) {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [bffUrl, setBffUrl] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);
  const [formData, setFormData] = useState({
    sku: '',
    name: '',
    slug: '',
    description: '',
    shortDescription: '',
    basePrice: 0,
    status: 'draft'
  });

  // Toast notification state
  const [toast, setToast] = useState({ show: false, title: '', message: '', type: 'success' });

  // Confirm modal state
  const [confirmModal, setConfirmModal] = useState({ show: false, productId: null, productName: '' });

  const showToast = (title, message, type = 'success') => {
    setToast({ show: true, title, message, type });
  };

  useEffect(() => {
    initializeAndFetchProducts();
  }, [filterTenantId]);

  const initializeAndFetchProducts = async () => {
    try {
      const url = await configService.getBffProductUrl();
      setBffUrl(url);
      await fetchProducts(url);
    } catch (err) {
      setError('Failed to initialize configuration');
      setLoading(false);
    }
  };

  const fetchProducts = async (url) => {
    try {
      setLoading(true);
      const tenantParam = filterTenantId ? `?tenantId=${filterTenantId}` : '';
      const response = await axios.get(`${url}/api/products${tenantParam}`);
      setProducts(response.data);
    } catch (err) {
      setError('Failed to load products');
      console.error('Error fetching products:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (product = null) => {
    if (product) {
      setEditingProduct(product);
      setFormData({
        sku: product.sku,
        name: product.name,
        slug: product.slug,
        description: product.description || '',
        shortDescription: product.shortDescription || '',
        basePrice: product.basePrice,
        status: product.status
      });
    } else {
      setEditingProduct(null);
      setFormData({
        sku: '',
        name: '',
        slug: '',
        description: '',
        shortDescription: '',
        basePrice: 0,
        status: 'draft'
      });
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingProduct(null);
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: name === 'basePrice' ? parseFloat(value) || 0 : value
    }));

    // Auto-generate slug from name
    if (name === 'name' && !editingProduct) {
      setFormData(prev => ({
        ...prev,
        slug: value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '')
      }));
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingProduct) {
        // Include id and other required fields from original product
        const updatePayload = {
          ...formData,
          id: editingProduct.id,
          tenantId: editingProduct.tenantId,
          compareAtPrice: editingProduct.compareAtPrice,
          costPrice: editingProduct.costPrice,
          primaryImageUrl: editingProduct.primaryImageUrl,
          productType: editingProduct.productType,
          vendor: editingProduct.vendor,
          weight: editingProduct.weight,
          weightUnit: editingProduct.weightUnit || 'lb',
          requiresShipping: editingProduct.requiresShipping ?? true,
          isTaxable: editingProduct.isTaxable ?? true,
          trackInventory: editingProduct.trackInventory ?? true,
          seoMetaTitle: editingProduct.seoMetaTitle,
          seoMetaDescription: editingProduct.seoMetaDescription
        };
        await axios.put(`${bffUrl}/api/products/${editingProduct.id}`, updatePayload);
      } else {
        await axios.post(`${bffUrl}/api/products`, formData);
      }
      handleCloseModal();
      await fetchProducts(bffUrl);
    } catch (err) {
      console.error('Error saving product:', err);
      showToast('Error', 'Failed to save product: ' + (err.response?.data?.error || err.message), 'error');
    }
  };

  const handleDeleteClick = (product) => {
    setConfirmModal({ show: true, productId: product.id, productName: product.name });
  };

  const handleDeleteConfirm = async () => {
    const { productId } = confirmModal;
    setConfirmModal({ show: false, productId: null, productName: '' });

    try {
      await axios.delete(`${bffUrl}/api/products/${productId}`);
      await fetchProducts(bffUrl);
    } catch (err) {
      console.error('Error deleting product:', err);
      showToast('Error', 'Failed to delete product', 'error');
    }
  };

  const handleDeleteCancel = () => {
    setConfirmModal({ show: false, productId: null, productName: '' });
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

      {/* Delete Confirmation Modal */}
      <Suspense fallback={null}>
        <ConfirmModal
          show={confirmModal.show}
          title="Delete Product"
          message={`Are you sure you want to delete "${confirmModal.productName}"? This action cannot be undone.`}
          type="danger"
          confirmText="Delete"
          cancelText="Cancel"
          onConfirm={handleDeleteConfirm}
          onCancel={handleDeleteCancel}
        />
      </Suspense>

      <div className="mb-4 flex justify-between items-center px-6 py-4">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Product Management</h2>
          <p className="text-sm text-gray-500 mt-1">Manage product catalog and inventory</p>
        </div>
        <button
          onClick={() => handleOpenModal()}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          Add Product
        </button>
      </div>

      <div className="overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                SKU
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Name
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Price
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {products.length === 0 ? (
              <tr>
                <td colSpan="5" className="px-6 py-4 text-center text-sm text-gray-500">
                  No products found. Create your first product!
                </td>
              </tr>
            ) : (
              products.map((product) => (
                <tr key={product.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">{product.sku}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">{product.name}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    ${product.basePrice.toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      product.status === 'active' ? 'bg-green-100 text-green-800' :
                      product.status === 'draft' ? 'bg-yellow-100 text-yellow-800' :
                      'bg-gray-100 text-gray-800'
                    }`}>
                      {product.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleOpenModal(product)}
                      className="text-blue-600 hover:text-blue-900 mr-4"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDeleteClick(product)}
                      className="text-red-600 hover:text-red-900"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="px-6 py-4 bg-gray-50 border-t border-gray-200 text-sm text-gray-500">
        Total products: {products.length}
      </div>

      {/* Modal for Create/Edit Product */}
      {showModal && (
        <div
          className="fixed inset-0 bg-black/50 overflow-y-auto z-50 flex items-center justify-center transition-all duration-300"
          style={{ right: 'var(--esb-panel-offset, 0px)' }}
        >
          <div className="relative bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-medium text-gray-900">
                {editingProduct ? 'Edit Product' : 'Add New Product'}
              </h3>
            </div>

            <form onSubmit={handleSubmit} className="px-6 py-4 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="sku" className="block text-sm font-medium text-gray-700">
                    SKU
                  </label>
                  <input
                    type="text"
                    name="sku"
                    id="sku"
                    value={formData.sku}
                    onChange={handleChange}
                    required
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                  />
                </div>
                <div>
                  <Suspense fallback={<div className="h-10 bg-gray-100 rounded animate-pulse" />}>
                    <Select
                      label="Status"
                      options={statusOptions}
                      value={statusOptions.find(s => s.id === formData.status)}
                      onChange={(selected) => setFormData(prev => ({ ...prev, status: selected.id }))}
                      displayKey="name"
                      valueKey="id"
                    />
                  </Suspense>
                </div>
              </div>

              <div>
                <label htmlFor="name" className="block text-sm font-medium text-gray-700">
                  Product Name
                </label>
                <input
                  type="text"
                  name="name"
                  id="name"
                  value={formData.name}
                  onChange={handleChange}
                  required
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                />
              </div>

              <div>
                <label htmlFor="slug" className="block text-sm font-medium text-gray-700">
                  Slug
                </label>
                <input
                  type="text"
                  name="slug"
                  id="slug"
                  value={formData.slug}
                  onChange={handleChange}
                  required
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                />
              </div>

              <div>
                <label htmlFor="shortDescription" className="block text-sm font-medium text-gray-700">
                  Short Description
                </label>
                <input
                  type="text"
                  name="shortDescription"
                  id="shortDescription"
                  value={formData.shortDescription}
                  onChange={handleChange}
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                />
              </div>

              <div>
                <label htmlFor="description" className="block text-sm font-medium text-gray-700">
                  Description
                </label>
                <textarea
                  name="description"
                  id="description"
                  value={formData.description}
                  onChange={handleChange}
                  rows="4"
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                />
              </div>

              <div>
                <label htmlFor="basePrice" className="block text-sm font-medium text-gray-700">
                  Base Price
                </label>
                <input
                  type="number"
                  name="basePrice"
                  id="basePrice"
                  value={formData.basePrice}
                  onChange={handleChange}
                  step="0.01"
                  min="0"
                  required
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                />
              </div>

              <div className="flex justify-end gap-3 pt-4 border-t border-gray-200 mt-6">
                <button
                  type="button"
                  onClick={handleCloseModal}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                >
                  {editingProduct ? 'Save Changes' : 'Create Product'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default ProductManagement;
