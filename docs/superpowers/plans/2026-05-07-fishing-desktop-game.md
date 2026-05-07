# Fishing Desktop Game Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows graphical fishing game executable with scenes, unique fish, hidden fish, rods, aquariums, ticket income, saving, and validation tests.

**Architecture:** Keep gameplay rules in a small C# core that can be tested without UI. Build the desktop experience as a WinForms program that consumes the core model and renders standard panels plus a custom painted water area. Use Windows' built-in .NET Framework compiler (`csc.exe`) through a PowerShell build script, avoiding web wrappers and external SDKs.

**Tech Stack:** C# on .NET Framework, WinForms, GDI+, `JavaScriptSerializer` for JSON saves, PowerShell build script, custom console test runner.

---

## File Structure

- `src/FishingGame.Core/Models.cs`: Plain data classes for scenes, fish, rods, aquariums, catches, save state.
- `src/FishingGame.Core/GameData.cs`: Deterministic scene, fish, rod, and aquarium tier generation.
- `src/FishingGame.Core/GameRules.cs`: Unlock rules, fish selection, catch rewards, aquarium capacity, ticket income, and daily sign-in logic.
- `src/FishingGame.Core/SaveStore.cs`: JSON save/load with fallback to new game.
- `src/FishingGame.WinForms/Program.cs`: WinForms entry point, main form, water painting panel, and UI event wiring.
- `tests/CoreTests.cs`: Console test runner covering data validation and economy rules.
- `build.ps1`: Compiles tests and the Windows executable into `dist/`.
- `README.md`: How to build, run, and play.

## Task 1: Core Tests First

**Files:**
- Create: `tests/CoreTests.cs`

- [ ] **Step 1: Write the failing test runner**

Create a console runner that imports `FishingGame.Core` and asserts:

```csharp
AssertEqual(12, GameData.Scenes.Count, "scene count");
AssertTrue(GameData.FishByScene.Values.All(list => list.Count >= 50 && list.Count <= 100), "fish count per scene");
AssertTrue(GameData.FishByScene.Values.All(list => list.Count(f => f.IsHidden) >= 1 && list.Count(f => f.IsHidden) <= 2), "hidden fish per scene");
AssertEqual(allNames.Count, allNames.Distinct().Count(), "unique fish names");
AssertTrue(GameData.Rods.Count >= 20, "rod count");
AssertEqual(5, GameData.AquariumTiers[0].Capacity, "smallest aquarium");
AssertEqual(1000, GameData.AquariumTiers[GameData.AquariumTiers.Count - 1].Capacity, "largest aquarium");
AssertTrue(GameData.AquariumTiers.Any(t => t.Name == "观赏鱼廊" && t.TicketEnabled), "ticket starts at viewing gallery");
AssertTrue(GameRules.HiddenChanceForRod(hiddenFish, luckyRod) > GameRules.HiddenChanceForRod(hiddenFish, starterRod), "luck improves hidden chance");
AssertTrue(GameRules.CanUnlockNextScene(stateWithAllNonHidden, firstScene, secondScene), "non-hidden collection unlocks next scene");
AssertFalse(GameRules.CanUnlockNextScene(stateMissingOneNonHidden, firstScene, secondScene), "missing non-hidden blocks unlock");
AssertTrue(GameRules.TryMoveCatchToAquarium(state, firstCatch.Id), "aquarium accepts fish while capacity exists");
AssertFalse(GameRules.TryMoveCatchToAquarium(fullState, overflowCatch.Id), "aquarium rejects over capacity");
AssertTrue(GameRules.CollectTicketIncome(ticketState, today) > 0, "ticket income after viewing gallery");
AssertEqual(0, GameRules.CollectTicketIncome(ticketState, today), "ticket income only once per day");
```

