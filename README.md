# Jaybird

Grasshopper plugin for my own experimenting

## Build

### Prerequisites
- .NET 8 SDK
- Rhino 8 with Grasshopper installed

### Build via Script (Windows/Mac)
```bash
./build.sh
```

### Build via VS Code (Windows/Mac)
1. Open folder in VS Code
2. Press `Ctrl+Shift+B` (Windows) or `Cmd+Shift+B` (Mac)
3. Select "build" task

### Manual Build
```bash
cd src
dotnet build -c Release
```

### Output
Built plugin: `src/bin/Release/net8.0/Jaybird.gha`

## Installation

### Manual Installation
Copy `Jaybird.gha` to your Grasshopper components folder:
- **Windows**: `%APPDATA%\Grasshopper\Libraries`
- **Mac**: `~/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries`

### Via Package Manager
```bash
yak install jaybird
```

## Packaging

### Create yak package locally
```bash
./package.sh
```

This creates a `.yak` file for distribution.

## Publishing

### Automated (GitHub Actions)
Publishing is automated via GitHub Actions:

1. **Create a release** on GitHub with a version tag (e.g., `v0.1.0`)
2. Workflow automatically builds and publishes to yak

### Manual publish
```bash
# Build and package
./package.sh

# Publish to yak (requires YAK_TOKEN)
yak push jaybird-*.yak
```

### Setup for automated publishing
1. Get a yak API token from https://www.rhino3d.com/my-account
2. Add `YAK_TOKEN` to repository secrets (Settings → Secrets → Actions)
3. Create a release to trigger automated publishing
