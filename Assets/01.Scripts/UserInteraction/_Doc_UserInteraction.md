# Unity Furniture Placement System - Click Detection & Interaction Handling Documentation

## System Overview

The Unity Furniture Placement System implements a sophisticated click detection and interaction handling system that manages furniture creation, selection, movement, and UI interactions across both desktop (mouse) and mobile (touch) platforms. The system uses a priority-based approach to prevent input conflicts between UI elements, furniture objects, and camera controls.

## Core Click Detection Architecture

### Input Priority Hierarchy

The system implements a three-tier priority system to handle input conflicts:

1. **Highest Priority: UI Elements** - Unity EventSystem handles UI interactions
2. **Medium Priority: Furniture Objects** - MousePlacementManager handles furniture interactions  
3. **Lowest Priority: Camera Controls** - CameraInputManager handles scene navigation

### State-Based Click Handling

The `MousePlacementManager` uses a finite state machine to manage different interaction modes:

```csharp
public enum PlacementState { 
    Idle,           // No active interactions
    Creating,       // Placing new furniture
    Placed,         // Furniture placed, ready for interaction
    Moving,         // Dragging existing furniture
    WaitPointerUp,  // Transition state
    waitingForUI    // UI interaction active
}
```

## Tap vs Long-Press Detection System

### Timing-Based Interaction Classification

The system distinguishes between different click intentions using time-based detection:

- **Tap Threshold**: 0.35 seconds (configurable via `tapThreshold`)
- **Short Tap** (< 0.35s): Opens DataUI panel with dimension controls
- **Long Press** (> 0.35s): Enters movement mode for furniture repositioning

### Implementation Flow

```csharp
void HandleTapDetection(GameObject go)
{
    if (tapStarted)
    {
        tapTimer += Time.deltaTime;
        
        if (Input.GetMouseButtonUp(0))  // Mouse release
        {
            if (tapTimer <= tapThreshold)
            {
                // Short tap - Open DataUI
                uiController.EnableDataUI(screenPosition);
            }
        }
        
        if (tapTimer > tapThreshold)
        {
            // Long press - Enter movement mode
            OnLongPress(go);
        }
    }
}
```

## Cross-Platform Input Handling

### Desktop Mouse Input

- **Click Detection**: `Input.GetMouseButtonDown(0)`
- **Position Tracking**: `Input.mousePosition`
- **Scroll Interaction**: `Input.mouseScrollDelta.y` for furniture rotation

### Mobile Touch Input

- **Touch Detection**: `TouchPhase.Began/Moved/Ended`
- **Position Tracking**: `touch.position`
- **Multi-touch Prevention**: `if (Input.touchCount > 1) return;`

### Unified Input Processing

Both input methods feed into the same interaction logic, ensuring consistent behavior across platforms:

```csharp
private void HandleTouchBegan(Touch touch)
{
    if (!IsPointerOverUI())
    {
        Ray ray = mainCam.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, furnitureLayer))
        {
            // Same logic as mouse click
            selectedObject = hit.collider.gameObject;
            tapTimer = 0f;
            tapStarted = true;
        }
    }
}
```

## Furniture Selection & Targeting

### Raycast-Based Selection

The system uses precise raycast detection to identify furniture objects:

```csharp
Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
if (Physics.Raycast(ray, out RaycastHit hit, 100f, furnitureLayer))
{
    selectedObject = hit.collider.gameObject;
    currentObject = selectedObject;
}
```

### Hover Interaction System

During the `Placed` state, the system continuously checks for furniture hover:

- **Hover Enter**: Shows RotationUI for visual feedback
- **Hover Exit**: Hides RotationUI
- **Scroll During Hover**: Rotates furniture in 11.25° increments

## UI Positioning & Smart Offset System

### DataUI Smart Positioning

When opening the DataUI panel, the system implements intelligent positioning to prevent off-screen clipping:

```csharp
public void EnableDataUI(Vector3 screenPosition)
{
    // Position anchor button at exact click coordinates
    anchorRect.position = screenPosition;
    
    // Calculate smart horizontal offset for content box
    Vector3 smartPosition = CalculateHorizontalSmartOffset(screenPosition, contentRect);
    contentRect.position = smartPosition;
}
```

### Offset Calculation Algorithm

1. **Natural Positioning**: Content appears 120px right, 65px above click point
2. **Boundary Detection**: Checks if natural position would cause clipping
3. **Dynamic Clamping**: Adjusts position only if necessary to stay on-screen
4. **Coordinate Conversion**: Screen → Canvas Local → World position mapping

## Conflict Resolution & Priority Management

### Frame-Delay Coordination

The CameraInputManager implements a frame-delay system to ensure furniture interactions take priority:

```csharp
private IEnumerator CheckForCameraPan()
{
    yield return null; // Wait one frame for furniture system
    
    if (!IsFurnitureInteractionActive() && !IsOverExistingFurniture())
    {
        // Safe to start camera panning
        isPanning = true;
    }
}
```

### UI Conflict Prevention

Multiple layers of UI conflict detection prevent simultaneous interactions:

1. **General UI Check**: `IsPointerOverUI()` using EventSystem
2. **Furniture UI Check**: `IsPointerOverFurnitureUI()` for specific furniture panels
3. **System State Check**: `IsFurnitureSystemActive()` for active furniture operations

### Multi-Touch Management

On mobile devices, the system prevents conflicts during multi-touch gestures:

```csharp
private void HandleSingleTouch()
{
    // Prevent single touch when multiple fingers are down
    if (Input.touchCount > 1) return;
    
    // Process single touch for furniture interaction
}
```

## Interaction Flow Examples

### Furniture Creation Flow

1. User clicks UI button → `CreateObject(id)` called
2. Prefab instantiated → State changes to `Creating`
3. Furniture follows mouse/touch position
4. Click on valid placement → State changes to `Placed`

### Furniture Selection Flow

1. Click on placed furniture → Raycast detects furniture object
2. Tap timer starts → Wait for release or threshold
3. **Short tap**: DataUI opens at click position
4. **Long press**: Movement mode activated with MovementUI

### Movement Flow

1. Long press detected → State changes to `Moving`
2. Furniture follows mouse/touch with collision feedback
3. Release on valid position → Furniture placed, state returns to `Placed`

## Scene Hierarchy Structure

```
MVP_02/
├── Systems/
│   ├── BuildingSystem (MousePlacementManager)
│   ├── CameraInputHandler (CameraInputManager)
│   └── FurnitureRotationManager (ButtonRotateFurniture)
├── UI/RoomyUI (furniture selection buttons)
├── EventSystem (Unity UI input handling)
└── Instantiated Furniture: PF_F_[Name](Clone)
    ├── 3D Model + SlicerController + Components
    ├── FurnitureUIController script
    ├── FurnitureDimensionController script
    └── UI/Canvas/PF_UI_Box_FurnitureDetails/
```

## Key Script Responsibilities

### MousePlacementManager
- Primary furniture lifecycle management
- Tap vs long-press detection
- State machine coordination
- Cross-platform input handling

### FurnitureUIController
- Per-furniture UI management
- DataUI smart positioning
- UI state control (Rotation, Movement, Data panels)

### CameraInputManager
- Low-priority camera controls
- Frame-delay conflict resolution
- Touch gesture handling (pan, pinch-zoom)

### FurnitureDimensionController
- Real-time dimension control
- 27 Slicer integration
- Ground anchoring calculations

This comprehensive click detection system provides robust, platform-agnostic furniture interaction with intelligent UI positioning and conflict resolution, ensuring smooth user experience across desktop and mobile WebGL deployments.