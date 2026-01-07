# Jaybird

Grasshopper plugin for my own experimenting

## Build

### Prerequisites
- .NET Framework 4.8 SDK
- Rhino 7 with Grasshopper installed

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
Built plugin: `src/bin/Release/net48/Jaybird.gha`

## Installation
Copy `Jaybird.gha` to your Grasshopper components folder:
- **Windows**: `%APPDATA%\Grasshopper\Libraries`
- **Mac**: `~/Library/Application Support/McNeel/Rhinoceros/7.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries`
