# Roomy WebGL Autosave System

## Overview
This system automatically saves user session data in a Unity WebGL build to Supabase Storage.  
It tracks changes in the scene and uploads a single JSON file per session, overwriting it instead of creating duplicates.

---

## Architecture
**Core scripts:**
1. **SessionDataExporter.cs**  
   - Serializes current session state into compact JSON.  
   - Provides methods for quick JSON retrieval and hashing.

2. **SessionDataUploader.cs**  
   - Uploads JSON to the backend endpoint.  
   - Uses fixed key format: `userId/sessions/sessionId.json` (no timestamps).  
   - Supports normal and final-save modes.

3. **SessionAutosaveManager.cs**  
   - Orchestrates saving process:  
     - Debounce (3s) after changes.  
     - Minimum upload interval (15s).  
     - Backup timer (45s).  
   - Avoids duplicate uploads via content hashing.

4. **SessionChangeWatcher.cs**  
   - Watches floorplan and furniture objects for changes.  
   - Filters objects by naming convention (`PF_F_` for furniture).

5. **WebGLLifecycle.jslib** + **sendFinalBeacon helper**  
   - Detects browser lifecycle events (`visibilitychange`, `pagehide`, `freeze`).  
   - Triggers a final save using cached JSON and `navigator.sendBeacon`.

---

## Save Triggers
1. **Event-driven interactions** (via `MarkDirty()`):  
   - Drag/drop end  
   - Rotate/scale release  
   - Dimension apply  
   - Material/model change  
   - Delete/duplicate done  
   - Modal close with apply  
   - Floorplan apply

2. **Backup interval**: Every 45 seconds if unsaved changes remain.

3. **Final save**: On browser exit/tab close.

---

## Backend Requirements
- Accepts `userId` and `sessionId` in either **headers** (UnityWebRequest) or **JSON body** (`sendBeacon`).  
- Overwrites existing file instead of appending timestamps.  
- File path:  
  ```
  {userId}/sessions/{sessionId}.json
  ```

---

## Deployment Notes
- `sendFinalBeacon()` must send both `userId` and `sessionId` in JSON payload.  
- Cache-busting query parameter `?v={productVersion}` used for Unity loader/data/framework/wasm files.  
- React switched to production UMD builds for reduced size.

---

## Expected Behavior
- **Active session**: ~6–10 uploads in 5 minutes under busy use.  
- **No flooding**: Hash-based deduplication ensures unchanged states aren’t re-uploaded.  
- **Crash/exit safe**: Last state is saved via `sendBeacon`.

---

## Known Limitations
- Without login, all users have `userId = "anonymous"`.  
- Floorplan must be assigned for watcher to track changes.
