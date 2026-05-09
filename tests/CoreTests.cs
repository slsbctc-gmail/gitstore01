using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishingGame.Core;

namespace FishingGame.Tests
{
    internal static class CoreTests
    {
        private static int _failures;

        private static int Main()
        {
            Run("data catalog meets content requirements", TestDataCatalog);
            Run("scene fish counts ramp from light to hard", TestSceneFishCountRamp);
            Run("non-hidden collection unlocks next scene", TestSceneUnlockRequiresNonHiddenCollection);
            Run("luck improves hidden fish chance", TestLuckImprovesHiddenChance);
            Run("fish and rods change tension windows", TestTensionWindowsVaryByFishAndRod);
            Run("fish traits change pull and release behavior", TestFishTraitsVaryActionBehavior);
            Run("weight classes control release and sale prices", TestWeightClassesControlSale);
            Run("rarity pricing never inverts within a scene and grade", TestRarityPricingNeverInverts);
            Run("sale prices honor weight and rarity", TestSalePricesHonorWeightAndRarity);
            Run("tackle catalog supports bait hooks and lines", TestTackleCatalog);
            Run("bait and hook preferences affect fish selection", TestTacklePreferencesAffectSelection);
            Run("last missing scene fish gets collection pity", TestLastMissingFishGetsCollectionPity);
            Run("collection pity only applies to early scenes", TestCollectionPityOnlyAppliesToEarlyScenes);
            Run("starter tension actions are not too sensitive", TestStarterTensionActionsAreNotTooSensitive);
            Run("safe tension prevents slip and line cut", TestSafeTensionPreventsSlipAndLineCut);
            Run("stamina is infinite during testing", TestTestingStaminaIsInfinite);
            Run("missed bites consume bait", TestMissedBiteConsumesBait);
            Run("line cuts shorten line and lose hook", TestLineCutShortensLineAndLosesHook);
            Run("hidden non-fish items exist and sell for coins", TestHiddenItems);
            Run("aquarium capacity is enforced", TestAquariumCapacity);
            Run("ticket income starts at viewing gallery and is daily", TestTicketIncome);
            Run("save store preserves progress", TestSaveStoreRoundTrip);

            if (_failures > 0)
            {
                Console.WriteLine("FAILED: " + _failures + " test(s)");
                return 1;
            }

            Console.WriteLine("PASS: all core tests");
            return 0;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS " + name);
            }
            catch (Exception ex)
            {
                _failures++;
                Console.WriteLine("FAIL " + name + ": " + ex.Message);
            }
        }

        private static void TestDataCatalog()
        {
            AssertEqual(12, GameData.Scenes.Count, "scene count");

            foreach (SceneInfo scene in GameData.Scenes)
            {
                List<FishSpecies> fish = GameData.FishByScene[scene.Id];
                AssertTrue(fish.Count >= 20 && fish.Count <= 100, "fish count for " + scene.Name);

                int hiddenCount = fish.Count(f => f.IsHidden);
                AssertTrue(hiddenCount >= 1 && hiddenCount <= 2, "hidden fish count for " + scene.Name);
                AssertTrue(fish.Count(f => !f.IsHidden) >= 18, "non-hidden fish count for " + scene.Name);
            }

            List<string> allNames = GameData.AllFish.Select(f => f.Name).ToList();
            AssertEqual(allNames.Count, allNames.Distinct().Count(), "unique fish names");
            AssertTrue(GameData.Rods.Count >= 20, "rod count");
            AssertEqual(5, GameData.AquariumTiers[0].Capacity, "smallest aquarium");
            AssertEqual(1000, GameData.AquariumTiers[GameData.AquariumTiers.Count - 1].Capacity, "largest aquarium");
            AssertTrue(GameData.AquariumTiers.Any(t => t.Name == "观赏鱼廊" && t.TicketEnabled), "ticket starts at viewing gallery");
            AssertEqual(12, GameData.HiddenItemsByScene.Count, "hidden item scene count");
            AssertTrue(GameData.HiddenItemsByScene.Values.All(list => list.Count >= 2), "hidden items per scene");

            int viewingIndex = GameData.AquariumTiers.FindIndex(t => t.Name == "观赏鱼廊");
            AssertTrue(viewingIndex > 0, "viewing gallery exists");
            for (int i = 0; i < viewingIndex; i++)
            {
                AssertFalse(GameData.AquariumTiers[i].TicketEnabled, "ticket disabled before viewing gallery");
            }
        }

