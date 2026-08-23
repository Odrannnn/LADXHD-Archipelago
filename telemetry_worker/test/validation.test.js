import assert from "node:assert/strict";
import test from "node:test";
import { MAX_BODY_BYTES, validateEnvelope } from "../src/validation.js";
import worker, { testing } from "../src/index.js";

const NOW = Date.parse("2026-08-23T12:00:00.000Z");

function validEnvelope() {
  return {
    schema_version: 1,
    installation_id: "6ba7b810-9dad-41d1-80b4-00c04fd430c8",
    session_id: "6ba7b811-9dad-41d1-80b4-00c04fd430c8",
    app_version: "2.0.7-ap1",
    platform: "android",
    events: [{
      id: "6ba7b812-9dad-41d1-80b4-00c04fd430c8",
      occurred_at: "2026-08-23T11:59:00.000Z",
      category: "randomizer",
      name: "ap_connect_failure",
      attributes: { attempt: 2, duration_ms: 3500, error_category: "network" },
    }],
  };
}

test("accepts an allowlisted event", () => {
  assert.equal(validateEnvelope(validEnvelope(), NOW).ok, true);
});

test("rejects identifiers and fields that could carry private randomizer data", () => {
  for (const [key, value] of [
    ["server", "archipelago.gg:38281"],
    ["slot_name", "Leonardo"],
    ["seed_name", "SecretSeed"],
    ["file_path", "C:\\Users\\name\\save.dat"],
    ["exception_message", "password=secret"],
    ["top_frame", "C:\\Users\\name\\Game.cs:42"],
  ]) {
    const envelope = validEnvelope();
    envelope.events[0].attributes[key] = value;
    const result = validateEnvelope(envelope, NOW);
    assert.equal(result.ok, false, key);
    assert.match(result.error, /unsupported attribute/);
  }
});

test("rejects unknown events and mismatched consent categories", () => {
  const unknown = validEnvelope();
  unknown.events[0].name = "raw_log";
  assert.equal(validateEnvelope(unknown, NOW).ok, false);

  const wrongCategory = validEnvelope();
  wrongCategory.events[0].category = "diagnostics";
  assert.equal(validateEnvelope(wrongCategory, NOW).ok, false);
});

test("rejects private text smuggled through constrained fields", () => {
  const exception = validEnvelope();
  exception.events[0] = {
    id: "6ba7b812-9dad-41d1-80b4-00c04fd430c8",
    occurred_at: "2026-08-23T11:59:00.000Z",
    category: "diagnostics",
    name: "crash",
    attributes: {
      exception_type: "System.Exception: server=example.org",
      stack_hash: "a".repeat(64),
      game_state: "unknown",
      fatal: true,
    },
  };
  assert.equal(validateEnvelope(exception, NOW).ok, false);

  const world = validEnvelope();
  world.events[0].name = "ap_connect_success";
  world.events[0].attributes = { attempt: 1, duration_ms: 10, world_version: "my-secret-seed" };
  assert.equal(validateEnvelope(world, NOW).ok, false);
});

test("rejects stale, future, duplicate, and malformed events", () => {
  const stale = validEnvelope();
  stale.events[0].occurred_at = "2026-01-01T00:00:00.000Z";
  assert.equal(validateEnvelope(stale, NOW).ok, false);

  const duplicate = validEnvelope();
  duplicate.events.push(structuredClone(duplicate.events[0]));
  assert.match(validateEnvelope(duplicate, NOW).error, /duplicate id/);

  const malformed = validEnvelope();
  malformed.installation_id = "device-123";
  assert.equal(validateEnvelope(malformed, NOW).ok, false);
});

test("health route is minimal and ingestion rejects oversized bodies", async () => {
  const health = await worker.fetch(new Request("https://example.test/health"), {});
  assert.equal(health.status, 200);
  assert.deepEqual(await health.json(), { status: "ok" });

  const request = new Request("https://example.test/v1/events", {
    method: "POST",
    headers: { "content-type": "application/json", "content-length": String(MAX_BODY_BYTES + 1) },
    body: "{}",
  });
  const response = await worker.fetch(request, { TELEMETRY_DB: {}, INGEST_SALT: "x".repeat(32) });
  assert.equal(response.status, 413);
});

test("installation hashing is salted and deterministic", async () => {
  const first = await testing.keyedHash("a".repeat(32), "installation:one");
  const again = await testing.keyedHash("a".repeat(32), "installation:one");
  const differentSalt = await testing.keyedHash("b".repeat(32), "installation:one");
  assert.equal(first, again);
  assert.notEqual(first, differentSalt);
  assert.match(first, /^[0-9a-f]{64}$/);
});
