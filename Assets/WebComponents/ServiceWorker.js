// /ServiceWorker.js — custom SW copied to build root by HTMLBuilder
// Cache only GET requests; never intercept Netlify Functions or POSTs
const CACHE_NAME = 'app-cache-v2';

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

function isUnityAssetPath(pathname) {
  // Bypass EVERYTHING Unity loader needs so headers/streaming stay intact
  // Build folder contains wasm/data/framework files (with .br or .unityweb)
  // TemplateData holds loader JSON and other runtime assets
  return pathname.startsWith('/Build/') || pathname.startsWith('/TemplateData/');
}

self.addEventListener('fetch', (event) => {
  const req = event.request;
  const url = new URL(req.url);

  // --- HARD BYPASS: never intercept Unity assets ---
  if (isUnityAssetPath(url.pathname)) {
    if (req.method === 'GET') {
      event.respondWith(fetch(req));
    }
    return;
  }

  // Never cache the HTML app shell; always fetch fresh on navigations
  const isHTML = req.mode === 'navigate' || req.destination === 'document' || url.pathname.endsWith('/index.html');
  if (isHTML) {
    event.respondWith(
      fetch(req, { cache: 'no-store' }).catch(() => caches.match('/index.html'))
    );
    return;
  }

  // Only cache GET; ignore non-http(s) and Netlify functions
  if (req.method !== 'GET') return;
  if (url.protocol !== 'http:' && url.protocol !== 'https:') return;
  if (url.pathname.startsWith('/.netlify/functions/')) return;

  // Skip caching for range/no-store requests
  if (req.headers && req.headers.get('Range')) return;
  if (req.cache === 'no-store') return;

  event.respondWith(
    caches.open(CACHE_NAME).then(async (cache) => {
      try {
        const res = await fetch(req);
        try { await cache.put(req, res.clone()); } catch (_) {}
        return res;
      } catch (err) {
        const cached = await cache.match(req);
        if (cached) return cached;
        throw err;
      }
    })
  );
});