        private static void TestSceneFishCountRamp()
        {
            int firstCount = GameData.FishByScene[GameData.Scenes[0].Id].Count;
            int lastCount = GameData.FishByScene[GameData.Scenes[GameData.Scenes.Count - 1].Id].Count;
            AssertTrue(firstCount <= 25, "scene one should be light enough for early collection");
            AssertEqual(100, lastCount, "last scene reaches 100 fish");

            int previous = 0;
            foreach (SceneInfo scene in GameData.Scenes)
            {
                int count = GameData.FishByScene[scene.Id].Count;
                AssertTrue(count >= previous, "scene fish counts should not go down");
                previous = count;
            }
        }

        private static void TestSceneUnlockRequiresNonHiddenCollection()
        {
            SceneInfo firstScene = GameData.Scenes[0];
            SceneInfo secondScene = GameData.Scenes[1];
            List<FishSpecies> nonHidden = GameData.FishByScene[firstScene.Id].Where(f => !f.IsHidden).ToList();

            GameState complete = GameRules.CreateNewGame();
            complete.CollectionSpeciesIds = nonHidden.Select(f => f.Id).ToList();
            AssertTrue(GameRules.CanUnlockNextScene(complete, firstScene, secondScene), "all non-hidden unlocks next scene");

            GameState missingOne = GameRules.CreateNewGame();
            missingOne.CollectionSpeciesIds = nonHidden.Take(nonHidden.Count - 1).Select(f => f.Id).ToList();
            FishSpecies hiddenFish = GameData.FishByScene[firstScene.Id].First(f => f.IsHidden);
            missingOne.CollectionSpeciesIds.Add(hiddenFish.Id);
            AssertFalse(GameRules.CanUnlockNextScene(missingOne, firstScene, secondScene), "hidden fish does not replace missing non-hidden fish");
        }

        private static void TestLuckImprovesHiddenChance()
        {
            FishSpecies hiddenFish = GameData.AllFish.First(f => f.IsHidden);
            Rod starterRod = GameData.Rods[0];
            Rod luckyRod = GameData.Rods.OrderByDescending(r => r.Luck).First();

            double starterChance = GameRules.HiddenChanceForRod(hiddenFish, starterRod);
            double luckyChance = GameRules.HiddenChanceForRod(hiddenFish, luckyRod);

            AssertTrue(luckyChance > starterChance, "lucky rod hidden chance");
            AssertTrue(luckyChance <= 0.05, "hidden chance cap");
        }

        private static void TestTensionWindowsVaryByFishAndRod()
        {
            Rod starterRod = GameData.Rods[0];
            Rod highControlRod = GameData.Rods.OrderByDescending(r => r.Control).First();
            FishSpecies calmFish = GameData.AllFish.Where(f => !f.IsHidden).OrderBy(f => f.TensionVolatility).First();
            FishSpecies wildFish = GameData.AllFish.Where(f => !f.IsHidden).OrderByDescending(f => f.TensionVolatility).First();

            TensionWindow calmStarter = GameRules.CalculateTensionWindow(calmFish, starterRod);
            TensionWindow wildStarter = GameRules.CalculateTensionWindow(wildFish, starterRod);
            TensionWindow wildGoodRod = GameRules.CalculateTensionWindow(wildFish, highControlRod);

            AssertTrue(calmStarter.Low != wildStarter.Low || calmStarter.High != wildStarter.High, "fish changes safe window");
            AssertTrue(wildGoodRod.Width > wildStarter.Width, "better rod widens safe window");
        }

