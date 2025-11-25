#!/bin/sh
# Vault Docker Entrypoint for Fabrica Commerce Cloud
# Handles initialization and auto-unsealing for local development

set -e

VAULT_UNSEAL_KEY_FILE="/vault/data/.unseal-key"
VAULT_SECRETS_INITIALIZED="/vault/data/.secrets-initialized"

# Known dev token for consistency with services
DEV_ROOT_TOKEN="fabrica_dev_root_token"

# Start Vault server in background
vault server -config=/vault/config/vault.hcl &
VAULT_PID=$!

# Wait for Vault to be ready
echo "Waiting for Vault to start..."
sleep 3

export VAULT_ADDR="http://127.0.0.1:8200"

# Get Vault status
VAULT_STATUS=$(vault status 2>&1 || true)

# Handle initialization if needed
if echo "$VAULT_STATUS" | grep -q "Initialized.*false"; then
  echo "Initializing Vault for the first time..."

  # Initialize with 1 key share and 1 threshold for simplicity in dev
  INIT_OUTPUT=$(vault operator init -key-shares=1 -key-threshold=1 2>&1)

  # Extract unseal key from output
  UNSEAL_KEY=$(echo "$INIT_OUTPUT" | grep "Unseal Key 1:" | awk '{print $4}')
  INIT_ROOT_TOKEN=$(echo "$INIT_OUTPUT" | grep "Initial Root Token:" | awk '{print $4}')

  # Store unseal key for persistence
  echo "$UNSEAL_KEY" > "$VAULT_UNSEAL_KEY_FILE"

  echo "Vault initialized. Unseal key stored."

  # Unseal
  echo "Unsealing Vault..."
  vault operator unseal "$UNSEAL_KEY"

  # Create dev root token with known ID
  export VAULT_TOKEN="$INIT_ROOT_TOKEN"
  echo "Creating dev root token..."
  vault token create -id="$DEV_ROOT_TOKEN" -policy=root -no-default-policy=false -orphan=true 2>/dev/null || true

elif echo "$VAULT_STATUS" | grep -q "Sealed.*true"; then
  # Already initialized but sealed - unseal it
  echo "Vault is sealed. Unsealing..."
  if [ -f "$VAULT_UNSEAL_KEY_FILE" ]; then
    UNSEAL_KEY=$(cat "$VAULT_UNSEAL_KEY_FILE")
    vault operator unseal "$UNSEAL_KEY"
  else
    echo "ERROR: No unseal key found!"
    exit 1
  fi
else
  echo "Vault is already unsealed."
fi

# Use dev token for all operations
export VAULT_TOKEN="$DEV_ROOT_TOKEN"

# Setup secrets engine and initial secrets if not done
if [ ! -f "$VAULT_SECRETS_INITIALIZED" ]; then
  echo "Setting up Fabrica secrets..."

  # Enable KV v2 secrets engine
  if ! vault secrets list 2>/dev/null | grep -q "fabrica/"; then
    echo "Enabling fabrica/ secrets engine..."
    vault secrets enable -path=fabrica kv-v2
  fi

  echo "Storing initial secrets..."

  # Infrastructure credentials
  vault kv put fabrica/infrastructure/postgres \
    username="fabrica_admin" \
    password="fabrica_dev_password"

  vault kv put fabrica/infrastructure/rabbitmq \
    username="fabrica_admin" \
    password="fabrica_dev_password"

  vault kv put fabrica/infrastructure/redis \
    password=""

  # Shared connection details
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
  vault kv put fabrica/admin/stytch \
    project_id="project-test-e1867408-c033-4090-8e01-3d97fe9a059b" \
    project_domain="https://abundant-tsunami-7349.customers.stytch.dev" \
    secret="secret-test-04j3jY-XM2w_jc3j86C-GdJvVOx9q0wfk_Q=" \
    public_token="public-token-test-61dffad1-babe-4b64-bebf-a52e41c13c40"

  # Google OAuth secrets (replace with your actual credentials)
  vault kv put fabrica/admin/oauth/google \
    client_id="${GOOGLE_OAUTH_CLIENT_ID:-your-google-client-id.apps.googleusercontent.com}" \
    client_secret="${GOOGLE_OAUTH_CLIENT_SECRET:-your-google-client-secret}" \
    redirect_uri="http://localhost:3001/authenticate"

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

  # Mark as initialized
  touch "$VAULT_SECRETS_INITIALIZED"

  echo "Initial secrets stored successfully."
else
  echo "Secrets already initialized."
fi

echo "Vault is ready!"

# Keep container running
wait $VAULT_PID
