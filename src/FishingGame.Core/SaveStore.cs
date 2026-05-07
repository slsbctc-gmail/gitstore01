using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace FishingGame.Core
{
    public static class SaveStore
    {
        public static GameState Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return GameRules.CreateNewGame();
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                GameState state = serializer.Deserialize<GameState>(json);
                if (state == null)
                {
                    return GameRules.CreateNewGame();
                }

                GameRules.NormalizeState(state);
                GameRules.RefreshSceneUnlocks(state);
                return state;
            }
            catch
            {
                return GameRules.CreateNewGame();
            }
        }

        public static void Save(string path, GameState state)
        {
            GameRules.NormalizeState(state);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(state);
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }
}

