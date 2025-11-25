# Micro-Frontend (MFE) Deployment Guide

## Standard Operating Procedure for MFE Services

This guide establishes the standard configuration for all Micro-Frontend (MFE) services in the Fabrica Commerce Cloud platform.

## Table of Contents

1. [CORS Configuration](#cors-configuration)
2. [Dockerfile Template](#dockerfile-template)
3. [Vite Configuration](#vite-configuration)
4. [Module Federation Setup](#module-federation-setup)

---

## CORS Configuration

### Why CORS is Required

Module Federation requires MFE services to be accessible from different origins (host applications). Without proper CORS headers, browsers will block the cross-origin requests needed for Module Federation to work.

### Standard CORS Headers

All MFE services MUST include the following CORS headers in their nginx configuration:

```nginx
add_header "Access-Control-Allow-Origin" "*" always;
add_header "Access-Control-Allow-Methods" "GET, OPTIONS" always;
add_header "Access-Control-Allow-Headers" "Content-Type, Authorization" always;
```

### Preflight Request Handling

All MFE services MUST handle OPTIONS preflight requests:

```nginx
if ($request_method = OPTIONS) {
    return 204;
}
```

---

## Dockerfile Template

### Standard MFE Dockerfile

Use this template for all MFE services. Replace `${PORT}` with your service's port number.

```dockerfile
FROM node:20-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source files
COPY . .

# Build for production
RUN npm run build

# Production stage with nginx
FROM nginx:alpine

# Copy built files
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx config with CORS support for Module Federation
RUN echo 'server { \
    listen ${PORT}; \
    location / { \
        root /usr/share/nginx/html; \
        try_files $uri /index.html; \
        \
        # CORS headers for Module Federation \
        add_header "Access-Control-Allow-Origin" "*" always; \
        add_header "Access-Control-Allow-Methods" "GET, OPTIONS" always; \
        add_header "Access-Control-Allow-Headers" "Content-Type, Authorization" always; \
        \
        # Handle preflight requests \
        if ($request_method = OPTIONS) { \
            return 204; \
        } \
    } \
}' > /etc/nginx/conf.d/default.conf

# Expose port
EXPOSE ${PORT}

CMD ["nginx", "-g", "daemon off;"]
```

---

## Vite Configuration

### MFE Service (Remote)

Services that expose components via Module Federation:

```javascript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'yourMfeName',
      filename: 'remoteEntry.js',
      // Expose components
      exposes: {
        './ComponentName': './src/ComponentPath.jsx'
      },
      shared: ['react', 'react-dom']
    })
  ],
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false
  }
})
```

### Shell Application (Host)

Applications that consume MFE components:

```javascript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'yourShellName',
      remotes: {
        remoteName: 'http://localhost:${PORT}/assets/remoteEntry.js'
      },
      shared: ['react', 'react-dom']
    })
  ],
  build: {
    modulePreload: false,
    target: 'esnext',
    minify: false,
    cssCodeSplit: false
  }
})
```

---

## Module Federation Setup

### 1. Install Dependencies

Both host and remote applications need the Module Federation plugin:

```bash
npm install @originjs/vite-plugin-federation --save-dev
```

### 2. Configure Remote (MFE Service)

Update `vite.config.js` to expose components (see [Vite Configuration](#vite-configuration) above).

### 3. Configure Host (Shell Application)

Update `vite.config.js` to consume remote modules (see [Vite Configuration](#vite-configuration) above).

### 4. Load Remote Component in Host

Use React.lazy() and Suspense for dynamic imports:

```jsx
import { lazy, Suspense } from 'react';

const RemoteComponent = lazy(() => import('remoteName/ComponentName'));

function YourPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <RemoteComponent />
    </Suspense>
  );
}
```

---

## Verification Checklist

Before deploying a new MFE service, verify:

- [ ] CORS headers are configured in nginx
- [ ] Preflight OPTIONS requests return 204
- [ ] remoteEntry.js is accessible at `/assets/remoteEntry.js`
- [ ] CORS headers are present in HTTP response (check with `curl -I`)
- [ ] Module Federation plugin is configured in vite.config.js
- [ ] Components are properly exposed (remote) or imported (host)
- [ ] Shared dependencies (react, react-dom) are configured
- [ ] Build settings include modulePreload: false

---

## Testing CORS Configuration

Verify CORS headers are correctly set:

```bash
curl -I http://localhost:${PORT}/assets/remoteEntry.js
```

Expected headers in response:
```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
```

---

## Port Mapping

Refer to the project's PORTS.md file for standard port assignments.

---

## Common Issues

### Issue: CORS errors in browser console

**Solution**: Verify nginx CORS headers are configured and container has been rebuilt with `--no-cache` flag.

### Issue: remoteEntry.js returns 404

**Solution**: Ensure Vite build has completed and files are in `/usr/share/nginx/html` in the container.

### Issue: Module Federation not loading components

**Solution**:
1. Check browser network tab for failed requests
2. Verify remote URL matches the exposed port
3. Ensure shared dependencies versions are compatible
4. Check component is correctly exposed in remote's vite.config.js

---

## References

- [@originjs/vite-plugin-federation](https://github.com/originjs/vite-plugin-federation)
- [Module Federation Documentation](https://module-federation.io/)
- [Nginx CORS Configuration](https://enable-cors.org/server_nginx.html)
