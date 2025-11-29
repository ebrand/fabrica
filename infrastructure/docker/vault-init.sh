#!/bin/bash
# Vault Initialization Script for Fabrica Commerce Cloud
# This script sets up policies, groups, and auth methods

set -e

# Vault configuration
export VAULT_ADDR='http://localhost:8200'
export VAULT_TOKEN='fabrica_dev_root_token'

echo "🔐 Initializing Vault for Fabrica Commerce Cloud..."

# Enable AppRole auth method (for services)
echo "📝 Enabling AppRole authentication..."
vault auth enable approle 2>/dev/null || echo "AppRole already enabled"

# Enable KV v2 secrets engine
echo "📝 Enabling KV secrets engine..."
vault secrets enable -version=2 -path=fabrica kv 2>/dev/null || echo "KV engine already enabled"

# Create policies
echo "📝 Creating policies..."

# Admin service policy
cat <<EOF | vault policy write admin-service-policy -
path "fabrica/data/admin/*" {
  capabilities = ["create", "read", "update", "delete", "list"]
}
path "fabrica/data/shared/*" {
  capabilities = ["read", "list"]
}
path "database/creds/admin-db" {
  capabilities = ["read"]
}
EOF

# Customer service policy
cat <<EOF | vault policy write customer-service-policy -
path "fabrica/data/customer/*" {
  capabilities = ["create", "read", "update", "delete", "list"]
}
path "fabrica/data/shared/*" {
  capabilities = ["read", "list"]
}
path "database/creds/customer-db" {
  capabilities = ["read"]
}
EOF

# Product service policy
cat <<EOF | vault policy write product-service-policy -
path "fabrica/data/product/*" {
  capabilities = ["create", "read", "update", "delete", "list"]
}
path "fabrica/data/shared/*" {
  capabilities = ["read", "list"]
}
path "database/creds/product-db" {
  capabilities = ["read"]
}
EOF

# Shared secrets policy (all services can read)
cat <<EOF | vault policy write shared-secrets-policy -
path "fabrica/data/shared/*" {
  capabilities = ["read", "list"]
}
EOF

# Store some initial secrets
echo "🔑 Storing initial secrets..."

# Infrastructure credentials
echo "  📦 Storing infrastructure credentials..."
vault kv put fabrica/infrastructure/postgres \
  username="fabrica_admin" \
  password="fabrica_dev_password"

vault kv put fabrica/infrastructure/rabbitmq \
  username="fabrica_admin" \
  password="fabrica_dev_password"

vault kv put fabrica/infrastructure/kafka \
  bootstrap_servers="kafka:9092"

vault kv put fabrica/infrastructure/redis \
  host="redis" \
  port="6379" \
  password=""

# Shared connection details
echo "  🔗 Storing shared connection details..."
vault kv put fabrica/shared/database \
  host="postgres" \
  port="5432" \
  ssl_mode="disable"

vault kv put fabrica/shared/rabbitmq \
  host="rabbitmq" \
  port="5672" \
  management_port="15672"

vault kv put fabrica/shared/redis \
  host="redis" \
  port="6379"

vault kv put fabrica/shared/consul \
  host="consul" \
  port="8500"

# Admin service secrets
vault kv put fabrica/admin/database \
  name="fabrica-admin-db" \
  username="fabrica_admin" \
  password="fabrica_dev_password"

vault kv put fabrica/admin/jwt \
  secret="admin-jwt-secret-change-in-production" \
  expiration="24h"

# Stytch authentication secrets
echo "  🔐 Storing Stytch credentials..."
vault kv put fabrica/admin/stytch \
  project_id="project-test-e1867408-c033-4090-8e01-3d97fe9a059b" \
  project_domain="https://abundant-tsunami-7349.customers.stytch.dev" \
  secret="secret-test-04j3jY-XM2w_jc3j86C-GdJvVOx9q0wfk_Q=" \
  public_token="public-token-test-61dffad1-babe-4b64-bebf-a52e41c13c40"

# Google OAuth secrets (replace with your actual credentials)
echo "  🔐 Storing Google OAuth credentials..."
vault kv put fabrica/admin/oauth/google \
  client_id="${GOOGLE_OAUTH_CLIENT_ID:-your-google-client-id.apps.googleusercontent.com}" \
  client_secret="${GOOGLE_OAUTH_CLIENT_SECRET:-your-google-client-secret}" \
  redirect_uri="http://localhost:3001/authenticate"

