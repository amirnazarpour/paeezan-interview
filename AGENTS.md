# Repository Guidelines

## Project Structure & Module Organization

This Unity 6 project uses Editor `6000.3.13f1`. Game code lives in `Assets/Scripts/`, grouped by feature (`GameCore/`, `Script_UI/`, `AudioManger/`); Editor code belongs in `Assets/Editor/`. The playable scenes are `Scene_Splash`, `Scene_Loading`, `Scene_Menu`, and `Scene_Game` under `Assets/Scenes/`. Reusable objects live in `Assets/Prefab/`. Package versions are in `Packages/manifest.json`; project settings are in `ProjectSettings/`. Treat `Assets/Plugins/`, `Assets/NaughtyAttributes/`, and `Assets/TextMesh Pro/` as third-party content.

## Build, Test, and Development Commands

- Open the project with Unity Editor `6000.3.13f1`; start from `Assets/Scenes/Scene_Splash.unity` and press Play for a local run.
- Run `unity test . --mode EditMode` or `unity test . --mode PlayMode` from the root. Results go to `test-results.xml` by default.
- Build through **File > Build Profiles** in the Editor, using the enabled scenes in `ProjectSettings/EditorBuildSettings.asset`. There is no repository build script or checked-in build profile.

Do not commit generated `Library/`, `Temp/`, `Logs/`, or `Build/` output.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, braces on separate lines, `PascalCase` for types and methods, and `camelCase` for locals. Match component filenames to class names. Keep serialized fields private with `[SerializeField]`. Preserve Unity `.meta` files when assets move. No formatter or linter is configured; match neighboring files.

## Testing Guidelines

The project includes Unity Test Framework, but no project-owned automated test suite or coverage threshold is present. Add focused EditMode tests for pure logic and PlayMode tests for scene behavior under `Assets/Tests/`, with names such as `BallControllerTests.cs`. Run the relevant mode and manually check affected scenes before submitting changes.

## Commit & Pull Request Guidelines

Use short, imperative commit subjects such as `Fix score reset on restart`. Pull requests should describe changes, list tested scenes and results, link related issues, and include screenshots or recordings for visible changes.

## Unity Workflow

Check `unity status` before Editor work. Use the connected Unity Editor through Unity Pipeline and Unity CLI for scene, prefab, asset, and Editor changes; diagnose connection failures before changing methods. Make gameplay components usable when placed in a scene: provide sensible defaults and a prefab for features spanning objects. If setup is unavoidable, emit one clear error naming the missing requirement and keep runtime behavior safe.
