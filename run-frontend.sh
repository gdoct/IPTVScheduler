#!/bin/bash
# Exit immediately if a command exits with a non-zero status
set -e

# Function to handle errors
handle_error() {
    local exit_code=$?
    local error_msg="$1"
    if [ $exit_code -ne 0 ]; then
        echo "ERROR: $error_msg (exit code: $exit_code)" >&2
        exit $exit_code
    fi
}

# Function to display information
log_info() {
    echo "INFO: $1"
}
# Navigate to the React app directory
cd "$(dirname "$0")/ipvcr.Frontend"

# Install dependencies if needed
echo "Installing dependencies..."
npm install

# Create the target directory in the ASP.NET Core project
echo "Creating target directory..."
mkdir -p ../ipvcr.Web/wwwroot

# Remove existing React app files from wwwroot
echo "Removing existing files from wwwroot..."
rm -rf ../ipvcr.Web/wwwroot/*

# Build the React app
echo "Building the React app..."
npm run build

# Copy to asp.net web root
echo "Copying build files to ASP.NET Core project..."
cp -r build/* ../ipvcr.Web/wwwroot/ 

#start the sp.net core backend
echo "Starting ASP.NET Core backend..."
cd ..
# Start ASP.NET Core backend
dotnet watch run --project ipvcr.Web/ipvcr.Web.csproj 

