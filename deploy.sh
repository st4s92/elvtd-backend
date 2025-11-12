#!/bin/bash
set -e  # stop on first error

# Colors for pretty output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}📦 Starting deployment...${NC}"

# Go to project folder
cd "$(dirname "$0")"

echo -e "${YELLOW}🔄 Pulling latest code from master...${NC}"
git fetch origin master
git reset --hard origin/master

echo -e "${YELLOW}🛠️  Rebuilding Docker images...${NC}"
docker-compose build

echo -e "${YELLOW}♻️  Restarting containers...${NC}"
docker-compose up -d

echo -e "${YELLOW}🧹 Cleaning unused images...${NC}"
docker image prune -f

echo -e "${GREEN}✅ Deployment complete!${NC}"
docker ps
