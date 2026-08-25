# LADXHD Archipelago agent instructions

These instructions apply to the entire repository. Keep this file suitable for the public
repository: operational rules belong here, but private paths, copyrighted inputs, credentials,
signing material, room data, and telemetry records do not.

## Project boundary

This fork provides the LADXHD Archipelago integration and an assetless Android application. The
public APK may contain game code, Archipelago code, and redistribution-safe migration inputs, but
it must never contain the original game's `Content` or `Data` trees.

Before changing an area, read the relevant public documentation:

- `README.md` for repository scope and attribution;
- `ARCHIPELAGO.md` for client, APWorld, and verification behavior;
- `ANDROID.md` for assetless installation and update behavior;
- `RELEASING.md` for signing, compatibility, and publication requirements.

Treat private handoff material as confidential context, not as content to copy into source files,
commit messages, logs, release notes, or GitHub.

## Non-negotiable privacy rules

Never commit, push, package, upload, or quote:

- the original LADXHD v1.0.0 ZIP or its host filesystem path;
- extracted original `Content` or `Data` files;
- a generated `GameAssets` tree;
- anything under `.local/`;
- keystores, passwords, signing keys, signing-lineage files, or private certificate material;
- Archipelago room files, generated seeds, spoiler logs, connection passwords, or private player
  configurations;
- raw telemetry rows, device identifiers, crash payloads, or user data;
- aligned-but-unsigned APKs, `.idsig` files, or APKs made by the legacy asset-embedding workflow.

The tracked `dist/ladxhd.apworld` is public. Release APKs are ignored local artifacts and are
published only after all checks below pass.

Run `python tools/verify_public_source.py` before every commit and again before every push. Inspect
the exact staged paths as well; a successful automated guard is not permission to publish private
data.

## Working-tree and commit discipline

- Start by checking `git status --short --branch`, the active branch, and its upstream.
- Preserve unrelated and pre-existing user changes. Never discard them to make a task easier.
- Make one logical patch per commit. Different bugs and release preparation must remain separate
  commits.
- Stage explicit paths only. Do not use `git add .`, `git add -A`, or another broad staging command.
- Do not rewrite, squash, reset, or force-push history unless the user explicitly requests it.
- Do not push, tag, publish a release, or change external state unless the user explicitly requests
  that action.
- The local public release branch is normally `public-main`, tracking GitHub `origin/main`. Before
  pushing, fetch and verify `origin/main...public-main`; only perform a fast-forward update unless
  the user authorizes a different operation.
- Keep implementation commits separate from the version/changelog/rebuilt-APWorld release commit.

Every bug fix should include the narrowest useful regression coverage in
`ProjectZ.Archipelago.SmokeTests` or another appropriate test surface.

## Pinned build environment

Always use the pinned `ladxhd-android-builder:net9` Docker image for local .NET compilation,
smoke tests, full asset migration, and Android APK builds. Do not use a host-installed .NET SDK or
silently substitute another SDK, workload, Java version, Android SDK, or container tag.

The pinned image is defined by `.local/Dockerfile.android-builder`. That file and its build cache
are local-only operational material. If the image is absent or cannot run, stop and report the
problem rather than falling back to an unpinned build.

Do not impose a CPU limit on the container unless the user requests one. Read-only source
inspection, Git/GitHub operations, and repository Python utilities may use their configured host
runtime; they do not replace the pinned .NET/Android build environment.

## Canonical full migration

The full migration smoke test has exactly two logical inputs:

- `LADXHD_V100_ZIP`: the user's private, untouched v1.0.0 source archive, mounted read-only in the
  container;
- `LADXHD_ANDROID_BOOTSTRAP`: `/workspace/ladxhd_patcher_source_code/Resources` inside the
  container.

Never point `LADXHD_ANDROID_BOOTSTRAP` at an extracted patch cache. The canonical Resources
directory must include all four bootstrap inputs:

- `patches_android.zip`;
- `android_buttons.zip`;
- `d3map`;
- `d3mapdata`.

Mount the repository at `/workspace`, mount the private source ZIP read-only at a neutral container
path, set both environment variables, and run this command inside the pinned image:

