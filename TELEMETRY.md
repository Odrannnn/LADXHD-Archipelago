# Optional telemetry and privacy

LADXHD Archipelago telemetry is optional and enabled by default for new installations of this early test build. Both categories are selected in the mandatory first-run privacy notice, and nothing is uploaded until that notice is acknowledged. You can disable either crash diagnostics or randomizer connection statistics there, change either choice later through **Settings > Diagnostics**, or choose **Disable all**. Existing explicit choices are preserved during updates. Disabling a category immediately removes queued events of that category from the device.

The endpoint is:

```text
https://ladxhd-archipelago-telemetry.leonardo-701.workers.dev/v1/events
```

## What can be sent

Crash diagnostics contain only the exception type, a one-way SHA-256 hash of the stack trace, a coarse game state, and whether the crash was fatal. Exception messages and raw stack traces are not sent.

Randomizer diagnostics contain categorical connection outcomes and aggregate counts such as connection duration, reconnects, completed checks, received items, and unsupported items. They may also contain the APWorld version and a small allowlist of supported boolean/choice options.

Every batch includes the app version, platform, a session UUID, and an anonymous installation UUID. The installation UUID is generated locally and rotates after 30 days. The service transforms both UUIDs with a Worker-only keyed hash before storing them.

## What is not sent

The client and server schemas do not accept:

- Archipelago server addresses, ports, or passwords;
- player, slot, or seed names;
- seed files, placements, save data, or room/spoiler files;
- exact item names or location names;
- file paths, arbitrary logs, exception messages, or raw stack traces;
- the original game ZIP or extracted game assets.

The local `location-catalog.jsonl` developer aid is not part of telemetry and is never uploaded by this system.

## Storage and retention

While offline, enabled events use a capped app-private queue: at most 256 events or 512 KiB. A successful upload removes them. Disabling a category purges the affected local queue.

The Cloudflare Worker stores allowlisted event rows in D1 for up to 60 days, then a daily retention task deletes them. The Worker does not read or store the request IP address or user-agent. Cloudflare necessarily processes connection metadata, including the source IP, while serving the HTTPS request and may retain platform logs under the Cloudflare account's own service policies.

There is no public query endpoint. Database analysis requires authenticated Cloudflare account access. Previously received anonymous events cannot reliably be matched back to a person; they expire through the retention policy after consent is withdrawn.

## Source and availability

The ingestion schema, validation tests, D1 migration, and deployment configuration are in [`telemetry_worker`](telemetry_worker). Telemetry failures never block gameplay or Archipelago reconnects. If the endpoint is unavailable, opted-in events remain in the bounded queue for a later retry.
