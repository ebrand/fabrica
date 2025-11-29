import { useState } from 'react';

/**
 * Modal component for inviting a user to the current tenant
 *
 * @param {Object} props
 * @param {boolean} props.show - Whether to show the modal
 * @param {Function} props.onClose - Callback when modal is closed
 * @param {Function} props.onInvite - Callback with email when invitation is submitted
 * @param {boolean} props.loading - External loading state
 * @param {string} props.error - External error message
 */
function InviteUserModal({ show, onClose, onInvite, loading = false, error = null }) {
  const [email, setEmail] = useState('');
  const [internalError, setInternalError] = useState(null);

  const displayError = error || internalError;

  const handleSubmit = async (e) => {
    e.preventDefault();
    setInternalError(null);

    if (!email.trim()) {
      setInternalError('Email address is required');
      return;
    }

    if (!email.includes('@')) {
      setInternalError('Please enter a valid email address');
      return;
    }

    try {
      await onInvite(email.trim().toLowerCase());
      setEmail('');
      onClose();
    } catch (err) {
      setInternalError(err.response?.data?.error || err.message || 'Failed to send invitation');
    }
  };

  const handleClose = () => {
    setEmail('');
    setInternalError(null);
    onClose();
  };

  if (!show) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/50 transition-opacity"
        onClick={handleClose}
      />

      {/* Modal Content */}
      <div className="flex min-h-full items-center justify-center p-4">
        <div className="relative bg-white rounded-lg shadow-xl w-full max-w-md">
          <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
            <h3 className="text-lg font-medium text-gray-900">
              Invite User
            </h3>
            <button
              onClick={handleClose}
              className="text-gray-400 hover:text-gray-600"
            >
              <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <form onSubmit={handleSubmit} className="px-6 py-4">
            {displayError && (
              <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                {displayError}
              </div>
            )}

            <div className="mb-4">
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                Email Address
              </label>
              <input
                type="email"
                id="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="user@example.com"
                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
                autoFocus
                disabled={loading}
              />
              <p className="mt-2 text-sm text-gray-500">
                The user will receive access to this workspace when they sign in with this email address.
              </p>
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
              <button
                type="button"
                onClick={handleClose}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                disabled={loading}
              >
                Cancel
              </button>
              <button
                type="submit"
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                disabled={loading}
              >
                {loading ? 'Sending...' : 'Send Invitation'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export default InviteUserModal;
