# Unity Furniture Placement System - Master Documentation

## System Overview

The Unity Furniture Placement System is a comprehensive WebGL-ready furniture placement solution that combines 3D visualization, real-time dimension control, cross-platform input handling, and floorplan integration. The system orchestrates furniture lifecycle management through a sophisticated state machine while providing seamless user interactions across desktop and mobile platforms.

## Scene Architecture

### Core Scene Structure
```
MVP_02/
├── Systems/
│   ├── BuildingSystem (MousePlacementManager)     # Central orchestrator
│   ├── CameraInputHandler (CameraInputManager)    # Scene navigation
│   └── FurnitureRotationManager                   # Button-based rotation
├── UI/RoomyUI                                      # Furniture selection buttons
├── EventSystem                                     # Unity UI input handling
└── FloorplanManager                               # Floorplan upload & scaling
    ├── Polygon Outlining System                   # Room dimension setup
    └── React Integration Bridge                    # WebGL communication
```

### Instantiated Furniture Hierarchy
```
PF_F_[FurnitureName](Clone)/
├── 3D Model Meshes
├── SlicerController + 27 Slicer Components        # Real-time scaling
├── FurnitureUIController                          # UI orchestration
├── FurnitureDimensionController                   # Dimension input handling
└── UI/Canvas/PF_UI_Box_FurnitureDetails/
    ├── Input_Dimensions/                          # Length/Width/Height inputs
    ├── FurnitureTitle                             # Name display
    └── Delete/Action Buttons
```

## System Orchestration

### MousePlacementManager - Central Orchestrator

The `MousePlacementManager` serves as the primary conductor for all furniture interactions, managing the complete furniture lifecycle through a finite state machine:

```csharp
public enum PlacementState { 
    Idle,           // Ready for new interactions
    Creating,       // Placing new furniture from prefab
    Placed,         // Furniture ready for user interaction
    Moving,         // Dragging existing furniture
    WaitPointerUp,  // Transition state
    waitingForUI    // UI panel active
}
```

#### Key Responsibilities
- **Furniture Creation**: Instantiates furniture from `prefabArray` via `CreateObject(id)`
- **Input Coordination**: Manages mouse/touch input with priority-based conflict resolution
- **State Management**: Orchestrates transitions between placement, selection, and movement
- **Collision Detection**: Real-time overlap checking with visual feedback
- **UI Integration**: Coordinates with `FurnitureUIController` for per-furniture interfaces

#### Interaction Flow Orchestration
1. **Creation Flow**: UI Button → `CreateObject()` → State: Creating → Follow mouse → Place → State: Placed
2. **Selection Flow**: Click furniture → Tap detection → Short tap: DataUI / Long press: Movement
3. **Movement Flow**: Long press → State: Moving → Drag with collision feedback → Release → State: Placed

### FurnitureUIController - Per-Furniture UI Management

Each furniture instance has its own `FurnitureUIController` that manages three distinct UI states:

- **RotationUI**: Visual feedback during hover with scroll wheel rotation
- **MovementUI**: Visual indicators during drag operations  
- **DataUI**: Dimension input panel with smart positioning

#### UI Coordination with MousePlacementManager
```csharp
// MousePlacementManager triggers UI based on interaction type
if (tapTimer <= tapThreshold)
{
    uiController.EnableDataUI(screenPosition);     // Short tap
}
else if (tapTimer > tapThreshold)
{
    uiController.EnableMovementUI();               // Long press
}
```

#### Smart UI Positioning
The DataUI implements intelligent positioning to prevent off-screen clipping:
- **Anchor Point**: Exact click coordinates
- **Content Offset**: +120px right, +65px up from click
- **Boundary Detection**: Auto-adjusts if content would clip off-screen

## Input Priority System

The system implements a three-tier priority hierarchy to prevent input conflicts:

1. **Highest Priority**: UI Elements (Unity EventSystem)
2. **Medium Priority**: Furniture Objects (MousePlacementManager)
3. **Lowest Priority**: Camera Controls (CameraInputManager)

