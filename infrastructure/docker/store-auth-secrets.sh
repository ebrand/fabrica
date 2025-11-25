#!/bin/bash
# Store OAuth and Stytch credentials in Vault

set -e

export VAULT_ADDR='http://localhost:8200'
export VAULT_TOKEN='fabrica_dev_root_token'

echo "🔐 Storing authentication secrets in Vault..."

# Load .env file
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
else
    echo "❌ .env file not found!"
    exit 1
fi

# Store Stytch credentials
echo "📝 Storing Stytch credentials..."
vault kv put fabrica/admin/stytch \
  project_id="$STYTCH_PROJECT_ID" \
  project_domain="$STYTCH_PROJECT_DOMAIN" \
  secret="$STYTCH_SECRET" \
  public_token="$STYTCH_PUBLIC_TOKEN"

# Store Google OAuth credentials
echo "📝 Storing Google OAuth credentials..."
vault kv put fabrica/admin/oauth/google \
  client_id="$GOOGLE_CLIENT_ID" \
  client_secret="$GOOGLE_CLIENT_SECRET" \
  redirect_uri="$OAUTH_REDIRECT_URI"

# Verify storage
echo ""
echo "✅ Secrets stored successfully!"
echo ""
echo "Verify with:"
echo "  vault kv get fabrica/admin/stytch"
echo "  vault kv get fabrica/admin/oauth/google"
echo ""
