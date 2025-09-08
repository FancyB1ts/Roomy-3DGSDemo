# Roomy Unity WebGL Deployment Guide

Follow these steps whenever you create a **new Unity WebGL build** and want to deploy it to Netlify with backend functions enabled.

---

## 1. Open Terminal in the New Build Folder

After Unity finishes building, open the Terminal and navigate into your new build folder.

Example:

```bash
cd /path/to/Unity_WebGL_Build_Folder
```

---

## 2. Install Production Dependencies

Netlify Functions require certain Node.js dependencies to be present in the **top level** of the build folder.\
Install **only production dependencies**:

```bash
npm ci --only=prod
```

> 💡 This ensures `node_modules` contains only what's needed for deployment, making the build smaller and faster.

---

## 3. Deploy with Netlify CLI

Run the deploy command:

```bash
netlify deploy --prod --dir . --functions functions
```

- `--prod` → Deploy to production (public URL).
- `--dir .` → Use the current folder as the site publish directory.
- `--functions functions` → Deploy serverless functions from the `functions` directory.

---

## Important Notes

- **Do not use the Netlify Web UI drag-and-drop** — it will **not** deploy the `functions` backend.

---