        private static void TestFishTraitsVaryActionBehavior()
        {
            Rod middleRod = GameData.Rods[8];
            FishSpecies stiffFish = GameData.AllFish.Where(f => !f.IsHidden).OrderByDescending(f => f.PullResistance).First();
            FishSpecies softFish = GameData.AllFish.Where(f => !f.IsHidden).OrderBy(f => f.PullResistance).First();
            FishSpecies diverFish = GameData.AllFish.Where(f => !f.IsHidden).OrderByDescending(f => f.ReleaseSensitivity).First();

            TensionActionProfile stiffProfile = GameRules.CalculateActionProfile(stiffFish, middleRod);
            TensionActionProfile softProfile = GameRules.CalculateActionProfile(softFish, middleRod);
            TensionActionProfile diverProfile = GameRules.CalculateActionProfile(diverFish, middleRod);

            AssertTrue(softProfile.PullAmount > stiffProfile.PullAmount, "pull resistance affects拉线");
            AssertTrue(diverProfile.ReleaseAmount != stiffProfile.ReleaseAmount, "release sensitivity affects放线");
        }

        private static void TestWeightClassesControlSale()
        {
            FishSpecies fish = GameData.AllFish.First(f => !f.IsHidden && f.MaxWeight - f.MinWeight > 1.0);
            double span = fish.MaxWeight - fish.MinWeight;
            CatchRecord small = GameRules.CreateCatchForWeight(fish, fish.MinWeight + span * 0.1);
            CatchRecord normal = GameRules.CreateCatchForWeight(fish, fish.MinWeight + span * 0.55);
            CatchRecord trophy = GameRules.CreateCatchForWeight(fish, fish.MinWeight + span * 0.94);

            AssertEqual("偏小", small.WeightGrade, "small fish grade");
            AssertFalse(small.IsSellable, "small fish not sellable");
            AssertEqual(0, small.SellPrice, "small fish sell price");
            AssertEqual("一般", normal.WeightGrade, "normal fish grade");
            AssertTrue(normal.IsSellable, "normal fish sellable");
            AssertEqual("极品", trophy.WeightGrade, "trophy fish grade");
            AssertTrue(trophy.SellPrice >= normal.SellPrice * 2, "trophy at least double normal");
            AssertTrue(trophy.SellPrice <= normal.SellPrice * 10, "trophy capped at ten times normal");

            GameState state = GameRules.CreateNewGame();
            GameRules.RegisterCatch(state, small);
            AssertEqual(0, state.Bag.Count, "small fish is released instead of bagged");
            AssertTrue(state.CollectionSpeciesIds.Contains(fish.Id), "released small fish still records collection");
        }

        private static void TestRarityPricingNeverInverts()
        {
            string[] order = { "普通", "优秀", "稀有", "史诗", "传说" };
            foreach (SceneInfo scene in GameData.Scenes)
            {
                List<FishSpecies> fish = GameData.FishByScene[scene.Id].Where(f => !f.IsHidden).ToList();
                for (int i = 0; i < order.Length - 1; i++)
                {
                    List<FishSpecies> lower = fish.Where(f => f.Rarity == order[i]).ToList();
                    List<FishSpecies> higher = fish.Where(f => f.Rarity == order[i + 1]).ToList();
                    if (lower.Count == 0 || higher.Count == 0)
                    {
                        continue;
                    }

                    int lowerBestTrophy = lower.Max(f => GameRules.CreateCatchForWeight(f, f.MinWeight + (f.MaxWeight - f.MinWeight) * 0.94).SellPrice);
                    int higherWorstTrophy = higher.Min(f => GameRules.CreateCatchForWeight(f, f.MinWeight + (f.MaxWeight - f.MinWeight) * 0.94).SellPrice);
                    AssertTrue(higherWorstTrophy > lowerBestTrophy, scene.Name + " " + order[i + 1] + "极品 should beat " + order[i] + "极品");

                    int lowerBestNormal = lower.Max(f => GameRules.CreateCatchForWeight(f, f.MinWeight + (f.MaxWeight - f.MinWeight) * 0.55).SellPrice);
                    int higherWorstNormal = higher.Min(f => GameRules.CreateCatchForWeight(f, f.MinWeight + (f.MaxWeight - f.MinWeight) * 0.55).SellPrice);
                    AssertTrue(higherWorstNormal > lowerBestNormal, scene.Name + " " + order[i + 1] + "一般 should beat " + order[i] + "一般");
                }
            }
        }

