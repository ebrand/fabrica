import { useState, useEffect } from 'react';
import axios from 'axios';
import configService from '../services/config';
import UserEditor from '../components/UserEditor';

function UserProfile({ user, syncedUser, onProfileUpdate }) {
  const [bffUrl, setBffUrl] = useState(null);
  const [userId, setUserId] = useState(null);
  const [userData, setUserData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);

  useEffect(() => {
    initializeProfile();
  }, [user, syncedUser]);

  const initializeProfile = async () => {
    try {
      setLoading(true);
      const url = await configService.getBffAdminUrl();
      setBffUrl(url);

      // Build user data from synced user or fall back to Stytch user
      const email = syncedUser?.email || user?.emails?.[0]?.email || '';
      const firstName = syncedUser?.firstName || user?.name?.first_name || '';
      const lastName = syncedUser?.lastName || user?.name?.last_name || '';
      const displayName = syncedUser?.displayName || `${firstName} ${lastName}`.trim() || '';
      const dbUserId = syncedUser?.userId;

      setUserId(dbUserId);
      setUserData({
        userId: dbUserId,
        email,
        firstName,
        lastName,
        displayName,
        isActive: syncedUser?.isActive ?? true,
        isSystemAdmin: syncedUser?.isSystemAdmin ?? false,
        lastLoginAt: syncedUser?.lastLoginAt
      });
    } catch (err) {
      setError('Failed to initialize profile');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = () => {
    setShowModal(true);
  };

  const handleCloseModal = () => {
    setShowModal(false);
  };

  const handleSaveProfile = async (formData) => {
    if (!userId) {
      throw new Error('User ID not found. Please refresh the page.');
    }

    await axios.put(`${bffUrl}/api/users/${userId}`, formData);

    // Update local user data with saved values
    setUserData(prev => ({
      ...prev,
      ...formData
    }));

    // Close modal
    handleCloseModal();

    // Notify parent (shell) to refresh user data in header
    if (onProfileUpdate) {
      onProfileUpdate();
    }
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

  if (error && !userData) {
    return (
      <div className="max-w-2xl mx-auto p-6">
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto">
      {/* Profile Card */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
          <div>
            <h2 className="text-xl font-semibold text-gray-900">My Profile</h2>
            <p className="mt-1 text-sm text-gray-500">Your personal information</p>
          </div>
          <button
            onClick={handleOpenModal}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Edit Profile
          </button>
        </div>

        {/* Profile Details */}
        <div className="px-6 py-6 space-y-6">
          {/* Name Section */}
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <div>
              <label className="block text-sm font-medium text-gray-500">First Name</label>
              <p className="mt-1 text-sm text-gray-900">{userData?.firstName || '-'}</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-500">Last Name</label>
              <p className="mt-1 text-sm text-gray-900">{userData?.lastName || '-'}</p>
            </div>
          </div>

          {/* Email */}
          <div>
            <label className="block text-sm font-medium text-gray-500">Email Address</label>
            <p className="mt-1 text-sm text-gray-900">{userData?.email || '-'}</p>
          </div>

          {/* Display Name */}
          <div>
            <label className="block text-sm font-medium text-gray-500">Display Name</label>
            <p className="mt-1 text-sm text-gray-900">{userData?.displayName || '-'}</p>
          </div>

          {/* Account Info Section */}
          <div className="pt-6 border-t border-gray-200">
            <h3 className="text-sm font-medium text-gray-900 mb-4">Account Information</h3>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label className="block text-sm font-medium text-gray-500">Status</label>
                <p className="mt-1">
                  <span className={`inline-flex px-2 text-xs leading-5 font-semibold rounded-full ${
                    userData?.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                  }`}>
                    {userData?.isActive ? 'Active' : 'Inactive'}
                  </span>
                </p>
              </div>
              {userData?.isSystemAdmin && (
                <div>
                  <label className="block text-sm font-medium text-gray-500">Role</label>
                  <p className="mt-1">
                    <span className="inline-flex px-2 text-xs leading-5 font-semibold rounded-full bg-purple-100 text-purple-800">
                      System Administrator
                    </span>
                  </p>
                </div>
              )}
              <div>
                <label className="block text-sm font-medium text-gray-500">Last Login</label>
                <p className="mt-1 text-sm text-gray-900">{formatDateTime(userData?.lastLoginAt)}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Edit Profile Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          {/* Backdrop */}
          <div
            className="fixed inset-0 bg-black/50 transition-opacity"
            onClick={handleCloseModal}
          />

          {/* Modal Content */}
          <div className="flex min-h-full items-center justify-center p-4">
            <div className="relative bg-white rounded-lg shadow-xl w-full max-w-2xl">
              <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                <h3 className="text-lg font-medium text-gray-900">Edit Profile</h3>
                <button
                  onClick={handleCloseModal}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <div className="px-6 py-4">
                <UserEditor
                  mode="profile"
                  user={userData}
                  onSave={handleSaveProfile}
                  onCancel={handleCloseModal}
                  isModal={true}
                />
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default UserProfile;
