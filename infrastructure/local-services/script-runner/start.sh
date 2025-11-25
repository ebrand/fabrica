#!/bin/bash

# Find the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR" || exit 1

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo -e "${YELLOW}Installing dependencies...${NC}"
    npm install
fi

# Check if already running
if lsof -ti:3800 > /dev/null 2>&1; then
    echo -e "${YELLOW}Script runner is already running on port 3800${NC}"
    echo "To restart, run: $0 --restart"
    if [ "$1" == "--restart" ]; then
        echo -e "${YELLOW}Killing existing process...${NC}"
        kill $(lsof -ti:3800) 2>/dev/null
        sleep 1
    else
        exit 0
    fi
fi

echo -e "${GREEN}Starting Fabrica Script Runner...${NC}"
npm start
