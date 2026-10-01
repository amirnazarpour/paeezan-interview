# Architecture note

TrickyRing keeps the supplied Splash -> Loading -> Menu -> Game loop. The shop work sits around that loop rather than replacing its scene or event flow.

**Catalog.** `Assets/Resources/ShopCatalog.json` defines ordered sections and items with stable IDs, prices, copy, theme colors, powerup settings, and optional powerup preview sprites. `ShopCatalog` loads and validates it once. **Tools > Shop > Catalog Editor** edits the JSON and syncs the authored scene containers. New ball or ring themes need catalog data; a new powerup behavior also needs a gameplay handler.

**Economy and save.** `BallController` measures completed orbit travel and awards one coin per lap through the serialized `CoinWallet` asset. The wallet saves `CoinBalance` through `PlayerPrefsSaveService`. `ShopStateService` stores owned theme IDs, one equipped ball ID, one equipped ring ID, and unused powerup counts in `ShopStateV1`. A purchase first checks the item and balance, then spends coins and saves inventory. Buying a theme does not equip it. A typed failure result drives the insufficient-coins popup.

**UI and gameplay.** `Scene_Menu` contains the uGUI shop sections and popup. `ShopPanelController` instantiates a card prefab for each JSON item and binds its current price, ownership, count, and preview. `Scene_Game` contains the uGUI powerup HUD; its controller instantiates powerup card prefabs and enables use only during an active run. `BallController` consumes a saved charge only after a valid activation, handles shield hits and timed score multiplication, and ignores HUD taps for lane switching. `GameThemeApplier` colors the ball and ring from separately equipped themes when the game scene loads.

**Boundaries.** Data and inventory are local PlayerPrefs, with no server or IAP. The launch catalog supports the two implemented powerup kinds. The original high-score key mismatch is documented in the README.
