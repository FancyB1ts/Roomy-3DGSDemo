const { createClient } = require('@supabase/supabase-js');
const crypto = require('node:crypto');

function parseUserAgent(uaRaw) {
  const ua = (uaRaw || '').trim();

  // Default values
  let browser = 'Unknown';
  let device_type = 'Desktop';

  // Device type inference (simple, conservative)
  const uaLower = ua.toLowerCase();
  if (uaLower.includes('mobile') || uaLower.includes('iphone') || uaLower.includes('android') && uaLower.includes('mobile')) {
    device_type = 'Mobile';
  } else if (uaLower.includes('ipad') || uaLower.includes('tablet')) {
    device_type = 'Tablet';
  }

  // Browser family/version (best-effort)
  // Edge (Chromium)
  const mEdge = ua.match(/Edg\/(\d+)/);
  if (mEdge) {
    browser = `Edge ${mEdge[1]}`;
    return { browser, device_type };
  }

  // Chrome (exclude Edge)
  const mChrome = ua.match(/Chrome\/(\d+)/);
  if (mChrome && !mEdge) {
    // Distinguish mobile vs desktop label
    browser = device_type === 'Mobile' ? `Chrome Mobile ${mChrome[1]}` : `Chrome ${mChrome[1]}`;
    return { browser, device_type };
  }

  // Safari (Version/x or fallback)
  const mSafariVer = ua.match(/Version\/(\d+)[.\d]*.*Safari/);
  if (mSafariVer && ua.includes('Safari') && !ua.includes('Chrome') && !ua.includes('Chromium')) {
    browser = device_type === 'Mobile' ? `Safari Mobile ${mSafariVer[1]}` : `Safari ${mSafariVer[1]}`;
    return { browser, device_type };
  }
  if (ua.includes('Safari') && !ua.includes('Chrome') && !ua.includes('Chromium')) {
    browser = device_type === 'Mobile' ? `Safari Mobile` : `Safari`;
    return { browser, device_type };
  }

  // Firefox
  const mFx = ua.match(/Firefox\/(\d+)/);
  if (mFx) {
    browser = `Firefox ${mFx[1]}`;
    return { browser, device_type };
  }

  return { browser, device_type };
}

function verifyJWT(jwt, secret) {
  if (!jwt || !secret) return false;
  const parts = jwt.split('.');
  if (parts.length !== 3) return false;
  const [h, p, s] = parts;
  const body = `${h}.${p}`;
  const expected = crypto.createHmac('sha256', secret).update(body).digest('base64url');
  try {
    if (!crypto.timingSafeEqual(Buffer.from(expected), Buffer.from(s))) return false;
  } catch { return false; }
  const payload = JSON.parse(Buffer.from(p, 'base64url').toString());
  const now = Math.floor(Date.now() / 1000);
  if (payload.exp && payload.exp < now) return false;
  if (payload.scope !== 'upload') return false;
  return true;
}

function isoTs() {
  return new Date().toISOString().replace(/[:.]/g, '-'); // e.g. 2025-08-26T12-34-56Z
}
function dayFromTs(ts) {
  return ts.slice(0, 10); // YYYY-MM-DD
}
// Build keys under _UserSessions/<YYYY-MM-DD>/<timestamp>__<sessionId>__<kind>.(json|geo.json)
function userSessionsKey(sessionId, kind, ext /* 'json' | 'geo.json' */) {
  const ts = isoTs();
  const day = dayFromTs(ts);
  const safeId = (sessionId || 'unknown').toString().replace(/[^\w.-]+/g, '_');
  const safeKind = (kind || 'session').toString().replace(/[^\w.-]+/g, '_');
  const suffix = ext === 'geo.json' ? 'geo.json' : 'json';
  return `_UserSessions/${day}/${ts}__${safeId}__${safeKind}.${suffix}`;
}

function corsHeaders() {
  return {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization, X-Requested-With, X-User-Id, X-Session-Id, X-Overwrite, X-Dev-Bypass',
    'Access-Control-Allow-Methods': 'OPTIONS, POST',
    'Vary': 'Origin',
    'Content-Type': 'application/json',
    'Cache-Control': 'no-store',
  };
}

