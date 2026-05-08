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
            return state;
        }

        public static void NormalizeState(GameState state)
        {
            if (state.OwnedRodIds == null) state.OwnedRodIds = new List<string>();
            if (state.UnlockedSceneIds == null) state.UnlockedSceneIds = new List<string>();
            if (state.Bag == null) state.Bag = new List<CatchRecord>();
            if (state.Aquarium == null) state.Aquarium = new List<CatchRecord>();
            if (state.CollectionSpeciesIds == null) state.CollectionSpeciesIds = new List<string>();
            if (state.ItemCollectionIds == null) state.ItemCollectionIds = new List<string>();
            if (state.LastSignInDate == null) state.LastSignInDate = "";
            if (state.LastTicketDate == null) state.LastTicketDate = "";
            if (string.IsNullOrEmpty(state.EquippedRodId)) state.EquippedRodId = GameData.Rods[0].Id;
            if (!state.OwnedRodIds.Contains(state.EquippedRodId)) state.OwnedRodIds.Add(state.EquippedRodId);
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
            double pull = 8.0 + rod.Power / 18.0 - fish.PullResistance * 0.42;
            double release = 7.0 + rod.Control / 24.0 + fish.ReleaseSensitivity * 0.38;
            double drift = 1.0 + fish.TensionVolatility / 8.0 + fish.RunStrength / 18.0 - rod.Stability / 55.0;
            return new TensionActionProfile
            {
                PullAmount = ClampDouble(pull, 3.0, 24.0),
                ReleaseAmount = ClampDouble(release, 4.0, 25.0),
                DriftAmount = ClampDouble(drift, 0.35, 8.0)
            };
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
                double weight = RarityWeight(f.Rarity);
                if (!state.CollectionSpeciesIds.Contains(f.Id))
                {
                    weight *= 3.5;
                }
                return weight;
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
            double weight = fish.MinWeight + random.NextDouble() * Math.Max(0.1, fish.MaxWeight - fish.MinWeight);
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
