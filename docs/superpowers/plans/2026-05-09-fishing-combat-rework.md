# Fishing Combat Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current tension-window fishing minigame with a stamina, line-distance, and line-load based fight model.

**Architecture:** Keep all deterministic fight math in `FishingGame.Core` so it can be tested without the UI. Let WinForms own only transient presentation concerns such as reel input, shake feedback, labels, and gauge painting, while consuming a core-generated fight state.

**Tech Stack:** C# on .NET Framework, WinForms, GDI+, PowerShell build script, custom console test runner.

---

## File Structure

- `src/FishingGame.Core/Models.cs`: extend fish metadata and add transient fight state/result models.
- `src/FishingGame.Core/GameData.cs`: seed new fish stamina and burst traits from scene/rarity/weight.
- `src/FishingGame.Core/GameRules.cs`: fight creation, tick resolution, retrieve/release rules, line break logic, luck-to-weight bias, and early-scene assist.
- `src/FishingGame.WinForms/Program.cs`: swap old progress/safe-zone loop for the new fight loop and add stamina feedback.
- `tests/CoreTests.cs`: regression coverage for the new fight rules.

### Task 1: Add Failing Core Tests

**Files:**
- Modify: `tests/CoreTests.cs`

- [ ] **Step 1: Write failing tests for the new combat model**

Add tests shaped like:

```csharp
Run("heavier fish have more stamina", TestHeavierFishHaveMoreStamina);
Run("fish with zero stamina are not landed until line is retrieved", TestExhaustedFishStillNeedsRetrieve);
Run("strong line can overpower smaller fish", TestStrongLineCanOverpowerSmallFish);
Run("line does not break below rated load", TestLineOnlyBreaksAboveRatedLoad);
Run("luck improves trophy bias", TestLuckImprovesTrophyBias);
```

- [ ] **Step 2: Run test-only build to verify RED**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -TestOnly`

Expected: compilation failure or failing assertions because the new fight APIs do not exist yet.

### Task 2: Extend Core Models And Data

**Files:**
- Modify: `src/FishingGame.Core/Models.cs`
- Modify: `src/FishingGame.Core/GameData.cs`

- [ ] **Step 1: Add transient fight models**

Introduce a model shaped like:

```csharp
public class FishingFightState
{
    public double FishStaminaMax { get; set; }
    public double FishStamina { get; set; }
    public double LineDistance { get; set; }
    public double Tension { get; set; }
    public double Load { get; set; }
    public bool IsBursting { get; set; }
    public int BurstTicks { get; set; }
    public double RetrieveProgress { get; set; }
}
```

- [ ] **Step 2: Add fish combat attributes**

Extend `FishSpecies` with values such as:

```csharp
public double BaseStamina { get; set; }
public double BurstStrength { get; set; }
public int BurstFrequency { get; set; }
public int BurstDuration { get; set; }
public double FatigueResistance { get; set; }
public double RecommendedLineStrength { get; set; }
```

- [ ] **Step 3: Seed the new values from existing fish data**

Keep generation deterministic and derive combat values from scene difficulty, rarity, and weight band.

### Task 3: Implement Fight Rules

**Files:**
- Modify: `src/FishingGame.Core/GameRules.cs`
- Test: `tests/CoreTests.cs`

- [ ] **Step 1: Create the fight state API**

Add methods shaped like:

```csharp
public static FishingFightState CreateFightState(FishSpecies fish, Rod rod, FishingLine line, CatchRecord catchRecord)
public static void ApplyPlayerAction(FishingFightState state, FishSpecies fish, Rod rod, FishingLine line, bool pull)
public static FishingFightTick ResolveFightTick(FishingFightState state, FishSpecies fish, Rod rod, FishingLine line, Random random)
```

- [ ] **Step 2: Encode new win/lose conditions**

Implement:

- fish not landed until `LineDistance` reaches landing threshold
- no regular hook-slip loss during fight
- line break only when load exceeds `line.MaxTension`
- exhausted fish can still require manual retrieval

- [ ] **Step 3: Encode equipment dominance and early-scene assist**

Implement:

- strong line and rod can hard-pull weak fish
- scenes 1-4 reduce stamina and burst pressure
- scene 5+ keep the true-random selection flow

- [ ] **Step 4: Add luck-based trophy bias**

Move weight generation so higher luck nudges catches upward without removing randomness.

- [ ] **Step 5: Run test-only build to verify GREEN**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -TestOnly`

Expected: all core tests pass.

### Task 4: Integrate WinForms

**Files:**
- Modify: `src/FishingGame.WinForms/Program.cs`

- [ ] **Step 1: Replace old fight loop state**

Replace `_catchProgress`-centric flow with `FishingFightState` fields owned by the form.

- [ ] **Step 2: Update bite handling**

Implement:

- early strike ends the event quietly with no bait loss
- late strike still consumes bait
- hook/item path continues to work

- [ ] **Step 3: Update reel interaction and visuals**

Implement:

- wider reel sensitivity
- fish stamina bar
- reel highlight and screen shake on hook-up
- status text showing bait/hook/line recommendations during test phase

- [ ] **Step 4: Build full app**

Run: `powershell -ExecutionPolicy Bypass -File .\build.ps1`

Expected: `dist\FishingGame.exe` is rebuilt successfully.

### Task 5: Final Verification

**Files:**
- Modify if needed: `tests/CoreTests.cs`, `src/FishingGame.Core/GameRules.cs`, `src/FishingGame.WinForms/Program.cs`

- [ ] **Step 1: Run full verification**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
git diff --check
```

Expected: tests pass, UI smoke passes, and diff has no patch-format issues.

- [ ] **Step 2: Manual sanity check**

Launch `dist\FishingGame.exe` and confirm:

- early strike causes empty event without resource loss
- fish stamina bar appears after hook-up
- small fish can be hard-reeled by strong line
- line only breaks under obvious overload
