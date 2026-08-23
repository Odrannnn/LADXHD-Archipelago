export const MAX_BODY_BYTES = 64 * 1024;
export const MAX_EVENTS_PER_BATCH = 20;
export const MAX_EVENTS_PER_INSTALLATION_HOUR = 240;

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const VERSION = /^[0-9A-Za-z][0-9A-Za-z.+_-]{0,31}$/;
const WORLD_VERSION = /^[0-9]{1,4}(?:\.[0-9A-Za-z_-]{1,16}){0,3}$/;
const TYPE_NAME = /^[A-Za-z_][A-Za-z0-9_.+`]{0,95}$/;
const FRAME_TYPE = /^ProjectZ\.[A-Za-z_][A-Za-z0-9_.+`<>]{0,158}$/;
const FRAME_METHOD = /^[A-Za-z_.<>][A-Za-z0-9_.+`<>]{0,95}$/;
const PLATFORM = /^(android|windows|linux|macos)$/;

const EVENT_SCHEMAS = Object.freeze({
  app_started: schema("diagnostics", {
    launch_source: enumValue(["companion", "direct", "resume", "unknown"]),
    previous_crash: booleanValue,
  }),
  app_stopped: schema("diagnostics", {
    runtime_seconds: integerValue(0, 604800),
  }),
  crash: schema("diagnostics", {
    exception_type: regexValue(TYPE_NAME),
    stack_hash: hexValue(64),
    build_id: hexValue(32),
    frames: stackFramesValue,
    game_state: enumValue(["startup", "menu", "gameplay", "shutdown", "unknown"]),
    fatal: booleanValue,
  }),
  ap_connect_attempt: schema("randomizer", {
    attempt: integerValue(1, 1000),
  }),
  ap_connect_success: schema("randomizer", {
    attempt: integerValue(1, 1000),
    duration_ms: integerValue(0, 3600000),
    world_version: regexValue(WORLD_VERSION),
  }),
  ap_connect_failure: schema("randomizer", {
    attempt: integerValue(1, 1000),
    duration_ms: integerValue(0, 3600000),
    error_category: enumValue(["network", "authentication", "seed_mismatch", "protocol", "timeout", "unknown"]),
  }),
  ap_disconnected: schema("randomizer", {
    connected_seconds: integerValue(0, 604800),
    reason_category: enumValue(["network", "server", "client", "protocol", "unknown"]),
  }),
  ap_reconnect_scheduled: schema("randomizer", {
    attempt: integerValue(1, 1000),
    delay_seconds: integerValue(0, 3600),
  }),
  ap_session_summary: schema("randomizer", {
    connected_seconds: integerValue(0, 604800),
    disconnect_count: integerValue(0, 1000000),
    reconnect_count: integerValue(0, 1000000),
    checks_reported: integerValue(0, 1000000),
    items_received: integerValue(0, 1000000),
    unsupported_items: integerValue(0, 1000000),
  }),
  randomizer_manifest: schema("randomizer", {
    world_version: regexValue(WORLD_VERSION),
    logic: enumValue(["normal", "hard", "glitched", "hell", "unknown"]),
    trade_quest: booleanValue,
    rooster: booleanValue,
    warp_to_start: booleanValue,
  }),
});

function schema(category, attributes) {
  return Object.freeze({ category, attributes: Object.freeze(attributes) });
}

function hexValue(length) {
  const regex = new RegExp(`^[0-9a-f]{${length}}$`, "i");
  return value => typeof value === "string" && regex.test(value);
}

function regexValue(regex) {
  return value => typeof value === "string" && regex.test(value);
}

function enumValue(values) {
  const allowed = new Set(values);
  return value => typeof value === "string" && allowed.has(value);
}

function integerValue(min, max) {
  return value => Number.isInteger(value) && value >= min && value <= max;
}

function booleanValue(value) {
  return typeof value === "boolean";
}

function stackFramesValue(value) {
  return Array.isArray(value) && value.length >= 1 && value.length <= 8 && value.every(frame =>
    isPlainObject(frame) &&
    hasExactKeys(frame, ["assembly", "type", "method", "metadata_token", "il_offset"]) &&
    (frame.assembly === "ProjectZ.Core" || frame.assembly === "ProjectZ.Android") &&
    regexValue(FRAME_TYPE)(frame.type) &&
    regexValue(FRAME_METHOD)(frame.method) &&
    integerValue(1, 2147483647)(frame.metadata_token) &&
    integerValue(-1, 2147483647)(frame.il_offset));
}

export function validateEnvelope(value, now = Date.now()) {
  if (!isPlainObject(value)) return invalid("body must be a JSON object");
  if (!hasExactKeys(value, ["schema_version", "installation_id", "session_id", "app_version", "platform", "events"]))
    return invalid("body contains missing or unsupported fields");
  if (value.schema_version !== 1) return invalid("unsupported schema_version");
  if (!UUID.test(value.installation_id)) return invalid("invalid installation_id");
  if (!UUID.test(value.session_id)) return invalid("invalid session_id");
  if (typeof value.app_version !== "string" || !VERSION.test(value.app_version)) return invalid("invalid app_version");
  if (typeof value.platform !== "string" || !PLATFORM.test(value.platform)) return invalid("invalid platform");
  if (!Array.isArray(value.events) || value.events.length < 1 || value.events.length > MAX_EVENTS_PER_BATCH)
    return invalid(`events must contain 1-${MAX_EVENTS_PER_BATCH} entries`);

  const seenIds = new Set();
  for (let index = 0; index < value.events.length; index += 1) {
    const result = validateEvent(value.events[index], now);
    if (!result.ok) return invalid(`events[${index}]: ${result.error}`);
    if (seenIds.has(value.events[index].id)) return invalid(`events[${index}]: duplicate id`);
    seenIds.add(value.events[index].id);
  }
  return { ok: true, envelope: value };
}

function validateEvent(event, now) {
  if (!isPlainObject(event)) return invalid("must be an object");
  if (!hasExactKeys(event, ["id", "occurred_at", "category", "name", "attributes"]))
    return invalid("contains missing or unsupported fields");
  if (!UUID.test(event.id)) return invalid("invalid id");
  if (typeof event.occurred_at !== "string") return invalid("invalid occurred_at");
  const occurredAt = Date.parse(event.occurred_at);
  if (!Number.isFinite(occurredAt) || occurredAt < now - 30 * 86400000 || occurredAt > now + 10 * 60000)
    return invalid("occurred_at is outside the accepted window");

  const eventSchema = EVENT_SCHEMAS[event.name];
  if (!eventSchema) return invalid("unsupported name");
  if (event.category !== eventSchema.category) return invalid("category does not match name");
  if (!isPlainObject(event.attributes)) return invalid("attributes must be an object");

  const keys = Object.keys(event.attributes);
  if (keys.length > 16) return invalid("too many attributes");
  for (const key of keys) {
    const validator = eventSchema.attributes[key];
    if (!validator) return invalid(`unsupported attribute '${key}'`);
    if (!validator(event.attributes[key])) return invalid(`invalid attribute '${key}'`);
  }
  return { ok: true };
}

function hasExactKeys(value, keys) {
  const actual = Object.keys(value).sort();
  const expected = [...keys].sort();
  return actual.length === expected.length && actual.every((key, index) => key === expected[index]);
}

function isPlainObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value) && Object.getPrototypeOf(value) === Object.prototype;
}

function invalid(error) {
  return { ok: false, error };
}
