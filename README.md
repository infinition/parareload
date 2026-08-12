<div align="center">
  <img src="assets/icon.png" alt="ParaReload Logo" width="160" />

  # ParaReload

  [English](#english) | [Français](#francais)
</div>

---

<a name="english"></a>
## English

**ParaReload** is a BepInEx plugin for Paralives that allows modders and players to hot-reload mod assets, textures, 3D models, and settings directly in-game without restarting.

Creator: **infinition**  
Target Game: Paralives Early Access (BepInEx 5.4+)

### Key Features

- **Live Asset Reloading**: Update 3D models (FBX), textures (PNG), and configuration files while playing.
- **Cheat Console Commands**: Trigger reloads instantly with `CTRL` + `SHIFT` + `C`.
- **Automatic File Watching**: Monitor mod directories and reload automatically when saving files in Blender, Photoshop, or text editors.
- **World Restoration**: Automatically repairs in-memory mesh and material references for objects already placed in the world via `VisualRefresher`.

### Console Commands

Press `CTRL` + `SHIFT` + `C` in-game to open the cheat console:

| Command | Action |
| --- | --- |
| `reload` | Reloads all user mods (new `.mod` folders, modified textures, FBX files, and settings). Recommended for Blender modding. |
| `reload <ModName>` | Reloads a specific mod (supports partial name matching). |
| `reloadall` | Reloads all mods including base game data (`Main.mod`). Heavy operation. |
| `reloadlist` | Lists all currently loaded mods, status, and asset counts. |
| `autoreload [on\|off]` | Toggles automatic file watching mode. |

### Recommended Modding Workflow (Blender / Photoshop)

> [!TIP]
> **Complementary Blender Addon**: Combine **ParaReload** with [**ParaForge**](https://github.com/infinition/paraforge), a Blender extension that automatically validates 3D models against Paralives rules, builds texture maps, and exports ready-to-use `.mod` packages straight into your game folder.

For standard 3D modeling and texture editing in Blender:
1. Enable `autoreload` in game or run `reload` when saving changes.
2. Use standard `reload` instead of `reloadall`. `reload` handles all custom `.mod` directories while leaving base game assets untouched for fast iteration.

### Scope & Limitations

- **System Mods Excluded**: System folders (`Local`, `MyOptions`, `MySavedGames`, `MyPremadeHouseholds`, `MyPremadeLot`, `MyPremadeOutfits`) are skipped to protect active game state.
- **Para Characters Visuals**: Character visuals are not automatically rebuilt on reload to avoid interrupting ongoing character actions and interactions. If character models appear untextured after a full `reloadall`, simply reload your save file.
- **Base Game Data**: `Main.mod` (2222 base game objects) is excluded by default during `reload`. Use `reloadall` only if you explicitly modify base game files, and save your game first.

### Quick Installation

1. Install **BepInEx 5.4** for Paralives.
2. Download `ParaReload.zip` from the [Releases](https://github.com/infinition/parareload/releases) page.
3. Extract `ParaReload.dll` into your BepInEx plugins directory:  
   `Paralives/BepInEx/plugins/ParaReload/ParaReload.dll`
4. Launch Paralives and open the cheat console using `CTRL` + `SHIFT` + `C`.

### Configuration

Options are configured in `BepInEx/config/infinition.paralives.parareload.cfg`:

| Setting Key | Default | Description |
| --- | --- | --- |
| `AutoReload / Enabled` | `false` | Enables automatic file watching when the game starts. |
| `AutoReload / DebounceSeconds` | `1.5` | Delay in seconds after file modifications before triggering reload. |
| `Reload / RegenerateThumbnails` | `true` | Regenerates missing preview thumbnails after reloading. |
| `Reload / RefreshBuildCatalog` | `true` | Forces the Build Mode catalog to redraw. |

### Technical & Architecture Documentation

For details regarding world stitching algorithms (`VisualRefresher`), Harmony reflection injection, and building the C# project source code, view the [Technical Details Documentation](docs/technical-details.md).

---

<a name="francais"></a>
## Français

**ParaReload** est un plugin BepInEx pour Paralives permettant aux moddeurs et joueurs de recharger a chaud les assets, textures, modeles 3D et reglages de mods directement en jeu sans redemarrer.

Créateur : **infinition**  
Cible : Paralives Early Access (BepInEx 5.4+)

### Fonctionnalités Principales

- **Rechargement en Direct** : Mettez a jour vos modeles 3D (FBX), textures (PNG) et fichiers de configuration en cours de partie.
- **Console de Triche** : Lancez les rechargements facilement via `CTRL` + `MAJ` + `C`.
- **Surveillance Automatique** : Surveillez les dossiers de mods et rechargez automatiquement lors de la sauvegarde sous Blender, Photoshop ou un editeur de texte.
- **Restauration du Monde** : Repare automatiquement les references de maillages et materiaux pour les objets deja poses dans le monde via `VisualRefresher`.

### Commandes Console

Appuyez sur `CTRL` + `MAJ` + `C` en jeu pour ouvrir la console :

| Commande | Action |
| --- | --- |
| `reload` | Recharge tous les mods utilisateur (nouveaux dossiers `.mod`, textures, FBX et parametres). Recommande pour la creation sous Blender. |
| `reload <NomDuMod>` | Recharge un mod specifique (accepte la recherche partielle par nom). |
| `reloadall` | Recharge tous les mods, y compris le contenu de base du jeu (`Main.mod`). Operation plus lourde. |
| `reloadlist` | Liste l'ensemble des mods charges, leur statut et leur nombre d'assets. |
| `autoreload [on\|off]` | Bascule le mode de surveillance automatique des fichiers. |

### Flux de Travail Recommandé (Blender / Photoshop)

> [!TIP]
> **Extension Blender complémentaire** : Associez **ParaReload** avec [**ParaForge**](https://github.com/infinition/paraforge), une extension Blender qui valide automatiquement vos modèles 3D selon les règles de Paralives, génère les textures et exporte vos packages `.mod` directement dans le dossier du jeu.

Pour la creation 3D et le texte de mods :
1. Activez `autoreload` en jeu ou tapez `reload` lors de la sauvegarde dans Blender.
2. Utilisez la commande `reload` simple au lieu de `reloadall`. `reload` prend en charge vos dossiers `.mod` sans toucher aux assets du jeu de base, garantissant des rechargements rapides.

### Portée et Limites

- **Mods Système Exclus** : Les dossiers systeme (`Local`, `MyOptions`, `MySavedGames`, `MyPremadeHouseholds`, `MyPremadeLot`, `MyPremadeOutfits`) sont ignores afin de preserver l'etat en memoire du jeu.
- **Visuels des Paras** : Les visuels des personnages ne sont pas rebatis automatiquement pour eviter d'annuler les interactions en cours. Si des personnages apparaissent sans texture apres un `reloadall`, rechargez simplement votre sauvegarde.
- **Données du Jeu de Base** : `Main.mod` (les 2222 objets du jeu) est exclu par defaut lors d'un `reload`. N'utilisez `reloadall` que si vous modifiez sciemment les fichiers du jeu de base, et sauvegardez votre partie au prealable.

### Installation Rapide

1. Installez **BepInEx 5.4** pour Paralives.
2. Telechargez `ParaReload.zip` depuis la page des [Releases](https://github.com/infinition/parareload/releases).
3. Extrayez `ParaReload.dll` dans votre dossier de plugins BepInEx :  
   `Paralives/BepInEx/plugins/ParaReload/ParaReload.dll`
4. Lancez Paralives et ouvrez la console avec `CTRL` + `MAJ` + `C`.

### Configuration

Les options sont enregistrees dans `BepInEx/config/infinition.paralives.parareload.cfg` :

| Clé | Défaut | Description |
| --- | --- | --- |
| `AutoReload / Enabled` | `false` | Active la surveillance des fichiers des le lancement du jeu. |
| `AutoReload / DebounceSeconds` | `1.5` | Delai d'attente en secondes apres une modification avant le rechargement. |
| `Reload / RegenerateThumbnails` | `true` | Regenere les vignettes de previsualisation manquantes apres le rechargement. |
| `Reload / RefreshBuildCatalog` | `true` | Force le redessinage du catalogue du mode Construction. |

### Documentation Technique et Compilation

Pour en savoir plus sur les algorithmes de recouture du monde, les injections Harmony et la compilation du projet C#, consultez la [Documentation Technique (en anglais)](docs/technical-details.md).
