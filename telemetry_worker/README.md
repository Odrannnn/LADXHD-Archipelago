# LADXHD Archipelago telemetry Worker

This optional service accepts a small, fixed schema of anonymous diagnostic events from builds whose user explicitly enabled telemetry. It deliberately rejects server addresses, credentials, slot/player names, seed names, save data, arbitrary logs, exact item/location names, and file paths.

## Data handling

- Installation and session UUIDs are hashed with the Worker-only `INGEST_SALT` before storage.
- Only allowlisted events and attributes in `src/validation.js` are accepted.
- A request is limited to 64 KiB and 20 events. An installation is limited to 240 events per UTC hour.
- Event rows expire after 60 days via the daily Cron Trigger.
- No ingestion credential is embedded in the game. The endpoint must therefore also be protected with Cloudflare abuse controls appropriate to its deployment.
- Cloudflare necessarily processes connection metadata such as the source IP, but this Worker does not read or store it.

## Local verification

```sh
pnpm install
pnpm test
pnpm check
```

Create a local secret before using `wrangler dev`:

```sh
wrangler secret put INGEST_SALT
```

Use at least 32 random bytes. Do not commit `.dev.vars`, `.env`, Wrangler state, or secret values.

## Deployment

1. Run `wrangler d1 create ladxhd-archipelago-telemetry`.
2. Replace `REPLACE_AFTER_D1_CREATE` in `wrangler.jsonc` with the returned database ID.
3. Run `wrangler d1 migrations apply TELEMETRY_DB --remote`.
4. Run `wrangler secret put INGEST_SALT` and enter a randomly generated secret.
5. Run `wrangler deploy`.
6. Configure Cloudflare rate limiting/WAF controls for `POST /v1/events` and keep the generated `workers.dev` hostname out of unrelated applications.

The Worker intentionally exposes no query or administration endpoint. Analyze the database through authenticated Cloudflare D1 tooling.
