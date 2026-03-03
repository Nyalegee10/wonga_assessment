#!/usr/bin/env bash
# =============================================================================
# Wonga Assessment — Build Script
# Usage: ./build.sh [options]
#
# Options:
#   --skip-tests    Skip dotnet test step
#   --no-cache      Force Docker to rebuild from scratch
#   --up            Start containers after building
#   --help          Show this message
# =============================================================================
set -euo pipefail

# ── Colours ──────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; BOLD='\033[1m'; RESET='\033[0m'

info()    { echo -e "${CYAN}  →${RESET} $1"; }
success() { echo -e "${GREEN}  ✓${RESET} $1"; }
warn()    { echo -e "${YELLOW}  ⚠${RESET} $1"; }
error()   { echo -e "${RED}  ✗${RESET} $1"; exit 1; }
section() { echo -e "\n${BOLD}[ $1 ]${RESET} $2"; }

# ── Flags ────────────────────────────────────────────────────────────────────
SKIP_TESTS=false
NO_CACHE=false
START_UP=false

for arg in "$@"; do
  case $arg in
    --skip-tests) SKIP_TESTS=true ;;
    --no-cache)   NO_CACHE=true ;;
    --up)         START_UP=true ;;
    --help)
      echo "Usage: ./build.sh [--skip-tests] [--no-cache] [--up] [--help]"
      exit 0 ;;
  esac
done

# ── Header ───────────────────────────────────────────────────────────────────
echo -e "${BOLD}"
echo "  ╔══════════════════════════════════════╗"
echo "  ║     Wonga Assessment Build Script    ║"
echo "  ╚══════════════════════════════════════╝"
echo -e "${RESET}"

START_TIME=$SECONDS

# ── Prerequisites ────────────────────────────────────────────────────────────
section "0/3" "Checking prerequisites"

command -v dotnet  >/dev/null 2>&1 && success "dotnet  $(dotnet --version)" || error ".NET SDK not found"
command -v node    >/dev/null 2>&1 && success "node    $(node --version)"   || error "Node.js not found"
command -v docker  >/dev/null 2>&1 && success "docker  $(docker --version | awk '{print $3}' | tr -d ',')" || error "Docker not found"
docker info        >/dev/null 2>&1                                           || error "Docker daemon is not running"

# ── Backend ──────────────────────────────────────────────────────────────────
section "1/3" "Backend  (.NET 8 — Clean Architecture)"

cd backend

info "Restoring NuGet packages..."
dotnet restore --nologo -q

info "Building solution (Release)..."
dotnet build UserAuth.sln -c Release --no-restore --nologo -q

if [ "$SKIP_TESTS" = false ]; then
  info "Running tests..."
  dotnet test UserAuth.Tests/UserAuth.Tests.csproj \
    -c Release \
    --no-build \
    --nologo \
    --logger "console;verbosity=minimal" \
    --results-directory ./TestResults
  success "All tests passed"
else
  warn "Tests skipped (--skip-tests)"
fi

cd ..

# ── Frontend ─────────────────────────────────────────────────────────────────
section "2/3" "Frontend (Angular 17)"

cd frontend

info "Installing npm packages..."
npm ci --silent

info "Building production bundle..."
npm run build:prod

success "Angular build complete"
cd ..

# ── Docker ───────────────────────────────────────────────────────────────────
section "3/3" "Docker images"

CACHE_FLAG=""
[ "$NO_CACHE" = true ] && CACHE_FLAG="--no-cache" && warn "Cache disabled"

info "Building images..."
docker compose build $CACHE_FLAG

success "All images built"

# ── Start (optional) ─────────────────────────────────────────────────────────
if [ "$START_UP" = true ]; then
  echo ""
  info "Starting containers..."
  docker compose up -d
  echo ""
  info "Waiting for services to be healthy..."
  sleep 5
  docker compose ps
fi

# ── Summary ──────────────────────────────────────────────────────────────────
ELAPSED=$(( SECONDS - START_TIME ))
echo -e "\n${GREEN}${BOLD}"
echo "  ╔══════════════════════════════════════╗"
echo "  ║          Build complete! 🎉          ║"
printf "  ║     Finished in %02dm %02ds              ║\n" $(( ELAPSED/60 )) $(( ELAPSED%60 ))
echo "  ╠══════════════════════════════════════╣"
echo "  ║  Frontend  →  http://localhost:4200  ║"
echo "  ║  API       →  http://localhost:8080  ║"
echo "  ║  Swagger   →  http://localhost:8080  ║"
echo "  ║             /swagger                 ║"
echo "  ╚══════════════════════════════════════╝"
echo -e "${RESET}"
echo -e "  Run ${CYAN}docker compose up${RESET} to start"
echo -e "  Run ${CYAN}docker compose up --build${RESET} to rebuild & start"
