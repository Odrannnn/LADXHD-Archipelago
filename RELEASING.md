# Publishing the assetless Archipelago build

Only publish source, the APWorld, and the assetless Android APK. Never commit or attach the
v1.0.0 ZIP, an extracted `Content`/`Data` tree, a locally generated `GameAssets` directory, or
an APK produced by the legacy asset-embedding workflow.

## Stable Android updates

Android accepts an in-place update only when these remain stable:

- application ID: `com.zelda.ladxhd.archipelago`;
- signing certificate;
- monotonically increasing `ApplicationVersion`/version code for public releases.

Keep the permanent release keystore offline, back it up securely, and never place it or its
passwords in Git. Losing that key means existing users cannot install trusted updates.

Set both `GameVersion` and `GameVersionCode` in `Directory.Build.props`. The Android version code
is independent of the dotted version name and must exceed previous public and device-test builds;
do not derive it by removing dots from the version name.

Public CI runs the synthetic smoke regressions without original assets. For the additional
installed-map and animation regressions, set `LADXHD_TEST_GAME_DATA` to the private migrated `Data`
directory; an explicitly configured but missing directory fails the run. The canonical full
migration check remains mandatory before releases and uses the separate source-ZIP/bootstrap inputs.

The permanent release certificate has SHA-256 fingerprint:

```text
05:45:9E:51:0A:84:C0:42:AD:2D:ED:15:87:C7:F6:FF:
08:17:BF:63:E3:97:BD:AA:50:A2:D8:05:41:45:03:9D
```

The first public release uses an APK Signature Scheme v3 proof-of-rotation lineage from the
earlier local development certificate. Android 9/API 28 and newer recognize the permanent key
while preserving the existing installation and app data. Android 7–8.1 require the legacy signer
for v2 compatibility because those systems do not support certificate rotation. Keep both the
permanent key and the lineage backed up; keep the legacy key only for that compatibility path.

Ordinary code-only updates reuse the installed game data. When the bundled migration inputs or
their output change, increment `GameAssetMigrator.AssetVersion` (and `PatchVersion` when the
patch-set format changes), migrate the canonical v1.0.0 ZIP, and update
`ExpectedTreeSha256`. That causes the next APK to route existing users through the transactional
rebuild screen. Do not change the expected source-archive hash unless support deliberately moves
away from the untouched v1.0.0 release.

## Release checklist

1. Build and run the Core/Archipelago smoke tests, including a migration with the canonical ZIP.
2. Build the Android Release APK without the content-generation import.
3. Align and sign it with the permanent release key and recorded signing lineage.
4. Run `python tools/verify_assetless_apk.py <apk>` and verify the signing certificate.
5. Install it over the previous public version and confirm it preserves saves/profiles.
6. If the asset version changed, test both selecting the ZIP and reusing its persisted document
   permission; confirm a new `UpdateBackups` snapshot and successful game launch.
7. Run `python tools/build_apworld.py` and test the generated `dist/ladxhd.apworld` with the
   supported Archipelago 0.6.7 installation.
8. Attach only the verified signed APK, `ladxhd.apworld`, checksums, and release notes to the
   GitHub release.

Users update by downloading and installing the newer APK over the existing app. They should not
uninstall first. If Android still has access to their original ZIP, a required asset rebuild is
one tap; otherwise the setup screen asks them to select it again.