        private static void TestSalePricesHonorWeightAndRarity()
        {
            FishSpecies commonTrophy = GameData.AllFish.First(f => f.Name == "晴溪琥珀月纹龙鱼10");
            FishSpecies excellentTrophy = GameData.AllFish.First(f => f.Name == "晴溪碧眼剑尾灯鱼16");
            CatchRecord common = GameRules.CreateCatchForWeight(commonTrophy, 1.39);
            CatchRecord excellent = GameRules.CreateCatchForWeight(excellentTrophy, 2.98);
            AssertEqual("极品", common.WeightGrade, "common screenshot grade");
            AssertEqual("极品", excellent.WeightGrade, "excellent screenshot grade");
            AssertTrue(excellent.SellPrice > common.SellPrice, "heavier excellent trophy should beat lighter common trophy");

            FishSpecies fish = GameData.AllFish.First(f => !f.IsHidden && f.MaxWeight - f.MinWeight > 1.0);
            double span = fish.MaxWeight - fish.MinWeight;
            CatchRecord lighterNormal = GameRules.CreateCatchForWeight(fish, fish.MinWeight + span * 0.38);
            CatchRecord heavierNormal = GameRules.CreateCatchForWeight(fish, fish.MinWeight + span * 0.72);
            AssertEqual("一般", lighterNormal.WeightGrade, "lighter normal grade");
            AssertEqual("一般", heavierNormal.WeightGrade, "heavier normal grade");
            AssertTrue(heavierNormal.SellPrice > lighterNormal.SellPrice, "normal fish should sell by weight");
        }

        private static void TestTackleCatalog()
        {
            AssertTrue(GameData.Baits.Count >= 8, "bait catalog");
            AssertTrue(GameData.Hooks.Count >= 6, "hook catalog");
            AssertTrue(GameData.Lines.Count >= 6, "line catalog");

            HashSet<string> baitIds = new HashSet<string>(GameData.Baits.Select(b => b.Id));
            foreach (FishSpecies fish in GameData.AllFish)
            {
                AssertTrue(baitIds.Contains(fish.FavoriteBaitId), fish.Name + " favorite bait exists");
                AssertTrue(baitIds.Contains(fish.AcceptableBaitId), fish.Name + " acceptable bait exists");
                AssertTrue(fish.PreferredHookSize >= 1 && fish.PreferredHookSize <= 5, fish.Name + " hook size preference");
            }

            GameState state = GameRules.CreateNewGame();
            AssertTrue(GameRules.GetInventoryCount(state.BaitInventory, state.EquippedBaitId) > 0, "starter bait owned");
            AssertTrue(GameRules.GetInventoryCount(state.HookInventory, state.EquippedHookId) > 0, "starter hook owned");
            AssertTrue(state.EquippedLineLength > 20, "starter line equipped");
        }

