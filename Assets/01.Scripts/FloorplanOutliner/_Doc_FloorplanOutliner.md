# Floorplan Polygon Outlining System Documentation

## Overview

The Floorplan Polygon Outlining System allows users to interactively draw polygons on uploaded floorplan images to establish real-world scale. Users outline a room with known dimensions, input the real area, and the system automatically calculates and applies the correct scale to the entire 3D floorplan.

## System Workflow

### 1. Initial State
- User uploads floorplan image via React interface
- System automatically activates outlining mode
- **UI State**: "Outline Room" button highlighted, instruction text appears

### 2. Polygon Drawing
- User clicks points on floorplan to create polygon outline
- Visual feedback shows red points and connecting lines
- **UI State**: Dynamic instruction text based on point count
- First point highlights when 3+ points placed for closing

### 3. Polygon Completion  
- User clicks highlighted first point to close polygon
- Polygon turns blue, area calculation triggers
- **UI State**: Dimension input dialog appears, "Set Dimensions" button highlighted

### 4. Scale Calculation
- User inputs known room area in Jo/m²/ft²
- System calculates scale factor and applies to 3D plane
- **UI State**: Dialog closes, system returns to normal state

## Core Components

### PolygonDrawer.cs
**Purpose**: Interactive polygon creation and editing

**Key Features:**
- **Point-based Drawing**: Click to place points, automatic line generation
- **Polygon Closure**: Click first point when 3+ points placed to close
- **Visual Feedback**: Different colors for drawing vs completed state
- **Drag Editing**: Move points after completion to adjust shape
- **Self-intersection Detection**: Warns users about overlapping lines
- **Area Calculation**: Uses shoelace formula for accurate area computation

**Integration Points:**
- Notifies `PolygonRenderer` for visual updates
- Passes area data to `FloorplanScaler` for dimension input

### PolygonRenderer.cs
**Purpose**: Advanced polygon visualization with fill rendering

**Key Features:**
- **Line Rendering**: Uses `UILineRenderer` for smooth polygon outlines
- **Fill Generation**: Creates sprite-based polygon fill using scanline algorithm
- **Color Management**: Separate colors for outline, fill, and completion states
- **UI Integration**: Works within Unity's Canvas system for proper layering

### UILineRenderer.cs
**Purpose**: High-performance line rendering for UI

**Key Features:**
- **Mesh Generation**: Creates optimized vertex/triangle meshes for lines
- **Canvas Scale Normalization**: Automatically adjusts thickness for different screen densities  
- **Corner Smoothing**: Intelligent corner joining for professional appearance
- **Performance Optimization**: Caching system for static lines

### FloorplanScaler.cs
**Purpose**: Real-world dimension input and scale calculation

**Key Features:**
- **Multi-unit Support**: Jo (tatami mats), square meters, square feet
- **UV-based Scaling**: Calculates scale based on polygon area ratio to total image
- **3D Plane Application**: Applies calculated scale to Unity 3D floorplan object
- **Automatic UI Management**: Shows/hides dimension input interface

### InstructionManager.cs
**Purpose**: Dynamic user guidance system

**Key Features:**
- **State-aware Instructions**: Different messages based on current outlining state
- **Real-time Updates**: Monitors polygon drawer state 10 times per second
- **Priority System**: Intersection warnings override normal instructions
- **Progressive Guidance**: Step-by-step instructions from start to completion

### ButtonHighlighter.cs
**Purpose**: Visual workflow indicators

**Key Features:**
- **State-based Highlighting**: Highlights appropriate buttons based on current step
- **Automatic Detection**: Monitors component states to determine active workflow step
- **Visual Feedback**: Uses Unity's button color system for highlighting

## Unity Scene Setup

### Hierarchy Structure
```
FloorplanOutliner_Container/
├── RawImage (displays uploaded floorplan)
│   ├── PolygonDrawer (Script)
│   ├── PolygonRenderer (Script) 
│   ├── FloorplanScaler (Script)
│   ├── UILineRenderer (Component)
│   └── PointsContainer (Transform)
├── Info_InstructionTexts/
│   ├── InstructionText0 (intersection warning)
│   ├── InstructionText1 ("Click to begin drawing")
│   ├── InstructionText2-5 (point count instructions)
├── Dialogue_RoomDimensions (dimension input UI)
└── ButtonHighlighter (Script)
```

