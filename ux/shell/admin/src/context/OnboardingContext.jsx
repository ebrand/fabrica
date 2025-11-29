import { createContext, useContext, useState, useEffect } from 'react';
import axios from 'axios';
import configService from '../services/config';

const OnboardingContext = createContext(null);

export const useOnboarding = () => {
  const context = useContext(OnboardingContext);
  if (!context) {
    throw new Error('useOnboarding must be used within OnboardingProvider');
  }
  return context;
};

export const ONBOARDING_STEPS = {
  TENANT: 0,
  INVITATIONS: 1,
  PAYMENT: 2,
  COMPLETE: 3,
};

export const OnboardingProvider = ({ children }) => {
  const [currentStep, setCurrentStep] = useState(ONBOARDING_STEPS.TENANT);
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Onboarding data collected through the wizard
  const [onboardingData, setOnboardingData] = useState({
    // Step 1: Tenant
    tenantId: null,
    tenantName: '',
    tenantDescription: '',
    tenantSlug: '',
    planId: null,
    planName: '',
    // Step 2: Invitations
    invitations: [],
    // Step 3: Payment
    stripeCustomerId: '',
    paymentMethodId: '',
    billingEmail: '',
  });

  // Fetch subscription plans on mount
  useEffect(() => {
    const fetchPlans = async () => {
      try {
        const bffUrl = await configService.getBffAdminUrl();
        const response = await axios.get(`${bffUrl}/api/onboarding/plans`, {
          withCredentials: true
        });
        setPlans(response.data);
        setLoading(false);
      } catch (err) {
        console.error('Error fetching plans:', err);
        setError('Failed to load subscription plans');
        setLoading(false);
      }
    };

    fetchPlans();
  }, []);

  // Step 1: Create tenant
  const createTenant = async (name, description, planId) => {
    setLoading(true);
    setError(null);
    try {
      const bffUrl = await configService.getBffAdminUrl();
      const response = await axios.post(`${bffUrl}/api/onboarding/tenant`, {
        name,
        description,
        planId
      }, { withCredentials: true });

      const selectedPlan = plans.find(p => p.planId === planId);

      setOnboardingData(prev => ({
        ...prev,
        tenantId: response.data.tenantId,
        tenantName: response.data.name,
        tenantSlug: response.data.slug,
        tenantDescription: response.data.description,
        planId,
        planName: selectedPlan?.name || '',
      }));

      setCurrentStep(ONBOARDING_STEPS.INVITATIONS);
      setLoading(false);
      return response.data;
    } catch (err) {
      console.error('Error creating tenant:', err);
      setError(err.response?.data?.error || 'Failed to create tenant');
      setLoading(false);
      throw err;
    }
  };

  // Step 2: Create invitations
  const createInvitations = async (emails) => {
    setLoading(true);
    setError(null);
    try {
      const bffUrl = await configService.getBffAdminUrl();
      const response = await axios.post(`${bffUrl}/api/onboarding/invitations`, {
        tenantId: onboardingData.tenantId,
        emails
      }, { withCredentials: true });

      setOnboardingData(prev => ({
        ...prev,
        invitations: emails
      }));

      setCurrentStep(ONBOARDING_STEPS.PAYMENT);
      setLoading(false);
      return response.data;
    } catch (err) {
      console.error('Error creating invitations:', err);
      setError(err.response?.data?.error || 'Failed to send invitations');
      setLoading(false);
      throw err;
    }
  };

  // Skip invitations step
  const skipInvitations = () => {
    setCurrentStep(ONBOARDING_STEPS.PAYMENT);
  };

  // Step 3a: Create Stripe SetupIntent
  const createSetupIntent = async (billingEmail) => {
    setLoading(true);
    setError(null);
    try {
      const bffUrl = await configService.getBffAdminUrl();
      const response = await axios.post(`${bffUrl}/api/onboarding/setup-intent`, {
        tenantId: onboardingData.tenantId,
        billingEmail,
        tenantName: onboardingData.tenantName
      }, { withCredentials: true });

      setOnboardingData(prev => ({
        ...prev,
        billingEmail,
        stripeCustomerId: response.data.customerId,
        clientSecret: response.data.clientSecret,
        publishableKey: response.data.publishableKey
      }));

      setLoading(false);
      return response.data;
    } catch (err) {
      console.error('Error creating setup intent:', err);
      setError(err.response?.data?.error || 'Failed to initialize payment');
      setLoading(false);
      throw err;
    }
  };

  // Step 3b: Confirm payment after successful card entry
  const confirmPayment = async (paymentMethodId) => {
    setLoading(true);
    setError(null);
    try {
      const bffUrl = await configService.getBffAdminUrl();
      await axios.post(`${bffUrl}/api/onboarding/payment-confirm`, {
        tenantId: onboardingData.tenantId,
        customerId: onboardingData.stripeCustomerId,
        paymentMethodId,
        billingEmail: onboardingData.billingEmail
      }, { withCredentials: true });

      setOnboardingData(prev => ({
        ...prev,
        paymentMethodId
      }));

      setCurrentStep(ONBOARDING_STEPS.COMPLETE);
      setLoading(false);
      return true;
    } catch (err) {
      console.error('Error confirming payment:', err);
      setError(err.response?.data?.error || 'Failed to save payment method');
      setLoading(false);
      throw err;
    }
  };

  // Step 4: Complete onboarding
  const completeOnboarding = async () => {
    setLoading(true);
    setError(null);
    try {
      const bffUrl = await configService.getBffAdminUrl();
      await axios.post(`${bffUrl}/api/onboarding/complete`, {
        tenantId: onboardingData.tenantId
      }, { withCredentials: true });

      setLoading(false);
      return onboardingData.tenantId;
    } catch (err) {
      console.error('Error completing onboarding:', err);
      setError(err.response?.data?.error || 'Failed to complete onboarding');
      setLoading(false);
      throw err;
    }
  };

  // Reset onboarding state
  const resetOnboarding = () => {
    setCurrentStep(ONBOARDING_STEPS.TENANT);
    setOnboardingData({
      tenantId: null,
      tenantName: '',
      tenantDescription: '',
      tenantSlug: '',
      planId: null,
      planName: '',
      invitations: [],
      stripeCustomerId: '',
      paymentMethodId: '',
      billingEmail: '',
    });
    setError(null);
  };

  const value = {
    currentStep,
    setCurrentStep,
    plans,
    loading,
    error,
    setError,
    onboardingData,
    createTenant,
    createInvitations,
    skipInvitations,
    createSetupIntent,
    confirmPayment,
    completeOnboarding,
    resetOnboarding,
  };

  return (
    <OnboardingContext.Provider value={value}>
      {children}
    </OnboardingContext.Provider>
  );
};