        private static void TestTacklePreferencesAffectSelection()
        {
            FishSpecies fish = GameData.FishByScene[GameData.Scenes[0].Id].First(f => !f.IsHidden);
            GameState good = GameRules.CreateNewGame();
            good.EquippedBaitId = fish.FavoriteBaitId;
            good.EquippedHookId = GameData.Hooks.OrderBy(h => Math.Abs(h.Size - fish.PreferredHookSize)).First().Id;

            GameState bad = GameRules.CreateNewGame();
            bad.EquippedBaitId = GameData.Baits.First(b => b.Id != fish.FavoriteBaitId && b.Id != fish.AcceptableBaitId).Id;
            bad.EquippedHookId = GameData.Hooks.OrderByDescending(h => Math.Abs(h.Size - fish.PreferredHookSize)).First().Id;

            double goodWeight = GameRules.FishAttractionWeight(good, fish);
            double badWeight = GameRules.FishAttractionWeight(bad, fish);
            AssertTrue(goodWeight > badWeight * 2.0, "favorite bait and matching hook should strongly improve attraction");
        }

        private static void TestLastMissingFishGetsCollectionPity()
        {
            SceneInfo first = GameData.Scenes[0];
            List<FishSpecies> regular = GameData.FishByScene[first.Id].Where(f => !f.IsHidden).ToList();
            FishSpecies missingEpic = regular.Single(f => f.Rarity == "史诗");
            GameState state = GameRules.CreateNewGame();
            state.CollectionSpeciesIds = regular.Where(f => f.Id != missingEpic.Id).Select(f => f.Id).ToList();
            int hits = 0;
            Random random = new Random(1234);
            for (int i = 0; i < 120; i++)
            {
                FishSpecies chosen = GameRules.ChooseFish(state, first.Id, random);
                if (chosen.Id == missingEpic.Id)
                {
                    hits++;
                }
            }
            AssertTrue(hits >= 25, "last missing epic should not be a one-percent grind");
        }

        private static void TestCollectionPityOnlyAppliesToEarlyScenes()
        {
            SceneInfo early = GameData.Scenes[3];
            SceneInfo later = GameData.Scenes[4];
            FishSpecies earlyTarget = GameData.FishByScene[early.Id].First(f => !f.IsHidden);
            FishSpecies laterTarget = GameData.FishByScene[later.Id].First(f => !f.IsHidden);

            GameState earlyBase = GameRules.CreateNewGame();
            earlyBase.EquippedBaitId = earlyTarget.FavoriteBaitId;
            earlyBase.EquippedHookId = GameData.Hooks.OrderBy(h => Math.Abs(h.Size - earlyTarget.PreferredHookSize)).First().Id;
            GameState earlyPity = GameRules.CreateNewGame();
            earlyPity.EquippedBaitId = earlyBase.EquippedBaitId;
            earlyPity.EquippedHookId = earlyBase.EquippedHookId;
            earlyPity.CollectionSpeciesIds = GameData.FishByScene[early.Id].Where(f => !f.IsHidden && f.Id != earlyTarget.Id).Select(f => f.Id).ToList();

            GameState laterBase = GameRules.CreateNewGame();
            laterBase.EquippedBaitId = laterTarget.FavoriteBaitId;
            laterBase.EquippedHookId = GameData.Hooks.OrderBy(h => Math.Abs(h.Size - laterTarget.PreferredHookSize)).First().Id;
            GameState laterPity = GameRules.CreateNewGame();
            laterPity.EquippedBaitId = laterBase.EquippedBaitId;
            laterPity.EquippedHookId = laterBase.EquippedHookId;
            laterPity.CollectionSpeciesIds = GameData.FishByScene[later.Id].Where(f => !f.IsHidden && f.Id != laterTarget.Id).Select(f => f.Id).ToList();

            double earlyBoost = GameRules.FishAttractionWeight(earlyPity, earlyTarget) / GameRules.FishAttractionWeight(earlyBase, earlyTarget);
            double laterBoost = GameRules.FishAttractionWeight(laterPity, laterTarget) / GameRules.FishAttractionWeight(laterBase, laterTarget);
            AssertTrue(earlyBoost > 30.0, "early scenes get last-missing pity");
            AssertTrue(laterBoost < 5.0, "scene five and later remain true random apart from normal missing bonus");
        }

