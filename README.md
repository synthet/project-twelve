# ProjectTwelve - Unity C# Project

A Unity C# project configured for JetBrains Rider IDE.

## Setup

1. Open this project in Unity Editor 6.0.2 or later
2. Unity will automatically regenerate the `.sln` and `.csproj` files on first open
3. In Unity, go to `Edit > Preferences > External Tools`
4. Set `External Script Editor` to your JetBrains Rider installation path (e.g., `C:\Users\<YourUsername>\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\<version>\bin\rider64.exe`)
5. Open the regenerated `.sln` file in Rider, or double-click any `.cs` script to open it in Rider automatically

**Note:** The `.sln` and `.csproj` files included in this project are templates. Unity will regenerate them when you open the project, which is normal and expected behavior.

## Project Structure

```
ProjectTwelve/
├── Assets/
│   └── Scripts/
│       └── PlayerController.cs    # Basic player movement script
├── ProjectSettings/                # Unity project configuration
├── Packages/                       # Package dependencies
└── README.md                       # This file
```

### Scripts

- `Assets/Scripts/PlayerController.cs` - Basic player movement script that handles input using Unity's Input system (arrow keys or WASD)
- `Assets/Scripts/HexGrid.cs` - Generates and manages a hexagonal grid field
- `Assets/Scripts/HexTile.cs` - Individual hex tile component with highlighting support
- `Assets/Scripts/MouseControlledObject.cs` - Object that can be moved by clicking on hex tiles with the mouse

## Design Documents

- [Unity 2D Sandbox Architecture Plan](docs/terraria-like-unity-design.md) - Technical plan for evolving the project toward a Terraria-like 2D sandbox, including chunked world data, rendering, physics, lighting, liquids, saving, multiplayer, modding, testing, and roadmap guidance.

## Requirements

- Unity Editor 6.0.2 or later
- JetBrains Rider 2023.3 or later (recommended for Unity 6 support)

## Getting Started

The project includes a basic `PlayerController` script that handles player movement using arrow keys or WASD. The script uses Unity's built-in `Input.GetAxis()` method for smooth movement input.

## Hexagonal Grid Demo

A demo "Hello World" game featuring a hexagonal grid with a mouse-controlled object.

### Setup Instructions

1. **Create the Hex Grid:**
   - Create an empty GameObject in your scene (GameObject > Create Empty)
   - Name it "HexGrid"
   - Add the `HexGrid` component to it
   - Adjust the grid settings in the inspector:
     - Grid Width: 5 (default)
     - Grid Height: 5 (default)
     - Hex Size: 1 (default)
     - Hex Color: White (default)

2. **Create the Movable Object:**
   - Create a GameObject (e.g., a Cube or Sphere) to represent the movable object
   - Position it slightly above the grid (e.g., Y = 0.5)
   - Add the `MouseControlledObject` component to it
   - Adjust movement settings if needed:
     - Move Speed: 5 (default)
     - Highlight Color: Yellow (default)

3. **Set Up the Camera:**
   - Position your camera to look down at the grid at an angle (recommended: position at (0, 10, -10), rotate to look at (0, 0, 0))
   - Ensure the camera can see the entire grid

4. **Play and Test:**
   - Press Play in Unity
   - Click on any hex tile to move the object to that position
   - The clicked tile will be highlighted in yellow

### How It Works

- The `HexGrid` script automatically generates a hexagonal grid of tiles when the scene starts
- Each hex tile has a collider for mouse raycast detection
- Clicking on a hex tile with the left mouse button moves the `MouseControlledObject` to that tile
- The target tile is highlighted to show where the object will move
