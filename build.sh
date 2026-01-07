#!/bin/bash

set -e

echo "Building Jaybird..."

cd src
dotnet build -c Release
cd ..

echo "Build complete!"
echo "Output: build/Jaybird.gha"
