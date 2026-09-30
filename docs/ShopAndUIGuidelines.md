# Shop and UI development guidelines

These conventions apply to the shop, its uGUI, and related game HUD code in this Unity 6 project.

## References and components

- Declare required scene, prefab, and asset references as private `[SerializeField]` fields. Assign them in the Inspector and save the scene or prefab.
- Do not locate required dependencies at runtime with `GameObject.Find`, `FindGameObjectWithTag`, `FindFirstObjectByType`, `FindAnyObjectByType`, `FindObjectOfType`, or hierarchy name searches.
- Do not hide a missing Inspector assignment with a `GetComponent` fallback, a silent `return`, or repeated null checks such as `if (!actionButton)`. A missing required reference should produce an obvious setup error. Use `GetComponent` only for a component guaranteed to be on the same GameObject, preferably with `[RequireComponent]`.
- Match each `MonoBehaviour` class name to its `.cs` filename. Keep the script and its `.meta` file together so serialized references keep their GUID.

## Shop UI

- Author the shop sections, popup, and HUD containers as uGUI objects in `Scene_Menu` and `Scene_Game`. Author card layouts as prefabs. Runtime code may `Instantiate` a card prefab for each catalog entry and bind its data; it should not build controls from new `GameObject` and `AddComponent` calls.
- Keep the insufficient-coins popup centered and scene-authored. Its **Go Play** action selects the Game tab; it does not start a run.
- Keep powerup controls hidden and non-interactable until `BallController.IsRunActive` is true. A successful use consumes one saved charge; taps on the HUD must not switch lanes.

## Catalog and saved state

- Edit `Assets/Resources/ShopCatalog.json` through **Tools > Shop > Catalog Editor** and use **Save and Sync Scenes**. The catalog controls item text, prices, section order, colors, and supported effect values.
- Use `ShopItemKind` for behavior and UI state decisions. JSON stores its numeric enum values. Do not branch on item kind names or UI label text.
- Keep item IDs stable after release. Saved ownership and powerup counts use those IDs, while kinds describe behavior. New themes can use new IDs; a new powerup kind needs a gameplay handler and catalog validation.
- Buying a theme unlocks it; equipping is a separate action. Save coins, owned themes, equipped ball and ring themes, and unused powerup counts across restarts.

## Verification

- Run `unity status` before Editor work. Use Unity Pipeline and Unity CLI for scene, prefab, asset, and Editor changes.
- Recompile and run the shop EditMode tests after changing catalog or shop logic. Check purchases, popup navigation, active-run HUD behavior, and saved state in Play mode. Inspect affected uGUI at portrait and landscape sizes.
