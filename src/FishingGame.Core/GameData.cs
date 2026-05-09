using System;
using System.Collections.Generic;
using System.Linq;

namespace FishingGame.Core
{
    public static class GameData
    {
        public static List<SceneInfo> Scenes { get; private set; }
        public static List<FishSpecies> AllFish { get; private set; }
        public static Dictionary<string, List<FishSpecies>> FishByScene { get; private set; }
        public static List<HiddenItem> AllHiddenItems { get; private set; }
        public static Dictionary<string, List<HiddenItem>> HiddenItemsByScene { get; private set; }
        public static List<Bait> Baits { get; private set; }
        public static List<Hook> Hooks { get; private set; }
        public static List<FishingLine> Lines { get; private set; }
        public static List<Rod> Rods { get; private set; }
        public static List<AquariumTier> AquariumTiers { get; private set; }

        static GameData()
        {
            Scenes = BuildScenes();
            Baits = BuildBaits();
            Hooks = BuildHooks();
            Lines = BuildLines();
            Rods = BuildRods();
            AquariumTiers = BuildAquariumTiers();
            AllFish = BuildFish();
            FishByScene = AllFish.GroupBy(f => f.SceneId).ToDictionary(g => g.Key, g => g.ToList());
            AllHiddenItems = BuildHiddenItems();
            HiddenItemsByScene = AllHiddenItems.GroupBy(i => i.SceneId).ToDictionary(g => g.Key, g => g.ToList());
        }

        public static SceneInfo FindScene(string id)
        {
            return Scenes.First(s => s.Id == id);
        }

        public static FishSpecies FindFish(string id)
        {
            return AllFish.First(f => f.Id == id);
        }

        public static Rod FindRod(string id)
        {
            return Rods.First(r => r.Id == id);
        }

        public static Bait FindBait(string id)
        {
            return Baits.First(b => b.Id == id);
        }

        public static Hook FindHook(string id)
        {
            return Hooks.First(h => h.Id == id);
        }

        public static FishingLine FindLine(string id)
        {
            return Lines.First(l => l.Id == id);
        }

        private static List<SceneInfo> BuildScenes()
        {
            string[] names = {
                "晴溪浅滩", "荷叶小塘", "柳影长河", "雾色湖心",
                "芦苇湿地", "海港栈桥", "月湾渔场", "珊瑚海岸",
                "冰封峡湾", "风暴海峡", "深渊海沟", "龙潮禁海"
            };
            string[] top = {
                "#93d5ff", "#a8e6a1", "#9fd3b8", "#b8c7e6",
                "#b7c77a", "#8fcbe8", "#6279c7", "#63d4cf",
                "#d8f3ff", "#6f88aa", "#1f4468", "#733f7d"
            };
            string[] bottom = {
                "#2f9bd8", "#55b86f", "#4d947f", "#6980a9",
                "#768f48", "#397faf", "#253d88", "#179895",
                "#8bb9dc", "#334c73", "#0e2138", "#2c173a"
            };
            int[] counts = { 22, 30, 38, 46, 54, 62, 70, 78, 86, 92, 96, 100 };
            List<SceneInfo> scenes = new List<SceneInfo>();
            for (int i = 0; i < names.Length; i++)
            {
                scenes.Add(new SceneInfo
                {
                    Id = "scene-" + (i + 1).ToString("00"),
                    Name = names[i],
                    Difficulty = i + 1,
                    FishTargetCount = counts[i],
                    HiddenFishCount = i < 6 ? 1 : 2,
                    PaletteTop = top[i],
                    PaletteBottom = bottom[i]
                });
            }
            return scenes;
        }