        private static void TestStarterTensionActionsAreNotTooSensitive()
        {
            Rod starter = GameData.Rods[0];
            FishSpecies firstSceneFish = GameData.FishByScene[GameData.Scenes[0].Id].First(f => !f.IsHidden);
            TensionActionProfile profile = GameRules.CalculateActionProfile(firstSceneFish, starter);
            AssertTrue(profile.PullAmount <= 5.5, "starter pull should not jump to the limit");
            AssertTrue(profile.ReleaseAmount <= 6.2, "starter release should not jump to the limit");
            AssertTrue(profile.DriftAmount <= 2.2, "early fish drift should be readable");
        }

        private static void TestSafeTensionPreventsSlipAndLineCut()
        {
            FishSpecies fish = GameData.FishByScene[GameData.Scenes[0].Id].OrderByDescending(f => f.RunStrength).First(f => !f.IsHidden);
            Hook hook = GameData.Hooks.OrderBy(h => Math.Abs(h.Size - fish.PreferredHookSize)).First();
            FishingLine line = GameData.Lines[0];
            TensionWindow window = GameRules.CalculateTensionWindow(fish, GameData.Rods[0]);
            double safeTension = (window.Low + window.High) / 2.0;
            AssertEqual(0.0, GameRules.HookSlipChance(fish, hook, safeTension, window), "no hook slip inside safe window");
            AssertEqual(0.0, GameRules.LineCutChance(fish, line, safeTension, window), "no line cut inside safe window");

            double dangerTension = Math.Min(100, window.High + 18);
            AssertTrue(GameRules.LineCutChance(fish, line, dangerTension, window) < 0.08, "line cut is not a constant per-tick punishment");
        }

        private static void TestTestingStaminaIsInfinite()
        {
            GameState state = GameRules.CreateNewGame();
            state.Stamina = 0;
            state.InfiniteStamina = true;
            AssertTrue(GameRules.CanStartCast(state), "infinite stamina allows fishing");
            AssertTrue(GameRules.ConsumeStamina(state), "consume stamina succeeds");
            AssertEqual(0, state.Stamina, "infinite stamina does not decrement stored stamina");
        }

        private static void TestMissedBiteConsumesBait()
        {
            GameState state = GameRules.CreateNewGame();
            int before = GameRules.GetInventoryCount(state.BaitInventory, state.EquippedBaitId);
            AssertTrue(GameRules.ConsumeBait(state), "bait consumed");
            AssertEqual(before - 1, GameRules.GetInventoryCount(state.BaitInventory, state.EquippedBaitId), "bait count after miss");
        }

        private static void TestLineCutShortensLineAndLosesHook()
        {
            GameState state = GameRules.CreateNewGame();
            int hooksBefore = GameRules.GetInventoryCount(state.HookInventory, state.EquippedHookId);
            double lineBefore = state.EquippedLineLength;
            GameRules.ApplyLineCut(state, 14.0);
            AssertTrue(state.EquippedLineLength < lineBefore, "line length shortened");
            AssertEqual(hooksBefore - 1, GameRules.GetInventoryCount(state.HookInventory, state.EquippedHookId), "hook lost on cut line");
        }

        private static void TestHiddenItems()
        {
            foreach (SceneInfo scene in GameData.Scenes)
            {
                List<HiddenItem> items = GameData.HiddenItemsByScene[scene.Id];
                AssertTrue(items.Any(i => i.Name.Contains("手表") || i.Name.Contains("宝石") || i.Name.Contains("手机")), "scene has recognizable hidden item");
                AssertTrue(items.All(i => i.BasePrice > 0), "hidden item has sale value");
            }

            HiddenItem itemCatch = GameData.HiddenItemsByScene[GameData.Scenes[0].Id].First();
            GameState state = GameRules.CreateNewGame();
            CatchRecord record = GameRules.CreateItemCatch(itemCatch);
            int bonus = GameRules.RegisterCatch(state, record);
            AssertFalse(record.IsFish, "hidden item catch is not fish");
            AssertTrue(record.IsSellable, "hidden item is sellable");
            AssertTrue(state.Bag.Count == 1, "hidden item enters bag");
            AssertTrue(state.ItemCollectionIds.Contains(itemCatch.Id), "hidden item collection recorded");
            AssertTrue(bonus > 0, "hidden item first find bonus");
            AssertFalse(GameRules.TryMoveCatchToAquarium(state, record.Id), "hidden item cannot go into aquarium");
        }

