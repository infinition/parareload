# ParaReload

[English](#english) | [Français](#francais)

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
- **World Restoration**: Automatically repairs in-memory mesh and material references for objects already placed in the world.

### Console Commands

Press `CTRL` + `SHIFT` + `C` in-game to open the cheat console:

| Command | Action |
| --- | --- |
| `reload` | Reloads all user mods (new `.mod` folders, modified textures, FBX files, and settings). |
| `reload <ModName>` | Reloads a specific mod (supports partial name matching). |
| `reloadall` | Reloads all mods including base game data (`Main.mod`). |
| `reloadlist` | Lists all currently loaded mods, status, and asset counts. |
| `autoreload [on\|off]` | Toggles automatic file watching mode. |

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

For details regarding world stitching algorithms, Harmony reflection injection, and building the C# project source code, view the [Technical Details Documentation](docs/technical-details.md).

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
- **Restauration du Monde** : Repare automatiquement les references de maillages et materiaux pour les objets deja poses dans le monde.

### Commandes Console

Appuyez sur `CTRL` + `MAJ` + `C` en jeu pour ouvrir la console :

| Commande | Action |
| --- | --- |
| `reload` | Recharge tous les mods utilisateur (nouveaux dossiers `.mod`, textures, FBX et parametres). |
| `reload <NomDuMod>` | Recharge un mod specifique (accepte la recherche partielle par nom). |
| `reloadall` | Recharge tous les mods, y compris le contenu de base du jeu (`Main.mod`). |
| `reloadlist` | Liste l'ensemble des mods charges, leur statut et leur nombre d'assets. |
| `autoreload [on\|off]` | Bascule le mode de surveillance automatique des fichiers. |

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
