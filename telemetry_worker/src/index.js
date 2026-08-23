import {
  MAX_BODY_BYTES,
  MAX_EVENTS_PER_INSTALLATION_HOUR,
  validateEnvelope,
} from "./validation.js";

const JSON_HEADERS = Object.freeze({
  "content-type": "application/json; charset=utf-8",
  "cache-control": "no-store",
  "x-content-type-options": "nosniff",
});

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === "GET" && url.pathname === "/health")
      return jsonResponse({ status: "ok" }, 200);
    if (request.method !== "POST" || url.pathname !== "/v1/events")
      return jsonResponse({ error: "not_found" }, 404);

    return ingest(request, env);
  },

  async scheduled(_controller, env, ctx) {
    ctx.waitUntil(deleteExpiredRows(env.TELEMETRY_DB));
  },
};

async function ingest(request, env) {
  if (!env.TELEMETRY_DB || typeof env.INGEST_SALT !== "string" || env.INGEST_SALT.length < 32)
    return jsonResponse({ error: "service_unavailable" }, 503);

  const contentType = request.headers.get("content-type")?.split(";", 1)[0].trim().toLowerCase();
  if (contentType !== "application/json")
    return jsonResponse({ error: "content_type_must_be_json" }, 415);

  const declaredLength = Number(request.headers.get("content-length"));
  if (Number.isFinite(declaredLength) && declaredLength > MAX_BODY_BYTES)
    return jsonResponse({ error: "body_too_large" }, 413);

  let bodyText;
  try {
    bodyText = await request.text();
  } catch {
    return jsonResponse({ error: "invalid_body" }, 400);
  }
  if (new TextEncoder().encode(bodyText).byteLength > MAX_BODY_BYTES)
    return jsonResponse({ error: "body_too_large" }, 413);

  let body;
  try {
    body = JSON.parse(bodyText);
  } catch {
    return jsonResponse({ error: "invalid_json" }, 400);
  }

  const validation = validateEnvelope(body);
  if (!validation.ok)
    return jsonResponse({ error: "invalid_event_batch", detail: validation.error }, 400);

  const envelope = validation.envelope;
  const installationHash = await keyedHash(env.INGEST_SALT, `installation:${envelope.installation_id}`);
  const sessionHash = await keyedHash(env.INGEST_SALT, `session:${envelope.session_id}`);
  const hourBucket = new Date().toISOString().slice(0, 13);

  const rateResult = await env.TELEMETRY_DB.prepare(
    `INSERT INTO telemetry_rate_limits (installation_hash, hour_bucket, event_count)
     VALUES (?1, ?2, ?3)
     ON CONFLICT (installation_hash, hour_bucket)
     DO UPDATE SET event_count = event_count + excluded.event_count
     RETURNING event_count`,
  ).bind(installationHash, hourBucket, envelope.events.length).first();

  if (!rateResult || rateResult.event_count > MAX_EVENTS_PER_INSTALLATION_HOUR)
    return jsonResponse({ error: "rate_limited" }, 429, { "retry-after": "3600" });

  const receivedAt = new Date().toISOString();
  const statement = env.TELEMETRY_DB.prepare(
    `INSERT OR IGNORE INTO telemetry_events
      (event_id, received_at, occurred_at, installation_hash, session_hash,
       category, event_name, app_version, platform, attributes_json, schema_version)
     VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11)`,
  );
  const inserts = envelope.events.map(event => statement.bind(
    event.id,
    receivedAt,
    event.occurred_at,
    installationHash,
    sessionHash,
    event.category,
    event.name,
    envelope.app_version,
    envelope.platform,
    JSON.stringify(event.attributes),
    envelope.schema_version,
  ));
  await env.TELEMETRY_DB.batch(inserts);

  return jsonResponse({ accepted: envelope.events.length }, 202);
}

async function keyedHash(salt, value) {
  const encoder = new TextEncoder();
  const key = await crypto.subtle.importKey(
    "raw",
    encoder.encode(salt),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const digest = await crypto.subtle.sign("HMAC", key, encoder.encode(value));
  return [...new Uint8Array(digest)].map(byte => byte.toString(16).padStart(2, "0")).join("");
}

async function deleteExpiredRows(database) {
  await database.batch([
    database.prepare("DELETE FROM telemetry_events WHERE received_at < datetime('now', '-60 days')"),
    database.prepare("DELETE FROM telemetry_rate_limits WHERE hour_bucket < strftime('%Y-%m-%dT%H', 'now', '-2 days')"),
  ]);
}

function jsonResponse(body, status, extraHeaders = {}) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { ...JSON_HEADERS, ...extraHeaders },
  });
}

export const testing = Object.freeze({ ingest, keyedHash, deleteExpiredRows });
