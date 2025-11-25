# Secrets Management with HashiCorp Vault

This document explains how Fabrica Commerce Cloud manages secrets using HashiCorp Vault.

## Overview

All sensitive credentials are stored in HashiCorp Vault and accessed at runtime. This includes:

- **Infrastructure credentials**: PostgreSQL, RabbitMQ, Redis
- **OAuth credentials**: Stytch, Google OAuth
- **Application secrets**: JWT secrets, API keys

## Architecture

```
┌─────────────────┐
│  HashiCorp Vault│  ← Secrets stored here
└────────┬────────┘
         │
         ├──────────────────┐
         │                  │
    ┌────▼─────┐      ┌────▼──────────┐
    │  Docker  │      │  Config Server│
    │Containers│      │  (Port 3630)  │
    └──────────┘      └────┬──────────┘
                           │
                      ┌────▼─────┐
                      │  Admin   │
                      │  Shell   │
                      └──────────┘
```

## Secrets Storage Locations in Vault

### Infrastructure Credentials
- `fabrica/infrastructure/postgres` - PostgreSQL username/password
- `fabrica/infrastructure/rabbitmq` - RabbitMQ username/password
- `fabrica/infrastructure/redis` - Redis password

### Application Secrets
- `fabrica/admin/stytch` - Stytch credentials (project_id, secret, public_token, project_domain)
- `fabrica/admin/oauth/google` - Google OAuth (client_id, client_secret, redirect_uri)
- `fabrica/admin/jwt` - JWT configuration

### Shared Configuration
- `fabrica/shared/database` - Database connection details
- `fabrica/shared/rabbitmq` - RabbitMQ connection details
- `fabrica/shared/redis` - Redis connection details
- `fabrica/shared/consul` - Consul connection details

## Workflows

### Local Development (Quick Start)

For local development, you can use the `.env` file with fallback values:

```bash
# Start infrastructure
docker-compose -f fabrica-compose.yml up
```

The `.env` file contains safe default values for local development only.

### Production/Secure Mode

For production or secure development, fetch secrets from Vault:

```bash
# 1. Ensure Vault is running and initialized
docker-compose -f fabrica-compose.yml up vault

# 2. Initialize Vault with secrets (first time only)
./vault-init.sh

# 3. Store your production secrets in Vault
docker exec -e VAULT_TOKEN=fabrica_dev_root_token vault vault kv put \
  fabrica/infrastructure/postgres \
  username=your_username \
  password=your_password

# 4. Generate .env.vault from Vault
./vault-to-env.sh

# 5. Start services with Vault-sourced credentials
docker-compose --env-file .env.vault -f fabrica-compose.yml up
```

### Storing New Secrets

```bash
# Infrastructure secret
docker exec -e VAULT_TOKEN=fabrica_dev_root_token vault \
  vault kv put fabrica/infrastructure/postgres \
  username=myuser \
  password=mypassword

# Application secret
docker exec -e VAULT_TOKEN=fabrica_dev_root_token vault \
  vault kv put fabrica/admin/stytch \
  project_id=project-test-xxx \
  secret=secret-test-xxx \
  public_token=public-token-test-xxx \
  project_domain=https://your-domain.stytch.dev
```

### Reading Secrets

```bash
# List all secrets
docker exec -e VAULT_TOKEN=fabrica_dev_root_token vault \
  vault kv list fabrica/infrastructure

# Read specific secret
docker exec -e VAULT_TOKEN=fabrica_dev_root_token vault \
  vault kv get fabrica/infrastructure/postgres

# Read specific field
docker exec -e VAULT_TOKEN=fabrica_dev_root_token vault \
  vault kv get -field=username fabrica/infrastructure/postgres
```

## Application Access Patterns

### Frontend Applications (Admin Shell)

Frontend apps access secrets via the **Config Server** (port 3630):

```javascript
// src/services/config.js
const response = await fetch('http://localhost:3630/api/config/auth');
const config = await response.json();
// { stytch: { publicToken: "..." }, google: { clientId: "..." } }
```

The Config Server fetches secrets from Vault server-side and exposes only public/frontend-safe values.

### Backend Services

Backend services use **AppRole authentication** to access Vault directly:

```javascript
// Using VAULT_ROLE_ID and VAULT_SECRET_ID from vault-init.sh
const vaultClient = new VaultClient({
  endpoint: 'http://vault:8200',
  roleId: process.env.VAULT_ROLE_ID,
  secretId: process.env.VAULT_SECRET_ID
});

const dbCreds = await vaultClient.read('fabrica/infrastructure/postgres');
```

## Security Best Practices

1. **Never commit secrets to git**
   - `.env` contains fallback values only
   - `.env.vault` is gitignored
   - Real secrets stored only in Vault

2. **Use AppRoles for service authentication**
   - Each service has its own AppRole with limited permissions
   - Services authenticate to Vault using role_id + secret_id

3. **Rotate secrets regularly**
   ```bash
   # Update secret in Vault
   vault kv put fabrica/infrastructure/postgres password=new_password

   # Regenerate .env.vault
   ./vault-to-env.sh

   # Restart services
   docker-compose restart
   ```

4. **Principle of least privilege**
   - Services can only access secrets they need
   - Policies defined in `vault-init.sh`

## Files

- **`.env`** - Fallback values for local development (safe to commit)
- **`.env.example`** - Template for new developers (safe to commit)
- **`.env.vault`** - Auto-generated from Vault (NEVER commit - in .gitignore)
- **`vault-init.sh`** - Initialize Vault with policies and default secrets
- **`vault-to-env.sh`** - Fetch secrets from Vault and generate .env.vault

## Troubleshooting

### "Vault is sealed"
```bash
# In dev mode, Vault should auto-unseal. If not, restart:
docker-compose restart vault
```

### "Permission denied"
```bash
# Check your VAULT_TOKEN
export VAULT_TOKEN=fabrica_dev_root_token

# Verify you can access Vault
vault status
```

### "Secret not found"
```bash
# Re-run initialization
./vault-init.sh

# Or manually create the secret
vault kv put fabrica/infrastructure/postgres username=user password=pass
```

## Migration from .env to Vault

If you have existing secrets in `.env`:

```bash
# 1. Store secrets in Vault
source .env
docker exec -e VAULT_TOKEN=fabrica_dev_root_token vault \
  vault kv put fabrica/infrastructure/postgres \
  username=$POSTGRES_USER \
  password=$POSTGRES_PASSWORD

# 2. Generate .env.vault
./vault-to-env.sh

# 3. Test with Vault-sourced secrets
docker-compose --env-file .env.vault up

# 4. Once verified, remove secrets from .env (keep only fallback values)
```

## Additional Resources

- [HashiCorp Vault Documentation](https://www.vaultproject.io/docs)
- [AppRole Authentication](https://www.vaultproject.io/docs/auth/approle)
- [KV Secrets Engine](https://www.vaultproject.io/docs/secrets/kv/kv-v2)
