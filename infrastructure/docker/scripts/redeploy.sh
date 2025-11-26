#!/bin/bash

################################################################################
# Find and change to the infrastructure/docker directory
# This allows the script to be run from anywhere (even if copied elsewhere)
################################################################################
find_docker_dir() {
    # Method 1: Try to find git root and navigate from there
    local GIT_ROOT=$(git rev-parse --show-toplevel 2>/dev/null)
    if [ -n "$GIT_ROOT" ] && [ -d "$GIT_ROOT/infrastructure/docker" ]; then
        echo "$GIT_ROOT/infrastructure/docker"
        return 0
    fi

    # Method 2: If script is in the expected location, use relative path
    local SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" 2>/dev/null && pwd)"
    if [ -n "$SCRIPT_DIR" ] && [ -d "$SCRIPT_DIR/.." ] && [ -f "$SCRIPT_DIR/../fabrica-compose.yml" ]; then
        echo "$SCRIPT_DIR/.."
        return 0
    fi

    # Method 3: Check if we're already in the docker directory
    if [ -f "./fabrica-compose.yml" ]; then
        echo "$(pwd)"
        return 0
    fi

    # Method 4: Fallback to known absolute path
    local FALLBACK_PATH="/Users/eric.brand/Documents/source/github/Eric-Brand_swi/fabrica/infrastructure/docker"
    if [ -d "$FALLBACK_PATH" ]; then
        echo "$FALLBACK_PATH"
        return 0
    fi

    return 1
}

DOCKER_DIR=$(find_docker_dir)
if [ -z "$DOCKER_DIR" ]; then
    echo "Error: Could not find infrastructure/docker directory"
    echo "Please run this script from within the fabrica project, or ensure the project exists at the expected location."
    exit 1
fi

cd "$DOCKER_DIR" || exit 1

################################################################################
# Rebuild and Redeploy Single Component
#
# This script rebuilds a single component (MFE, BFF, Shell, or Domain service)
# with optional --no-cache and redeploys it to Docker
#
# Usage:
#   ./scripts/redeploy.sh <component> [--no-cache]
#
# Arguments:
#   component: Admin: mfe-admin | shell-admin | bff-admin | acl-admin
#              Product: mfe-product | bff-product | acl-product
#              Content: mfe-content | bff-content | acl-content
#   --no-cache: (optional) Force clean build without cache
#
# Examples:
#   ./scripts/redeploy.sh shell-admin              # Rebuild admin shell (smart caching, fast)
#   ./scripts/redeploy.sh mfe-admin                # Rebuild admin MFE (smart caching, fast)
#   ./scripts/redeploy.sh bff-admin                # Rebuild admin BFF (smart caching, fast)
#   ./scripts/redeploy.sh mfe-product              # Rebuild product MFE (smart caching, fast)
#   ./scripts/redeploy.sh bff-product              # Rebuild product BFF (smart caching, fast)
#   ./scripts/redeploy.sh acl-product              # Rebuild product domain service (smart caching, fast)
#   ./scripts/redeploy.sh acl-content              # Rebuild content domain service (smart caching, fast)
#   ./scripts/redeploy.sh bff-content              # Rebuild content BFF (smart caching, fast)
#   ./scripts/redeploy.sh mfe-content              # Rebuild content MFE (smart caching, fast)
#   ./scripts/redeploy.sh shell-admin --no-cache   # Rebuild admin shell (clean build, slow)
################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

