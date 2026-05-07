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
        public static List<Rod> Rods { get; private set; }
        public static List<AquariumTier> AquariumTiers { get; private set; }

        static GameData()
        {
            Scenes = BuildScenes();
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