        private static List<FishSpecies> BuildFish()
        {
            string[] colors = {
                "青鳞", "银背", "金纹", "赤尾", "墨影", "蓝脊", "白腹", "翠须",
                "紫点", "琥珀", "雪鳍", "云斑", "铜额", "霞光", "夜纹", "碧眼"
            };
            string[] shapes = {
                "短吻", "长须", "圆鳍", "弯背", "流线", "厚唇", "尖吻", "宽尾",
                "细鳞", "高背", "星点", "月纹", "羽鳍", "剑尾", "玉腹", "虹纹"
            };
            string[] families = {
                "溪鲫", "河鲤", "银鲈", "石斑", "花鲦", "鳟鱼", "鲑鱼", "鳗鱼",
                "鲷鱼", "灯鱼", "鲭鱼", "鳕鱼", "鲨鱼", "魟鱼", "鲟鱼", "龙鱼"
            };
            string[] sceneTokens = {
                "晴溪", "荷塘", "柳河", "雾湖", "苇泽", "海港",
                "月湾", "珊瑚", "冰峡", "风暴", "深渊", "龙潮"
            };
            string[] baitCycle = {
                "bait-redworm", "bait-corn", "bait-shrimp", "bait-dough",
                "bait-minnow", "bait-crab", "bait-metal-lure", "bait-squid-lure"
            };
            string[] hookStyles = { "单钩", "小钩", "双钩", "圆钩", "巨物钩" };
            string[] hiddenNames = {
                "晴溪隐鳞", "荷塘睡莲王", "柳河无声鲤", "雾湖镜影鱼",
                "苇泽暮光鳗", "海港旧锚鲷", "月湾银月鲑", "月湾潮汐魟",
                "珊瑚七彩龙", "珊瑚秘礁鲈", "冰峡蓝晶鳕", "冰峡霜须鲟",
                "风暴雷纹鲨", "风暴云眼鳗", "深渊黑灯鱼", "深渊古骨鲨",
                "龙潮金角龙鱼", "龙潮赤鳞皇", "龙潮星海鲟", "龙潮潮核鳐"
            };

            List<FishSpecies> fish = new List<FishSpecies>();
            int hiddenNameIndex = 0;
            for (int sceneIndex = 0; sceneIndex < Scenes.Count; sceneIndex++)
            {
                SceneInfo scene = Scenes[sceneIndex];
                int regularCount = scene.FishTargetCount - scene.HiddenFishCount;
                for (int i = 0; i < regularCount; i++)
                {
                    string rarity = RarityForIndex(i, regularCount);
                    int rarityBonus = RarityPriceBonus(rarity);
                    string name = sceneTokens[sceneIndex]
                        + colors[(i + sceneIndex) % colors.Length]
                        + shapes[(i * 3 + sceneIndex) % shapes.Length]
                        + families[(i * 7 + sceneIndex) % families.Length]
                        + (i + 1).ToString("00");

                    fish.Add(new FishSpecies
                    {
                        Id = scene.Id + "-fish-" + (i + 1).ToString("000"),
                        Name = name,
                        SceneId = scene.Id,
                        Rarity = rarity,
                        Difficulty = scene.Difficulty * 8 + i % 9 + RarityDifficultyBonus(rarity),
                        BasePrice = 12 + scene.Difficulty * 9 + rarityBonus + i % 5,
                        MinWeight = Math.Round(0.2 + scene.Difficulty * 0.18 + (i % 6) * 0.12, 2),
                        MaxWeight = Math.Round(1.0 + scene.Difficulty * 0.42 + (i % 9) * 0.3, 2),
                        Description = scene.Name + "中常被钓手记录的" + rarity + "鱼。",
                        IsHidden = false,
                        HiddenBaseChance = 0,
                        FavoriteBaitId = baitCycle[(i + sceneIndex * 2) % baitCycle.Length],
                        AcceptableBaitId = baitCycle[(i + sceneIndex * 2 + 3) % baitCycle.Length],
                        PreferredHookSize = Math.Min(5, Math.Max(1, 1 + (i % 5) + RarityDifficultyBonus(rarity) / 12)),
                        PreferredHookStyle = hookStyles[(i + sceneIndex) % hookStyles.Length],
                        PreferredTension = 34 + (i * 11 + sceneIndex * 5) % 33,
                        TensionWindow = Math.Max(7, 17 - sceneIndex / 2 - RarityDifficultyBonus(rarity) / 8),
                        PullResistance = 2 + (i * 5 + sceneIndex * 2) % 13 + RarityDifficultyBonus(rarity) / 6,
                        ReleaseSensitivity = 2 + (i * 7 + sceneIndex) % 14,
                        TensionVolatility = 2 + (i * 3 + sceneIndex * 2) % 13 + RarityDifficultyBonus(rarity) / 5,
                        RunStrength = 2 + (i * 9 + sceneIndex) % 14 + scene.Difficulty / 2,
                        IconSymbol = "鱼"
                    });
                }

                for (int h = 0; h < scene.HiddenFishCount; h++)
                {
                    string hiddenName = hiddenNames[hiddenNameIndex++];
                    fish.Add(new FishSpecies
                    {
                        Id = scene.Id + "-hidden-" + (h + 1).ToString("00"),
                        Name = hiddenName,
                        SceneId = scene.Id,
                        Rarity = "传说",
                        Difficulty = scene.Difficulty * 14 + 40 + h * 5,
                        BasePrice = 180 + scene.Difficulty * 90 + h * 70,
                        MinWeight = Math.Round(2.0 + scene.Difficulty * 0.7 + h, 2),
                        MaxWeight = Math.Round(5.0 + scene.Difficulty * 1.6 + h * 2, 2),
                        Description = "只在" + scene.Name + "极少现身的隐藏鱼。",
                        IsHidden = true,
                        HiddenBaseChance = Math.Max(0.0005, 0.004 - sceneIndex * 0.00028),
                        FavoriteBaitId = sceneIndex >= 5 ? "bait-squid-lure" : "bait-minnow",
                        AcceptableBaitId = sceneIndex >= 7 ? "bait-metal-lure" : "bait-shrimp",
                        PreferredHookSize = Math.Min(5, 4 + h),
                        PreferredHookStyle = "巨物钩",
                        PreferredTension = 42 + (sceneIndex * 7 + h * 13) % 24,
                        TensionWindow = Math.Max(5, 10 - sceneIndex / 4),
                        PullResistance = 13 + sceneIndex + h * 3,
                        ReleaseSensitivity = 8 + sceneIndex % 8 + h * 2,
                        TensionVolatility = 14 + sceneIndex + h * 3,
                        RunStrength = 15 + sceneIndex + h * 4,
                        IconSymbol = "秘"
                    });
                }
            }

            return fish;
        }

