#!/bin/bash

# Change to docker directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.." || exit 1

################################################################################
# Deploy Product UI Components (BFF + MFE)
#
# This script deploys the Product BFF and MFE services
################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Docker compose files
COMPOSE_FILES="-f fabrica-compose.yml -f infrastructure-compose.yml -f admin-compose.yml -f product-compose.yml -f ux-compose.yml"

echo -e "${BLUE}════════════════════════════════════════${NC}"
echo -e "${BLUE}  Deploy Product UI Components${NC}"
echo -e "${BLUE}════════════════════════════════════════${NC}"
echo ""

# Step 1: Build Product BFF
echo -e "${YELLOW}[1/4] Building Product BFF...${NC}"
docker compose $COMPOSE_FILES build --no-cache bff-product

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed for Product BFF${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Product BFF built${NC}"
echo ""

# Step 2: Build Product MFE
echo -e "${YELLOW}[2/4] Building Product MFE...${NC}"
docker compose $COMPOSE_FILES build --no-cache mfe-product

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed for Product MFE${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Product MFE built${NC}"
echo ""

# Step 3: Start Product BFF
echo -e "${YELLOW}[3/4] Starting Product BFF...${NC}"
docker compose $COMPOSE_FILES up -d bff-product

if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to start Product BFF${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Product BFF started${NC}"
echo ""

# Step 4: Start Product MFE
echo -e "${YELLOW}[4/4] Starting Product MFE...${NC}"
docker compose $COMPOSE_FILES up -d mfe-product

if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to start Product MFE${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Product MFE started${NC}"
echo ""

# Show service status
echo -e "${YELLOW}Service status:${NC}"
docker compose $COMPOSE_FILES ps bff-product mfe-product
echo ""

echo -e "${GREEN}════════════════════════════════════════${NC}"
echo -e "${GREEN}  Product UI Deployed Successfully!${NC}"
echo -e "${GREEN}════════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}Services available at:${NC}"
echo -e "  • Product BFF:  http://localhost:3220"
echo -e "  • Product MFE:  http://localhost:3110"
echo ""
echo -e "${BLUE}Useful commands:${NC}"
echo "  • View BFF logs:  docker compose $COMPOSE_FILES logs -f bff-product"
echo "  • View MFE logs:  docker compose $COMPOSE_FILES logs -f mfe-product"
echo "  • Restart BFF:    docker compose $COMPOSE_FILES restart bff-product"
echo "  • Restart MFE:    docker compose $COMPOSE_FILES restart mfe-product"
echo ""