exports.handler = async (event) => {
  const headers = corsHeaders();

  if (event.httpMethod === 'OPTIONS') {
    return { statusCode: 204, headers, body: '' };
  }

  if (event.httpMethod !== 'POST') {
    return { statusCode: 405, headers, body: 'Method Not Allowed' };
  }

  const ct = event.headers['content-type'] || '';
  if (!ct.startsWith('application/json')) {
    return { statusCode: 415, headers, body: JSON.stringify({ error: 'Unsupported content type' }) };
  }

  const cookies = event.headers.cookie || '';
  const m = cookies.match(/(?:^|;\s*)roomy_upl=([^;]+)/);
  const devBypassHeader = event.headers['x-dev-bypass'] || event.headers['X-Dev-Bypass'];
  const devBypassOk = !!process.env.ROOMY_DEV_BYPASS && devBypassHeader === process.env.ROOMY_DEV_BYPASS;

  if (!m && !devBypassOk) {
    return { statusCode: 403, headers, body: JSON.stringify({ error: 'Missing auth cookie' }) };
  }

  if (m) {
    const token = decodeURIComponent(m[1]);
    if (!verifyJWT(token, process.env.ROOMY_UPLOAD_SECRET)) {
      return { statusCode: 403, headers, body: JSON.stringify({ error: 'Bad or expired token' }) };
    }
  }

  try {
    // Environment variables
    const supabaseUrl = process.env.SUPABASE_URL;
    const supabaseKey = process.env.SUPABASE_SERVICE_KEY || process.env.SUPABASE_SERVICE_ROLE_KEY;
    const bucket = process.env.SUPABASE_BUCKET || 'roomy-sessions';

    if (!supabaseUrl || !supabaseKey) {
      return {
        statusCode: 500,
        headers,
        body: JSON.stringify({ error: "Missing SUPABASE_URL or service role key in Netlify settings" }),
      };
    }

    const supabase = createClient(supabaseUrl, supabaseKey);

    // Parse request body (raw JSON from Unity)
    let body;
    try {
      body = JSON.parse(event.body || "{}");
    } catch {
      return { statusCode: 400, headers, body: JSON.stringify({ error: "Invalid JSON body" }) };
    }

    // --- Build sidecar (.geo.json) metadata from server-only signals ---
    // Netlify Geo (JSON string in header)
    let nfGeo = null;
    try {
      const geoHeader = event.headers['x-nf-geo'] || event.headers['X-NF-Geo'];
      if (geoHeader) nfGeo = JSON.parse(geoHeader);
    } catch { /* ignore parse errors */ }

    const country_code =
      (nfGeo && (nfGeo.country?.code || nfGeo.country_code)) ||
      event.headers['x-country'] || event.headers['X-Country'] || null;

    const country_name = nfGeo?.country?.name ?? null;
    const city = nfGeo?.city ?? null;
    const timezone = nfGeo?.timezone ?? null;

    const language =
      event.headers['accept-language'] || event.headers['Accept-Language'] || null;

    const ua =
      event.headers['user-agent'] || event.headers['User-Agent'] || '';

    const { browser, device_type } = parseUserAgent(ua);

    // Keep fields minimal and nullable
    const sidecar = {
      country_code: country_code || null,
      country_name,
      city,
      timezone,
      saved_at: new Date().toISOString(),
      browser: browser || 'Unknown',
      device_type: device_type || 'Desktop',
      language
    };
    // --- end sidecar build ---

    if ((event.body ? Buffer.byteLength(event.body, 'utf8') : 0) > 2_000_000) {
      return { statusCode: 413, headers, body: JSON.stringify({ error: 'Payload too large' }) };
    }

    // Accept IDs from headers first; fall back to body
    const headerUserId = event.headers['x-user-id'] || event.headers['X-User-Id'];
    const headerSessionId = event.headers['x-session-id'] || event.headers['X-Session-Id'];
    const userId = (headerUserId || body.userId || 'anonymous').toString();
    const sessionId = (headerSessionId || body.sessionId || 'unknown').toString();

    const mainKey = userSessionsKey(sessionId, 'session', 'json');
    const sidecarKey = userSessionsKey(sessionId, 'session', 'geo.json');

    // We want exactly one *original* timestamp per session. Preserve the first one if the file already exists.
    let originalTimestamp = null;
    {
      const existing = await supabase.storage.from(bucket).download(mainKey);
      if (!existing.error && existing.data) {
        try {
          const text = await existing.data.text();
          const prev = JSON.parse(text || '{}');
          originalTimestamp = prev.timestamp || prev.createdAt || null;
        } catch { /* ignore parse errors; treat as new */ }
      }
    }

    // Compose new document: keep original timestamp if present, refresh updatedAt
    const incomingTimestamp = body.timestamp || new Date().toISOString();
    const doc = {
      ...body,
      userId,
      sessionId,
      timestamp: originalTimestamp || incomingTimestamp, // first-write timestamp
      createdAt: originalTimestamp || incomingTimestamp, // alias for clarity
      updatedAt: new Date().toISOString(),
    };

    const fileBuffer = Buffer.from(JSON.stringify(doc), 'utf8');

    const { error } = await supabase.storage
      .from(bucket)
      .upload(mainKey, fileBuffer, { contentType: 'application/json', upsert: true });

    if (error) {
      return { statusCode: 500, headers, body: JSON.stringify({ error: error.message }) };
    }

    // After main upload succeeds, upload the sidecar geo file.
    // Use same path with .geo.json suffix. Failures here do not affect the main response.
    try {
      const sidecarBuffer = Buffer.from(JSON.stringify(sidecar), 'utf8');
      await supabase.storage
        .from(bucket)
        .upload(sidecarKey, sidecarBuffer, { contentType: 'application/json', upsert: true });
    } catch { /* swallow sidecar errors */ }

    // --- Maintain a simple daily index for admin UI (best-effort) ---
    try {
      const ts = new Date().toISOString();
      const day = dayFromTs(ts);
      const indexKey = `_UserSessions/${day}/_index.json`;

      // Load current index (ignore errors)
      let index = [];
      const existingIndex = await supabase.storage.from(bucket).download(indexKey);
      if (!existingIndex.error && existingIndex.data) {
        try {
          const text = await existingIndex.data.text();
          const arr = JSON.parse(text);
          if (Array.isArray(arr)) index = arr;
        } catch { /* ignore parse errors */ }
      }

      // Prepend the latest entry (keep at most 500 entries to limit size)
      const entry = { ts, id: sessionId, key: mainKey };
      index.unshift(entry);
      if (index.length > 500) index = index.slice(0, 500);

      const indexBuf = Buffer.from(JSON.stringify(index), 'utf8');
      await supabase.storage.from(bucket).upload(indexKey, indexBuf, { contentType: 'application/json', upsert: true });
    } catch { /* ignore index errors */ }
    // --- end index writer ---

    return { statusCode: 201, headers, body: JSON.stringify({ success: true, key: mainKey }) };
  } catch (err) {
    return {
      statusCode: 500,
      headers,
      body: JSON.stringify({ error: err.message }),
    };
  }
};