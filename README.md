# TrickyRing - Paeezan interview task

This Unity 6 project extends the supplied game with coins, a configurable shop, persistent themes, and powerups used during a run. It is authored with Unity Editor `6000.3.13f1`.

## Open and play

1. Open the repository folder as a Unity project. Open `Assets/Scenes/Scene_Splash.unity` and press **Play**. The first launch asks for a player name.
2. On the menu, choose **Game** and press **Play**. Tap or click away from UI controls to switch the ball between the inner and outer lanes. Collect stars for score and avoid obstacles.
3. Each completed orbit earns one coin. The game HUD and game-over panel show coins earned in that run. The Shop tab shows the saved balance.
4. In **Shop**, buy shields, score boosts, or ball and ring colors. Buying a theme unlocks it; press **Equip** separately to use it. If a purchase costs more than the balance, a popup shows the missing amount and offers **Go Play**.
5. During an active run, tap a powerup control on the HUD. Shield blocks one obstacle hit; Double Score multiplies star score for 10 seconds. The controls cannot consume charges before a run or after game over.

The four enabled scenes are Splash, Loading, Menu, and Game. The project does not use in-app purchases.

## What was added

- A wallet that awards one coin per completed orbit and saves the balance immediately.
- A JSON catalog with two powerups and four ball themes plus four ring themes (including free defaults). Prices, labels, section order, theme colors, and effect values can be edited.
- Separate saved ownership and equipment for the ball and ring, plus saved unused powerup counts.
- A scrollable uGUI shop, purchase feedback, an insufficient-coins popup, and an in-run powerup HUD. Shop and HUD cards are instantiated from authored uGUI prefabs using the catalog order; their containers and the popup are authored in the scenes.
- EditMode shop tests in `Assets/Editor/ShopTests.cs`.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the short system design note.

## Edit the shop catalog

Open **Tools > Shop > Catalog Editor**. Edit a section or item, then choose **Save and Sync Scenes**. The editor writes `Assets/Resources/ShopCatalog.json` and updates the scene containers. A powerup's **Preview image** accepts a single Sprite asset; the editor copies it into `Resources` when needed so the shop can show it in a build. The launch powerups have gameplay handlers; adding a new powerup behavior requires code as well as a catalog entry.

The shop save uses PlayerPrefs keys `CoinBalance` and `ShopStateV1`. Removing the app's local data resets them. No account or cloud save is involved.

## Verification and known issue

Run the EditMode tests with `unity test . --mode EditMode` when the Editor is closed, or use the connected Unity Editor's test runner. The project version is `6000.3.13f1`; opening and playing it in the task document's `6000.0.58f2` version has not been verified here.

The original game's high-score readers still use inconsistent PlayerPrefs keys: gameplay saves `PlayerScore`, while the menu reads `PlayerHighScore` and the JSON exporter reads `Score`. The menu or exported score can therefore show a fallback value.

Submission files and the final packaging checklist are in [Submit/README.md](Submit/README.md).
