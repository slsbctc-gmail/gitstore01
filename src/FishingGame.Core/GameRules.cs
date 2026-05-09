using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FishingGame.Core
{
    public static class GameRules
    {
        public static GameState CreateNewGame()
        {
            GameState state = new GameState();
            state.Coins = 120;
            state.OwnedRodIds.Add(GameData.Rods[0].Id);
            state.EquippedRodId = GameData.Rods[0].Id;
            state.UnlockedSceneIds.Add(GameData.Scenes[0].Id);
            state.AquariumTierIndex = 0;
            state.EquippedBaitId = GameData.Baits[0].Id;
            state.EquippedHookId = GameData.Hooks[1].Id;
            state.EquippedLineId = GameData.Lines[0].Id;
            state.EquippedLineLength = GameData.Lines[0].MaxLength;
            state.MaxStamina = 30;
            state.Stamina = state.MaxStamina;
            state.InfiniteStamina = true;
            AddInventory(state.BaitInventory, state.EquippedBaitId, 30);
            AddInventory(state.HookInventory, state.EquippedHookId, 20);
            AddInventory(state.LineInventory, state.EquippedLineId, 1);
            return state;
        }

        public static void NormalizeState(GameState state)
        {
            if (state.OwnedRodIds == null) state.OwnedRodIds = new List<string>();
            if (state.UnlockedSceneIds == null) state.UnlockedSceneIds = new List<string>();
            if (state.Bag == null) state.Bag = new List<CatchRecord>();
            if (state.Aquarium == null) state.Aquarium = new List<CatchRecord>();
            if (state.BaitInventory == null) state.BaitInventory = new List<InventoryStack>();
            if (state.HookInventory == null) state.HookInventory = new List<InventoryStack>();
            if (state.LineInventory == null) state.LineInventory = new List<InventoryStack>();
            if (state.CollectionSpeciesIds == null) state.CollectionSpeciesIds = new List<string>();
            if (state.ItemCollectionIds == null) state.ItemCollectionIds = new List<string>();
            if (state.LastSignInDate == null) state.LastSignInDate = "";
            if (state.LastTicketDate == null) state.LastTicketDate = "";
            if (state.LastStaminaAt == null) state.LastStaminaAt = "";
            if (string.IsNullOrEmpty(state.EquippedRodId)) state.EquippedRodId = GameData.Rods[0].Id;
            if (!state.OwnedRodIds.Contains(state.EquippedRodId)) state.OwnedRodIds.Add(state.EquippedRodId);
            if (string.IsNullOrEmpty(state.EquippedBaitId)) state.EquippedBaitId = GameData.Baits[0].Id;
            if (string.IsNullOrEmpty(state.EquippedHookId)) state.EquippedHookId = GameData.Hooks[1].Id;
            if (string.IsNullOrEmpty(state.EquippedLineId)) state.EquippedLineId = GameData.Lines[0].Id;
            if (state.BaitInventory.Count == 0) AddInventory(state.BaitInventory, state.EquippedBaitId, 12);
            if (state.HookInventory.Count == 0) AddInventory(state.HookInventory, state.EquippedHookId, 8);
            if (state.LineInventory.Count == 0) AddInventory(state.LineInventory, state.EquippedLineId, 1);
            FishingLine equippedLine = GameData.FindLine(state.EquippedLineId);
            if (state.EquippedLineLength <= 0 || state.EquippedLineLength > equippedLine.MaxLength)
            {
                state.EquippedLineLength = equippedLine.MaxLength;
            }
            if (state.MaxStamina <= 0) state.MaxStamina = 30;
            if (state.Stamina < 0) state.Stamina = 0;
            if (state.Stamina > state.MaxStamina) state.Stamina = state.MaxStamina;
            state.InfiniteStamina = true;
            if (state.UnlockedSceneIds.Count == 0) state.UnlockedSceneIds.Add(GameData.Scenes[0].Id);
            if (state.AquariumTierIndex < 0) state.AquariumTierIndex = 0;
            if (state.AquariumTierIndex >= GameData.AquariumTiers.Count) state.AquariumTierIndex = GameData.AquariumTiers.Count - 1;
            NormalizeCatchPrices(state.Bag);
            NormalizeCatchPrices(state.Aquarium);
        }

        public static double HiddenChanceForRod(FishSpecies fish, Rod rod)
        {
            if (fish == null || !fish.IsHidden) return 0;
            double luckMultiplier = 1.0 + rod.Luck / 18.0;
            double chance = fish.HiddenBaseChance * luckMultiplier;
            return Math.Min(0.05, chance);
        }

        public static double HiddenItemChanceForRod(HiddenItem item, Rod rod)
        {
            if (item == null) return 0;
            double luckMultiplier = 1.0 + rod.Luck / 24.0;
            return Math.Min(0.04, item.HiddenBaseChance * luckMultiplier);
        }

        public static TensionWindow CalculateTensionWindow(FishSpecies fish, Rod rod)
        {
            int center = fish.PreferredTension + (rod.Control - 40) / 18;
            int halfWidth = fish.TensionWindow + rod.Control / 14 - fish.TensionVolatility / 3;
            halfWidth = ClampInt(halfWidth, 6, 28);
            int low = ClampInt(center - halfWidth, 8, 84);
            int high = ClampInt(center + halfWidth, 16, 92);
            if (high - low < 8)
            {
                high = Math.Min(92, low + 8);
            }
            return new TensionWindow { Low = low, High = high };
        }

        public static TensionActionProfile CalculateActionProfile(FishSpecies fish, Rod rod)
        {
            int sceneDifficulty = GameData.FindScene(fish.SceneId).Difficulty;
            double pull = 4.2 + rod.Power / 34.0 - fish.PullResistance * 0.18;
            double release = 4.4 + rod.Control / 38.0 + fish.ReleaseSensitivity * 0.14;
            double drift = 0.55 + fish.TensionVolatility / 14.0 + fish.RunStrength / 36.0 - rod.Stability / 90.0;
            if (sceneDifficulty <= 4)
            {
                drift *= 0.75;
            }
            return new TensionActionProfile
            {
                PullAmount = ClampDouble(pull, 2.0, 13.0),
                ReleaseAmount = ClampDouble(release, 2.2, 14.0),
                DriftAmount = ClampDouble(drift, 0.18, 4.2)
            };
        }

        public static double HookSlipChance(FishSpecies fish, Hook hook, double tension, TensionWindow window)
        {
            if (fish == null || hook == null || window == null) return 0.02;
            if (tension >= window.Low && tension <= window.High) return 0.0;

            double distance = tension < window.Low ? window.Low - tension : tension - window.High;
            int sizeDiff = Math.Abs(hook.Size - fish.PreferredHookSize);
            double pressure = Math.Max(0, fish.RunStrength * 3.2 - hook.HoldStrength) / 420.0;
            double mismatch = sizeDiff * 0.006;
            if (hook.Style != fish.PreferredHookStyle) mismatch += 0.006;
            return ClampDouble(distance / 900.0 + pressure + mismatch, 0.001, 0.055);
        }

        public static double LineCutChance(FishSpecies fish, FishingLine line, double tension, TensionWindow window)
        {
            if (fish == null || line == null || window == null) return 0.02;
            if (tension <= window.High) return 0.0;

            double lineStress = tension + fish.RunStrength * 0.9;
            if (lineStress <= line.MaxTension) return 0.0;
            double overload = (lineStress - line.MaxTension) / 90.0;
            double danger = Math.Max(0, tension - window.High) / 120.0;
            return ClampDouble(overload * danger * (1.0 - line.CutResistance), 0.004, 0.075);
        }

        public static FishingFightState CreateFightState(FishSpecies fish, Rod rod, FishingLine line, CatchRecord catchRecord)
        {
            if (fish == null) return new FishingFightState();
            if (rod == null) rod = GameData.Rods[0];
            if (line == null) line = GameData.Lines[0];
            double weight = catchRecord == null ? Math.Round((fish.MinWeight + fish.MaxWeight) / 2.0, 2) : catchRecord.Weight;
            double ratio = WeightRatio(fish, weight);
            int sceneDifficulty = GameData.FindScene(fish.SceneId).Difficulty;
            double assist = FightAssistMultiplier(sceneDifficulty);
            double stamina = (fish.BaseStamina + weight * 9.0 + ratio * 22.0 + fish.FatigueResistance * 2.8) * assist;
            double initialDistance = 8.5 + weight * 1.9 + fish.RunStrength * 0.32 - rod.Control * 0.04;
            initialDistance *= 0.94 + sceneDifficulty * 0.015;
            initialDistance *= assist;
            initialDistance = ClampDouble(initialDistance, 4.5, Math.Max(5.5, line.MaxLength * 0.62));
            double load = Math.Max(18.0, fish.RunStrength * 1.8 + fish.BurstStrength * 0.25 - rod.Stability * 0.14);

            return new FishingFightState
            {
                FishStaminaMax = Math.Round(stamina, 2),
                FishStamina = Math.Round(stamina, 2),
                FishWeight = weight,
                InitialLineDistance = Math.Round(initialDistance, 2),
                LineDistance = Math.Round(initialDistance, 2),
                LandingDistance = 0.35,
                Tension = LoadToTension(load, line),
                Load = Math.Round(load, 2),
                IsBursting = false,
                BurstTicks = 0,
                BurstCooldownTicks = AdjustedBurstFrequency(fish, sceneDifficulty),
                RetrieveProgress = 0
            };
        }

        public static void ApplyPlayerAction(FishingFightState state, FishSpecies fish, Rod rod, FishingLine line, bool pull)
        {
            if (state == null || fish == null) return;
            if (rod == null) rod = GameData.Rods[0];
            if (line == null) line = GameData.Lines[0];

            double pressureEdge = PressureAdvantage(fish, rod, line);
            double linePower = Math.Max(0, (line.MaxTension - fish.RecommendedLineStrength) / 120.0);
            double fatigueDivisor = 1.0 + fish.FatigueResistance / 18.0;
            if (pull)
            {
                double burstPenalty = state.IsBursting ? 0.52 : 1.0;
                double retrieve = (0.16 + rod.Power / 140.0 + Math.Max(0, pressureEdge) * 0.32 + linePower * 0.22) * burstPenalty;
                if (pressureEdge > 0.2)
                {
                    retrieve += 0.16;
                }
                state.LineDistance = Math.Max(0, state.LineDistance - retrieve);

                double loadGain = 4.5 + fish.RunStrength * 0.38 + fish.BurstStrength * 0.16 - rod.Control * 0.08;
                if (pressureEdge > 0.1)
                {
                    loadGain *= 0.9;
                }
                if (linePower > 0)
                {
                    loadGain *= ClampDouble(1.0 - linePower * 0.16, 0.72, 1.0);
                }
                if (!state.IsBursting)
                {
                    loadGain *= 0.72;
                }
                state.Load += Math.Max(1.2, loadGain);

                double staminaLoss = (3.8 + rod.Power / 10.0 + Math.Max(0, pressureEdge) * 4.6) / fatigueDivisor;
                staminaLoss *= state.IsBursting ? 1.22 : 0.95;
                state.FishStamina = Math.Max(0, state.FishStamina - staminaLoss);
            }
            else
            {
                double giveLine = 0.12 + fish.ReleaseSensitivity / 130.0;
                if (state.IsBursting)
                {
                    giveLine += fish.BurstStrength / 210.0;
                }
                state.LineDistance = Math.Min(line.MaxLength, state.LineDistance + giveLine);

                double unload = 7.0 + rod.Control / 6.5 + line.CutResistance * 18.0;
                if (state.IsBursting)
                {
                    unload *= 1.28;
                }
                state.Load = Math.Max(0, state.Load - unload);

                double staminaLoss = (state.IsBursting ? 3.0 : 0.8) + fish.BurstStrength / 28.0;
                state.FishStamina = Math.Max(0, state.FishStamina - staminaLoss / fatigueDivisor);
            }

            if (state.FishStamina <= 0)
            {
                state.IsBursting = false;
                state.BurstTicks = 0;
            }
            state.Tension = LoadToTension(state.Load, line);
            state.RetrieveProgress = CalculateRetrieveProgress(state);
        }

        public static FishingFightTick ResolveFightTick(FishingFightState state, FishSpecies fish, Rod rod, FishingLine line, Random random)
        {
            FishingFightTick tick = new FishingFightTick();
            if (state == null || fish == null) return tick;
            if (rod == null) rod = GameData.Rods[0];
            if (line == null) line = GameData.Lines[0];
            if (random == null) random = new Random();

            int sceneDifficulty = GameData.FindScene(fish.SceneId).Difficulty;
            double fatigueDivisor = 1.0 + fish.FatigueResistance / 20.0;
            if (state.FishStamina <= 0)
            {
                state.IsBursting = false;
                state.BurstTicks = 0;
                state.BurstCooldownTicks = 9999;
                state.Load = Math.Max(0, state.Load - (4.0 + rod.Stability / 12.0));
            }
            else if (state.BurstTicks > 0)
            {
                state.IsBursting = true;
                state.BurstTicks--;
                state.LineDistance = Math.Min(line.MaxLength, state.LineDistance + 0.08 + fish.BurstStrength / 260.0);
                state.Load += 5.8 + fish.BurstStrength * BurstPressureMultiplier(sceneDifficulty) * 0.26 - rod.Stability * 0.07;
                state.FishStamina = Math.Max(0, state.FishStamina - (2.2 + fish.BurstStrength / 24.0) / fatigueDivisor);
                if (state.BurstTicks <= 0)
                {
                    state.IsBursting = false;
                    state.BurstCooldownTicks = AdjustedBurstFrequency(fish, sceneDifficulty);
                }
            }
            else
            {
                state.IsBursting = false;
                state.BurstCooldownTicks--;
                state.Load = Math.Max(0, state.Load - (2.8 + rod.Stability / 12.0 + line.CutResistance * 6.0));
                state.LineDistance = Math.Min(line.MaxLength, state.LineDistance + Math.Max(0.02, fish.RunStrength / 360.0 - rod.Control / 700.0));
                state.FishStamina = Math.Max(0, state.FishStamina - Math.Max(0.22, (0.7 + fish.RunStrength / 42.0 - rod.Control / 160.0) / fatigueDivisor));
                if (state.BurstCooldownTicks <= 0 && random.NextDouble() < BurstStartChance(fish, sceneDifficulty))
                {
                    state.IsBursting = true;
                    state.BurstTicks = AdjustedBurstDuration(fish, sceneDifficulty);
                    tick.BurstStarted = true;
                }
            }

            if (state.FishStamina <= 0)
            {
                state.IsBursting = false;
                state.BurstTicks = 0;
            }

            state.Tension = LoadToTension(state.Load, line);
            state.RetrieveProgress = CalculateRetrieveProgress(state);
            double breakChance = LineBreakChance(state.Load, line);
            if (breakChance > 0 && random.NextDouble() < breakChance)
            {
                tick.LineBroken = true;
            }
            tick.FishLanded = IsFishLanded(state);
            return tick;
        }

        public static bool IsFishLanded(FishingFightState state)
        {
            return state != null && state.LineDistance <= state.LandingDistance;
        }

        public static double LineBreakChance(double load, FishingLine line)
        {
            if (line == null) return 0.02;
            if (load <= line.MaxTension) return 0.0;
            double overload = (load - line.MaxTension) / Math.Max(1.0, line.MaxTension);
            double chance = overload * (0.12 + (1.0 - line.CutResistance) * 0.18);
            return ClampDouble(chance, 0.01, 0.35);
        }

        public static int GetInventoryCount(List<InventoryStack> inventory, string itemId)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId)) return 0;
            InventoryStack stack = inventory.FirstOrDefault(i => i.ItemId == itemId);
            return stack == null ? 0 : stack.Quantity;
        }

        public static void AddInventory(List<InventoryStack> inventory, string itemId, int quantity)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId) || quantity <= 0) return;
            InventoryStack stack = inventory.FirstOrDefault(i => i.ItemId == itemId);
            if (stack == null)
            {
                inventory.Add(new InventoryStack { ItemId = itemId, Quantity = quantity });
            }
            else
            {
                stack.Quantity += quantity;
            }
        }

        public static bool ConsumeInventory(List<InventoryStack> inventory, string itemId, int quantity)
        {
            if (inventory == null || string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            InventoryStack stack = inventory.FirstOrDefault(i => i.ItemId == itemId);
            if (stack == null || stack.Quantity < quantity) return false;
            stack.Quantity -= quantity;
            return true;
        }

        public static bool CanStartCast(GameState state)
        {
            NormalizeState(state);
            return (state.InfiniteStamina || state.Stamina > 0)
                && GetInventoryCount(state.BaitInventory, state.EquippedBaitId) > 0
                && GetInventoryCount(state.HookInventory, state.EquippedHookId) > 0
                && state.EquippedLineLength >= 8;
        }

        public static bool ConsumeStamina(GameState state)
        {
            NormalizeState(state);
            if (state.InfiniteStamina) return true;
            if (state.Stamina <= 0) return false;
            state.Stamina -= 1;
            return true;
        }

        public static bool ConsumeBait(GameState state)
        {
            NormalizeState(state);
            return ConsumeInventory(state.BaitInventory, state.EquippedBaitId, 1);
        }

        public static void ApplyLineCut(GameState state, double lostLength)
        {
            NormalizeState(state);
            state.EquippedLineLength = Math.Max(0, state.EquippedLineLength - Math.Max(1.0, lostLength));
            ConsumeInventory(state.HookInventory, state.EquippedHookId, 1);
        }

        public static double FishAttractionWeight(GameState state, FishSpecies fish)
        {
            NormalizeState(state);
            double weight = RarityWeight(fish.Rarity);
            if (!state.CollectionSpeciesIds.Contains(fish.Id))
            {
                weight *= 3.5;
            }

            Bait bait = GameData.FindBait(state.EquippedBaitId);
            Hook hook = GameData.FindHook(state.EquippedHookId);
            Rod rod = GameData.FindRod(state.EquippedRodId);
            weight *= BaitAttractionMultiplier(fish, bait);
            weight *= HookAttractionMultiplier(fish, hook);
            if (bait.RarityBias > 0)
            {
                weight *= 1.0 + bait.RarityBias * RarityTier(fish.Rarity) * 0.08;
            }
            weight *= 1.0 + Math.Max(0, rod.Luck - 2) * 0.008 * RarityTier(fish.Rarity);
            if (IsLastMissingRegularFish(state, fish))
            {
                weight *= 60.0;
            }
            return Math.Max(0.000001, weight);
        }

        public static bool CanUnlockNextScene(GameState state, SceneInfo currentScene, SceneInfo nextScene)
        {
            NormalizeState(state);
            if (currentScene == null || nextScene == null) return false;
            List<FishSpecies> required = GameData.FishByScene[currentScene.Id].Where(f => !f.IsHidden).ToList();
            return required.All(f => state.CollectionSpeciesIds.Contains(f.Id));
        }

        public static FishSpecies ChooseFish(GameState state, string sceneId, Random random)
        {
            NormalizeState(state);
            Rod rod = GameData.FindRod(state.EquippedRodId);
            List<FishSpecies> fish = GameData.FishByScene[sceneId];
            List<FishSpecies> hidden = fish.Where(f => f.IsHidden).ToList();
            double hiddenChance = hidden.Sum(f => HiddenChanceForRod(f, rod));
            if (hidden.Count > 0 && random.NextDouble() < Math.Min(0.08, hiddenChance))
            {
                return WeightedPick(hidden, delegate(FishSpecies f) { return HiddenChanceForRod(f, rod); }, random);
            }

            List<FishSpecies> regular = fish.Where(f => !f.IsHidden).ToList();
            return WeightedPick(regular, delegate(FishSpecies f)
            {
                return FishAttractionWeight(state, f);
            }, random);
        }

        public static HiddenItem ChooseHiddenItem(GameState state, string sceneId, Random random)
        {
            NormalizeState(state);
            Rod rod = GameData.FindRod(state.EquippedRodId);
            List<HiddenItem> items = GameData.HiddenItemsByScene.ContainsKey(sceneId)
                ? GameData.HiddenItemsByScene[sceneId]
                : new List<HiddenItem>();
            if (items.Count == 0) return null;

            double totalChance = items.Sum(i => HiddenItemChanceForRod(i, rod));
            if (random.NextDouble() >= Math.Min(0.08, totalChance)) return null;
            return WeightedPick(items, delegate(HiddenItem item) { return HiddenItemChanceForRod(item, rod); }, random);
        }

        public static CatchRecord CreateCatch(FishSpecies fish, Random random)
        {
            return CreateCatch(fish, GameData.Rods[0], random);
        }

        public static CatchRecord CreateCatch(FishSpecies fish, Rod rod, Random random)
        {
            if (rod == null) rod = GameData.Rods[0];
            double ratio = RollWeightRatio(fish, rod, random);
            double weight = fish.MinWeight + ratio * Math.Max(0.1, fish.MaxWeight - fish.MinWeight);
            weight = Math.Round(weight, 2);
            return CreateCatchForWeight(fish, weight);
        }

        public static CatchRecord CreateCatchForWeight(FishSpecies fish, double weight)
        {
            weight = Math.Round(weight, 2);
            double ratio = WeightRatio(fish, weight);
            string grade;
            bool sellable;
            int sellPrice;
            if (ratio < 0.28)
            {
                grade = "偏小";
                sellable = false;
                sellPrice = 0;
            }
            else if (ratio < 0.86)
            {
                grade = "一般";
                sellable = true;
                sellPrice = Math.Max(1, (int)Math.Round(MarketPriceForWeight(fish, weight) * (0.75 + ratio * 0.7)));
            }
            else
            {
                grade = "极品";
                sellable = true;
                double trophyRatio = (ratio - 0.86) / 0.14;
                double multiplier = 2.5 + ClampDouble(trophyRatio, 0, 1) * 7.5;
                sellPrice = Math.Max(1, (int)Math.Round(MarketPriceForWeight(fish, weight) * multiplier));
            }

            return new CatchRecord
            {
                Id = "catch-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                SpeciesId = fish.Id,
                SpeciesName = fish.Name,
                SceneId = fish.SceneId,
                Rarity = fish.Rarity,
                IsHidden = fish.IsHidden,
                IsFish = true,
                Weight = weight,
                WeightGrade = grade,
                IsSellable = sellable,
                SellPrice = sellPrice,
                CaughtAt = DateTime.Now.ToString("s"),
                IconSymbol = fish.IconSymbol
            };
        }

        public static CatchRecord CreateItemCatch(HiddenItem item)
        {
            return new CatchRecord
            {
                Id = "item-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                SpeciesId = item.Id,
                SpeciesName = item.Name,
                SceneId = item.SceneId,
                Rarity = "隐藏物品",
                IsHidden = true,
                IsFish = false,
                Weight = 0,
                WeightGrade = "物品",
                IsSellable = true,
                SellPrice = item.BasePrice,
                CaughtAt = DateTime.Now.ToString("s"),
                IconSymbol = item.IconSymbol
            };
        }

        public static int RegisterCatch(GameState state, CatchRecord catchRecord)
        {
            NormalizeState(state);
            int bonus = 0;
            if (!catchRecord.IsFish)
            {
                if (!state.ItemCollectionIds.Contains(catchRecord.SpeciesId))
                {
                    state.ItemCollectionIds.Add(catchRecord.SpeciesId);
                    bonus = 120;
                    state.Coins += bonus;
                }
                state.Bag.Add(catchRecord);
                return bonus;
            }

            if (!state.CollectionSpeciesIds.Contains(catchRecord.SpeciesId))
            {
                state.CollectionSpeciesIds.Add(catchRecord.SpeciesId);
                bonus = FirstCatchBonus(catchRecord.Rarity, catchRecord.IsHidden);
                state.Coins += bonus;
            }
            if (catchRecord.IsSellable)
            {
                state.Bag.Add(catchRecord);
            }
            RefreshSceneUnlocks(state);
            return bonus;
        }

        public static void RefreshSceneUnlocks(GameState state)
        {
            NormalizeState(state);
            for (int i = 0; i < GameData.Scenes.Count - 1; i++)
            {
                SceneInfo current = GameData.Scenes[i];
                SceneInfo next = GameData.Scenes[i + 1];
                if (state.UnlockedSceneIds.Contains(current.Id)
                    && !state.UnlockedSceneIds.Contains(next.Id)
                    && CanUnlockNextScene(state, current, next))
                {
                    state.UnlockedSceneIds.Add(next.Id);
                }
            }
        }

        public static int SellAllBag(GameState state)
        {
            NormalizeState(state);
            int total = state.Bag.Where(c => c.IsSellable).Sum(c => c.SellPrice);
            state.Coins += total;
            state.Bag = state.Bag.Where(c => !c.IsSellable).ToList();
            return total;
        }

        public static bool BuyRod(GameState state, string rodId)
        {
            NormalizeState(state);
            Rod rod = GameData.FindRod(rodId);
            if (state.OwnedRodIds.Contains(rodId)) return true;
            if (state.Coins < rod.Price) return false;
            state.Coins -= rod.Price;
            state.OwnedRodIds.Add(rodId);
            return true;
        }

        public static bool BuyBait(GameState state, string baitId)
        {
            NormalizeState(state);
            Bait bait = GameData.FindBait(baitId);
            if (state.Coins < bait.Price) return false;
            state.Coins -= bait.Price;
            AddInventory(state.BaitInventory, bait.Id, bait.PackSize);
            return true;
        }

        public static bool BuyHook(GameState state, string hookId)
        {
            NormalizeState(state);
            Hook hook = GameData.FindHook(hookId);
            if (state.Coins < hook.Price) return false;
            state.Coins -= hook.Price;
            AddInventory(state.HookInventory, hook.Id, hook.PackSize);
            return true;
        }

        public static bool BuyLine(GameState state, string lineId)
        {
            NormalizeState(state);
            FishingLine line = GameData.FindLine(lineId);
            if (state.Coins < line.Price) return false;
            state.Coins -= line.Price;
            AddInventory(state.LineInventory, line.Id, 1);
            return true;
        }

        public static bool EquipBait(GameState state, string baitId)
        {
            NormalizeState(state);
            if (GetInventoryCount(state.BaitInventory, baitId) <= 0) return false;
            state.EquippedBaitId = baitId;
            return true;
        }

        public static bool EquipHook(GameState state, string hookId)
        {
            NormalizeState(state);
            if (GetInventoryCount(state.HookInventory, hookId) <= 0) return false;
            state.EquippedHookId = hookId;
            return true;
        }

        public static bool EquipLine(GameState state, string lineId)
        {
            NormalizeState(state);
            if (GetInventoryCount(state.LineInventory, lineId) <= 0) return false;
            state.EquippedLineId = lineId;
            state.EquippedLineLength = GameData.FindLine(lineId).MaxLength;
            return true;
        }

        public static bool EquipRod(GameState state, string rodId)
        {
            NormalizeState(state);
            if (!state.OwnedRodIds.Contains(rodId)) return false;
            state.EquippedRodId = rodId;
            return true;
        }

        public static bool BuyNextAquarium(GameState state)
        {
            NormalizeState(state);
            int nextIndex = state.AquariumTierIndex + 1;
            if (nextIndex >= GameData.AquariumTiers.Count) return false;
            AquariumTier next = GameData.AquariumTiers[nextIndex];
            if (state.Coins < next.Price) return false;
            state.Coins -= next.Price;
            state.AquariumTierIndex = nextIndex;
            return true;
        }

        public static bool TryMoveCatchToAquarium(GameState state, string catchId)
        {
            NormalizeState(state);
            AquariumTier tier = GameData.AquariumTiers[state.AquariumTierIndex];
            if (state.Aquarium.Count >= tier.Capacity) return false;
            CatchRecord item = state.Bag.FirstOrDefault(c => c.Id == catchId);
            if (item == null) return false;
            if (!item.IsFish) return false;
            state.Bag.Remove(item);
            state.Aquarium.Add(item);
            return true;
        }

        public static bool TryMoveCatchToBag(GameState state, string catchId)
        {
            NormalizeState(state);
            CatchRecord item = state.Aquarium.FirstOrDefault(c => c.Id == catchId);
            if (item == null) return false;
            state.Aquarium.Remove(item);
            state.Bag.Add(item);
            return true;
        }

        public static int SignIn(GameState state, DateTime today)
        {
            NormalizeState(state);
            string date = today.ToString("yyyy-MM-dd");
            if (state.LastSignInDate == date) return 0;
            int reward = 80 + state.UnlockedSceneIds.Count * 35;
            state.Coins += reward;
            state.LastSignInDate = date;
            return reward;
        }

        public static int CollectTicketIncome(GameState state, DateTime today)
        {
            NormalizeState(state);
            AquariumTier tier = GameData.AquariumTiers[state.AquariumTierIndex];
            if (!tier.TicketEnabled) return 0;
            string date = today.ToString("yyyy-MM-dd");
            if (state.LastTicketDate == date) return 0;
            if (state.Aquarium.Count == 0) return 0;

            int displayScore = 0;
            foreach (CatchRecord item in state.Aquarium.Where(i => i.IsFish))
            {
                displayScore += 3 + RarityTicketBonus(item.Rarity);
                if (item.IsHidden) displayScore += 80;
            }

            int income = (int)Math.Round((35 + displayScore) * tier.TicketMultiplier);
            state.Coins += income;
            state.LastTicketDate = date;
            return income;
        }

        public static int AquariumFreeSlots(GameState state)
        {
            NormalizeState(state);
            return GameData.AquariumTiers[state.AquariumTierIndex].Capacity - state.Aquarium.Count;
        }

        private static FishSpecies WeightedPick(List<FishSpecies> fish, Func<FishSpecies, double> weight, Random random)
        {
            double total = fish.Sum(f => Math.Max(0.000001, weight(f)));
            double roll = random.NextDouble() * total;
            double cursor = 0;
            foreach (FishSpecies item in fish)
            {
                cursor += Math.Max(0.000001, weight(item));
                if (roll <= cursor) return item;
            }
            return fish[fish.Count - 1];
        }

        private static HiddenItem WeightedPick(List<HiddenItem> items, Func<HiddenItem, double> weight, Random random)
        {
            double total = items.Sum(i => Math.Max(0.000001, weight(i)));
            double roll = random.NextDouble() * total;
            double cursor = 0;
            foreach (HiddenItem item in items)
            {
                cursor += Math.Max(0.000001, weight(item));
                if (roll <= cursor) return item;
            }
            return items[items.Count - 1];
        }

        private static double RarityWeight(string rarity)
        {
            if (rarity == "优秀") return 55;
            if (rarity == "稀有") return 18;
            if (rarity == "史诗") return 6;
            if (rarity == "传说") return 1.5;
            return 100;
        }

        private static double BaitAttractionMultiplier(FishSpecies fish, Bait bait)
        {
            if (bait == null) return 0.25;
            if (fish.FavoriteBaitId == bait.Id) return 3.0;
            if (fish.AcceptableBaitId == bait.Id) return 1.45;
            if (bait.IsLure && GameData.FindScene(fish.SceneId).Difficulty >= bait.MinSceneDifficulty) return 0.85;
            return 0.35;
        }

        private static double HookAttractionMultiplier(FishSpecies fish, Hook hook)
        {
            if (hook == null) return 0.25;
            int sizeDiff = Math.Abs(hook.Size - fish.PreferredHookSize);
            double multiplier = 1.75 - sizeDiff * 0.34;
            if (hook.Style == fish.PreferredHookStyle) multiplier += 0.35;
            if (hook.Style == "巨物钩" && fish.MaxWeight < 2.5) multiplier *= 0.2;
            if (hook.Style == "小钩" && fish.MaxWeight > 3.0) multiplier *= 0.65;
            return ClampDouble(multiplier, 0.18, 2.25);
        }

        private static bool IsLastMissingRegularFish(GameState state, FishSpecies fish)
        {
            if (fish.IsHidden || state.CollectionSpeciesIds.Contains(fish.Id)) return false;
            if (GameData.FindScene(fish.SceneId).Difficulty > 4) return false;
            List<FishSpecies> missing = GameData.FishByScene[fish.SceneId]
                .Where(f => !f.IsHidden && !state.CollectionSpeciesIds.Contains(f.Id))
                .ToList();
            return missing.Count == 1 && missing[0].Id == fish.Id;
        }

        private static int RarityTier(string rarity)
        {
            if (rarity == "优秀") return 1;
            if (rarity == "稀有") return 2;
            if (rarity == "史诗") return 3;
            if (rarity == "传说") return 4;
            return 0;
        }

        private static int FirstCatchBonus(string rarity, bool hidden)
        {
            if (hidden) return 500;
            if (rarity == "优秀") return 35;
            if (rarity == "稀有") return 90;
            if (rarity == "史诗") return 180;
            if (rarity == "传说") return 320;
            return 15;
        }

        private static int RarityTicketBonus(string rarity)
        {
            if (rarity == "优秀") return 4;
            if (rarity == "稀有") return 10;
            if (rarity == "史诗") return 22;
            if (rarity == "传说") return 40;
            return 0;
        }

        private static void NormalizeCatchPrices(List<CatchRecord> catches)
        {
            if (catches == null) return;
            foreach (CatchRecord catchRecord in catches.Where(c => c != null && c.IsFish))
            {
                FishSpecies fish = GameData.AllFish.FirstOrDefault(f => f.Id == catchRecord.SpeciesId);
                if (fish == null) continue;
                CatchRecord priced = CreateCatchForWeight(fish, catchRecord.Weight);
                catchRecord.WeightGrade = priced.WeightGrade;
                catchRecord.IsSellable = priced.IsSellable;
                catchRecord.SellPrice = priced.SellPrice;
                catchRecord.IconSymbol = priced.IconSymbol;
            }
        }

        private static double MarketPriceForWeight(FishSpecies fish, double weight)
        {
            return fish.BasePrice * Math.Max(0.2, weight) * RaritySaleMultiplier(fish.Rarity);
        }

        private static double RaritySaleMultiplier(string rarity)
        {
            if (rarity == "优秀") return 1.55;
            if (rarity == "稀有") return 2.2;
            if (rarity == "史诗") return 3.2;
            if (rarity == "传说") return 4.2;
            return 1.0;
        }

        private static double WeightRatio(FishSpecies fish, double weight)
        {
            return ClampDouble((weight - fish.MinWeight) / Math.Max(0.1, fish.MaxWeight - fish.MinWeight), 0, 1);
        }

        private static double RollWeightRatio(FishSpecies fish, Rod rod, Random random)
        {
            if (random == null) random = new Random();
            double roll = random.NextDouble();
            double exponent = ClampDouble(1.08 - rod.Luck / 85.0, 0.45, 1.08);
            return ClampDouble(Math.Pow(roll, exponent), 0, 1);
        }

        private static double PressureAdvantage(FishSpecies fish, Rod rod, FishingLine line)
        {
            double lineEdge = (line.MaxTension - fish.RecommendedLineStrength) / 55.0;
            double rodEdge = (rod.Power * 1.15 - fish.PullResistance * 2.4 - fish.RunStrength * 1.35) / 34.0;
            return ClampDouble(lineEdge + rodEdge, -1.2, 1.8);
        }

        private static double LoadToTension(double load, FishingLine line)
        {
            return ClampDouble(16 + load / Math.Max(1.0, line.MaxTension) * 84.0, 0, 100);
        }

        private static double CalculateRetrieveProgress(FishingFightState state)
        {
            if (state == null) return 0;
            return ClampDouble((1.0 - state.LineDistance / Math.Max(0.1, state.InitialLineDistance)) * 100.0, 0, 100);
        }

        private static int AdjustedBurstFrequency(FishSpecies fish, int sceneDifficulty)
        {
            int bonus = sceneDifficulty <= 4 ? 5 - sceneDifficulty : 0;
            return fish.BurstFrequency + 2 + bonus * 2;
        }

        private static int AdjustedBurstDuration(FishSpecies fish, int sceneDifficulty)
        {
            int reduction = sceneDifficulty <= 4 ? 1 : 0;
            return Math.Max(1, fish.BurstDuration - reduction);
        }

        private static double BurstStartChance(FishSpecies fish, int sceneDifficulty)
        {
            double chance = 0.42 + fish.TensionVolatility / 70.0;
            if (sceneDifficulty <= 4)
            {
                chance -= 0.08;
            }
            return ClampDouble(chance, 0.2, 0.78);
        }

        private static double BurstPressureMultiplier(int sceneDifficulty)
        {
            if (sceneDifficulty == 1) return 0.72;
            if (sceneDifficulty == 2) return 0.78;
            if (sceneDifficulty == 3) return 0.84;
            if (sceneDifficulty == 4) return 0.92;
            return 1.0;
        }

        private static double FightAssistMultiplier(int sceneDifficulty)
        {
            if (sceneDifficulty == 1) return 0.72;
            if (sceneDifficulty == 2) return 0.77;
            if (sceneDifficulty == 3) return 0.84;
            if (sceneDifficulty == 4) return 0.91;
            return 1.0;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static double ClampDouble(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
