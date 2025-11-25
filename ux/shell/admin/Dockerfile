# Build stage
FROM node:20-alpine AS builder

# Set working directory to match local relative paths
WORKDIR /app/shell/admin

# Copy package files for shell-admin
COPY shell/admin/package*.json ./

# Install dependencies
RUN npm ci

# Copy shell-admin source code
COPY shell/admin/ .

# Copy common MFE safelist (needed for Tailwind CSS generation)
# This preserves the relative path ../../mfe/common/ from shell/admin
COPY mfe/common/common.safelist.js /app/mfe/common/

# Build the application
RUN npm run build

# Production stage
FROM nginx:alpine

# Install curl for healthcheck
RUN apk add --no-cache curl

# Copy built assets from builder stage
COPY --from=builder /app/shell/admin/dist /usr/share/nginx/html

# Copy nginx configuration
COPY shell/admin/nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 3001
EXPOSE 3001

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
