using System.Collections.Generic;

namespace FishingGame.Core
{
    public class SceneInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Difficulty { get; set; }
        public int FishTargetCount { get; set; }
        public int HiddenFishCount { get; set; }
        public string PaletteTop { get; set; }
        public string PaletteBottom { get; set; }
    }

    public class FishSpecies
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SceneId { get; set; }
        public string Rarity { get; set; }
        public int Difficulty { get; set; }
        public int BasePrice { get; set; }
        public double MinWeight { get; set; }
        public double MaxWeight { get; set; }
        public string Description { get; set; }
        public bool IsHidden { get; set; }
        public double HiddenBaseChance { get; set; }
    }

    public class Rod
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Quality { get; set; }
        public int Price { get; set; }
        public int Power { get; set; }
        public int Control { get; set; }
        public int Luck { get; set; }
        public int Stability { get; set; }
    }

    public class AquariumTier
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public int Price { get; set; }
        public bool TicketEnabled { get; set; }
        public double TicketMultiplier { get; set; }
    }

    public class CatchRecord
    {
        public string Id { get; set; }
        public string SpeciesId { get; set; }
        public string SpeciesName { get; set; }
        public string SceneId { get; set; }
        public string Rarity { get; set; }
        public bool IsHidden { get; set; }
        public double Weight { get; set; }
        public int SellPrice { get; set; }
        public string CaughtAt { get; set; }
    }

    public class GameState
    {
        public int Coins { get; set; }
        public List<string> OwnedRodIds { get; set; }
        public string EquippedRodId { get; set; }
        public List<string> UnlockedSceneIds { get; set; }
        public List<CatchRecord> Bag { get; set; }
        public int AquariumTierIndex { get; set; }
        public List<CatchRecord> Aquarium { get; set; }
        public List<string> CollectionSpeciesIds { get; set; }
        public string LastSignInDate { get; set; }
        public string LastTicketDate { get; set; }

        public GameState()
        {
            OwnedRodIds = new List<string>();
            UnlockedSceneIds = new List<string>();
            Bag = new List<CatchRecord>();
            Aquarium = new List<CatchRecord>();
            CollectionSpeciesIds = new List<string>();
            LastSignInDate = "";
            LastTicketDate = "";
        }
    }
}

