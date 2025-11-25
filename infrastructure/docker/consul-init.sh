#!/bin/bash
# Consul Initialization Script for Fabrica Commerce Cloud
# Sets up port registry and service configuration

set -e

CONSUL_ADDR="http://localhost:8500"

echo "🔧 Initializing Consul for Fabrica Commerce Cloud..."

# Function to register a port
register_port() {
  local service=$1
  local port=$2
  local type=$3
  local description=$4

  curl -X PUT "$CONSUL_ADDR/v1/kv/fabrica/ports/$type/$service" \
    -d "{\"port\": $port, \"service\": \"$service\", \"type\": \"$type\", \"description\": \"$description\"}" \
    -s > /dev/null

  echo "✓ Registered: $service ($type) on port $port"
}

# Function to register service metadata
register_service_config() {
  local service=$1
  shift
  local config="$@"

  curl -X PUT "$CONSUL_ADDR/v1/kv/fabrica/config/$service" \
    -d "$config" \
    -s > /dev/null
}

echo ""
echo "📋 Registering Port Allocations..."
echo "=================================="

# Shell UXs (3000-3099)
register_port "storefront-shell" 3000 "shell" "Storefront customer-facing shell"
register_port "shell-admin" 3001 "shell" "Admin panel shell"
register_port "partner-shell" 3002 "shell" "Partner/merchant shell"

# MFEs (3100-3199)
register_port "admin-mfe" 3100 "mfe" "Admin micro-frontend (ACL/Users)"
register_port "catalog-mfe" 3110 "mfe" "Product catalog micro-frontend"
register_port "cart-mfe" 3120 "mfe" "Shopping cart micro-frontend"
register_port "account-mfe" 3130 "mfe" "User account micro-frontend"
register_port "orders-mfe" 3140 "mfe" "Orders management micro-frontend"
register_port "admin-catalog-mfe" 3150 "mfe" "Admin catalog micro-frontend"
register_port "admin-orders-mfe" 3160 "mfe" "Admin orders micro-frontend"
register_port "admin-customers-mfe" 3170 "mfe" "Admin customers micro-frontend"

# BFFs (3200-3299)
register_port "admin-bff" 3200 "bff" "Admin backend-for-frontend"
register_port "storefront-bff" 3210 "bff" "Storefront backend-for-frontend"
register_port "partner-bff" 3220 "bff" "Partner backend-for-frontend"
register_port "mobile-bff" 3230 "bff" "Mobile app backend-for-frontend"

# Domain Services (3400-3499)
# Customer Domain (3410-3419)
register_port "customer-api" 3410 "domain" "Customer domain API"
register_port "customer-command" 3411 "domain" "Customer command/write service"

# Product/Catalog Domain (3420-3429)
register_port "product-api" 3420 "domain" "Product/catalog API"
register_port "pricing-api" 3421 "domain" "Pricing service"

# Order Management Domain (3430-3439)
register_port "orders-api" 3430 "domain" "Orders API"
register_port "fulfillment-api" 3431 "domain" "Fulfillment/shipping service"

# Payments/Billing Domain (3440-3449)
register_port "payment-api" 3440 "domain" "Payment orchestration"
register_port "invoicing-api" 3441 "domain" "Invoicing service"

# Inventory Domain (3450-3459)
register_port "inventory-api" 3450 "domain" "Inventory service"
register_port "warehouse-api" 3451 "domain" "Warehouse allocation"

# ESB/Integration Layer (3500-3599)
register_port "esb-router" 3500 "esb" "ESB HTTP router"
register_port "event-ingress" 3510 "esb" "Event ingress adapter"
register_port "erp-integration" 3520 "esb" "ERP outbound integration"
register_port "crm-integration" 3530 "esb" "CRM outbound integration"
register_port "search-indexer" 3540 "esb" "Search indexer"
register_port "analytics-etl" 3550 "esb" "Analytics ETL/data pump"

# Shared/Cross-Cutting Services (3600-3699)
register_port "auth-iam" 3600 "shared" "Auth/IAM service"
register_port "notification" 3610 "shared" "Notification service"
register_port "media" 3620 "shared" "File/media service"
register_port "config" 3630 "shared" "Configuration/feature flags"
register_port "audit" 3640 "shared" "Audit/compliance API"

# Observability/Internal Tools (3700-3799)
register_port "ops-dashboard" 3700 "observability" "Internal ops dashboard"
register_port "tracing-ui" 3710 "observability" "Tracing UI"
register_port "metrics-dashboard" 3720 "observability" "Metrics dashboard"
register_port "log-viewer" 3730 "observability" "Log viewer/search"

# Infrastructure Services
register_port "postgres" 5432 "infrastructure" "PostgreSQL database"
register_port "redis" 6379 "infrastructure" "Redis cache"
register_port "rabbitmq" 5672 "infrastructure" "RabbitMQ AMQP"
register_port "rabbitmq-mgmt" 15672 "infrastructure" "RabbitMQ management UI"
register_port "consul" 8500 "infrastructure" "Consul HTTP API/UI"
register_port "consul-dns" 8600 "infrastructure" "Consul DNS"
register_port "vault" 8200 "infrastructure" "Vault API/UI"

echo ""
echo "📝 Registering Service Configurations..."
echo "========================================"

# Admin service config
register_service_config "admin-service" '{
  "name": "admin-service",
  "port": 3600,
  "database": "fabrica-admin-db",
  "vault_path": "fabrica/admin",
  "features": ["auth", "rbac", "oauth"],
  "dependencies": ["postgres", "redis", "vault", "rabbitmq"]
}'

# Customer service config
register_service_config "customer-service" '{
  "name": "customer-service",
  "port": 3410,
  "database": "fabrica-customer-db",
  "vault_path": "fabrica/customer",
  "features": ["profiles", "addresses", "preferences"],
  "dependencies": ["postgres", "redis", "rabbitmq"]
}'

# Product service config
register_service_config "product-service" '{
  "name": "product-service",
  "port": 3420,
  "database": "fabrica-product-db",
  "vault_path": "fabrica/product",
  "features": ["catalog", "inventory", "pricing"],
  "dependencies": ["postgres", "redis", "rabbitmq"]
}'

echo ""
echo "✅ Consul initialization complete!"
echo ""
echo "🌐 Access Consul UI: http://localhost:8500"
echo "   - Navigate to 'Key/Value' to see port registry"
echo "   - Path: fabrica/ports/"
echo ""
