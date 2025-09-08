import crypto from "node:crypto";

const MAX_AGE = 60 * 60; // 1 hour

function signJWT(payload, secret) {
  const enc = (o) => Buffer.from(JSON.stringify(o)).toString("base64url");
  const header = { alg: "HS256", typ: "JWT" };
  const body = `${enc(header)}.${enc(payload)}`;
  const sig = crypto.createHmac("sha256", secret).update(body).digest("base64url");
  return `${body}.${sig}`;
}

export async function handler() {
  const secret = process.env.ROOMY_UPLOAD_SECRET;
  if (!secret) {
    return { statusCode: 500, body: "Missing ROOMY_UPLOAD_SECRET" };
  }
  const now = Math.floor(Date.now() / 1000);
  const token = signJWT({ iat: now, exp: now + MAX_AGE, scope: "upload" }, secret);

  return {
    statusCode: 204,
    headers: {
      // HttpOnly cookie the browser will auto-attach on same-origin requests
      "Set-Cookie": `roomy_upl=${token}; Path=/; HttpOnly; Secure; SameSite=Strict; Max-Age=${MAX_AGE}`,
      "Cache-Control": "no-store",
    },
  };
}