# Stripe payment secrets (replace with your actual credentials)
echo "  💳 Storing Stripe credentials..."
vault kv put fabrica/admin/stripe \
  secret_key="${STRIPE_SECRET_KEY:-sk_test_your-stripe-secret-key}" \
  publishable_key="${STRIPE_PUBLISHABLE_KEY:-pk_test_your-stripe-publishable-key}"

# Customer service secrets
vault kv put fabrica/customer/database \
  name="fabrica-customer-db" \
  username="fabrica_admin" \
  password="fabrica_dev_password"

# Product service secrets
vault kv put fabrica/product/database \
  name="fabrica-product-db" \
  username="fabrica_admin" \
  password="fabrica_dev_password"

# Content service secrets
vault kv put fabrica/content/database \
  name="fabrica-content-db" \
  username="fabrica_admin" \
  password="fabrica_dev_password"

# ============================================================================
# SERVICE URLS - Container-to-container communication (Docker hostnames)
# ============================================================================
echo "  🔗 Storing service URLs (container-to-container)..."

vault kv put fabrica/services/acl \
  admin="http://acl-admin:3600" \
  product="http://acl-product:3420" \
  content="http://acl-content:3460" \
  customer="http://acl-customer:3410" \
  order="http://acl-order:3430" \
  finance="http://acl-finance:3440" \
  fulfillment="http://acl-fulfillment:3450"

vault kv put fabrica/services/bff \
  admin="http://bff-admin:3200" \
  product="http://bff-product:3220" \
  content="http://bff-content:3240" \
  customer="http://bff-customer:3250"

# ============================================================================
# BROWSER URLs - For frontend service discovery (localhost ports)
# ============================================================================
echo "  🌐 Storing browser URLs (frontend access)..."

vault kv put fabrica/services/browser/acl \
  admin="http://localhost:3600" \
  product="http://localhost:3420" \
  content="http://localhost:3460"

vault kv put fabrica/services/browser/bff \
  admin="http://localhost:3200" \
  product="http://localhost:3220" \
  content="http://localhost:3240" \
  customer="http://localhost:3250"

vault kv put fabrica/services/browser/mfe \
  admin="http://localhost:3100" \
  product="http://localhost:3110" \
  content="http://localhost:3180" \
  customer="http://localhost:3170" \
  common="http://localhost:3099"

vault kv put fabrica/services/browser/shell \
  admin="http://localhost:3001" \
  storefront="http://localhost:3000"

# Create AppRoles for each service
echo "👤 Creating AppRoles..."

# Admin service AppRole
vault write auth/approle/role/admin-service \
  token_policies="admin-service-policy,shared-secrets-policy" \
  token_ttl=1h \
  token_max_ttl=4h

# Customer service AppRole
vault write auth/approle/role/customer-service \
  token_policies="customer-service-policy,shared-secrets-policy" \
  token_ttl=1h \
  token_max_ttl=4h

# Product service AppRole
vault write auth/approle/role/product-service \
  token_policies="product-service-policy,shared-secrets-policy" \
  token_ttl=1h \
  token_max_ttl=4h

# Get role IDs and secret IDs
echo ""
echo "✅ Vault initialization complete!"
echo ""
echo "📋 AppRole Credentials:"
echo "======================="

# Admin service credentials
echo ""
echo "Admin Service:"
ADMIN_ROLE_ID=$(vault read -field=role_id auth/approle/role/admin-service/role-id)
ADMIN_SECRET_ID=$(vault write -field=secret_id -f auth/approle/role/admin-service/secret-id)
echo "  VAULT_ROLE_ID: $ADMIN_ROLE_ID"
echo "  VAULT_SECRET_ID: $ADMIN_SECRET_ID"

# Customer service credentials
echo ""
echo "Customer Service:"
CUSTOMER_ROLE_ID=$(vault read -field=role_id auth/approle/role/customer-service/role-id)
CUSTOMER_SECRET_ID=$(vault write -field=secret_id -f auth/approle/role/customer-service/secret-id)
echo "  VAULT_ROLE_ID: $CUSTOMER_ROLE_ID"
echo "  VAULT_SECRET_ID: $CUSTOMER_SECRET_ID"

# Product service credentials
echo ""
echo "Product Service:"
PRODUCT_ROLE_ID=$(vault read -field=role_id auth/approle/role/product-service/role-id)
PRODUCT_SECRET_ID=$(vault write -field=secret_id -f auth/approle/role/product-service/secret-id)
echo "  VAULT_ROLE_ID: $PRODUCT_ROLE_ID"
echo "  VAULT_SECRET_ID: $PRODUCT_SECRET_ID"

echo ""
echo "💡 Add these to your service environment variables or .env files"
echo ""