        private static List<HiddenItem> BuildHiddenItems()
        {
            string[] itemNames = {
                "旧手表", "河心宝石", "进水手机", "银戒指", "古铜钱", "珍珠胸针",
                "怀旧相机", "翡翠扣", "船长罗盘", "夜光贝", "蓝晶石", "密封钱包"
            };
            string[] icons = { "表", "宝", "机", "戒", "钱", "针", "相", "翠", "盘", "贝", "晶", "包" };
            string[] sceneTokens = {
                "晴溪", "荷塘", "柳河", "雾湖", "苇泽", "海港",
                "月湾", "珊瑚", "冰峡", "风暴", "深渊", "龙潮"
            };
            List<HiddenItem> items = new List<HiddenItem>();
            for (int sceneIndex = 0; sceneIndex < Scenes.Count; sceneIndex++)
            {
                int count = sceneIndex < 6 ? 2 : 3;
                for (int i = 0; i < count; i++)
                {
                    int itemIndex = i == 0 ? sceneIndex % 3 : (sceneIndex * 3 + i) % itemNames.Length;
                    items.Add(new HiddenItem
                    {
                        Id = Scenes[sceneIndex].Id + "-item-" + (i + 1).ToString("00"),
                        Name = sceneTokens[sceneIndex] + itemNames[itemIndex],
                        SceneId = Scenes[sceneIndex].Id,
                        BasePrice = 90 + sceneIndex * 75 + i * 55,
                        HiddenBaseChance = Math.Max(0.001, 0.012 - sceneIndex * 0.00055 + i * 0.0005),
                        Description = "从" + Scenes[sceneIndex].Name + "钓起的非鱼类隐藏物品，可出售换金币。",
                        IconSymbol = icons[itemIndex]
                    });
                }
            }
            return items;
        }

        private static string RarityForIndex(int index, int count)
        {
            double p = (double)index / (double)count;
            if (p < 0.48) return "普通";
            if (p < 0.75) return "优秀";
            if (p < 0.91) return "稀有";
            if (p < 0.98) return "史诗";
            return "传说";
        }

        private static int RarityPriceBonus(string rarity)
        {
            if (rarity == "优秀") return 18;
            if (rarity == "稀有") return 45;
            if (rarity == "史诗") return 95;
            if (rarity == "传说") return 180;
            return 0;
        }

