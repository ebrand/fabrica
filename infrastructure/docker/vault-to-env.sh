#!/bin/bash
# Vault to Environment Variables Script
# Fetches secrets from Vault and generates .env.vault file for docker-compose

set -e

VAULT_ADDR=${VAULT_ADDR:-http://localhost:8200}
VAULT_TOKEN=${VAULT_TOKEN:-fabrica_dev_root_token}

echo "Fetching secrets from Vault at $VAULT_ADDR..."

# Function to fetch secret from Vault
get_vault_secret() {
    local path=$1
    local field=$2
    docker exec -e VAULT_TOKEN=$VAULT_TOKEN vault vault kv get -field=$field $path 2>/dev/null || echo ""
}

# Create .env.vault file
cat > .env.vault << EOF
# Auto-generated from Vault - DO NOT EDIT MANUALLY
# Generated: $(date)

# Node Environment
NODE_ENV=development

# PostgreSQL Configuration (from Vault)
POSTGRES_USER=$(get_vault_secret fabrica/infrastructure/postgres username)
POSTGRES_PASSWORD=$(get_vault_secret fabrica/infrastructure/postgres password)

# RabbitMQ Configuration (from Vault)
RABBITMQ_USER=$(get_vault_secret fabrica/infrastructure/rabbitmq username)
RABBITMQ_PASSWORD=$(get_vault_secret fabrica/infrastructure/rabbitmq password)

# HashiCorp Vault Configuration
VAULT_ROOT_TOKEN=$VAULT_TOKEN
VAULT_ADDR=$VAULT_ADDR

# Service URLs (for development)
AUTH_SERVICE_URL=http://localhost:3600
CUSTOMER_SERVICE_URL=http://localhost:3410
PRODUCT_SERVICE_URL=http://localhost:3420
ORDER_SERVICE_URL=http://localhost:3430

# Observability (optional)
LOG_LEVEL=info
ENABLE_TRACING=false
ENABLE_METRICS=false
EOF

echo "✅ Generated .env.vault with secrets from Vault"
echo "📝 Use: docker-compose --env-file .env.vault up"
