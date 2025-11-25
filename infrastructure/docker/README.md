# Fabrica Commerce Cloud - Docker Infrastructure

This directory contains the Docker Compose configuration for the Fabrica Commerce Cloud platform.

## Architecture

The infrastructure is organized using a modular approach with a master compose file that includes domain-specific compose files:

- **fabrica-compose.yml** - Master compose file that orchestrates all services
- **infrastructure-compose.yml** - Shared infrastructure (PostgreSQL, Redis, RabbitMQ, Consul)
- **{domain}-compose.yml** - Domain-specific services (admin, customer, product, etc.)

## Prerequisites

- Docker Desktop or Docker Engine (with Docker Compose V2)
- Minimum 8GB RAM allocated to Docker
- Available ports: 3000-3799, 5432, 5672, 6379, 8500, 15672

## Quick Start

### 1. Set up environment variables

```bash
cd infrastructure/docker
cp .env.example .env
```

Edit `.env` with your configuration (the defaults work for local development).

### 2. Create the Docker network

```bash
docker network create fabrica_fabrica-network
```

### 3. Start infrastructure services only

```bash
docker compose -f fabrica-compose.yml up -d postgres redis rabbitmq consul
```

Wait for health checks to pass:

```bash
docker compose -f fabrica-compose.yml ps
```

### 4. Start a specific domain

For example, to start the Admin domain:

```bash
docker compose -f fabrica-compose.yml up -d admin-db-init acl-admin esb-admin-consumer esb-admin-producer
```

### 5. Start all services

```bash
docker compose -f fabrica-compose.yml up -d
```

## Managing Services

### View running services

```bash
docker compose -f fabrica-compose.yml ps
```

### View logs

```bash
# All services
docker compose -f fabrica-compose.yml logs -f

# Specific service
docker compose -f fabrica-compose.yml logs -f acl-admin

# Multiple services
docker compose -f fabrica-compose.yml logs -f postgres rabbitmq
```

### Stop services

```bash
# Stop all
docker compose -f fabrica-compose.yml down

# Stop specific domain (keeps infrastructure running)
docker compose -f fabrica-compose.yml stop acl-admin esb-admin-consumer esb-admin-producer

# Stop and remove volumes (WARNING: deletes all data)
docker compose -f fabrica-compose.yml down -v
```

### Restart a service

```bash
docker compose -f fabrica-compose.yml restart acl-admin
```

### Rebuild a service

```bash
# Rebuild and restart
docker compose -f fabrica-compose.yml up -d --build acl-admin

# Rebuild with no cache
docker compose -f fabrica-compose.yml build --no-cache acl-admin
docker compose -f fabrica-compose.yml up -d acl-admin
```

## Service URLs

### Infrastructure

- **PostgreSQL**: `localhost:5432`
- **Redis**: `localhost:6379`
- **RabbitMQ AMQP**: `localhost:5672`
- **RabbitMQ Management UI**: `http://localhost:15672`
- **Consul UI**: `http://localhost:8500`

### Domain Services (ACL APIs)

- **Admin (Auth/IAM)**: `http://localhost:3600`
- **Customer**: `http://localhost:3410`
- **Product**: `http://localhost:3420`
- **Order Management**: `http://localhost:3430`

See `/docs/PORTS.md` for complete port allocation scheme.

## Database Access

### Connect to PostgreSQL

```bash
docker exec -it postgres psql -U fabrica_admin -d postgres
```

### List all databases

```sql
\l
```

### Connect to a domain database

```sql
\c fabrica-admin-db
\dn  -- List schemas
\dt fabrica.*  -- List tables in fabrica schema
\dt cdc.*      -- List tables in cdc schema
```

## Troubleshooting

### Service won't start

1. Check logs:
   ```bash
   docker compose -f fabrica-compose.yml logs service-name
   ```

2. Verify dependencies are healthy:
   ```bash
   docker compose -f fabrica-compose.yml ps
   ```

3. Rebuild the service:
   ```bash
   docker compose -f fabrica-compose.yml up -d --build service-name
   ```

### Port conflicts

If you see port binding errors, check what's using the port:

```bash
# macOS/Linux
lsof -i :3600

# Kill the process or change the port mapping in the compose file
```

### Database connection issues

1. Verify PostgreSQL is healthy:
   ```bash
   docker compose -f fabrica-compose.yml ps postgres
   ```

2. Test connection:
   ```bash
   docker exec -it postgres pg_isready -U fabrica_admin
   ```

3. Check database exists:
   ```bash
   docker exec -it postgres psql -U fabrica_admin -c "\l"
   ```

### RabbitMQ connection issues

1. Access management UI: `http://localhost:15672` (default credentials in `.env`)
2. Check exchanges and queues are created
3. Verify service can connect to RabbitMQ

### Reset everything

```bash
# Stop all services and remove volumes
docker compose -f fabrica-compose.yml down -v

# Remove network
docker network rm fabrica_fabrica-network

# Start fresh
docker network create fabrica_fabrica-network
docker compose -f fabrica-compose.yml up -d
```

## Development Workflow

### Hot reload / Live development

Services are configured with volume mounts for live development:

```yaml
volumes:
  - ../../domain/admin/acl/AdminDomainService:/app
  - /app/node_modules
```

Changes to source code will automatically reload the service (if your framework supports it).

### Adding a new domain

1. Create domain-specific compose file (e.g., `inventory-compose.yml`)
2. Add reference in `fabrica-compose.yml` include section
3. Follow the pattern from existing domain compose files

### Running individual domain compose files

While not recommended, you can run individual compose files:

```bash
docker compose -f infrastructure-compose.yml up -d
docker compose -f admin-compose.yml up -d
```

However, the master `fabrica-compose.yml` approach is preferred for consistency.

## Container Naming Convention

All Fabrica containers follow this naming pattern:

- Infrastructure: `postgres`, `redis`, `rabbitmq`, `consul`
- Domain services: `{layer}-{domain}` (e.g., `acl-admin`, `acl-customer`)
- ESB services: `esb-{domain}-{consumer|producer}`
- Database init: `{domain}-db-init`

## Network

All services communicate via the `fabrica_fabrica-network` bridge network, enabling:

- Service discovery by container name
- Isolation from other Docker projects
- Inter-service communication without exposing ports to host

## Next Steps

- Configure OAuth providers in `.env`
- Set up database migrations for each domain
- Implement domain-specific business logic
- Configure API gateway routing
- Set up observability stack (tracing, metrics, logging)