        private static int RarityDifficultyBonus(string rarity)
        {
            if (rarity == "优秀") return 3;
            if (rarity == "稀有") return 8;
            if (rarity == "史诗") return 15;
            if (rarity == "传说") return 25;
            return 0;
        }

        private static List<Bait> BuildBaits()
        {
            return new List<Bait>
            {
                new Bait { Id = "bait-redworm", Name = "红虫饵", Kind = "活饵", Price = 35, PackSize = 12, MinSceneDifficulty = 1, RarityBias = 0, IsLure = false, Description = "浅水小型鱼常见口粮，便宜稳定。" },
                new Bait { Id = "bait-corn", Name = "甜玉米粒", Kind = "素饵", Price = 42, PackSize = 12, MinSceneDifficulty = 1, RarityBias = 0, IsLure = false, Description = "溪流和池塘鱼喜欢的素饵。" },
                new Bait { Id = "bait-shrimp", Name = "鲜虾仁", Kind = "荤饵", Price = 70, PackSize = 10, MinSceneDifficulty = 2, RarityBias = 1, IsLure = false, Description = "对肉食鱼有更强吸引。" },
                new Bait { Id = "bait-dough", Name = "香麦团", Kind = "面饵", Price = 58, PackSize = 14, MinSceneDifficulty = 1, RarityBias = 0, IsLure = false, Description = "泛用型饵料，适合补图鉴。" },
                new Bait { Id = "bait-minnow", Name = "小鱼活饵", Kind = "活饵", Price = 130, PackSize = 8, MinSceneDifficulty = 4, RarityBias = 2, IsLure = false, Description = "更容易吸引大型或稀有鱼。" },
                new Bait { Id = "bait-crab", Name = "小蟹饵", Kind = "甲壳饵", Price = 170, PackSize = 8, MinSceneDifficulty = 5, RarityBias = 2, IsLure = false, Description = "湿地、港口和礁区效果更好。" },
                new Bait { Id = "bait-metal-lure", Name = "银旋路亚", Kind = "路亚", Price = 260, PackSize = 6, MinSceneDifficulty = 6, RarityBias = 3, IsLure = true, Description = "海港以后开始发挥威力的高速路亚。" },
                new Bait { Id = "bait-squid-lure", Name = "夜光鱿鱼路亚", Kind = "路亚", Price = 420, PackSize = 5, MinSceneDifficulty = 8, RarityBias = 4, IsLure = true, Description = "高端海域、深水和夜钓的强力路亚。" }
            };
        }

        private static List<Hook> BuildHooks()
        {
            return new List<Hook>
            {
                new Hook { Id = "hook-micro", Name = "袖珍小钩", Style = "小钩", Size = 1, Price = 28, PackSize = 10, HoldStrength = 18, Description = "适合小型鱼，大鱼咬上容易脱钩。" },
                new Hook { Id = "hook-single", Name = "标准单钩", Style = "单钩", Size = 2, Price = 45, PackSize = 10, HoldStrength = 30, Description = "早期泛用钩。" },
                new Hook { Id = "hook-double", Name = "溪流双钩", Style = "双钩", Size = 2, Price = 78, PackSize = 8, HoldStrength = 34, Description = "提高咬口命中，但不适合巨物。" },
                new Hook { Id = "hook-circle", Name = "防脱圆钩", Style = "圆钩", Size = 3, Price = 120, PackSize = 8, HoldStrength = 48, Description = "降低脱钩率。" },
                new Hook { Id = "hook-heavy", Name = "重型单钩", Style = "单钩", Size = 4, Price = 190, PackSize = 6, HoldStrength = 66, Description = "为大型鱼准备。" },
                new Hook { Id = "hook-giant", Name = "巨物钩", Style = "巨物钩", Size = 5, Price = 320, PackSize = 5, HoldStrength = 88, Description = "只有大鱼愿意咬，小鱼基本不会碰。" }
            };
        }

