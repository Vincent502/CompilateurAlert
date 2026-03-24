# CompilateurSound - Explication du fonctionnement

Ce document explique le script `CompilateurSound.cs` et la logique utilisée pour jouer un son selon le resultat de la compilation Unity.

## Objectif

Le script joue un fichier audio apres une compilation:

- `error.wav` si une erreur C# a ete detectee
- `succes.wav` si aucune erreur C# n'a ete detectee

Les fichiers sont attendus dans le dossier `Sounds` à côté du script (package UPM ou copie sous `Assets/Editor/...`):

- `.../CompilateurAlert/Sounds/error.wav`
- `.../CompilateurAlert/Sounds/succes.wav`

## Installation (autre projet)

Dans `Packages/manifest.json`, ajouter par exemple:

`"com.compilateur.alert": "https://github.com/VOTRE_ORG/CompilateurAlert.git"`

Ou en local: `"com.compilateur.alert": "file:../CompilateurAlert"`

## Pourquoi ce script est un script Editor

Le code est entoure de:

- `#if UNITY_EDITOR`

et utilise des API Unity Editor (`CompilationPipeline`, `SessionState`, `AssemblyReloadEvents`), donc il est execute uniquement dans l'editeur Unity.

## Vue d'ensemble du flux

1. Unity demarre une compilation -> `OnCompilationStarted`
2. Pendant la compilation, les logs sont analyses -> `OnLogMessageReceived`
3. Unity signale la fin de compilation -> `OnCompilationFinished`
4. Le script attend un court delai pour laisser arriver d'eventuels logs tardifs -> `TryFinalizeAndPlay`
5. Le script lit l'etat final en `SessionState`, choisit le son et le joue -> `TryPlayPendingSoundAfterReload`

## Details des variables importantes

- `capturingErrors`: active/desactive la phase de capture des erreurs
- `hasErrors`: devient `true` si une ligne de log contient `error CS...`
- `finishedAt`: horodatage de fin de compilation
- `KeyPending`: indique qu'un son doit etre joue
- `KeyHadErrors`: memorise si la compilation contenait des erreurs

## Role de chaque methode

### `CompilateurSound()` (constructeur statique)

S'abonne aux evenements Unity:

- `CompilationPipeline.compilationStarted`
- `CompilationPipeline.compilationFinished`
- `Application.logMessageReceived`
- `AssemblyReloadEvents.afterAssemblyReload`

Puis tente immediatement de jouer un son si un etat `pending` existe deja.

### `OnCompilationStarted(object _)`

Prepare une nouvelle session:

- active `capturingErrors`
- remet `hasErrors` a `false`

### `OnLogMessageReceived(string condition, string stackTrace, LogType type)`

Pendant la compilation:

- ignore les logs non pertinents
- si un log de type erreur contient `error CS` (insensible a la casse), alors:
  - `hasErrors = true`
  - `SessionState[KeyHadErrors] = true`

Cela permet de capter les erreurs C# meme si elles arrivent tard.

### `OnCompilationFinished(object _)`

A la fin de compilation:

- marque qu'un son est en attente (`KeyPending = true`)
- stocke l'etat courant de `hasErrors`
- enregistre l'instant de fin (`finishedAt`)
- planifie la finalisation via `EditorApplication.update`

### `TryFinalizeAndPlay()`

Attend un petit delai (0.5s) avant de finaliser.

Ce delai est important car certains logs d'erreur peuvent arriver juste apres `compilationFinished`.

Quand le delai est passe:

- appelle `TryPlayPendingSoundAfterReload()`
- se desabonne de `EditorApplication.update`

### `TryPlayPendingSoundAfterReload()`

Lit `SessionState`:

- si rien n'est en attente, quitte
- sinon lit `KeyHadErrors`
- nettoie les cles (`EraseBool`) pour eviter les doubles lectures
- choisit `error.wav` ou `succes.wav`
- appelle `PlaySoundWavFile(...)` après résolution du dossier `Sounds`

Cette methode est aussi appelee apres un `domain reload` via `AssemblyReloadEvents.afterAssemblyReload`, ce qui rend le comportement plus fiable.

### `GetSoundsFolderFullPath()` / `PlaySoundWavFile(string fullPath)`

- localise le dossier `Sounds` via `AssetDatabase` (chemin du script `CompilateurSound.cs`), ce qui fonctionne dans `Assets/` ou dans un package sous `Packages/`
- joue le fichier avec `SoundPlayer` (`Load` + `PlaySync`)

En cas de probleme, log un warning explicite.

## Pourquoi utiliser `SessionState`

Pendant une compilation Unity, un rechargement de domaine peut interrompre des callbacks temporaires.
`SessionState` permet de conserver l'information essentielle entre les phases et de jouer le bon son de maniere plus fiable.

## Points d'attention

- Le nom du fichier doit etre exact (`succes.wav` et non `success.wav`).
- Le fichier doit exister physiquement (pas seulement le `.meta`).
- Si aucun son ne sort, verifier la Console pour:
  - `fichier audio introuvable`
  - `erreur lecture son`

