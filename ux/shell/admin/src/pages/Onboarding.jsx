import { useOnboarding, ONBOARDING_STEPS } from '../context/OnboardingContext';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import TenantStep from '../components/onboarding/TenantStep';
import InvitationsStep from '../components/onboarding/InvitationsStep';
import PaymentStep from '../components/onboarding/PaymentStep';
import CompleteStep from '../components/onboarding/CompleteStep';

const stepLabels = ['Create Organization', 'Invite Team', 'Payment', 'Complete'];

export default function Onboarding() {
  const { currentStep } = useOnboarding();
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-4xl mx-auto px-4 py-4 flex justify-between items-center">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-indigo-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-xl">F</span>
            </div>
            <span className="text-xl font-semibold text-gray-900">Fabrica</span>
          </div>
          <button
            onClick={handleLogout}
            className="text-sm text-gray-500 hover:text-gray-700"
          >
            Sign out
          </button>
        </div>
      </header>

      {/* Progress Stepper */}
      <div className="max-w-4xl mx-auto px-4 py-8">
        <nav aria-label="Progress">
          <ol className="flex items-center justify-center">
            {stepLabels.map((label, index) => (
              <li key={label} className={`relative ${index !== stepLabels.length - 1 ? 'pr-8 sm:pr-20' : ''}`}>
                <div className="flex items-center">
                  <div
                    className={`relative flex h-8 w-8 items-center justify-center rounded-full ${
                      index < currentStep
                        ? 'bg-indigo-600'
                        : index === currentStep
                        ? 'border-2 border-indigo-600 bg-white'
                        : 'border-2 border-gray-300 bg-white'
                    }`}
                  >
                    {index < currentStep ? (
                      <svg className="h-5 w-5 text-white" viewBox="0 0 20 20" fill="currentColor">
                        <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                      </svg>
                    ) : (
                      <span className={`text-sm font-medium ${index === currentStep ? 'text-indigo-600' : 'text-gray-500'}`}>
                        {index + 1}
                      </span>
                    )}
                  </div>
                  {index !== stepLabels.length - 1 && (
                    <div className={`absolute top-4 left-8 w-full h-0.5 ${index < currentStep ? 'bg-indigo-600' : 'bg-gray-300'}`} style={{ width: '4rem' }} />
                  )}
                </div>
                <span className={`absolute -bottom-6 left-1/2 -translate-x-1/2 text-xs font-medium whitespace-nowrap ${
                  index <= currentStep ? 'text-indigo-600' : 'text-gray-500'
                }`}>
                  {label}
                </span>
              </li>
            ))}
          </ol>
        </nav>
      </div>

      {/* Step Content */}
      <div className="max-w-2xl mx-auto px-4 py-12">
        <div className="bg-white rounded-2xl shadow-xl p-8">
          {currentStep === ONBOARDING_STEPS.TENANT && <TenantStep />}
          {currentStep === ONBOARDING_STEPS.INVITATIONS && <InvitationsStep />}
          {currentStep === ONBOARDING_STEPS.PAYMENT && <PaymentStep />}
          {currentStep === ONBOARDING_STEPS.COMPLETE && <CompleteStep />}
        </div>
      </div>
    </div>
  );
}