### Component Dependencies

**PolygonDrawer requires:**
- `UILineRenderer` component for line visualization
- `Transform pointsContainer` for point object management
- Auto-finds `PolygonRenderer` and `FloorplanScaler` on same GameObject

**PolygonRenderer requires:**
- `UILineRenderer` component for outline rendering
- Creates fill objects dynamically as children

**FloorplanScaler requires:**
- UI components: `TMP_InputField`, `Button`, `TMP_Dropdown`
- `Transform floorplanPlane` reference to 3D plane object
- `RectTransform imageContainer` for UV calculations

**InstructionManager requires:**
- Multiple `TextMeshProUGUI` references for different instruction states
- `PolygonDrawer` reference for state monitoring

## Technical Details

### Coordinate Systems
- **UI Coordinates**: All polygon drawing uses Unity UI local coordinates
- **UV Coordinates**: Converted for floorplan scaling calculations  
- **3D Coordinates**: Applied to world-space floorplan plane

### Canvas Scale Handling
The system automatically handles different screen densities through canvas scale normalization:
- Line thickness adjusts for high-DPI displays
- Point sizes scale appropriately
- Touch/click thresholds adapt to screen density

### Area Calculation
Uses the shoelace formula for accurate polygon area:
```
Area = |Σ(x[i] * y[i+1] - x[i+1] * y[i])| / 2
```

### Scale Calculation Process
1. Calculate polygon area in pixels
2. Determine polygon area as percentage of total image
3. Convert user input to square meters
4. Calculate total floorplan area: `realWorldArea / uvAreaRatio`
5. Generate scale factor: `√(totalArea) / 10` (Unity's default plane size)

## Configuration Options

### Visual Customization
- **Line Colors**: Drawing vs completion states
- **Fill Colors**: Semi-transparent polygon fill
- **Point Sprites**: Custom sprites for first vs regular points
- **Line Width**: Adjustable thickness for different screen sizes

### Interaction Settings  
- **Close Threshold**: Distance for polygon closure detection
- **Point Size**: Visual size of interactive points
- **Canvas Scale**: Automatic detection and normalization

### Unit Conversion
- **Jo (畳)**: 1 Jo = 1.62 m² (traditional Japanese tatami mat)
- **Square Meters**: Direct conversion
- **Square Feet**: 1 ft² = 0.092903 m²

## Integration with Floorplan Upload System

### Automatic Activation
The polygon outlining system integrates seamlessly with the floorplan upload workflow:

1. **Upload Completion**: `FloorplanReceiver.StartOutliningProcess()` automatically activates outlining
2. **UI Container Management**: Scaling UI container shows/hides automatically
3. **Completion Detection**: Listens for calculate button click to auto-hide interface

### Error Handling
- **Self-intersection Detection**: Visual warnings for overlapping polygon lines
- **Input Validation**: Ensures positive area values and valid polygon shapes
- **Component Missing**: Graceful degradation when optional components not found

## Performance Considerations

### Rendering Optimization
- **Mesh Caching**: UILineRenderer caches vertex data for static polygons
- **Canvas Batching**: UI components designed for efficient Canvas batching
- **Sprite Generation**: Fill sprites generated once and cached

### Update Frequency
- **Instruction Manager**: Updates 10 times per second for responsive UI
- **Button Highlighter**: Lightweight state checking every 100ms
- **Polygon Updates**: Only when user interacts (event-driven)

## Debugging and Testing

### Inspector Context Menus
- **PolygonDrawer**: Start/Reset outlining, test polygon completion
- **PolygonRenderer**: Clear renderer, debug state information
- **InstructionManager**: Test individual instruction messages

### Debug Logging
All components include comprehensive debug logging for:
- State transitions and user interactions
- Area calculations and scale factor computation
- Component communication and error conditions
- UI state changes and automatic activations

This system provides a complete, user-friendly solution for establishing real-world scale in Unity-based floorplan visualization applications.