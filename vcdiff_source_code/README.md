# VCDiff-CLI
This is just a simple command line cross-platform application that can create and apply ".vcdiff" patches. VCDiff is the algorithm that "XDelta" is based off of, so file size is very similar when creating patches with VCDiff. This application was created to be used with the **Link's Awakening DX HD Updated** project to generate patches for the patcher program. 

## Usage
Pre-compiled builds for all OS can be found in the `.builds` folder. Arguments are set up in the same was as XDelta so it can be a drop-in replacement. There is also the addition of a "manifest mode" that can batch files which is exponentially faster than having to open/close the program for every single patch. 

#### Arguments:
```
-create   : Creates a vcdiff patch from two files.
-apply    : Applies a vcdiff patch to a file.
-manifest : Uses a manifest (.txt) file to batch process multiple files.
-quiet    : Fully silences all console output.
```
#### Standard Usage:
```
vcdiff.exe -create <oldfile> <newfile> <patchfile>
vcdiff.exe -create <oldfile> <newfile> <patchfile> -quiet
vcdiff.exe -apply  <oldfile> <patchfile> <newfile>
vcdiff.exe -apply  <oldfile> <patchfile> <newfile> -quiet
```
#### Manifest Usage:
```
vcdiff.exe -create -manifest <manifestfile>
vcdiff.exe -create -manifest <manifestfile> -quiet
vcdiff.exe -apply  -manifest <manifestfile>
vcdiff.exe -apply  -manifest <manifestfile> -quiet
```
## Manifest Mode:
A manifest file should be created with a script, as manually generating it by hand would be somewhat cumbersome. The format is simple, it's just three file paths separated by the pipe `|` character. The order of files depends on whether `-create` or `-apply` is used. If neither of these commands is used, manifest mode will assume `-create` mode.

- A "manifest" is just a standard text document `manifest.txt` with entries.
- Each line in a manifest is the arguments separated by pipe \"|\" character.
- For create manifests, each line is: `oldFile|newFile|patchFile`.
- For apply manifests, each line is: `oldFile|patchFile|newFile`.

