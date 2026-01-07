#!/bin/bash

set -e

echo "Building Jaybird..."

cd src
dotnet build -c Release
cd ..

echo "Build complete!"
echo "Output: src/bin/Release/net48/Jaybird.gha"
