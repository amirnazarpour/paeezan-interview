# Paeezan Interview Project

Unity project for the Paeezan technical interview task. Open it with Unity Editor `6000.3.13f1` and start from `Assets/Scenes/Scene_Splash.unity`.

## Known issue: inconsistent score keys

The game saves the high score under the PlayerPrefs key `PlayerScore`, but the home screen reads `PlayerHighScore` and the `info.json` exporter reads `Score`. As a result, the home screen and exported JSON can show fallback scores instead of the saved high score. These keys have not yet been unified.
