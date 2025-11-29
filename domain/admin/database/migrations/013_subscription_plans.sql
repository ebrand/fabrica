-- Migration: 013_subscription_plans.sql
-- Description: Add subscription plan tables for tenant onboarding workflow
-- Date: 2025-11-28

-- Subscription plan definitions
CREATE TABLE fabrica.subscription_plan (
    plan_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    price_cents INT NOT NULL DEFAULT 0,
    billing_interval VARCHAR(20) NOT NULL DEFAULT 'monthly',
    max_users INT NOT NULL DEFAULT 5,
    max_products INT NOT NULL DEFAULT 100,
    is_active BOOLEAN NOT NULL DEFAULT true,
    display_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tenant subscription (one per tenant)
CREATE TABLE fabrica.tenant_subscription (
    subscription_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL UNIQUE REFERENCES fabrica.tenant(tenant_id) ON DELETE CASCADE,
    plan_id UUID NOT NULL REFERENCES fabrica.subscription_plan(plan_id),
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    stripe_customer_id VARCHAR(255),
    stripe_payment_method_id VARCHAR(255),
    billing_email VARCHAR(255),
    trial_ends_at TIMESTAMP WITH TIME ZONE,
    current_period_start TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    current_period_end TIMESTAMP WITH TIME ZONE,
    canceled_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for tenant_subscription
CREATE INDEX idx_tenant_subscription_tenant ON fabrica.tenant_subscription(tenant_id);
CREATE INDEX idx_tenant_subscription_status ON fabrica.tenant_subscription(status);
CREATE INDEX idx_tenant_subscription_stripe_customer ON fabrica.tenant_subscription(stripe_customer_id);

-- Add onboarding fields to tenant table
ALTER TABLE fabrica.tenant ADD COLUMN IF NOT EXISTS onboarding_completed BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE fabrica.tenant ADD COLUMN IF NOT EXISTS onboarding_step INT NOT NULL DEFAULT 0;

-- Seed initial subscription plans
INSERT INTO fabrica.subscription_plan (plan_id, name, description, price_cents, billing_interval, max_users, max_products, display_order)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Starter', 'Perfect for small teams getting started', 2900, 'monthly', 5, 100, 1),
    ('22222222-2222-2222-2222-222222222222', 'Professional', 'For growing businesses with more needs', 7900, 'monthly', 25, 1000, 2),
    ('33333333-3333-3333-3333-333333333333', 'Enterprise', 'Unlimited power for large organizations', 19900, 'monthly', 100, 10000, 3);

-- Update trigger for subscription_plan
CREATE TRIGGER update_subscription_plan_updated_at
    BEFORE UPDATE ON fabrica.subscription_plan
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- Update trigger for tenant_subscription
CREATE TRIGGER update_tenant_subscription_updated_at
    BEFORE UPDATE ON fabrica.tenant_subscription
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();
