#!/usr/bin/env bash
# Waymarked — initial server setup script
# Run as root on a fresh Hetzner Ubuntu 24.04 server.
#
# After running this script:
#   1. Create a 'deploy' user and add your CI public key (see below)
#   2. Seed GraphHopper OSM data (see below)
#   3. Make GHCR packages public or configure GHCR auth on the server
#   4. First deploy: cd /opt/waymarked && docker compose up -d
set -euo pipefail

DEPLOY_DIR=/opt/waymarked
GH_REPO=joshuahills/waymarked

echo "==> Installing Docker..."
apt-get update -qq
apt-get install -y -qq ca-certificates curl
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  > /etc/apt/sources.list.d/docker.list
apt-get update -qq
apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-compose-plugin

echo "==> Creating directory layout..."
mkdir -p "$DEPLOY_DIR/graphhopper/data"
mkdir -p "$DEPLOY_DIR/caddy-data"
mkdir -p "$DEPLOY_DIR/caddy-config"
mkdir -p /var/log/caddy

echo "==> Downloading production config from GitHub..."
curl -fsSL "https://raw.githubusercontent.com/$GH_REPO/master/infra/deploy/docker-compose.yaml" \
  -o "$DEPLOY_DIR/docker-compose.yaml"
curl -fsSL "https://raw.githubusercontent.com/$GH_REPO/master/infra/graphhopper/config.yml" \
  -o "$DEPLOY_DIR/graphhopper/config.yml"
curl -fsSL "https://raw.githubusercontent.com/$GH_REPO/master/infra/deploy/Caddyfile" \
  -o "$DEPLOY_DIR/Caddyfile"

echo "==> Writing .env file..."
cat > "$DEPLOY_DIR/.env" <<EOF
# Bind mounts — server-specific paths (do not commit this file)
CADDY_BINDMOUNT_0=$DEPLOY_DIR/Caddyfile
CADDY_BINDMOUNT_1=$DEPLOY_DIR/caddy-data
CADDY_BINDMOUNT_2=$DEPLOY_DIR/caddy-config
GRAPHHOPPER_BINDMOUNT_0=$DEPLOY_DIR/graphhopper/config.yml
GRAPHHOPPER_BINDMOUNT_1=$DEPLOY_DIR/graphhopper/data

# Container images — CI pushes :latest on every master build
GRAPHHOPPER_IMAGE=ghcr.io/$GH_REPO/graphhopper:latest
WAYMARKED_API_IMAGE=ghcr.io/$GH_REPO/waymarked-api:latest
WAYMARKED_WEB_IMAGE=ghcr.io/$GH_REPO/waymarked-web:latest

# ASP.NET Core internal HTTP port (default: 8080, matches SDK container publishing)
WAYMARKED_API_PORT=8080
WAYMARKED_WEB_PORT=8080
EOF

echo "==> Creating 'deploy' user for CI deployments..."
if ! id -u deploy &>/dev/null; then
  adduser --disabled-password --gecos "" deploy
fi
usermod -aG docker deploy
mkdir -p /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
touch /home/deploy/.ssh/authorized_keys
chmod 600 /home/deploy/.ssh/authorized_keys
chown -R deploy:deploy /home/deploy/.ssh

# Give the deploy user ownership of the deploy directory so CI SCP writes succeed
chown -R deploy:deploy "$DEPLOY_DIR"

echo ""
echo "┌──────────────────────────────────────────────────────────────────────┐"
echo "│  MANUAL STEPS REQUIRED                                               │"
echo "└──────────────────────────────────────────────────────────────────────┘"
echo ""
echo "1. Add your CI public key to the deploy user:"
echo ""
echo "   echo '<your-public-key>' >> /home/deploy/.ssh/authorized_keys"
echo ""
echo "   Generate a dedicated key pair on your dev machine:"
echo "   ssh-keygen -t ed25519 -C 'waymarked-deploy' -f ~/.ssh/waymarked_deploy"
echo "   Then add the private key as the DEPLOY_SSH_KEY secret in GitHub."
echo ""
echo "2. Set required GitHub Actions secrets (Settings → Secrets → Actions):"
echo "   DEPLOY_HOST   — this server's IP or hostname"
echo "   DEPLOY_USER   — deploy"
echo "   DEPLOY_SSH_KEY — contents of ~/.ssh/waymarked_deploy (private key)"
echo ""
echo "3. Make GHCR packages public (recommended for simplicity):"
echo "   After the first CI push, go to:"
echo "   https://github.com/joshuahills/waymarked/packages"
echo "   For each package (graphhopper, waymarked-api, waymarked-web):"
echo "   → Package settings → Change visibility → Public"
echo ""
echo "4. Seed GraphHopper OSM data (one-time, ~15-30 min):"
echo ""
echo "   # Download Great Britain OSM extract (~1.5 GB):"
echo "   wget -O /tmp/gb.osm.pbf https://download.geofabrik.de/europe/great-britain-latest.osm.pbf"
echo ""
echo "   # Copy to data directory:"
echo "   cp /tmp/gb.osm.pbf $DEPLOY_DIR/graphhopper/data/"
echo ""
echo "   # Mount the config — Aspire bind mounts config.yml into /data,"
echo "   # but GraphHopper reads it from /data/config.yml."
echo "   # First, pull the GraphHopper image (requires CI push to have run first):"
echo "   cd $DEPLOY_DIR && docker compose pull graphhopper"
echo ""
echo "   # Build the routing graph (takes ~10-20 min, uses 6 GB RAM):"
echo "   docker compose run --rm graphhopper"
echo ""
echo "   # Once you see 'Started server ...', the graph is cached."
echo "   # Stop (Ctrl+C) and start the full stack:"
echo "   docker compose up -d"
echo ""
echo "==> Server setup complete."