        private static List<FishingLine> BuildLines()
        {
            return new List<FishingLine>
            {
                new FishingLine { Id = "line-nylon-1", Name = "细尼龙线", Quality = "基础", Price = 60, MaxTension = 72, CutResistance = 0.18, MaxLength = 80, Description = "便宜，容易被大鱼切线。" },
                new FishingLine { Id = "line-nylon-2", Name = "加粗尼龙线", Quality = "基础", Price = 120, MaxTension = 86, CutResistance = 0.28, MaxLength = 95, Description = "第一、二场景够用。" },
                new FishingLine { Id = "line-fluoro", Name = "透明碳线", Quality = "精良", Price = 260, MaxTension = 98, CutResistance = 0.42, MaxLength = 110, Description = "更耐磨，适合石岸和港口。" },
                new FishingLine { Id = "line-braid", Name = "四编PE线", Quality = "稀有", Price = 520, MaxTension = 112, CutResistance = 0.58, MaxLength = 130, Description = "能承受更猛烈的拉扯。" },
                new FishingLine { Id = "line-deep", Name = "深水八编线", Quality = "史诗", Price = 980, MaxTension = 130, CutResistance = 0.72, MaxLength = 155, Description = "深水巨物专用。" },
                new FishingLine { Id = "line-dragon", Name = "龙潮复合线", Quality = "传说", Price = 1800, MaxTension = 155, CutResistance = 0.86, MaxLength = 190, Description = "最强线组，切线概率很低。" }
            };
        }

        private static List<Rod> BuildRods()
        {
            string[] names = {
                "竹影新手竿", "溪石轻竿", "柳枝弹竿", "青铜短竿", "浅滩稳竿",
                "荷风细竿", "白桦远投竿", "银线控竿", "海港硬竿", "月湾幸运竿",
                "珊瑚纹竿", "冰湖长竿", "潮汐强竿", "雷声控竿", "深水寻鱼竿",
                "星辉史诗竿", "霜月巨物竿", "风暴压线竿", "幽蓝探渊竿", "赤潮王竿",
                "龙骨传说竿", "星海秘银竿", "潮核神竿", "万鳞终极竿"
            };
            string[] qualities = {
                "基础", "基础", "基础", "基础", "基础",
                "精良", "精良", "精良", "精良", "精良",
                "稀有", "稀有", "稀有", "稀有", "稀有",
                "史诗", "史诗", "史诗", "史诗", "史诗",
                "传说", "传说", "传说", "传说"
            };
            int[] prices = {
                0, 160, 360, 680, 1050,
                1550, 2200, 3100, 4300, 5900,
                7800, 10200, 13500, 17600, 22600,
                29000, 37000, 47000, 59000, 74000,
                92000, 116000, 145000, 180000
            };

            List<Rod> rods = new List<Rod>();
            for (int i = 0; i < names.Length; i++)
            {
                rods.Add(new Rod
                {
                    Id = "rod-" + (i + 1).ToString("00"),
                    Name = names[i],
                    Quality = qualities[i],
                    Price = prices[i],
                    Power = 8 + i * 4,
                    Control = 7 + i * 3,
                    Luck = 2 + i * 3,
                    Stability = 5 + i * 3
                });
            }
            return rods;
        }

        private static List<AquariumTier> BuildAquariumTiers()
        {
            return new List<AquariumTier>
            {
                new AquariumTier { Id = "aquarium-01", Name = "普通鱼缸", Capacity = 5, Price = 80, TicketEnabled = false, TicketMultiplier = 0 },
                new AquariumTier { Id = "aquarium-02", Name = "加宽鱼缸", Capacity = 15, Price = 420, TicketEnabled = false, TicketMultiplier = 0 },
                new AquariumTier { Id = "aquarium-03", Name = "景观鱼缸", Capacity = 40, Price = 1350, TicketEnabled = false, TicketMultiplier = 0 },
                new AquariumTier { Id = "aquarium-04", Name = "家庭水池", Capacity = 100, Price = 4200, TicketEnabled = false, TicketMultiplier = 0 },
                new AquariumTier { Id = "aquarium-05", Name = "观赏鱼廊", Capacity = 250, Price = 12000, TicketEnabled = true, TicketMultiplier = 1.0 },
                new AquariumTier { Id = "aquarium-06", Name = "私人水族馆", Capacity = 500, Price = 36000, TicketEnabled = true, TicketMultiplier = 1.45 },
                new AquariumTier { Id = "aquarium-07", Name = "城市水族馆", Capacity = 1000, Price = 88000, TicketEnabled = true, TicketMultiplier = 2.1 }
            };
        }
    }
}
