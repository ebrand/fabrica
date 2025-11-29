import { useState, useEffect, Suspense, lazy } from 'react';

// Lazy load AvatarUpload from common MFE
const AvatarUpload = lazy(() => import('commonMfe/AvatarUpload'));

// Content BFF URL for fetching media URLs
const BFF_CONTENT_URL = import.meta.env.VITE_BFF_CONTENT_URL || 'http://localhost:3240';

/**
 * Unified UserEditor component for both admin user management and user profile editing.
 *
 * @param {Object} props
 * @param {'admin' | 'profile'} props.mode - 'admin' shows all fields, 'profile' hides admin-only fields
 * @param {Object} props.user - The user object to edit (null for create mode)
 * @param {Function} props.onSave - Callback when form is submitted: (formData) => Promise<void>
 * @param {Function} props.onCancel - Callback when cancel/reset is clicked (optional)
 * @param {boolean} props.isModal - Whether this is rendered in a modal (affects layout)
 * @param {boolean} props.loading - External loading state
 * @param {string} props.error - External error message
 * @param {string} props.success - External success message
 * @param {boolean} props.isCurrentUserSystemAdmin - Whether the current user is a system admin (controls access to admin-only fields)
 */
function UserEditor({
  mode = 'admin',
  user = null,
  onSave,
  onCancel,
  isModal = false,
  loading: externalLoading = false,
  error: externalError = null,
  success: externalSuccess = null,
  isCurrentUserSystemAdmin = false
}) {
  const isEditMode = !!user;
  const isAdminMode = mode === 'admin';
  const isProfileMode = mode === 'profile';

  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    displayName: '',
    avatarMediaId: null,
    isActive: true,
    isSystemAdmin: false
  });
  const [avatarUrl, setAvatarUrl] = useState(null);
  const [internalLoading, setInternalLoading] = useState(false);
  const [internalError, setInternalError] = useState(null);
  const [internalSuccess, setInternalSuccess] = useState(false);

  // Use external state if provided, otherwise use internal
  const loading = externalLoading || internalLoading;
  const error = externalError || internalError;
  const success = externalSuccess || internalSuccess;

  // Fetch avatar URL when user has avatarMediaId
  useEffect(() => {
    const fetchAvatarUrl = async (mediaId) => {
      try {
        const response = await fetch(`${BFF_CONTENT_URL}/api/content/media/${mediaId}`);
        if (response.ok) {
          const media = await response.json();
          setAvatarUrl(media.fileUrl);
        }
      } catch (err) {
        console.error('Error fetching avatar URL:', err);
      }
    };

    if (user) {
      setFormData({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || '',
        displayName: user.displayName || '',
        avatarMediaId: user.avatarMediaId || null,
        isActive: user.isActive ?? true,
        isSystemAdmin: user.isSystemAdmin ?? false
      });
      if (user.avatarMediaId) {
        fetchAvatarUrl(user.avatarMediaId);
      } else {
        setAvatarUrl(null);
      }
    } else {
      // Reset form for create mode
      setFormData({
        firstName: '',
        lastName: '',
        email: '',
        displayName: '',
        avatarMediaId: null,
        isActive: true,
        isSystemAdmin: false
      });
      setAvatarUrl(null);
    }
    setInternalError(null);
    setInternalSuccess(false);
  }, [user]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));

    // Auto-generate displayName from firstName + lastName when creating new user
    if ((name === 'firstName' || name === 'lastName') && !isEditMode) {
      const firstName = name === 'firstName' ? value : formData.firstName;
      const lastName = name === 'lastName' ? value : formData.lastName;
      const autoDisplayName = `${firstName} ${lastName}`.trim();
      if (autoDisplayName && !formData.displayName) {
        setFormData(prev => ({
          ...prev,
          displayName: autoDisplayName
        }));
      }
    }

    // Clear messages on change
    setInternalSuccess(false);
    setInternalError(null);
  };

  const handleAvatarUploadComplete = (mediaId, fileUrl) => {
    setFormData(prev => ({ ...prev, avatarMediaId: mediaId }));
    setAvatarUrl(fileUrl);
    setInternalSuccess(false);
    setInternalError(null);
  };

  const handleAvatarRemove = () => {
    setFormData(prev => ({ ...prev, avatarMediaId: null }));
    setAvatarUrl(null);
    setInternalSuccess(false);
    setInternalError(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setInternalLoading(true);
    setInternalError(null);
    setInternalSuccess(false);

    try {
      // Auto-generate displayName if not provided
      const displayName = formData.displayName || `${formData.firstName} ${formData.lastName}`.trim();

      const payload = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        displayName: displayName,
        avatarMediaId: formData.avatarMediaId
      };

      // Only include admin fields if in admin mode
      if (isAdminMode) {
        payload.isActive = formData.isActive;
        payload.isSystemAdmin = formData.isSystemAdmin;
      }

      await onSave(payload);
      setInternalSuccess(true);
    } catch (err) {
      setInternalError(err.response?.data?.error || err.message || 'Failed to save');
      console.error('Error saving user:', err);
    } finally {
      setInternalLoading(false);
    }
  };

  const handleReset = () => {
    if (onCancel) {
      onCancel();
    } else if (user) {
      // Reset to original user data
      setFormData({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || '',
        displayName: user.displayName || '',
        avatarMediaId: user.avatarMediaId || null,
        isActive: user.isActive ?? true,
        isSystemAdmin: user.isSystemAdmin ?? false
      });
      // Reset avatar URL - will be re-fetched by useEffect if needed
      if (user.avatarMediaId) {
        // Trigger refetch
        fetch(`${BFF_CONTENT_URL}/api/content/media/${user.avatarMediaId}`)
          .then(res => res.ok ? res.json() : null)
          .then(media => media && setAvatarUrl(media.fileUrl))
          .catch(() => setAvatarUrl(null));
      } else {
        setAvatarUrl(null);
      }
    }
    setInternalError(null);
    setInternalSuccess(false);
  };

  const getTitle = () => {
    if (isProfileMode) return 'My Profile';
    if (isEditMode) return 'Edit User';
    return 'Add New User';
  };

  const getSubtitle = () => {
    if (isProfileMode) return 'Update your personal information';
    if (isEditMode) return 'Update user information and permissions';
    return 'Create a new system user';
  };

  // Form content shared between modal and inline modes
  const formContent = (
    <form onSubmit={handleSubmit} className="space-y-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        {success && !isModal && (
          <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded">
            {isProfileMode ? 'Profile updated successfully!' : 'User saved successfully!'}
          </div>
        )}

        {/* Avatar Upload */}
        <div className="flex flex-col items-center">
          <Suspense fallback={
            <div className="w-24 h-24 rounded-full bg-gray-100 animate-pulse flex items-center justify-center">
              <span className="text-gray-400 text-xs">Loading...</span>
            </div>
          }>
            <AvatarUpload
              currentAvatarUrl={avatarUrl}
              currentMediaId={formData.avatarMediaId}
              onUploadComplete={handleAvatarUploadComplete}
              onRemove={handleAvatarRemove}
              uploadedBy={user?.userId || user?.id}
              size={96}
              disabled={loading}
            />
          </Suspense>
        </div>

        {/* Name Fields Row */}
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div>
            <label htmlFor="firstName" className="block text-sm font-medium text-gray-700">
              First Name *
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
              Last Name *
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

        {/* Email Field */}
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-gray-700">
            Email Address *
          </label>
          <input
            type="email"
            name="email"
            id="email"
            value={formData.email}
            onChange={handleChange}
            required
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
          />
        </div>

        {/* Display Name Field */}
        <div>
          <label htmlFor="displayName" className="block text-sm font-medium text-gray-700">
            Display Name
          </label>
          <input
            type="text"
            name="displayName"
            id="displayName"
            value={formData.displayName}
            onChange={handleChange}
            placeholder={`${formData.firstName} ${formData.lastName}`.trim()}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
          />
          <p className="mt-1 text-sm text-gray-500">
            Leave blank to use "First Name Last Name" format
          </p>
        </div>

        {/* Admin-only Fields */}
        {isAdminMode && (
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <div className="flex items-center">
              <input
                type="checkbox"
                name="isActive"
                id="isActive"
                checked={formData.isActive}
                onChange={handleChange}
                disabled={!isCurrentUserSystemAdmin}
                className={`h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded ${!isCurrentUserSystemAdmin ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              <label htmlFor="isActive" className={`ml-2 block text-sm ${!isCurrentUserSystemAdmin ? 'text-gray-500' : 'text-gray-900'}`}>
                Active User
                {!isCurrentUserSystemAdmin && <span className="text-xs text-gray-400 ml-1">(System Admin only)</span>}
              </label>
            </div>

            <div className="flex items-center">
              <input
                type="checkbox"
                name="isSystemAdmin"
                id="isSystemAdmin"
                checked={formData.isSystemAdmin}
                onChange={handleChange}
                disabled={!isCurrentUserSystemAdmin}
                className={`h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded ${!isCurrentUserSystemAdmin ? 'opacity-50 cursor-not-allowed' : ''}`}
              />
              <label htmlFor="isSystemAdmin" className={`ml-2 block text-sm ${!isCurrentUserSystemAdmin ? 'text-gray-500' : 'text-gray-900'}`}>
                System Administrator
                {!isCurrentUserSystemAdmin && <span className="text-xs text-gray-400 ml-1">(System Admin only)</span>}
              </label>
            </div>
          </div>
        )}

        {/* Read-only Info for Profile Mode */}
        {isProfileMode && user && (
          <div className="bg-gray-50 rounded-lg p-4 space-y-2">
            <h3 className="text-sm font-medium text-gray-700">Account Information</h3>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 text-sm">
              <div>
                <span className="text-gray-500">Status: </span>
                <span className={`font-medium ${user.isActive ? 'text-green-600' : 'text-gray-600'}`}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
              {user.isSystemAdmin && (
                <div>
                  <span className="text-gray-500">Role: </span>
                  <span className="font-medium text-purple-600">System Administrator</span>
                </div>
              )}
              {user.lastLoginAt && (
                <div>
                  <span className="text-gray-500">Last Login: </span>
                  <span className="font-medium text-gray-700">
                    {new Date(user.lastLoginAt).toLocaleString()}
                  </span>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Form Actions */}
        <div className={`flex justify-end gap-3 pt-4 border-t border-gray-200 ${isModal ? 'mt-6' : ''}`}>
          <button
            type="button"
            onClick={handleReset}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            disabled={loading}
          >
            {isModal ? 'Cancel' : 'Reset'}
          </button>
          <button
            type="submit"
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            disabled={loading}
          >
            {loading ? 'Saving...' : (isEditMode ? 'Save Changes' : 'Create User')}
          </button>
        </div>
      </form>
  );

  // Render based on modal vs inline mode
  if (isModal) {
    return formContent;
  }

  return (
    <div className="max-w-2xl mx-auto">
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">{getTitle()}</h2>
          <p className="mt-1 text-sm text-gray-500">{getSubtitle()}</p>
        </div>
        <div className="px-6 py-6">
          {formContent}
        </div>
      </div>
    </div>
  );
}

export default UserEditor;