################################################################################
# Kill Local Dev Servers
# Kills any local processes running on Fabrica port ranges to prevent conflicts
# with Docker containers. Port ranges defined in /docs/PORTS.md
################################################################################
kill_local_ports() {
    echo -e "${YELLOW}Checking for local dev servers on Fabrica ports...${NC}"

    # Define port ranges from PORTS.md
    local SHELL_PORTS="3000-3099"      # Shell UXs (Root Frontend Apps)
    local MFE_PORTS="3100-3199"        # MFEs (Micro-Frontends)
    local BFF_PORTS="3200-3299"        # BFFs (Backend-for-Frontend APIs)
    local DOMAIN_PORTS="3400-3499"     # Domain Services (DDD Bounded Contexts)
    local ESB_PORTS="3500-3599"        # ESB / Integration / Federation Layer
    local SHARED_PORTS="3600-3699"     # Shared / Cross-Cutting Services
    local OBS_PORTS="3700-3799"        # Observability / Internal Tools

    # Combine all port ranges
    local ALL_PORTS="$SHELL_PORTS,$MFE_PORTS,$BFF_PORTS,$DOMAIN_PORTS,$ESB_PORTS,$SHARED_PORTS,$OBS_PORTS"

    # Find and kill processes on these ports (excluding Docker)
    local KILLED_COUNT=0
    for PORT_RANGE in $(echo $ALL_PORTS | tr ',' ' '); do
        local START_PORT=$(echo $PORT_RANGE | cut -d'-' -f1)
        local END_PORT=$(echo $PORT_RANGE | cut -d'-' -f2)

        for PORT in $(seq $START_PORT $END_PORT); do
            # Find PIDs on this port, excluding Docker
            local PIDS=$(lsof -ti:$PORT 2>/dev/null | while read PID; do
                # Check if process is Docker (exclude it)
                if ! ps -p $PID -o comm= 2>/dev/null | grep -q "com.docker.backend"; then
                    echo $PID
                fi
            done)

            if [ ! -z "$PIDS" ]; then
                for PID in $PIDS; do
                    local PROC_NAME=$(ps -p $PID -o comm= 2>/dev/null || echo "unknown")
                    echo -e "${YELLOW}  Killing process $PID ($PROC_NAME) on port $PORT${NC}"
                    kill -9 $PID 2>/dev/null || true
                    KILLED_COUNT=$((KILLED_COUNT + 1))
                done
            fi
        done
    done

    if [ $KILLED_COUNT -eq 0 ]; then
        echo -e "${GREEN}✓ No local dev servers found${NC}"
    else
        echo -e "${GREEN}✓ Killed $KILLED_COUNT local dev server(s)${NC}"
    fi
    echo ""
}

