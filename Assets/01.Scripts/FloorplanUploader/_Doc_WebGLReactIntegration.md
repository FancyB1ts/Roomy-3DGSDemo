# Unity WebGL + React Integration Documentation

## Document Overview

This documentation explains the complete transformation of a standard Unity WebGL build into a professional web application with React integration for floorplan visualization. Users upload floorplan images through a React interface, which are then processed and displayed in Unity's 3D environment with intelligent scaling, aspect ratio preservation, and automatic polygon outlining.

## Table of Contents

1. Architecture
2. File Structure
3. HTML Modifications
4. Unity C# Scripts
5. React Component
6. Communication Protocol
7. Polygon Outlining Integration
8. Performance Optimizations
9. Browser Compatibility
10. Deployment

---

## 1. Architecture

**System Flow:**

```
User → React Upload Interface → Unity 3D Viewer → Polygon Outlining → Furniture Placement
```

**Component Structure:**

- **React Frontend:** File upload, drag/drop interface, status indicators, overlay management.
- **Unity Backend:** 3D floorplan visualization with material-based image display and polygon integration.
- **Bridge System:** JavaScript communication between React and Unity with enhanced messaging.
- **Polygon Integration:** Automatic outlining triggered after upload.

---

## 2. File Structure

```
Unity Project/
├── Assets/
│   ├── WebComponents/                # React components (auto-copied)
│   │   ├── floorplan-uploader.js     # React upload component
│   │   ├── Background/               # Optional background images
│   ├── Editor/
│   │   └── HTMLBuilder.cs            # Automatic HTML generation
│   └── Unity Scripts/                # Unity C# scripts
│       ├── UnityWebGLBridge.cs
│       ├── FloorplanReceiver.cs
│       ├── FloorplanDebugTester.cs
│       └── ButtonReplaceFloorplan.cs
```

---

## 3. HTML Modifications

**Automated HTML Generation** via `HTMLBuilder.cs`:

- Runs after WebGL build (`[PostProcessBuild]`).
- Creates backup of original HTML.
- Generates custom React-integrated HTML.
- Copies all WebComponents to build.

**Features:**

- Responsive canvas.
- Dual container system (React + Unity).
- Tailwind-style utility classes.
- Optional background images.
- Overlay modal system for uploader.

---

## 4. Unity C# Scripts

**UnityWebGLBridge.cs:** Handles communication between Unity and React.

- Methods: `ReceiveFloorplan`, `ShowFloorplanUploader`, `OnFloorplanUploadCancelled`, `SendMessageToReact`, `OnFloorplanProcessed`.
- Requires `GameObject` named **FloorplanManager**.

**FloorplanReceiver.cs:** Handles image decoding, texture application, UV scaling, and polygon integration.

- Maintains aspect ratio.
- Integrates with `RawImage` UI.
- Triggers polygon outlining.

**FloorplanDebugTester.cs:** Allows local testing without WebGL build.

- Test with project textures, manual base64, or file paths.

**ButtonReplaceFloorplan.cs:** Simple button click handler for replacing floorplan.

---

## 5. React Component

**Requirements:**

- File upload interface.
- Converts files to base64.
- Displays status messages.
- Manages overlay visibility.
- Handles cancellation.

**Unity → React Messages:**

```js
window.onUnityReady();
window.onFloorplanLoaded(status);
window.showFloorplanUploader("");
```

**React → Unity Messages:**

```js
unityInstance.SendMessage("FloorplanManager", "ReceiveFloorplan", base64Data);
unityInstance.SendMessage("FloorplanManager", "OnFloorplanUploadCancelled", "");
```

---

## 6. Polygon Outlining Integration

- Automatically starts after floorplan load.
- Activates scaling UI.
- Calls `PolygonDrawer.StartOutlining()`.
- Hides UI after completion.

**Setup:**

- Assign scaling UI container in `FloorplanReceiver`.
- Ensure `PolygonDrawer` exists inside scaling UI container.

---

## 7. Performance Optimizations

**Memory Management:**

- Automatic texture cleanup.
- Clamp wrap mode.
- Optimized texture format (RGBA32).

**WebGL:**

- Efficient base64 decoding.
- Minimized JavaScript calls.
- Aggressive UI refresh.

---

## 8. Browser Compatibility

- **Supported formats:** PNG, JPEG, WebP.
- **Tested browsers:** Chrome, Firefox, Safari, Edge.
- **Mobile:** iOS Safari, Chrome Mobile, Samsung Internet.

---

## 9. Deployment

**Automatic Build:**

1. Build WebGL in Unity.
2. `HTMLBuilder` runs automatically.
3. React components copied.
4. Ready for deployment.

**Manual HTML Generation:**

- Unity menu → Build → Generate/Update index.html.

---

## 10. API Reference

**UnityWebGLBridge Methods:**

- `ReceiveFloorplan(base64Image)`
- `ShowFloorplanUploader()`
- `OnFloorplanUploadCancelled()`
- `SendMessageToReact(functionName, message)`

**FloorplanReceiver Methods:**

- `ProcessFloorplan(base64Image)`

**ButtonReplaceFloorplan Methods:**

- `OnReplaceFloorplanClicked()`

**HTMLBuilder Methods:**

- `OnPostprocessBuild()`
- `GenerateCustomHTML()`
- `CopyWebComponents()`
- `GenerateHTMLManually()`
- `ForceRegenerateHTML()`