```text
dotnet run --project ladxhd_game_source_code/ProjectZ.Archipelago.SmokeTests/ProjectZ.Archipelago.SmokeTests.csproj -c Release
```

The canonical result is 958 files, 46,027,375 bytes, with generated-tree SHA-256
`D1150E5ADCA23A4D0DCC8A2A470630D12C45EC3D1838ADADA6EC7B0C1A3E3900`. A mismatch is a failure;
do not update the expected constants merely to accept unexpected output.

Run the full migration before every release and whenever migration code, migration inputs, asset
validation, Android bootstrap resources, or expected asset constants change. The ordinary smoke
suite without the two environment variables is not a substitute.

## Verification

Use checks proportional to the change. A release candidate requires all of the following:

1. Run the public-source guard.
2. Compile the Core project in the pinned image.
3. Run the Archipelago smoke suite in the pinned image.
4. Run the canonical full migration in the pinned image and verify its exact hash, file count, and
   byte count.
5. Compile the Python sources used by the APWorld and its tooling.
6. Build `dist/ladxhd.apworld` twice in the same environment and confirm identical SHA-256 output.
7. Build the Android Release APK in the pinned image using the assetless project path only.
8. Align and sign using the established private signing procedure and certificate lineage.
9. Run `python tools/verify_assetless_apk.py <signed-apk>`.
10. Verify package name, version name/code, APK signatures, signing lineage, and final checksums.
11. Confirm the APK contains the allowed bootstrap inputs and no original `Content` or `Data`.
12. Recheck the Git working tree and inspect the final diff and staged file list.

When a compatible device is available, install over the previous public APK and verify that saves,
settings, generated assets, and Archipelago profiles survive. Never uninstall as an update step.

## APWorld and randomizer behavior

- Build the APWorld with `python tools/build_apworld.py`; the tracked output is
  `dist/ladxhd.apworld`.
- Keep APWorld generation deterministic and compatible with the supported Archipelago version
  documented in `ARCHIPELAGO.md`.
- When changing item handling, event spawning, trade progression, access rules, or location checks,
  compare the game implementation with `archipelago_world/ladxhd` and test both sides of the
  contract.
- Do not infer vanilla event behavior when the APWorld logic is authoritative. Trace the relevant
  option, rule, item, location, and in-game state before patching.
- Preserve existing saves and generated seeds whenever possible. If a change requires a new seed,
  asset rebuild, or save migration, state that explicitly in code documentation and release notes.
- Network reconnection, save loading, item replay, and progressive-item behavior require regression
  coverage because they cross persistent state and Archipelago session state.

## Versioning and release conventions

- The public version is defined in the root `Directory.Build.props`. Keep Android's application ID
  `com.zelda.ladxhd.archipelago` stable and ensure the derived application version code increases.
- Use plain semantic release versions such as `2.0.13`; do not restore the historical `-ap1`
  suffix. Git tags use `vX.Y.Z` and release titles use `LADXHD Archipelago X.Y.Z`.
- Update `CHANGELOG.md` in the release-preparation commit.
- Existing saves and seeds should be described accurately in the release notes; never promise
  compatibility without checking the change.
- Wait for GitHub's `Verify assetless source` workflow to pass on the exact release commit before
  publishing.
- Create the tag from the exact verified commit and confirm it resolves back to that commit.
- Publish only the final signed APK, `ladxhd.apworld`, and `SHA256SUMS.txt`. Put release notes in the
  GitHub release body. Do not upload the aligned APK, `.idsig`, private logs, source ZIP, extracted
  assets, or local release-support files.
- Verify the published release is neither a draft nor an accidental prerelease and that every asset
  name, size, and digest matches the local final artifact.

## Telemetry and crash investigation

Treat telemetry as private operational data. Query only what is necessary for the reported problem,
avoid exposing identifiers or complete payloads in tool output, and summarize findings without
copying sensitive rows into the repository or GitHub. Stack traces and diagnostics must remain
privacy-safe and must respect the application's diagnostics controls.

## Completion reporting

Report the concrete result first. For code changes, identify the affected behavior and the checks
that passed. For releases, provide the release URL, version/tag, published artifacts, CI result, and
compatibility impact. If any required check was skipped or could not run in the pinned environment,
say so clearly; do not describe the work as fully verified.