- [ ] **Step 2: Run the tests to verify RED**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -TestOnly`

Expected: compilation fails because `FishingGame.Core` classes do not exist yet.

## Task 2: Core Data Model

**Files:**
- Create: `src/FishingGame.Core/Models.cs`
- Create: `src/FishingGame.Core/GameData.cs`

- [ ] **Step 1: Implement plain models**

Define `SceneInfo`, `FishSpecies`, `Rod`, `AquariumTier`, `CatchRecord`, and `GameState` with public get/set properties compatible with JSON serialization.

- [ ] **Step 2: Implement deterministic content generation**

Create 12 scenes, 24 rods, 7 aquarium tiers, and generated fish lists. Scene fish counts must be `[52, 56, 60, 66, 70, 76, 80, 86, 90, 94, 98, 100]`; hidden counts must be 1 for the first six scenes and 2 for the last six scenes.

- [ ] **Step 3: Run tests**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -TestOnly`

Expected: data tests pass or move forward to missing `GameRules` failures.

## Task 3: Rules And Economy

**Files:**
- Create: `src/FishingGame.Core/GameRules.cs`

- [ ] **Step 1: Implement new-game state**

`GameRules.CreateNewGame()` starts with scene 1 unlocked, starter rod owned/equipped, 120 coins, empty bag, empty aquarium, empty collection, no sign-in date, and no ticket collection date.

- [ ] **Step 2: Implement fish selection and hidden probability**

`HiddenChanceForRod(FishSpecies fish, Rod rod)` multiplies hidden base chance by rod luck and caps it at 5%. `ChooseFish` tries hidden fish first, then non-hidden fish with rarity weights and a boost for uncaught non-hidden fish.

- [ ] **Step 3: Implement progression**

`RegisterCatch` adds a catch to the bag, records the fish in collection, awards first-catch coins, and unlocks the next scene when all non-hidden fish in the current scene have been collected.

- [ ] **Step 4: Implement sales, rods, aquariums, and tickets**

Add functions to sell bag contents, buy/equip rods, buy aquarium upgrades, move fish between bag and aquarium, sign in once per day, and collect ticket income once per day for `观赏鱼廊` and higher.

- [ ] **Step 5: Run tests to verify GREEN**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -TestOnly`

Expected: all core tests pass.

## Task 4: Save Store

**Files:**
- Create: `src/FishingGame.Core/SaveStore.cs`
- Modify: `tests/CoreTests.cs`

- [ ] **Step 1: Add failing save/load test**

Assert that saving a modified state and loading it back preserves coins, owned rods, collection, aquarium tier, aquarium contents, and ticket collection date.

- [ ] **Step 2: Verify RED**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -TestOnly`

Expected: test fails because `SaveStore` is missing.

- [ ] **Step 3: Implement JSON save/load**

Use `System.Web.Script.Serialization.JavaScriptSerializer`. Invalid or missing saves return `GameRules.CreateNewGame()`.

- [ ] **Step 4: Verify GREEN**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -TestOnly`

Expected: all tests pass.

## Task 5: WinForms Game Window

**Files:**
- Create: `src/FishingGame.WinForms/Program.cs`

- [ ] **Step 1: Build main form**

Create a fixed minimum size WinForms window with top status bar, left scene list, center painted water panel, right action/shop panel, and bottom tabs for bag, aquarium, collection, and rods.

- [ ] **Step 2: Wire gameplay actions**

Implement sign-in, cast, tension timer, catch success/failure, sell all bag fish, move selected fish to aquarium, move selected aquarium fish to bag, buy/equip rods, buy aquarium upgrade, and collect tickets.

- [ ] **Step 3: Persist on meaningful changes and close**

Load from `fishing-save.json` at startup. Save after purchases, catches, sales, aquarium changes, sign-in, ticket collection, and form closing.

- [ ] **Step 4: Build executable**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1`

Expected: `dist\FishingGame.exe` is created.

## Task 6: Build Script And README

**Files:**
- Create: `build.ps1`
- Create: `README.md`

- [ ] **Step 1: Build script**

Compile tests into `dist\CoreTests.exe` and compile the app into `dist\FishingGame.exe` with `/target:winexe`, referencing `System.Windows.Forms.dll`, `System.Drawing.dll`, and `System.Web.Extensions.dll`.

- [ ] **Step 2: README**

Document build command, test command, run command, save file location, and gameplay basics.

- [ ] **Step 3: Final verification**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
.\dist\CoreTests.exe
Test-Path .\dist\FishingGame.exe
```

Expected: tests pass and executable exists.

