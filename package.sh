#!/bin/bash

set -e

echo "Building and packaging Jaybird..."

# Build
cd src
dotnet build -c Release
cd ..

# Create dist directory
mkdir -p dist
cp src/bin/Release/net48/Jaybird.gha dist/

# Package with yak (if available)
if command -v yak &> /dev/null; then
    echo "Creating yak package..."
    if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
        yak build --platform win
    else
        yak build --platform mac
    fi
    echo "Package created successfully!"
else
    echo "yak not found. Install from https://www.rhino3d.com/download/yak/latest"
    echo "Built .gha file is in dist/Jaybird.gha"
fi
