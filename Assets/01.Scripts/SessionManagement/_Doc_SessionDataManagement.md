# Doc_Roomy_Session_Management_System

## 1. System Overview
Unity 6 WebGL furniture placement system for Roomy with:
- **Cloud session tracking** (Supabase + Netlify backend)
- **Local session recovery** (for development/offline use)
- Supports **floorplan + furniture state saving** and restoration.

---

## 2. Architecture Components

### Core Scripts
- **SessionDataExporter** – Collect/export complete session data  
- **SessionDataUploader** – Uploads JSON to Supabase via Netlify  
- **SessionDataRecovery** – Restores sessions from local files  
- **MousePlacementManager** – Placement + session restoration  
- **FurnitureUIController** – UI per furniture, deletion callbacks  
- **FurnitureDimensionController** – Real-time dimension control + restore  

### Integration Points
- **27 Slicer System** – Mesh/collider scaling preservation  
- **FloorplanReceiver** – Image + UV letterboxing  
- **ItemDataBaseSO** – Furniture DB validation/mapping  

---

## 3. User Workflow

### Cloud (Production)
1. Place furniture / edit floorplan in WebGL  
2. Click **Save**  
3. Collect data via `SessionDataExporter`  
4. Generate JSON (`GetCurrentSessionAsJson()`)  
5. Upload via `SessionDataUploader` → Netlify function (`upload-session.js`)  
6. Netlify stores in **Supabase bucket** (private)  

### Local (Development)
1. Export with `ExportCurrentSession()` → local JSON + PNG  
2. Files saved to `{persistentDataPath}/SessionData/`  
3. Restore with `LoadSessionButtonClick()`  

---

## 4. Cloud Infrastructure
- **Endpoint:** `https://plan.roomy-app.co/.netlify/functions/upload-session`  
- **Auth:** Supabase Admin Key (server-side in Netlify env vars)  
- **Bucket:** `user-session-data` (private)  
- **File naming:** `yyyyMMdd_HHmmss_session_<id>_data.json`

---

## 5. Session Export Data Structure
Example JSON:
```json
{
  "sessionId": "anonymous_abc123",
  "userId": "anonymous",
  "timestamp": "2025-08-06T12:57:43Z",
  "floorplan": {
    "base64": "...",
    "scaleValue": 1.25,
    "imageDimensions": {"x": 1024, "y": 1024},
    "uvScale": {"x": 1.26, "y": 1.0},
    "uvOffset": {"x": -0.13, "y": 0.0}
  },
  "furniture": [
    {
      "furnitureType": "PF_F_Sofa",
      "itemDataId": 123,
      "position": {"world": {"x": 2.3, "y": 0, "z": 1.8}},
      "rotation": 45.0,
      "dimensions": {
        "original": {"x": 2.0, "y": 0.8, "z": 1.0},
        "current": {"x": 2.4, "y": 0.8, "z": 1.0}
      }
    }
  ]
}
```

---

## 6. Recovery System

### Local Recovery
- Scans `SessionData/` for `*_data.json`
- Parses JSON with Unity `JsonUtility`
- Restores floorplan (Base64 → Texture2D → Material w/ UVs)
- Sequentially restores furniture positions, rotations, dimensions

### Future Cloud Recovery
- Supabase download by `userId`/`sessionId`
- Hybrid mode: Cloud + local fallback

---

## 7. Furniture Mapping
```csharp
private Dictionary<string, int> furnitureTypeToPrefabIndex;
private void InitializeFurnitureMapping() {
    for (int i = 0; i < prefabArray.Length; i++) {
        furnitureTypeToPrefabIndex[prefabArray[i].name] = i;
    }
}
```

---

## 8. Error Handling
- **Validation:** Check file existence, required fields  
- **Fallbacks:** Skip unknown furniture, partial data load, auto-find scene refs  

---

## 9. File Organization
**Local:**
```
SessionData/
20250806_143052_session_abc123_data.json
20250806_143052_session_abc123_floorplan.png
20250806_143052_session_abc123_furniture.png
```

**Cloud:**
```
user-session-data/
20250807_123456_session_anonymous_abc123_data.json
```

---

## 10. Integration Requirements
- Attach session scripts to scene GameObjects  
- UI Buttons linked to:
  - Cloud Save → `SessionDataUploader.OnSaveSessionButtonClicked()`  
  - Local Export → `SessionDataExporter.ExportCurrentSession()`  
  - Local Load → `SessionDataRecovery.LoadSessionButtonClick()`  

---

## 11. Production Status
✅ **Ready for Production** – Cloud upload, data export, backend infra, security  
🛠 **Pending:** Cloud recovery, `MousePlacementManager.RestoreSession()`, improved error handling  

---

## 12. Maintainers
**Unity:** `SessionDataExporter`, `SessionDataUploader`, `SessionDataRecovery`  
**Backend:** `upload-session.js` (Netlify), Supabase project, Netlify env vars
