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
        public string FavoriteBaitId { get; set; }
        public string AcceptableBaitId { get; set; }
        public int PreferredHookSize { get; set; }
        public string PreferredHookStyle { get; set; }
        public int PreferredTension { get; set; }
        public int TensionWindow { get; set; }
        public int PullResistance { get; set; }
        public int ReleaseSensitivity { get; set; }
        public int TensionVolatility { get; set; }
        public int RunStrength { get; set; }
        public string IconSymbol { get; set; }
    }

    public class Bait
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int Price { get; set; }
        public int PackSize { get; set; }
        public int MinSceneDifficulty { get; set; }
        public int RarityBias { get; set; }
        public bool IsLure { get; set; }
        public string Description { get; set; }
    }

    public class Hook
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Style { get; set; }
        public int Size { get; set; }
        public int Price { get; set; }
        public int PackSize { get; set; }
        public int HoldStrength { get; set; }
        public string Description { get; set; }
    }

    public class FishingLine
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Quality { get; set; }
        public int Price { get; set; }
        public double MaxTension { get; set; }
        public double CutResistance { get; set; }
        public double MaxLength { get; set; }
        public string Description { get; set; }
    }

    public class InventoryStack
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class HiddenItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SceneId { get; set; }
        public int BasePrice { get; set; }
        public double HiddenBaseChance { get; set; }
        public string Description { get; set; }
        public string IconSymbol { get; set; }
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
        public bool IsFish { get; set; }
        public double Weight { get; set; }
        public string WeightGrade { get; set; }
        public bool IsSellable { get; set; }
        public int SellPrice { get; set; }
        public string CaughtAt { get; set; }
        public string IconSymbol { get; set; }
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
        public List<InventoryStack> BaitInventory { get; set; }
        public List<InventoryStack> HookInventory { get; set; }
        public List<InventoryStack> LineInventory { get; set; }
        public string EquippedBaitId { get; set; }
        public string EquippedHookId { get; set; }
        public string EquippedLineId { get; set; }
        public double EquippedLineLength { get; set; }
        public int Stamina { get; set; }
        public int MaxStamina { get; set; }
        public bool InfiniteStamina { get; set; }
        public string LastStaminaAt { get; set; }
        public List<string> CollectionSpeciesIds { get; set; }
        public List<string> ItemCollectionIds { get; set; }
        public string LastSignInDate { get; set; }
        public string LastTicketDate { get; set; }

        public GameState()
        {
            OwnedRodIds = new List<string>();
            UnlockedSceneIds = new List<string>();
            Bag = new List<CatchRecord>();
            Aquarium = new List<CatchRecord>();
            BaitInventory = new List<InventoryStack>();
            HookInventory = new List<InventoryStack>();
            LineInventory = new List<InventoryStack>();
            CollectionSpeciesIds = new List<string>();
            ItemCollectionIds = new List<string>();
            LastSignInDate = "";
            LastTicketDate = "";
            LastStaminaAt = "";
        }
    }

    public class TensionWindow
    {
        public int Low { get; set; }
        public int High { get; set; }

        public int Width
        {
            get { return High - Low; }
        }
    }

    public class TensionActionProfile
    {
        public double PullAmount { get; set; }
        public double ReleaseAmount { get; set; }
        public double DriftAmount { get; set; }
    }
}
