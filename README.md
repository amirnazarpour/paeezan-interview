# Paeezan Interview Project

Unity project for the Paeezan technical interview task. Open it with Unity Editor `6000.3.13f1` and start from `Assets/Scenes/Scene_Splash.unity`.

## Shop

Each completed lap earns one coin. The Shop tab sells one-hit shields, 10-second double-score charges, and color themes for the ball and ring. Buying a theme unlocks it; choose **Equip** separately. The game HUD builds a card for every catalog powerup, including those with zero charges; cards can be used during an active run when a charge is available. Coins, ownership, equipment, and unused charges are saved locally with PlayerPrefs.

Use **Tools > Shop > Catalog Editor** to edit prices, text, theme colors, shield blocked hits, and score boost duration and multiplier. Choose **Save and Sync Scenes** after editing. The JSON remains at `Assets/Resources/ShopCatalog.json`. Purchased shield charges use the edited hit count in a run, and score charges use the edited multiplier and duration; equipped colors apply when the game scene loads. The shop instantiates each item card from `Prefab_ShopItemCard` in catalog order; the shop sections, insufficient-coins popup, and game HUD remain serialized uGUI objects in `Scene_Menu` and `Scene_Game`. The popup's **Go Play** button opens the menu's Game tab. New powerup effects also need a gameplay handler.

## Known issue: inconsistent score keys

The game saves the high score under the PlayerPrefs key `PlayerScore`, but the home screen reads `PlayerHighScore` and the `info.json` exporter reads `Score`. As a result, the home screen and exported JSON can show fallback scores instead of the saved high score. These keys have not yet been unified.
