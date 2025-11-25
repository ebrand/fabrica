#!/bin/bash

# Change to docker directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.." || exit 1

################################################################################
# Deploy Product Domain Service
#
# This script deploys the Product domain (database + API service)
################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Docker compose files
COMPOSE_FILES="-f fabrica-compose.yml -f infrastructure-compose.yml -f admin-compose.yml -f product-compose.yml"

echo -e "${BLUE}════════════════════════════════════════${NC}"
echo -e "${BLUE}  Deploy Product Domain Service${NC}"
echo -e "${BLUE}════════════════════════════════════════${NC}"
echo ""

# Step 1: Initialize Product Database
echo -e "${YELLOW}[1/3] Initializing Product database...${NC}"
docker compose $COMPOSE_FILES up -d product-db-init

if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to initialize database${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Database initialization started${NC}"
echo ""

# Wait for database init to complete
echo -e "${YELLOW}Waiting for database initialization to complete...${NC}"
sleep 5

# Step 2: Build Product Service
echo -e "${YELLOW}[2/3] Building Product Domain Service...${NC}"
docker compose $COMPOSE_FILES build --no-cache acl-product

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Build completed${NC}"
echo ""

# Step 3: Start Product Service
echo -e "${YELLOW}[3/3] Starting Product Domain Service...${NC}"
docker compose $COMPOSE_FILES up -d acl-product

if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to start service${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Service started${NC}"
echo ""

# Show service status
echo -e "${YELLOW}Service status:${NC}"
docker compose $COMPOSE_FILES ps acl-product
echo ""

echo -e "${GREEN}════════════════════════════════════════${NC}"
echo -e "${GREEN}  Product Domain Deployed Successfully!${NC}"
echo -e "${GREEN}════════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}Product API available at:${NC} http://localhost:3420"
echo -e "${BLUE}Swagger UI:${NC} http://localhost:3420/swagger"
echo ""
echo -e "${BLUE}Useful commands:${NC}"
echo "  • View logs:    docker compose $COMPOSE_FILES logs -f acl-product"
echo "  • Restart:      docker compose $COMPOSE_FILES restart acl-product"
echo "  • Stop:         docker compose $COMPOSE_FILES stop acl-product"
echo ""
