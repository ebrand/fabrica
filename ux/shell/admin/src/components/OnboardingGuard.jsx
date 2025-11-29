import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useTenant } from '../context/TenantContext';

/**
 * OnboardingGuard redirects users who need to complete onboarding
 * to the onboarding wizard. Users who have completed onboarding
 * (or don't need it) are allowed to access protected content.
 * Also waits for tenant context to be ready before rendering children.
 */
export default function OnboardingGuard({ children }) {
  const { requiresOnboarding, loading: authLoading } = useAuth();
  const { loading: tenantLoading } = useTenant();

  // Don't redirect while still loading auth or tenant state
  if (authLoading || tenantLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
          <p className="mt-4 text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  // Redirect to onboarding if user needs to complete it
  if (requiresOnboarding) {
    return <Navigate to="/onboarding" replace />;
  }

  // User has completed onboarding or doesn't need it
  return children;
}
