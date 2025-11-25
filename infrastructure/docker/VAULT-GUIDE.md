# HashiCorp Vault Guide for Fabrica Commerce Cloud

## Overview

Vault is used to securely store and manage secrets across all Fabrica services.

## Architecture

### Secret Organization

```
fabrica/
├── shared/              # Shared across all services
│   ├── database         # PostgreSQL connection info
│   ├── rabbitmq         # RabbitMQ connection info
│   ├── redis            # Redis connection info
│   └── consul           # Consul connection info
├── admin/               # Admin service secrets
│   ├── database         # Admin DB credentials
│   ├── jwt              # JWT signing keys
│   └── oauth            # OAuth credentials
├── customer/            # Customer service secrets
│   └── database         # Customer DB credentials
└── product/             # Product service secrets
    └── database         # Product DB credentials
```

## Authentication Methods

### 1. **AppRole (Recommended for Services)**

Each service gets its own AppRole with specific permissions:

```
admin-service     → admin-service-policy + shared-secrets-policy
customer-service  → customer-service-policy + shared-secrets-policy
product-service   → product-service-policy + shared-secrets-policy
```

### 2. **Token (For Manual Access)**

Use the root token for administration: `fabrica_dev_root_token`

## Setup Instructions

### 1. Initialize Vault

Run the initialization script:

```bash
cd infrastructure/docker
chmod +x vault-init.sh
./vault-init.sh
```

This will:
- Enable AppRole authentication
- Create policies for each service
- Store initial secrets
- Generate AppRole credentials

### 2. Configure Services

Add the generated credentials to your service's `.env` file:

```env
# From vault-init.sh output
VAULT_ADDR=http://vault:8200
VAULT_ROLE_ID=<role-id-from-script>
VAULT_SECRET_ID=<secret-id-from-script>
```

### 3. Use in Code

```javascript
const VaultClient = require('./vault-client');

const vault = new VaultClient();

// Get database config
const dbConfig = await vault.getDatabaseConfig('admin');

// Get specific secret
const jwtConfig = await vault.getSecret('fabrica/data/admin/jwt');
```

## Answer to Your Questions

### Should I create groups or entities directly?

**For Fabrica, use AppRoles instead of Entities/Groups:**

1. **AppRoles** are designed for machine/service authentication
2. **Entities** are for tracking identity across multiple auth methods
3. **Groups** are for organizing entities with shared policies

**For our use case:**
- Each service = One AppRole
- Each AppRole = Specific policies
- No need for entities/groups with AppRole auth

### How do we access from code?

**AppRole Authentication Flow:**

```
1. Service starts with ROLE_ID + SECRET_ID (from env vars)
   ↓
2. Service authenticates to Vault
   POST /v1/auth/approle/login
   ↓
3. Vault returns a client TOKEN
   ↓
4. Service uses TOKEN to read secrets
   GET /v1/fabrica/data/admin/database
   Headers: X-Vault-Token: <token>
   ↓
5. Service renews token before expiry
```

**In Practice:**

```javascript
// Service startup
const vault = new VaultClient();
// Automatically authenticates using env vars:
// - VAULT_ROLE_ID
// - VAULT_SECRET_ID

// Read secrets
const dbCreds = await vault.getDatabaseConfig('admin');
// { host: 'postgres', port: 5432, database: 'fabrica-admin-db', ... }

// Token renewal is automatic
```

## Best Practices

### 1. **Secret Paths**

✅ **Good:**
```
fabrica/data/admin/database
fabrica/data/shared/rabbitmq
```

❌ **Bad:**
```
admin_database  # No organization
secrets/db      # Too generic
```

### 2. **Policy Design**

✅ **Principle of Least Privilege:**
```hcl
# Admin service can only access admin/* and shared/*
path "fabrica/data/admin/*" {
  capabilities = ["create", "read", "update", "delete", "list"]
}
path "fabrica/data/shared/*" {
  capabilities = ["read", "list"]  # Read-only for shared
}
```

❌ **Too Permissive:**
```hcl
path "fabrica/*" {
  capabilities = ["*"]  # Don't do this!
}
```

### 3. **Credential Rotation**

- AppRole secret IDs should be rotated regularly
- Use Vault's dynamic database credentials in production
- Tokens auto-renew but have max TTL

### 4. **Secret Versioning**

KV v2 keeps secret versions:

```bash
# View secret history
vault kv metadata get fabrica/admin/database

# Get specific version
vault kv get -version=2 fabrica/admin/database

# Rollback
vault kv rollback -version=1 fabrica/admin/database
```

## Manual Vault Access (via UI)

### Access Vault UI

1. Open http://localhost:8200
2. Login with Token: `fabrica_dev_root_token`

### View Secrets

1. Click "Secrets" in sidebar
2. Navigate to `fabrica/`
3. Browse secrets by service

### Create New Secret

1. Navigate to desired path (e.g., `fabrica/admin/`)
2. Click "Create secret +"
3. Enter key-value pairs
4. Click "Save"

## Common Operations

### Add a new service

1. **Create policy:**
```bash
vault policy write order-service-policy - <<EOF
path "fabrica/data/order/*" {
  capabilities = ["create", "read", "update", "delete", "list"]
}
path "fabrica/data/shared/*" {
  capabilities = ["read", "list"]
}
EOF
```

2. **Create AppRole:**
```bash
vault write auth/approle/role/order-service \
  token_policies="order-service-policy,shared-secrets-policy" \
  token_ttl=1h \
  token_max_ttl=4h
```

3. **Get credentials:**
```bash
vault read auth/approle/role/order-service/role-id
vault write -f auth/approle/role/order-service/secret-id
```

4. **Add to service .env:**
```env
VAULT_ROLE_ID=<from-above>
VAULT_SECRET_ID=<from-above>
```

### Update a secret

```bash
vault kv put fabrica/admin/database \
  name="fabrica-admin-db" \
  username="new_user" \
  password="new_password"
```

### Delete a secret

```bash
vault kv delete fabrica/admin/oauth
```

### Revoke a token

```bash
vault token revoke <token>
```

## Production Considerations

**Current Dev Mode Limitations:**
- ❌ Data stored in memory (lost on restart)
- ❌ Auto-unsealed (insecure)
- ❌ Root token in env var
- ❌ No TLS

**For Production:**
- ✅ Use persistent storage backend
- ✅ Implement proper seal/unseal process
- ✅ Use TLS for all connections
- ✅ Enable audit logging
- ✅ Use dynamic database credentials
- ✅ Implement secret rotation
- ✅ Configure HA cluster

## Troubleshooting

### "Permission Denied" errors

Check policy assignments:
```bash
vault token capabilities fabrica/data/admin/database
```

### Token expired

Re-authenticate:
```javascript
await vault.authenticate();
```

### Vault sealed

Unseal Vault (production):
```bash
vault operator unseal
```

Dev mode auto-unseals on restart.

## Resources

- [Vault AppRole Auth](https://www.vaultproject.io/docs/auth/approle)
- [Vault Policies](https://www.vaultproject.io/docs/concepts/policies)
- [KV Secrets Engine](https://www.vaultproject.io/docs/secrets/kv/kv-v2)
