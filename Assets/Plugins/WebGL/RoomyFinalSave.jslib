mergeInto(LibraryManager.library, {
  Roomy_InitLifecycleHandlers: function (goPtr, methodPtr) {
    // Convert Unity strings
    var go = UTF8ToString(goPtr);
    var method = UTF8ToString(methodPtr);

    function notifyUnity() {
      // Call into Unity GameObject method (no arg)
      if (typeof unityInstance !== 'undefined' && unityInstance.SendMessage) {
        unityInstance.SendMessage(go, method, "");
      } else if (window.SendMessage) {
        window.SendMessage(go, method, "");
      }
    }

    // Attach once
    if (!window.__roomyLifecycleInit) {
      window.__roomyLifecycleInit = true;
      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') notifyUnity();
      });
      window.addEventListener('pagehide', notifyUnity);
      if ('onfreeze' in document) {
        document.addEventListener('freeze', notifyUnity);
      }
    }
  },

  Roomy_SendFinalBeacon: function (urlPtr, jsonPtr /*, userIdPtr, sessionIdPtr */) {
    try {
      var rawUrl = UTF8ToString(urlPtr || 0) || "";
      var json   = UTF8ToString(jsonPtr || 0) || "{}";

      // Normalize to absolute URL (handles relative paths like "/.netlify/functions/upload-session")
      var url = rawUrl;
      try { url = new URL(rawUrl, window.location.origin).toString(); } catch (e) { /* keep rawUrl */ }

      // Use sendBeacon only; avoid fetch on pagehide to prevent uncaught rejections
      if (navigator && typeof navigator.sendBeacon === "function") {
        var blob = new Blob([json], { type: "application/json" });
        var ok = navigator.sendBeacon(url, blob); // returns boolean, never throws
        return ok ? 1 : 0;
      }
    } catch (e) {
      // absolutely never bubble errors back into Unity
    }
    // If no beacon support, fail quietly (regular autosaves already cover most cases)
    return 0;
  },

  Roomy_RefreshCookie: function() {
    try {
      fetch('/.netlify/functions/issue-upload-cookie', { credentials: 'include', cache: 'no-store' })
        .catch(function(){});
    } catch (e) {}
  }
});