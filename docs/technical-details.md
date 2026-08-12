# ParaReload Technical Architecture & Implementation Details

This document outlines the internal mechanics, injection hooks, and world-stitching algorithms powering **ParaReload**.

Creator: **infinition**

---

## 1. Hot-Reloading Pipeline Overview

When executing the `reload` command in the cheat console (`CTRL` + `SHIFT` + `C`), ParaReload executes a 6-stage lifecycle to reload game assets without requiring a game restart:

1. **New Mod Discovery**: Scans `%USERPROFILE%\AppData\LocalLow\Paralives\Paralives\` for any newly created `.mod` folders not yet registered in memory. Calls `ModManager.LoadExistingMod` while preserving user preferences and active edit states.
2. **Disk Unloading & Re-reading**: Calls `AssetManager.UnloadAssetPackage` followed by `LoadAssetPackage` for target mods. Forces invalidation of `_Metacache` by comparing modification timestamps.
3. **Setting Recompilation**: Flags `IsSettingsCompilationDirty` and `IsTranslationDirty` to trigger recompilation of `Items.setting`, `Surfaces.setting`, `Translations.setting`, and related mod setting files. Mod definitions are merged cleanly into game definitions.
4. **Source Asset Re-importing**: Detects changes in modified FBX or PNG files by comparing file checksums against `.meta` files, triggering the native asset import progress bar.
5. **World Stitching**: Executes `VisualRefresher` to repair broken in-memory Unity object references on existing objects placed in the world.
6. **Thumbnails & Build Catalog**: Generates missing preview thumbnails and forces a full redraw of the Build Mode catalog interface.

---

## 2. World Stitching & Object Restoration Mechanics

Standard asset package unloading (`AssetData.Unload()`) destroys active Unity `Mesh` and `Texture2D` instances in memory via `Object.Destroy`. However, persistent object GUIDs remain unchanged because they originate from `.meta` definitions.

Without active repair, objects already placed in the world lose their rendering references and disappear, while newly placed objects render without textures.

`VisualRefresher` solves these four reference breaks:

### Mesh Reassignment
Meshes must be reassigned using the explicit overload `SetMesh(Mesh, Mesh)` rather than `SetMesh(ulong)`. The single-argument `ulong` overload exits immediately if the requested GUID already matches the existing component GUID. Because GUIDs remain unchanged during a reload, only `SetMesh(Mesh, Mesh)` forces Unity to bind the freshly re-created `Mesh` asset.

### Material Batching Reset
Clears the batched material dictionary using `MaterialBuilder.ResetBatching`.

### MaterialBuilder Singleton Reset
Resets the `MaterialBuilder` singleton state. On initial startup, `MaterialBuilder` caches the global `Surfaces` definition and never re-reads it natively. Resetting the singleton ensures newly added surfaces from hot-loaded mods become visible to the material construction system.

### Material Invalidation
Flags materials, walls, floors, mouldings, and platforms as dirty, forcing native rendering systems to rebuild visual components over subsequent animation frames.

---

## 3. Command Injection via Harmony

The native game engine resolves cheat console commands via reflection within `ProcessCheatCommandEvent.UpdateMessage`:

```csharp
GetType().GetMethod(uppercaseCommandName).Invoke(this, null);
```

Because C# does not support adding methods to existing compiled classes at runtime, ParaReload applies a Harmony prefix patch on `UpdateMessage`:

1. Intercepts custom command names (`reload`, `reloadall`, `reloadlist`, `autoreload`) prior to native method resolution.
2. Registers alias entries inside `Settings.Get<Cheats>().Aliases` so that custom commands appear seamlessly in console autocompletion and `help` output.
3. Suppresses default reflection invocation when a ParaReload command is detected.

---

## 4. Automatic File System Watcher (`autoreload`)

The `autoreload` command initializes a recursive `FileSystemWatcher` monitoring mod folders:

- **Debounce Timer**: Configurable delay (default `1.5` seconds) ensures multi-file writes or slow export tools finish before triggering a reload.
- **Ignore Rules**: Excludes internal file types and directories to prevent infinite event loops (`.meta`, `.import`, `.metacache`, `.tmp`, `.blend1`, `.blend2`, `_Metacache/`, `_GeneratedThumbnails/`, and `AssetReimportInfo.txt`).

---

## 5. Compilation & Building

### Prerequisites
- .NET SDK (supporting `net472` target framework).
- Paralives game installation with BepInEx 5.4.x installed.

### Build Command
Run the build from the project root directory:

```bash
dotnet build -c Release
```

The output assembly (`ParaReload.dll`) deploys directly into:
`C:\Paralives.Access\Paralives\BepInEx\plugins\ParaReload\`

To customize the installation path, update the `<GameDir>` property inside `ParaReload.csproj`.
