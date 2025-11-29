import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useOnboarding } from '../../context/OnboardingContext';
import { useAuth } from '../../context/AuthContext';
import { useTenant } from '../../context/TenantContext';

export default function CompleteStep() {
  const navigate = useNavigate();
  const { onboardingData, loading, error, completeOnboarding } = useOnboarding();
  const { completeOnboarding: authCompleteOnboarding } = useAuth();
  const { selectTenant } = useTenant();
  const [completing, setCompleting] = useState(false);

  const handleComplete = async () => {
    setCompleting(true);
    try {
      const tenantId = await completeOnboarding();

      // Update auth context to refresh user data
      await authCompleteOnboarding(tenantId);

      // Select the new tenant as current
      await selectTenant({
        tenantId: onboardingData.tenantId,
        name: onboardingData.tenantName,
        slug: onboardingData.tenantSlug,
      });

      // Navigate to dashboard
      navigate('/');
    } catch (err) {
      console.error('Error completing onboarding:', err);
    } finally {
      setCompleting(false);
    }
  };

  return (
    <div className="text-center">
      {/* Success Icon */}
      <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-100 mb-6">
        <svg className="h-8 w-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
        </svg>
      </div>

      <h2 className="text-2xl font-bold text-gray-900 mb-2">You're All Set!</h2>
      <p className="text-gray-600 mb-8">
        Your organization <span className="font-semibold">{onboardingData.tenantName}</span> is ready to go.
      </p>

      {/* Summary */}
      <div className="bg-gray-50 rounded-lg p-6 text-left mb-8 space-y-4">
        <div className="flex justify-between">
          <span className="text-gray-500">Organization</span>
          <span className="font-medium text-gray-900">{onboardingData.tenantName}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-gray-500">Plan</span>
          <span className="font-medium text-gray-900">{onboardingData.planName}</span>
        </div>
        {onboardingData.invitations.length > 0 && (
          <div className="flex justify-between">
            <span className="text-gray-500">Invitations Sent</span>
            <span className="font-medium text-gray-900">{onboardingData.invitations.length}</span>
          </div>
        )}
        <div className="flex justify-between">
          <span className="text-gray-500">Billing Email</span>
          <span className="font-medium text-gray-900">{onboardingData.billingEmail}</span>
        </div>
      </div>

      {/* What's Next */}
      <div className="text-left mb-8">
        <h3 className="font-semibold text-gray-900 mb-3">What's Next?</h3>
        <ul className="space-y-2 text-sm text-gray-600">
          <li className="flex items-start gap-2">
            <svg className="w-5 h-5 text-indigo-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
            <span>Explore your dashboard and familiarize yourself with the platform</span>
          </li>
          <li className="flex items-start gap-2">
            <svg className="w-5 h-5 text-indigo-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
            <span>Add your first products to your catalog</span>
          </li>
          <li className="flex items-start gap-2">
            <svg className="w-5 h-5 text-indigo-600 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
            <span>Customize your content blocks and pages</span>
          </li>
        </ul>
      </div>

      {/* Error Message */}
      {error && (
        <div className="rounded-md bg-red-50 p-4 mb-4">
          <p className="text-sm text-red-700">{error}</p>
        </div>
      )}

      {/* Complete Button */}
      <button
        onClick={handleComplete}
        disabled={completing}
        className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
      >
        {completing ? (
          <>
            <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Finishing up...
          </>
        ) : (
          'Go to Dashboard'
        )}
      </button>
    </div>
  );
}
