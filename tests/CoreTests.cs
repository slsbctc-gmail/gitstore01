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
            Run("non-hidden collection unlocks next scene", TestSceneUnlockRequiresNonHiddenCollection);
            Run("luck improves hidden fish chance", TestLuckImprovesHiddenChance);
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
                AssertTrue(fish.Count >= 50 && fish.Count <= 100, "fish count for " + scene.Name);

                int hiddenCount = fish.Count(f => f.IsHidden);
                AssertTrue(hiddenCount >= 1 && hiddenCount <= 2, "hidden fish count for " + scene.Name);
                AssertTrue(fish.Count(f => !f.IsHidden) >= 48, "non-hidden fish count for " + scene.Name);
            }

            List<string> allNames = GameData.AllFish.Select(f => f.Name).ToList();
            AssertEqual(allNames.Count, allNames.Distinct().Count(), "unique fish names");
            AssertTrue(GameData.Rods.Count >= 20, "rod count");
            AssertEqual(5, GameData.AquariumTiers[0].Capacity, "smallest aquarium");
            AssertEqual(1000, GameData.AquariumTiers[GameData.AquariumTiers.Count - 1].Capacity, "largest aquarium");
            AssertTrue(GameData.AquariumTiers.Any(t => t.Name == "观赏鱼廊" && t.TicketEnabled), "ticket starts at viewing gallery");

            int viewingIndex = GameData.AquariumTiers.FindIndex(t => t.Name == "观赏鱼廊");
            AssertTrue(viewingIndex > 0, "viewing gallery exists");
            for (int i = 0; i < viewingIndex; i++)
            {
                AssertFalse(GameData.AquariumTiers[i].TicketEnabled, "ticket disabled before viewing gallery");
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
                Weight = fish.MinWeight,
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

