import { useState } from 'react';
import { useOnboarding } from '../../context/OnboardingContext';

export default function TenantStep() {
  const { plans, loading, error, setError, createTenant } = useOnboarding();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [selectedPlanId, setSelectedPlanId] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const formatPrice = (cents) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(cents / 100);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Organization name is required');
      return;
    }
    if (!selectedPlanId) {
      setError('Please select a subscription plan');
      return;
    }

    setSubmitting(true);
    try {
      await createTenant(name.trim(), description.trim(), selectedPlanId);
    } catch (err) {
      // Error is handled in context
    } finally {
      setSubmitting(false);
    }
  };

  if (loading && plans.length === 0) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  return (
    <div>
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900">Create Your Organization</h2>
        <p className="mt-2 text-gray-600">Set up your workspace and choose a plan</p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Organization Name */}
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700">
            Organization Name *
          </label>
          <input
            type="text"
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Acme Inc."
            className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-3 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
            disabled={submitting}
          />
        </div>

        {/* Description */}
        <div>
          <label htmlFor="description" className="block text-sm font-medium text-gray-700">
            Description (optional)
          </label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="A brief description of your organization"
            rows={2}
            className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-3 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
            disabled={submitting}
          />
        </div>

        {/* Subscription Plans */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-3">
            Choose Your Plan *
          </label>
          <div className="grid gap-4">
            {plans.map((plan) => (
              <div
                key={plan.planId}
                onClick={() => !submitting && setSelectedPlanId(plan.planId)}
                className={`relative flex cursor-pointer rounded-lg border p-4 shadow-sm transition-all ${
                  selectedPlanId === plan.planId
                    ? 'border-indigo-600 ring-2 ring-indigo-600 bg-indigo-50'
                    : 'border-gray-200 hover:border-gray-300'
                } ${submitting ? 'opacity-50 cursor-not-allowed' : ''}`}
              >
                <div className="flex w-full justify-between">
                  <div className="flex flex-col">
                    <span className="text-sm font-semibold text-gray-900">{plan.name}</span>
                    <span className="text-sm text-gray-500 mt-1">{plan.description}</span>
                    <span className="mt-2 text-xs text-gray-400">
                      Up to {plan.maxUsers} users, {plan.maxProducts} products
                    </span>
                  </div>
                  <div className="flex flex-col items-end">
                    <span className="text-lg font-bold text-gray-900">
                      {formatPrice(plan.priceCents)}
                    </span>
                    <span className="text-xs text-gray-500">/{plan.billingInterval}</span>
                  </div>
                </div>
                {selectedPlanId === plan.planId && (
                  <div className="absolute -top-2 -right-2 bg-indigo-600 rounded-full p-1">
                    <svg className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                    </svg>
                  </div>
                )}
              </div>
            ))}
          </div>
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
          disabled={submitting || !name.trim() || !selectedPlanId}
          className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {submitting ? (
            <>
              <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Creating...
            </>
          ) : (
            'Continue'
          )}
        </button>
      </form>
    </div>
  );
}