### Cross-Platform Input Handling
- **Desktop**: Mouse clicks, scroll wheel rotation, drag operations
- **Mobile**: Touch gestures, tap/long-press detection, single-finger interactions
- **Unified Logic**: Both platforms feed into the same interaction system

## Integration Systems

### 27 Slicer Integration
- **Real-time Scaling**: `FurnitureDimensionController` connects UI inputs to `SlicerController`
- **Ground Anchoring**: Automatic position adjustment to keep furniture grounded
- **Dimension Conversion**: 1 Unity unit = 100 centimeters for real-world measurements

### Floorplan System Integration
- **Automatic Activation**: Polygon outlining starts after floorplan upload
- **Scale Calculation**: Room dimensions establish real-world scale for entire scene
- **React Bridge**: WebGL communication for floorplan upload and processing

### HTMLBuilder - Automated WebGL Deployment
- **Post-Build Integration**: Automatically runs via `[PostProcessBuild]` callback after WebGL builds
- **HTML Generation**: Replaces Unity's default HTML with React-integrated version
- **Component Management**: Copies React components from `Assets/WebComponents/` to build folder
- **Background Support**: Optional background images with responsive positioning
- **Build Info Extraction**: Uses regex parsing to extract Unity build parameters
- **Backup System**: Creates `index_original.html` backup before replacing main file
- **Manual Tools**: Unity menu options for custom HTML generation and force regeneration

### Data Persistence
- **Session Management**: Complete scene state serialization to JSON
- **Cloud Storage**: Supabase integration for cross-device access
- **Local Export**: Development-mode PNG export for ML training

## Key Features

### Smart Collision System
- **OverlapBox Detection**: Scaled collider checking with 0.85x scale factor
- **Visual Feedback**: Invalid placement material overlay
- **Real-time Updates**: Continuous collision checking during movement

### Rotation System
- **Scroll-based Rotation**: 11.25° increments with rate limiting (13 scrolls/sec max)
- **Angle Snapping**: Automatic alignment to precise angles
- **Visual Feedback**: RotationUI appears during hover interactions

### Dimension Control
- **Real-time Scaling**: Live dimension updates via input fields
- **Metric System**: Centimeter-based input with automatic conversion
- **Proportional Scaling**: Maintains furniture proportions during dimension changes

## Related Documentation

For detailed technical implementation, refer to these comprehensive guides:

### 📋 Click Detection & Interaction Handling
Detailed documentation of the input system, tap/long-press detection, and cross-platform interaction handling. See the "_Doc_UserInteraction_20250810.pdf" for comprehensive coverage of input priority systems, touch/mouse unification, and conflict resolution mechanisms.

### 🏗️ Floorplan Polygon Outlining System
Complete guide to the polygon drawing system for establishing real-world scale through room outlining. Refer to the "_Doc_FloorplanOutliner_20250810.pdf" for details on interactive polygon creation, area calculation, and automatic scale application.

### 🌐 Unity WebGL + React Integration
Comprehensive documentation covering floorplan upload, React communication bridge, and automated build processes. The "_Doc_WebGLReactIntegration_20250810.pdf" provides complete implementation details for the HTMLBuilder system and React communication protocols.

### 💾 Session Data Management System
Technical details of the JSON serialization system, cloud storage integration, and data persistence workflows. The "_Doc_SessionDataManagement_20250810" covers the complete furniture state serialization and cloud storage implementation.

## Production Deployment

The system is designed for WebGL deployment with:
- **Cross-platform Compatibility**: Desktop browsers and mobile devices
- **Performance Optimization**: Efficient collision detection and UI rendering
- **Cloud Integration**: Ready for production with Supabase backend
- **Automated Build Process**: HTMLBuilder generates React-integrated deployment packages
- **WebComponents Architecture**: React components automatically copied from `Assets/WebComponents/`
- **Responsive Design**: Built-in CSS utility classes for consistent cross-device presentation
- **Background Customization**: Optional background images with automatic positioning
- **Unity-React Communication**: Seamless bidirectional messaging between Unity and React overlays

This master documentation provides the high-level overview of how the furniture placement system orchestrates complex interactions through the central MousePlacementManager while coordinating with specialized controllers for UI, dimensions, and platform-specific input handling.