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

Without active repair:
- Placed objects lose their mesh references and disappear from the world.
- Newly placed objects import their mesh correctly but render without textures.

`VisualRefresher` solves these reference breaks via four core steps:

### Mesh Reassignment (`SetMesh`)
Meshes must be reassigned using the explicit overload `SetMesh(Mesh, Mesh)` rather than `SetMesh(ulong)`. The single-argument `ulong` overload checks `if (meshGUID == AssetMesh) return false;` and exits immediately if the requested GUID matches the component's stored GUID. Because GUIDs remain unchanged during a reload, only `SetMesh(Mesh, Mesh)` forces Unity to bind the newly re-created `Mesh` asset.

### Material Batching Reset
Clears the batched material dictionary using `MaterialBuilder.ResetBatching`.

### MaterialBuilder Singleton Reset
Resets the `MaterialBuilder` singleton state (`_instance = null`). On initial startup, `MaterialBuilder.Init()` captures `Settings.Get<Surfaces>()` once and never refreshes it natively. Resetting the singleton ensures newly added surface definitions from hot-loaded mods become visible to the material builder.

### Manager Material Invalidation
Populates native dirty material lists to trigger frame-budgeted rebuilds (20 ms limit per frame):
- `SegmentManager.DirtyMaterials`: Flags wall segments.
- `MoldingManager.DirtyMaterials`: Flags moldings and trims.
- `ZoneManager.DirtyPlatformMaterials`: Flags floor platforms and zones.

---

## 3. Scope & Design Trade-offs

### Para Characters Visuals
Character models and CAS bodies are not automatically rebuilt on reload. Full reconstruction requires unloading and re-instantiating active character game objects, which would cancel ongoing character actions and interactions. If character models appear untextured after executing `reloadall`, reload the save file.

### Mod Isolation
`reload` targets user-added `.mod` directories exclusively, leaving `Main.mod` (base game content) untouched. `reloadall` includes `Main.mod` for full-game refreshes.

---

## 4. Command Injection via Harmony

The native game engine resolves cheat console commands via reflection within `ProcessCheatCommandEvent.UpdateMessage`:

```csharp
GetType().GetMethod(uppercaseCommandName).Invoke(this, null);
```

Because C# does not support adding methods to existing compiled classes at runtime, ParaReload applies a Harmony prefix patch on `UpdateMessage`:

1. Intercepts custom command names (`reload`, `reloadall`, `reloadlist`, `autoreload`) prior to native method resolution.
2. Registers alias entries inside `Settings.Get<Cheats>().Aliases` so that custom commands appear seamlessly in console autocompletion and `help` output.
3. Suppresses default reflection invocation when a ParaReload command is detected.

---

## 5. Automatic File System Watcher (`autoreload`)

The `autoreload` command initializes a recursive `FileSystemWatcher` monitoring mod folders:

- **Debounce Timer**: Configurable delay (default `1.5` seconds) ensures multi-file writes or slow export tools finish before triggering a reload.
- **Ignore Rules**: Excludes internal file types and directories to prevent infinite event loops (`.meta`, `.import`, `.metacache`, `.tmp`, `.blend1`, `.blend2`, `_Metacache/`, `_GeneratedThumbnails/`, and `AssetReimportInfo.txt`).

---

## 6. Compilation & Building

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
