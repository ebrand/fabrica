import { useState, useEffect } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { Elements, CardElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { useOnboarding } from '../../context/OnboardingContext';
import { useAuth } from '../../context/AuthContext';

// Card element styling
const CARD_ELEMENT_OPTIONS = {
  style: {
    base: {
      fontSize: '16px',
      color: '#374151',
      fontFamily: 'ui-sans-serif, system-ui, sans-serif',
      '::placeholder': {
        color: '#9CA3AF',
      },
    },
    invalid: {
      color: '#EF4444',
      iconColor: '#EF4444',
    },
  },
};

function PaymentForm() {
  const stripe = useStripe();
  const elements = useElements();
  const { onboardingData, error, setError, confirmPayment } = useOnboarding();
  const [processing, setProcessing] = useState(false);
  const [cardComplete, setCardComplete] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!stripe || !elements) {
      return;
    }

    setProcessing(true);
    setError(null);

    const cardElement = elements.getElement(CardElement);

    try {
      // Confirm the SetupIntent with the card details
      const { error: stripeError, setupIntent } = await stripe.confirmCardSetup(
        onboardingData.clientSecret,
        {
          payment_method: {
            card: cardElement,
            billing_details: {
              email: onboardingData.billingEmail,
            },
          },
        }
      );

      if (stripeError) {
        setError(stripeError.message);
        setProcessing(false);
        return;
      }

      // Save the payment method to our backend
      await confirmPayment(setupIntent.payment_method);
    } catch (err) {
      console.error('Payment error:', err);
      setError('Failed to save payment method. Please try again.');
    } finally {
      setProcessing(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Card Details */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Card Details
        </label>
        <div className="border border-gray-300 rounded-lg p-4 bg-white">
          <CardElement
            options={CARD_ELEMENT_OPTIONS}
            onChange={(e) => setCardComplete(e.complete)}
          />
        </div>
      </div>

      {/* Security Note */}
      <div className="flex items-start gap-3 text-sm text-gray-500">
        <svg className="w-5 h-5 text-green-500 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
          <path fillRule="evenodd" d="M10 1a4.5 4.5 0 00-4.5 4.5V9H5a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-.5V5.5A4.5 4.5 0 0010 1zm3 8V5.5a3 3 0 10-6 0V9h6z" clipRule="evenodd" />
        </svg>
        <p>
          Your payment information is encrypted and securely processed by Stripe.
          We never store your full card details.
        </p>
      </div>

      {/* Error Message */}
      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <p className="text-sm text-red-700">{error}</p>
        </div>
      )}

      {/* Submit Button */}
      <button
        type="submit"
        disabled={!stripe || processing || !cardComplete}
        className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {processing ? (
          <>
            <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Processing...
          </>
        ) : (
          'Save Payment Method'
        )}
      </button>
    </form>
  );
}

export default function PaymentStep() {
  const { user } = useAuth();
  const { onboardingData, loading, error, setError, createSetupIntent } = useOnboarding();
  const [stripePromise, setStripePromise] = useState(null);
  const [billingEmail, setBillingEmail] = useState(user?.emails?.[0]?.email || '');
  const [emailConfirmed, setEmailConfirmed] = useState(false);
  const [initializing, setInitializing] = useState(false);

  const initializePayment = async () => {
    if (!billingEmail.trim()) {
      setError('Billing email is required');
      return;
    }

    setInitializing(true);
    setError(null);

    try {
      await createSetupIntent(billingEmail.trim());
      setEmailConfirmed(true);
    } catch (err) {
      // Error handled in context
    } finally {
      setInitializing(false);
    }
  };

  // Initialize Stripe when we have the publishable key
  useEffect(() => {
    if (emailConfirmed && onboardingData.publishableKey && !stripePromise) {
      loadStripe(onboardingData.publishableKey).then(setStripePromise);
    }
  }, [emailConfirmed, onboardingData.publishableKey, stripePromise]);

  // Show email confirmation form first
  if (!emailConfirmed) {
    return (
      <div>
        <div className="text-center mb-8">
          <h2 className="text-2xl font-bold text-gray-900">Payment Information</h2>
          <p className="mt-2 text-gray-600">Add a payment method for your subscription</p>
        </div>

        {/* Plan Summary */}
        <div className="bg-gray-50 rounded-lg p-4 mb-6">
          <div className="flex justify-between items-center">
            <div>
              <p className="font-medium text-gray-900">{onboardingData.planName}</p>
              <p className="text-sm text-gray-500">{onboardingData.tenantName}</p>
            </div>
          </div>
        </div>

        <div className="space-y-4">
          <div>
            <label htmlFor="billingEmail" className="block text-sm font-medium text-gray-700">
              Billing Email
            </label>
            <input
              type="email"
              id="billingEmail"
              value={billingEmail}
              onChange={(e) => setBillingEmail(e.target.value)}
              placeholder="billing@example.com"
              className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-3 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
              disabled={initializing}
            />
            <p className="mt-1 text-sm text-gray-500">Invoices will be sent to this email address</p>
          </div>

          {error && (
            <div className="rounded-md bg-red-50 p-4">
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          <button
            onClick={initializePayment}
            disabled={initializing || !billingEmail.trim()}
            className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
          >
            {initializing ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Initializing...
              </>
            ) : (
              'Continue to Payment'
            )}
          </button>
        </div>
      </div>
    );
  }

  // Show Stripe card form
  return (
    <div>
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900">Payment Information</h2>
        <p className="mt-2 text-gray-600">Add a payment method for your subscription</p>
      </div>

      {/* Plan Summary */}
      <div className="bg-gray-50 rounded-lg p-4 mb-6">
        <div className="flex justify-between items-center">
          <div>
            <p className="font-medium text-gray-900">{onboardingData.planName}</p>
            <p className="text-sm text-gray-500">{onboardingData.tenantName}</p>
          </div>
          <div className="text-sm text-gray-500">
            Billing: {onboardingData.billingEmail}
          </div>
        </div>
      </div>

      {stripePromise && onboardingData.clientSecret && (
        <Elements stripe={stripePromise} options={{ clientSecret: onboardingData.clientSecret }}>
          <PaymentForm />
        </Elements>
      )}
    </div>
  );
}
