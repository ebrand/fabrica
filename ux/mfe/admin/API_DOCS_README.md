# API Documentation Feature

This admin MFE now includes an API Documentation page that displays Swagger/OpenAPI documentation for all Fabrica domain services through the Admin BFF.

## Architecture

The API documentation follows the **Backend-for-Frontend (BFF)** pattern:

```
Admin MFE (React)
    ↓
Admin BFF (Node.js) - Port 3200
    ↓
Domain Services (.NET/Node.js)
    ↓
Swagger/OpenAPI JSON
```

**Benefits:**
- Single point of entry for all API documentation
- Domain services don't need to be directly accessible from the browser
- Centralized service registry
- Easier to add authentication/authorization later
- Simplified CORS configuration

## Features

- **Multi-Service Support**: View API documentation for all domain services from a single page
- **Dynamic Service Discovery**: Services are loaded from the BFF's service registry
- **Interactive Testing**: Use Swagger UI to test API endpoints directly from the browser
- **Tab Navigation**: Switch between User Management and API Documentation pages
- **Reusable Components**: Easy to add new services to the documentation page

## Current Services

1. **Admin Domain Service** (Port 3600)
   - User management
   - Authentication and authorization
   - BFF Proxy: http://localhost:3200/api/docs/swagger/admin

## Adding New Services

To add documentation for a new domain service:

1. **Update the BFF Service Registry** (`ux/bff/admin-bff/services-config.js`):

```javascript
export const DOMAIN_SERVICES = {
  admin: {
    id: 'admin',
    name: 'Admin Domain Service',
    description: 'User management, authentication, and authorization',
    baseUrl: process.env.ADMIN_SERVICE_URL || 'http://acl-admin:3600',
    swaggerPath: '/swagger/v1/swagger.json',
    port: 3600
  },
  catalog: {
    id: 'catalog',
    name: 'Catalog Domain Service',
    description: 'Product catalog and inventory management',
    baseUrl: process.env.CATALOG_SERVICE_URL || 'http://catalog-service:3601',
    swaggerPath: '/swagger/v1/swagger.json',
    port: 3601
  }
  // Add more services here...
};
```

2. **Rebuild and restart the BFF**:

```bash
cd infrastructure/docker
docker-compose -f fabrica-compose.yml -f infrastructure-compose.yml -f ux-compose.yml build bff-admin
docker rm -f bff-admin
docker-compose -f fabrica-compose.yml -f infrastructure-compose.yml -f ux-compose.yml up -d bff-admin
```

The admin-mfe will automatically discover new services from the BFF's `/api/docs/services` endpoint.

## Requirements

Each domain service must:
1. Have Swagger/Swashbuckle installed and configured
2. Enable Swagger in Development environment
3. Expose the Swagger JSON endpoint at `/swagger/v1/swagger.json`
4. Be accessible from the BFF container (network connectivity)

## Usage

1. Navigate to the admin-mfe: http://localhost:3100
2. Click on the "API Documentation" tab
3. Select a service from the available buttons
4. Browse and test the API endpoints using Swagger UI

## Components

- **SwaggerViewer** (`src/components/SwaggerViewer.jsx`): Reusable component for displaying Swagger UI
- **ApiDocumentation** (`src/pages/ApiDocumentation.jsx`): Main page with service selector
- **UserManagement** (`src/pages/UserManagement.jsx`): Existing user management functionality
