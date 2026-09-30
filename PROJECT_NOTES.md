# Project Handoff Notes

## Project and working preferences

- This is the Paeezan interview Unity project in `E:\testProject\InterviewBaseProject`, using Unity Editor `6000.3.13f1` on `main`. The repository is `https://github.com/amirnazarpour/paeezan-interview.git`.
- Discuss design and important structure decisions with the project owner before implementing them. Use the connected Editor through Unity Pipeline and Unity CLI for scene, prefab, asset, and Editor changes; check `unity status` first.
- Keep gameplay UI authored in scenes. Do not generate UI at runtime. Commit future work only when the owner asks.
- `AGENTS.md` is the contributor guide. `README.md` is intended for the interviewer.

## Completed work

- Added the Unity interview project and Unity Pipeline package. The latest recorded commit at the time of this note is `d52d993` (`Add coin with persistent saving`).
- Added currency earned during runs: `BallController` grants one coin for each 360 degrees of active ball travel. Switching lanes preserves progress within a lap; starting a new run or dying clears incomplete lap progress.
- `CoinWallet` is a ScriptableObject asset. The ball prefab and `GameSceneManager` have serialized references to it, and the wallet asset has a serialized reference to `PlayerPrefsSaveService`. There is no static `CoinWallet.Main` accessor.
- The balance uses the existing save service with PlayerPrefs key `CoinBalance`, saved immediately when coins are awarded. `Scene_Game/Canvas/GameUi/PlayerCoins` is a scene-authored counter that updates during play and participates in the existing HUD animation. The balance is shown in game only for now.
- Unity compilation completed. A Play mode check earned a coin and updated the HUD; the pre-test balance was restored afterward.

## Known issue and pending work

- Run score is held in `BallController._score`. `GameSceneManager` saves high score as `PlayerScore`, but `HomePanel` reads `PlayerHighScore` and `PlayerDataJsonExporter` reads `Score`. The home screen and exported `info.json` can therefore show fallback scores. This is disclosed in `README.md` and has not been fixed.
- Currency is stored in PlayerPrefs, not in the exported `info.json`. Shop spending and its visible insufficient-funds response were deferred by the owner. Ask before deciding the shop structure or expanding currency UI beyond the game HUD.
