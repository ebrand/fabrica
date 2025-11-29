import { useState, useEffect, Suspense, lazy } from 'react';
import axios from 'axios';
import configService from '../services/config';

// Configure axios to send credentials (cookies) with all requests
axios.defaults.withCredentials = true;

// Import common components from commonMfe
const Select = lazy(() => import('commonMfe/Select'));
const Toast = lazy(() => import('commonMfe/Toast'));
const ConfirmModal = lazy(() => import('commonMfe/ConfirmModal'));

// Status options for the customer status dropdown
const statusOptions = [
  { id: 'Active', name: 'Active' },
  { id: 'Inactive', name: 'Inactive' },
  { id: 'Suspended', name: 'Suspended' }
];

function CustomerManagement({ filterTenantId = null }) {
  const [customers, setCustomers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [bffUrl, setBffUrl] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [formData, setFormData] = useState({
    email: '',
    firstName: '',
    lastName: '',
    phone: '',
    status: 'Active',
    marketingOptIn: false
  });

  // Toast notification state
  const [toast, setToast] = useState({ show: false, title: '', message: '', type: 'success' });

  // Confirm modal state
  const [confirmModal, setConfirmModal] = useState({ show: false, customerId: null, customerName: '' });

  const showToast = (title, message, type = 'success') => {
    setToast({ show: true, title, message, type });
  };

  useEffect(() => {
    initializeAndFetchCustomers();
  }, [filterTenantId]);

  const initializeAndFetchCustomers = async () => {
    try {
      const url = await configService.getBffCustomerUrl();
      setBffUrl(url);
      await fetchCustomers(url);
    } catch (err) {
      setError('Failed to initialize configuration');
      setLoading(false);
    }
  };

  const fetchCustomers = async (url, search = '') => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      if (filterTenantId) params.append('tenantId', filterTenantId);
      if (search) params.append('search', search);

      const queryString = params.toString();
      const response = await axios.get(`${url}/api/customers${queryString ? `?${queryString}` : ''}`);
      setCustomers(response.data);
    } catch (err) {
      setError('Failed to load customers');
      console.error('Error fetching customers:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (e) => {
    e.preventDefault();
    if (bffUrl) {
      await fetchCustomers(bffUrl, searchTerm);
    }
  };

  const handleOpenModal = (customer = null) => {
    if (customer) {
      setEditingCustomer(customer);
      setFormData({
        email: customer.email,
        firstName: customer.firstName,
        lastName: customer.lastName,
        phone: customer.phone || '',
        status: customer.status,
        marketingOptIn: customer.marketingOptIn
      });
    } else {
      setEditingCustomer(null);
      setFormData({
        email: '',
        firstName: '',
        lastName: '',
        phone: '',
        status: 'Active',
        marketingOptIn: false
      });
    }
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
    setEditingCustomer(null);
  };

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingCustomer) {
        await axios.put(`${bffUrl}/api/customers/${editingCustomer.customerId}`, formData);
        showToast('Success', 'Customer updated successfully');
      } else {
        await axios.post(`${bffUrl}/api/customers`, formData);
        showToast('Success', 'Customer created successfully');
      }
      handleCloseModal();
      await fetchCustomers(bffUrl, searchTerm);
    } catch (err) {
      console.error('Error saving customer:', err);
      showToast('Error', 'Failed to save customer: ' + (err.response?.data?.error || err.message), 'error');
    }
  };

  const handleDeleteClick = (customer) => {
    setConfirmModal({
      show: true,
      customerId: customer.customerId,
      customerName: `${customer.firstName} ${customer.lastName}`
    });
  };

  const handleDeleteConfirm = async () => {
    const { customerId } = confirmModal;
    setConfirmModal({ show: false, customerId: null, customerName: '' });

    try {
      await axios.delete(`${bffUrl}/api/customers/${customerId}`);
      showToast('Success', 'Customer deleted successfully');
      await fetchCustomers(bffUrl, searchTerm);
    } catch (err) {
      console.error('Error deleting customer:', err);
      showToast('Error', 'Failed to delete customer', 'error');
    }
  };

  const handleDeleteCancel = () => {
    setConfirmModal({ show: false, customerId: null, customerName: '' });
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
          title="Delete Customer"
          message={`Are you sure you want to delete "${confirmModal.customerName}"? This action cannot be undone.`}
          type="danger"
          confirmText="Delete"
          cancelText="Cancel"
          onConfirm={handleDeleteConfirm}
          onCancel={handleDeleteCancel}
        />
      </Suspense>

      <div className="mb-4 flex justify-between items-center px-6 py-4">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Customer Management</h2>
          <p className="text-sm text-gray-500 mt-1">Manage customer accounts and profiles</p>
        </div>
        <div className="flex gap-4 items-center">
          <form onSubmit={handleSearch} className="flex gap-2">
            <input
              type="text"
              placeholder="Search customers..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              type="submit"
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Search
            </button>
          </form>
          <button
            onClick={() => handleOpenModal()}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Add Customer
          </button>
        </div>
      </div>

      <div className="overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Customer
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Email
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Orders
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Total Spent
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {customers.length === 0 ? (
              <tr>
                <td colSpan="6" className="px-6 py-4 text-center text-sm text-gray-500">
                  No customers found. Add your first customer!
                </td>
              </tr>
            ) : (
              customers.map((customer) => (
                <tr key={customer.customerId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="h-10 w-10 flex-shrink-0">
                        <div className="h-10 w-10 rounded-full bg-blue-100 flex items-center justify-center">
                          <span className="text-blue-600 font-medium text-sm">
                            {customer.firstName?.[0]}{customer.lastName?.[0]}
                          </span>
                        </div>
                      </div>
                      <div className="ml-4">
                        <div className="text-sm font-medium text-gray-900">
                          {customer.firstName} {customer.lastName}
                        </div>
                        <div className="text-sm text-gray-500">
                          {customer.phone || 'No phone'}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">{customer.email}</div>
                    {customer.emailVerified && (
                      <span className="text-xs text-green-600">Verified</span>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      customer.status === 'Active' ? 'bg-green-100 text-green-800' :
                      customer.status === 'Inactive' ? 'bg-gray-100 text-gray-800' :
                      'bg-red-100 text-red-800'
                    }`}>
                      {customer.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {customer.totalOrders || 0}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    ${(customer.totalSpent || 0).toFixed(2)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleOpenModal(customer)}
                      className="text-blue-600 hover:text-blue-900 mr-4"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => handleDeleteClick(customer)}
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
        Total customers: {customers.length}
      </div>

      {/* Modal for Create/Edit Customer */}
      {showModal && (
        <div
          className="fixed inset-0 bg-black/50 overflow-y-auto z-50 flex items-center justify-center transition-all duration-300"
          style={{ right: 'var(--esb-panel-offset, 0px)' }}
        >
          <div className="relative bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4">
            <div className="px-6 py-4 border-b border-gray-200">
              <h3 className="text-lg font-medium text-gray-900">
                {editingCustomer ? 'Edit Customer' : 'Add New Customer'}
              </h3>
            </div>

            <form onSubmit={handleSubmit} className="px-6 py-4 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="firstName" className="block text-sm font-medium text-gray-700">
                    First Name
                  </label>
                  <input
                    type="text"
                    name="firstName"
                    id="firstName"
                    value={formData.firstName}
                    onChange={handleChange}
                    required
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                  />
                </div>
                <div>
                  <label htmlFor="lastName" className="block text-sm font-medium text-gray-700">
                    Last Name
                  </label>
                  <input
                    type="text"
                    name="lastName"
                    id="lastName"
                    value={formData.lastName}
                    onChange={handleChange}
                    required
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                  />
                </div>
              </div>

              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                  Email
                </label>
                <input
                  type="email"
                  name="email"
                  id="email"
                  value={formData.email}
                  onChange={handleChange}
                  required
                  disabled={!!editingCustomer}
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border disabled:bg-gray-100"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="phone" className="block text-sm font-medium text-gray-700">
                    Phone
                  </label>
                  <input
                    type="tel"
                    name="phone"
                    id="phone"
                    value={formData.phone}
                    onChange={handleChange}
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

              <div className="flex items-center">
                <input
                  type="checkbox"
                  name="marketingOptIn"
                  id="marketingOptIn"
                  checked={formData.marketingOptIn}
                  onChange={handleChange}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <label htmlFor="marketingOptIn" className="ml-2 block text-sm text-gray-900">
                  Subscribe to marketing emails
                </label>
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
                  {editingCustomer ? 'Save Changes' : 'Create Customer'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default CustomerManagement;