# Validate arguments
if [ $# -lt 1 ]; then
    echo -e "${RED}Error: Missing required arguments${NC}"
    echo ""
    echo "Usage: $0 <component> [--no-cache] [--kill-ports]"
    echo ""
    echo "Arguments:"
    echo "  component:    Admin: mfe-admin | shell-admin | bff-admin | acl-admin"
    echo "                Product: mfe-product | bff-product | acl-product"
    echo "                Content: mfe-content | bff-content | acl-content"
    echo "  --no-cache:   (optional) Force clean build without cache"
    echo "  --kill-ports: (optional) Kill local dev servers on Fabrica ports before rebuild"
    echo ""
    echo "Examples:"
    echo "  $0 shell-admin                      # Rebuild admin shell (smart caching, fast)"
    echo "  $0 mfe-admin                        # Rebuild admin MFE (smart caching, fast)"
    echo "  $0 bff-admin                        # Rebuild admin BFF (smart caching, fast)"
    echo "  $0 mfe-product                      # Rebuild product MFE (smart caching, fast)"
    echo "  $0 bff-product                      # Rebuild product BFF (smart caching, fast)"
    echo "  $0 acl-product                      # Rebuild product domain service (smart caching, fast)"
    echo "  $0 acl-content                      # Rebuild content domain service (smart caching, fast)"
    echo "  $0 bff-content                      # Rebuild content BFF (smart caching, fast)"
    echo "  $0 mfe-content                      # Rebuild content MFE (smart caching, fast)"
    echo "  $0 shell-admin --no-cache           # Rebuild admin shell (clean build, slow)"
    echo "  $0 mfe-product --kill-ports         # Kill local dev servers then rebuild"
    echo "  $0 shell-admin --no-cache --kill-ports  # Clean build with port cleanup"
    exit 1
fi

COMPONENT="$1"

# Parse optional flags
NO_CACHE=""
KILL_PORTS=""
for arg in "$@"; do
    if [ "$arg" == "--no-cache" ]; then
        NO_CACHE="--no-cache"
    elif [ "$arg" == "--kill-ports" ]; then
        KILL_PORTS="true"
    fi
done

# Valid components
VALID_COMPONENTS=("mfe-admin" "shell-admin" "bff-admin" "acl-admin" "mfe-product" "bff-product" "acl-product" "acl-content" "bff-content" "mfe-content")

# Check if component is valid
if [[ ! " ${VALID_COMPONENTS[@]} " =~ " ${COMPONENT} " ]]; then
    echo -e "${RED}Error: Invalid component '${COMPONENT}'${NC}"
    echo "Valid components: ${VALID_COMPONENTS[*]}"
    exit 1
fi

# Docker compose files - determine which compose files to use based on component
if [[ "$COMPONENT" == *"product"* ]]; then
    COMPOSE_FILES="-f fabrica-compose.yml -f infrastructure-compose.yml -f ux-compose.yml -f product-compose.yml"
elif [[ "$COMPONENT" == *"content"* ]]; then
    COMPOSE_FILES="-f fabrica-compose.yml -f infrastructure-compose.yml -f content-compose.yml"
else
    COMPOSE_FILES="-f fabrica-compose.yml -f infrastructure-compose.yml -f ux-compose.yml"
fi

echo -e "${BLUE}════════════════════════════════════════${NC}"
echo -e "${BLUE}  Rebuild Component${NC}"
echo -e "${BLUE}════════════════════════════════════════${NC}"
echo -e "${GREEN}Component:${NC}   $COMPONENT"
if [ -n "$NO_CACHE" ]; then
    echo -e "${GREEN}Build Mode:${NC}  Clean build (--no-cache)"
else
    echo -e "${GREEN}Build Mode:${NC}  Smart caching"
fi
echo ""

# Step 0: Kill any local dev servers on Fabrica ports (if --kill-ports flag is set)
if [ -n "$KILL_PORTS" ]; then
    kill_local_ports
fi

# Step 1: Remove the container
echo -e "${YELLOW}[1/3] Removing ${COMPONENT} container...${NC}"
docker rm -f "$COMPONENT" 2>/dev/null || true

echo -e "${GREEN}✓ Container removed${NC}"
echo ""

# Step 2: Build
if [ -n "$NO_CACHE" ]; then
    echo -e "${YELLOW}[2/3] Building ${COMPONENT} with --no-cache (slow, clean build)...${NC}"
    docker-compose $COMPOSE_FILES build --no-cache "$COMPONENT"
else
    echo -e "${YELLOW}[2/3] Building ${COMPONENT} with smart layer caching (fast)...${NC}"
    docker-compose $COMPOSE_FILES build "$COMPONENT"
fi

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Build completed${NC}"
echo ""

# Step 3: Start the service with new image (--no-deps to avoid recreating dependencies)
echo -e "${YELLOW}[3/3] Starting ${COMPONENT}...${NC}"
docker-compose $COMPOSE_FILES up -d --no-deps "$COMPONENT"

if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to start service${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Service started${NC}"
echo ""

# Show service status
echo -e "${YELLOW}Service status:${NC}"
docker-compose $COMPOSE_FILES ps "$COMPONENT"
echo ""

echo -e "${GREEN}════════════════════════════════════════${NC}"
echo -e "${GREEN}  Rebuild Successful!${NC}"
echo -e "${GREEN}════════════════════════════════════════${NC}"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "  • View logs:    docker-compose $COMPOSE_FILES logs -f $COMPONENT"
echo "  • Restart:      docker-compose $COMPOSE_FILES restart $COMPONENT"
echo "  • Stop:         docker-compose $COMPOSE_FILES stop $COMPONENT"
echo ""
