# Jaybird

Grasshopper plugin for Rhino 8+.

## Build

Prerequisites: .NET 8 SDK, Rhino 8

```bash
dotnet build src/Jaybird.csproj
```

Output: `build/Jaybird.gha`

## Development

1. Build using the build script `./build.sh` or in VS Code `Ctrl+Shift+B`
2. Format using `csharpier format src/`
3. In Rhino, run `GrasshopperDeveloperSettings`
4. Add the `build` folder as a plugin folder
5. Restart Rhino
6. In VS Code, press `F5` to debug with breakpoints

## Installation

Copy `Jaybird.gha` to your Grasshopper Libraries folder, or:

```bash
yak install jaybird
```

## Publishing

Create a GitHub release with a version tag (e.g., `v0.1.0`) to auto-publish to yak.

Requires `YAK_TOKEN` secret from <https://www.rhino3d.com/my-account>.
