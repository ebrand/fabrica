import { useState, useEffect, Suspense, lazy } from 'react';
import axios from 'axios';
import configService from '../services/config';
import UserEditor from '../components/UserEditor';
import InviteUserModal from '../components/InviteUserModal';
import PendingInvitations from '../components/PendingInvitations';

// Enable credentials for all axios requests to send cookies
axios.defaults.withCredentials = true;

// Import common components from commonMfe
const Toast = lazy(() => import('commonMfe/Toast'));
const ConfirmModal = lazy(() => import('commonMfe/ConfirmModal'));

function UserManagement({ isCurrentUserSystemAdmin = false, canManageUsers = false, filterTenantId = null }) {
  const [users, setUsers] = useState([]);
  const [invitations, setInvitations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [invitationsLoading, setInvitationsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [bffUrl, setBffUrl] = useState(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [editingUser, setEditingUser] = useState(null);
  const [inviteLoading, setInviteLoading] = useState(false);

  // Toast notification state
  const [toast, setToast] = useState({ show: false, title: '', message: '', type: 'success' });

  // Confirm modal state
  const [confirmModal, setConfirmModal] = useState({
    show: false,
    type: 'user', // 'user' or 'invitation'
    id: null,
    name: ''
  });

  const showToast = (title, message, type = 'success') => {
    setToast({ show: true, title, message, type });
  };

  useEffect(() => {
    initializeAndFetchData();
  }, [filterTenantId]);

  const initializeAndFetchData = async () => {
    try {
      const url = await configService.getBffAdminUrl();
      setBffUrl(url);
      await Promise.all([
        fetchUsers(url),
        fetchInvitations(url)
      ]);
    } catch (err) {
      setError('Failed to initialize configuration');
      setLoading(false);
    }
  };

  const fetchUsers = async (url) => {
    try {
      setLoading(true);
      const tenantParam = filterTenantId ? `?tenantId=${filterTenantId}` : '';
      const response = await axios.get(`${url}/api/users${tenantParam}`);
      setUsers(response.data);
    } catch (err) {
      setError('Failed to load users');
      console.error('Error fetching users:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchInvitations = async (url) => {
    try {
      setInvitationsLoading(true);
      const tenantParam = filterTenantId ? `?tenantId=${filterTenantId}` : '';
      const response = await axios.get(`${url}/api/invitations${tenantParam}`);
      setInvitations(response.data);
    } catch (err) {
      console.error('Error fetching invitations:', err);
      // Don't show error for invitations - it's not critical
    } finally {
      setInvitationsLoading(false);
    }
  };

  const handleOpenEditModal = (user) => {
    setEditingUser(user);
    setShowEditModal(true);
  };

  const handleCloseEditModal = () => {
    setShowEditModal(false);
    setEditingUser(null);
  };

  const handleSaveUser = async (formData) => {
    await axios.put(`${bffUrl}/api/users/${editingUser.userId}`, formData);
    handleCloseEditModal();
    await fetchUsers(bffUrl);
  };

  const handleInviteUser = async (email) => {
    setInviteLoading(true);
    try {
      await axios.post(`${bffUrl}/api/invitations`, { email });
      showToast('Invitation Sent', `An invitation has been sent to ${email}`, 'success');
      await fetchInvitations(bffUrl);
    } finally {
      setInviteLoading(false);
    }
  };

  const handleDeleteUserClick = (user) => {
    setConfirmModal({
      show: true,
      type: 'user',
      id: user.userId,
      name: `${user.firstName} ${user.lastName}`
    });
  };

  const handleRevokeInvitationClick = (invitation) => {
    setConfirmModal({
      show: true,
      type: 'invitation',
      id: invitation.invitationId,
      name: invitation.email
    });
  };

  const handleConfirmAction = async () => {
    const { type, id } = confirmModal;
    setConfirmModal({ show: false, type: 'user', id: null, name: '' });

    try {
      if (type === 'user') {
        await axios.delete(`${bffUrl}/api/users/${id}`);
        await fetchUsers(bffUrl);
        showToast('User Deleted', 'User has been deleted successfully', 'success');
      } else if (type === 'invitation') {
        await axios.delete(`${bffUrl}/api/invitations/${id}`);
        await fetchInvitations(bffUrl);
        showToast('Invitation Revoked', 'Invitation has been revoked successfully', 'success');
      }
    } catch (err) {
      console.error(`Error ${type === 'user' ? 'deleting user' : 'revoking invitation'}:`, err);
      showToast('Error', err.response?.data?.error || err.message, 'error');
    }
  };

  const handleCancelConfirm = () => {
    setConfirmModal({ show: false, type: 'user', id: null, name: '' });
  };

  const formatDateTime = (dateString) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleString();
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

      {/* Confirm Modal */}
      <Suspense fallback={null}>
        <ConfirmModal
          show={confirmModal.show}
          title={confirmModal.type === 'user' ? 'Delete User' : 'Revoke Invitation'}
          message={
            confirmModal.type === 'user'
              ? `Are you sure you want to delete "${confirmModal.name}"? This action cannot be undone.`
              : `Are you sure you want to revoke the invitation to "${confirmModal.name}"?`
          }
          type="danger"
          confirmText={confirmModal.type === 'user' ? 'Delete' : 'Revoke'}
          cancelText="Cancel"
          onConfirm={handleConfirmAction}
          onCancel={handleCancelConfirm}
        />
      </Suspense>

      {/* Invite User Modal */}
      <InviteUserModal
        show={showInviteModal}
        onClose={() => setShowInviteModal(false)}
        onInvite={handleInviteUser}
        loading={inviteLoading}
      />

      <div className="mb-4 flex justify-between items-center px-6 py-4">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">User Management</h2>
          <p className="text-sm text-gray-500 mt-1">
            {isCurrentUserSystemAdmin
              ? 'Manage system users and their permissions'
              : 'Manage workspace members'}
          </p>
        </div>
        {canManageUsers && (
          <button
            onClick={() => setShowInviteModal(true)}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Invite User
          </button>
        )}
      </div>

      {/* Pending Invitations Section - only show if user can manage */}
      {canManageUsers && (
        <div className="px-6 mb-6">
          <PendingInvitations
            invitations={invitations}
            onRevoke={handleRevokeInvitationClick}
            loading={invitationsLoading}
          />
        </div>
      )}

      {/* Users Table */}
      <div className="px-6 my-6">
        <div className="bg-white rounded-lg shadow">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">Current Members</h3>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Name
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Email
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Display Name
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Active
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    System Admin
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Role
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    {isCurrentUserSystemAdmin ? 'Tenants' : 'Last Login'}
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {users.length === 0 ? (
                  <tr>
                    <td colSpan="8" className="px-6 py-4 text-center text-sm text-gray-500">
                      No members found. Invite someone to get started!
                    </td>
                  </tr>
                ) : (
                  users.map((user) => (
                    <tr key={user.userId} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm font-medium text-gray-900">
                          {user.firstName} {user.lastName}
                        </div>
                        {user.stytchUserId && (
                          <div className="text-xs text-gray-400">
                            OAuth User
                          </div>
                        )}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm text-gray-900">{user.email}</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm text-gray-500">{user.displayName}</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center">
                        <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                          user.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                        }`}>
                          {user.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center">
                        {user.isSystemAdmin && (
                          <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-purple-100 text-purple-800">
                            Admin
                          </span>
                        )}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center">
                        {user.tenantRole && (
                          <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                            user.tenantRole === 'owner'
                              ? 'bg-amber-100 text-amber-800'
                              : 'bg-blue-100 text-blue-800'
                          }`}>
                            {user.tenantRole === 'owner' ? 'Owner' : 'Member'}
                          </span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        {isCurrentUserSystemAdmin && user.tenants ? (
                          <div className="flex flex-wrap gap-1">
                            {user.tenants.map((t, idx) => (
                              <span
                                key={idx}
                                className={`px-2 py-0.5 text-xs rounded-full ${
                                  t.role === 'owner'
                                    ? 'bg-amber-100 text-amber-800'
                                    : 'bg-gray-100 text-gray-600'
                                }`}
                                title={`${t.role}`}
                              >
                                {t.name}
                              </span>
                            ))}
                            {user.tenants.length === 0 && (
                              <span className="text-gray-400 italic">No tenants</span>
                            )}
                          </div>
                        ) : (
                          formatDateTime(user.lastLoginAt)
                        )}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                        {canManageUsers && (
                          <div className="flex justify-end gap-4">
                            <button
                              onClick={() => handleOpenEditModal(user)}
                              className="text-blue-600 hover:text-blue-900"
                            >
                              Edit
                            </button>
                            <button
                              onClick={() => handleDeleteUserClick(user)}
                              className="text-red-600 hover:text-red-900"
                            >
                              Delete
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
          <div className="px-6 py-4 bg-gray-50 border-t border-gray-200 text-sm text-gray-500">
            Total members: {users.length}
          </div>
        </div>
      </div>

      {/* Modal for Edit User */}
      {showEditModal && editingUser && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          {/* Backdrop */}
          <div
            className="fixed inset-0 bg-black/50 transition-opacity"
            onClick={handleCloseEditModal}
          />

          {/* Modal Content */}
          <div className="flex min-h-full items-center justify-center p-4">
            <div className="relative bg-white rounded-lg shadow-xl w-full max-w-2xl">
              <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                <h3 className="text-lg font-medium text-gray-900">
                  Edit User
                </h3>
                <button
                  onClick={handleCloseEditModal}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div className="px-6 py-4">
                <UserEditor
                  mode="admin"
                  user={editingUser}
                  onSave={handleSaveUser}
                  onCancel={handleCloseEditModal}
                  isModal={true}
                  isCurrentUserSystemAdmin={isCurrentUserSystemAdmin}
                />
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default UserManagement;
