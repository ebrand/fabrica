import { useState } from 'react';
import { useOnboarding } from '../../context/OnboardingContext';

export default function InvitationsStep() {
  const { onboardingData, loading, error, setError, createInvitations, skipInvitations } = useOnboarding();
  const [emails, setEmails] = useState(['']);
  const [submitting, setSubmitting] = useState(false);

  const addEmailField = () => {
    if (emails.length < 10) {
      setEmails([...emails, '']);
    }
  };

  const removeEmailField = (index) => {
    if (emails.length > 1) {
      setEmails(emails.filter((_, i) => i !== index));
    }
  };

  const updateEmail = (index, value) => {
    const updated = [...emails];
    updated[index] = value;
    setEmails(updated);
  };

  const validateEmail = (email) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const validEmails = emails.filter(e => e.trim() && validateEmail(e.trim()));

    if (validEmails.length === 0) {
      setError('Please enter at least one valid email address');
      return;
    }

    setSubmitting(true);
    try {
      await createInvitations(validEmails);
    } catch (err) {
      // Error handled in context
    } finally {
      setSubmitting(false);
    }
  };

  const handleSkip = () => {
    skipInvitations();
  };

  return (
    <div>
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900">Invite Your Team</h2>
        <p className="mt-2 text-gray-600">
          Add team members to <span className="font-medium">{onboardingData.tenantName}</span>
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Email Fields */}
        <div className="space-y-3">
          {emails.map((email, index) => (
            <div key={index} className="flex gap-2">
              <input
                type="email"
                value={email}
                onChange={(e) => updateEmail(index, e.target.value)}
                placeholder="colleague@example.com"
                className={`flex-1 rounded-lg border px-4 py-3 shadow-sm focus:ring-indigo-500 ${
                  email && !validateEmail(email) ? 'border-red-300 focus:border-red-500' : 'border-gray-300 focus:border-indigo-500'
                }`}
                disabled={submitting}
              />
              {emails.length > 1 && (
                <button
                  type="button"
                  onClick={() => removeEmailField(index)}
                  className="px-3 py-2 text-gray-400 hover:text-gray-600"
                  disabled={submitting}
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              )}
            </div>
          ))}
        </div>

        {/* Add More Button */}
        {emails.length < 10 && (
          <button
            type="button"
            onClick={addEmailField}
            className="flex items-center text-sm text-indigo-600 hover:text-indigo-700"
            disabled={submitting}
          >
            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
            </svg>
            Add another email
          </button>
        )}

        {/* Plan Info */}
        <div className="bg-gray-50 rounded-lg p-4 mt-6">
          <p className="text-sm text-gray-600">
            <span className="font-medium">{onboardingData.planName}</span> plan includes capacity for team members.
            You can always invite more people later from your settings.
          </p>
        </div>

        {/* Error Message */}
        {error && (
          <div className="rounded-md bg-red-50 p-4">
            <p className="text-sm text-red-700">{error}</p>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex gap-3 pt-4">
          <button
            type="button"
            onClick={handleSkip}
            disabled={submitting}
            className="flex-1 py-3 px-4 border border-gray-300 rounded-lg shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
          >
            Skip for now
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
          >
            {submitting ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Sending...
              </>
            ) : (
              'Send Invitations'
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
