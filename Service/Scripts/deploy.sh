#!/bin/bash

# Help function
show_usage() {
    echo "Usage: $0 [-d <install_dir>] [-i] [-u <user>]"
    echo "Options:"
    echo "  -d  Installation directory (default: /opt/clockpulse)"
    echo "  -i  Install as system service"
    echo "  -u  User to run service as (default: current user)"
    exit 1
}

# Parse command line arguments
INSTALL_DIR="/opt/clockpulse"  # Default value
INSTALL_SERVICE=false
SERVICE_USER=$(whoami)  # Default to current user

while getopts "d:iu:" opt; do
    case $opt in
        d) INSTALL_DIR="$OPTARG" ;;
        i) INSTALL_SERVICE=true ;;
        u) SERVICE_USER="$OPTARG" ;;
        ?) show_usage ;;
    esac
done

# Get script location
SCRIPT_DIR="$(dirname "$(readlink -f "$0")")"

# Create installation directory and set permissions
echo "Setting up installation directory..."
sudo mkdir -p "$INSTALL_DIR"
sudo chown "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"

# Copy files
echo "Copying application files..."
sudo cp "$SCRIPT_DIR/Tellurian.Trains.ClockPulseApp.Service" "$SCRIPT_DIR/appsettings.json" "$INSTALL_DIR/"

# Set execute permissions
echo "Setting permissions..."
sudo chmod +x "$INSTALL_DIR/Tellurian.Trains.ClockPulseApp.Service"

if [ "$INSTALL_SERVICE" = true ]; then
    # Create systemd service file
    echo "Creating systemd service..."
    sudo tee /etc/systemd/system/clockpulse.service << EOF
[Unit]
Description=Clock Pulse Service
After=network.target

[Service]
Type=simple
User=$SERVICE_USER
WorkingDirectory=$INSTALL_DIR
ExecStart=$INSTALL_DIR/Tellurian.Trains.ClockPulseApp.Service
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

    # Reload systemd and enable service
    echo "Configuring service..."
    sudo systemctl daemon-reload
    sudo systemctl enable clockpulse.service

    echo "Installation complete!"
    echo "Service management commands:"
    echo "  Start:  sudo systemctl start clockpulse.service"
    echo "  Stop:   sudo systemctl stop clockpulse.service"
    echo "  Status: sudo systemctl status clockpulse.service"
    echo "  Logs:   sudo journalctl -u clockpulse.service -f"
else
    echo "Installation complete!"
    echo "To run the service manually:"
    echo "  cd $INSTALL_DIR"
    echo "  ./Tellurian.Trains.ClockPulseApp.Service"
fi