        private static void TestAquariumCapacity()
        {
            GameState state = GameRules.CreateNewGame();
            FishSpecies fish = GameData.AllFish.First(f => !f.IsHidden);

            for (int i = 0; i < GameData.AquariumTiers[0].Capacity; i++)
            {
                state.Bag.Add(CreateCatch(fish, i));
                AssertTrue(GameRules.TryMoveCatchToAquarium(state, "catch-" + i), "move fish " + i);
            }

            state.Bag.Add(CreateCatch(fish, 99));
            AssertFalse(GameRules.TryMoveCatchToAquarium(state, "catch-99"), "reject over capacity");
        }

        private static void TestTicketIncome()
        {
            GameState state = GameRules.CreateNewGame();
            state.AquariumTierIndex = GameData.AquariumTiers.FindIndex(t => t.Name == "观赏鱼廊");
            FishSpecies common = GameData.AllFish.First(f => !f.IsHidden && f.Rarity == "普通");
            FishSpecies hidden = GameData.AllFish.First(f => f.IsHidden);
            state.Aquarium.Add(CreateCatch(common, 1));
            state.Aquarium.Add(CreateCatch(hidden, 2));

            DateTime today = new DateTime(2026, 5, 7);
            int income = GameRules.CollectTicketIncome(state, today);
            AssertTrue(income > 0, "ticket income amount");
            AssertEqual(0, GameRules.CollectTicketIncome(state, today), "ticket only once per day");

            GameState smallTank = GameRules.CreateNewGame();
            smallTank.Aquarium.Add(CreateCatch(common, 3));
            AssertEqual(0, GameRules.CollectTicketIncome(smallTank, today), "no tickets before viewing gallery");
        }

        private static void TestSaveStoreRoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), "fishing-game-test-save.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            GameState state = GameRules.CreateNewGame();
            state.Coins = 4321;
            state.OwnedRodIds.Add(GameData.Rods[1].Id);
            state.CollectionSpeciesIds.Add(GameData.AllFish[0].Id);
            state.AquariumTierIndex = 4;
            state.Aquarium.Add(CreateCatch(GameData.AllFish[0], 77));
            state.LastTicketDate = "2026-05-07";

            SaveStore.Save(path, state);
            GameState loaded = SaveStore.Load(path);

            AssertEqual(4321, loaded.Coins, "saved coins");
            AssertTrue(loaded.OwnedRodIds.Contains(GameData.Rods[1].Id), "saved rods");
            AssertTrue(loaded.CollectionSpeciesIds.Contains(GameData.AllFish[0].Id), "saved collection");
            AssertEqual(4, loaded.AquariumTierIndex, "saved aquarium tier");
            AssertEqual(1, loaded.Aquarium.Count, "saved aquarium contents");
            AssertEqual("2026-05-07", loaded.LastTicketDate, "saved ticket date");

            File.Delete(path);
        }

        private static CatchRecord CreateCatch(FishSpecies fish, int index)
        {
            return new CatchRecord
            {
                Id = "catch-" + index,
                SpeciesId = fish.Id,
                SpeciesName = fish.Name,
                SceneId = fish.SceneId,
                Rarity = fish.Rarity,
                IsHidden = fish.IsHidden,
                IsFish = true,
                Weight = fish.MinWeight,
                WeightGrade = "一般",
                IsSellable = true,
                SellPrice = fish.BasePrice,
                CaughtAt = "2026-05-07T00:00:00"
            };
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new Exception(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception(message + " expected <" + expected + "> but was <" + actual + ">");
            }
        }
    }
}
