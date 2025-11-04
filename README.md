# ProjectTwelve - Unity C# Project

A Unity C# project configured for JetBrains Rider IDE.

## Setup

1. Open this project in Unity Editor (2022.3.0f1 or later)
2. Unity will automatically regenerate the `.sln` and `.csproj` files on first open
3. In Unity, go to `Edit > Preferences > External Tools`
4. Set `External Script Editor` to your JetBrains Rider installation path (e.g., `C:\Users\<YourUsername>\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\<version>\bin\rider64.exe`)
5. Open the regenerated `.sln` file in Rider, or double-click any `.cs` script to open it in Rider automatically

**Note:** The `.sln` and `.csproj` files included in this project are templates. Unity will regenerate them when you open the project, which is normal and expected behavior.

## Project Structure

- `Assets/Scripts/` - C# scripts
  - `PlayerController.cs` - Basic player movement script

## Requirements

- Unity Editor 2022.3.0f1 or later
- JetBrains Rider 2022.1 or later

## Getting Started

The project includes a basic `PlayerController` script that handles player movement using arrow keys or WASD.
