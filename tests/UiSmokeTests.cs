using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace FishingGame.Tests
{
    internal static class UiSmokeTests
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                Assembly app = Assembly.LoadFrom("FishingGame.exe");
                Type gameDataType = app.GetType("FishingGame.Core.GameData", true);
                Type gameRulesType = app.GetType("FishingGame.Core.GameRules", true);
                IEnumerable scenes = (IEnumerable)gameDataType.GetProperty("Scenes").GetValue(null, null);
                object fishByScene = gameDataType.GetProperty("FishByScene").GetValue(null, null);
                MethodInfo createCatchForWeight = gameRulesType.GetMethods().First(m => m.Name == "CreateCatchForWeight");
                Type waterPanelType = app.GetType("FishingGame.WinForms.WaterPanel", true);
                PropertyInfo sceneProperty = waterPanelType.GetProperty("Scene");
                PropertyInfo lastCatchProperty = waterPanelType.GetProperty("LastCatch");
                MethodInfo onPaint = waterPanelType.GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic);
                object firstDisplayFish = null;

                foreach (object scene in scenes)
                {
                    using (Control panel = (Control)Activator.CreateInstance(waterPanelType, true))
                    using (Bitmap bitmap = new Bitmap(640, 360))
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    using (PaintEventArgs paintArgs = new PaintEventArgs(graphics, new Rectangle(0, 0, bitmap.Width, bitmap.Height)))
                    {
                        panel.Size = new Size(640, 360);
                        sceneProperty.SetValue(panel, scene, null);
                        string sceneId = (string)scene.GetType().GetProperty("Id").GetValue(scene, null);
                        string sceneName = (string)scene.GetType().GetProperty("Name").GetValue(scene, null);
                        object displayFish = FirstRegularFish(fishByScene, sceneId);
                        if (firstDisplayFish == null) firstDisplayFish = displayFish;
                        double minWeight = (double)displayFish.GetType().GetProperty("MinWeight").GetValue(displayFish, null);
                        double maxWeight = (double)displayFish.GetType().GetProperty("MaxWeight").GetValue(displayFish, null);
                        object catchRecord = createCatchForWeight.Invoke(null, new object[] { displayFish, minWeight + (maxWeight - minWeight) * 0.92 });
                        lastCatchProperty.SetValue(panel, catchRecord, null);
                        onPaint.Invoke(panel, new object[] { paintArgs });
                        AssertNotRedErrorCross(bitmap, sceneName);
                    }
                }

                Type reelType = app.GetType("FishingGame.WinForms.ReelControl", true);
                using (Control reel = (Control)Activator.CreateInstance(reelType, true))
                using (Bitmap bitmap = new Bitmap(310, 300))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (PaintEventArgs paintArgs = new PaintEventArgs(graphics, new Rectangle(0, 0, bitmap.Width, bitmap.Height)))
                {
                    reel.Size = new Size(310, 300);
                    reelType.GetProperty("IsFishing").SetValue(reel, true, null);
                    reelType.GetProperty("Tension").SetValue(reel, 54.0, null);
                    reelType.GetProperty("Progress").SetValue(reel, 68.0, null);
                    reelType.GetProperty("SafeLow").SetValue(reel, 38, null);
                    reelType.GetProperty("SafeHigh").SetValue(reel, 64, null);
                    reelType.GetProperty("ActiveFish").SetValue(reel, firstDisplayFish, null);
                    MethodInfo reelPaint = reelType.GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic);
                    reelPaint.Invoke(reel, new object[] { paintArgs });
                    AssertNotRedErrorCross(bitmap, "reel control");

                    int pulls = 0;
                    int releases = 0;
                    reelType.GetEvent("PullRequested").AddEventHandler(reel, new EventHandler(delegate { pulls++; }));
                    reelType.GetEvent("ReleaseRequested").AddEventHandler(reel, new EventHandler(delegate { releases++; }));
                    MethodInfo mouseDown = reelType.GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo mouseMove = reelType.GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo mouseUp = reelType.GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
                    mouseDown.Invoke(reel, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 155, 126, 0) });
                    mouseMove.Invoke(reel, new object[] { new MouseEventArgs(MouseButtons.Left, 0, 223, 194, 0) });
                    mouseUp.Invoke(reel, new object[] { new MouseEventArgs(MouseButtons.Left, 0, 223, 194, 0) });
                    mouseDown.Invoke(reel, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 223, 194, 0) });
                    mouseMove.Invoke(reel, new object[] { new MouseEventArgs(MouseButtons.Left, 0, 155, 126, 0) });
                    mouseUp.Invoke(reel, new object[] { new MouseEventArgs(MouseButtons.Left, 0, 155, 126, 0) });
                    AssertTrue(pulls > 0, "clockwise reel movement pulls line");
                    AssertTrue(releases > 0, "counter-clockwise reel movement releases line");
                }

                Console.WriteLine("PASS: UI smoke tests");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: UI smoke tests: " + ex.GetType().Name + " " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("INNER: " + ex.InnerException.GetType().Name + " " + ex.InnerException.Message);
                }
                return 1;
            }
        }

        private static void AssertNotRedErrorCross(Bitmap bitmap, string sceneName)
        {
            int redPixels = 0;
            for (int y = 0; y < bitmap.Height; y += 8)
            {
                for (int x = 0; x < bitmap.Width; x += 8)
                {
                    Color c = bitmap.GetPixel(x, y);
                    if (c.R > 230 && c.G < 40 && c.B < 40)
                    {
                        redPixels++;
                    }
                }
            }

            AssertTrue(redPixels < 20, "scene rendered red error cross: " + sceneName);
        }

        private static object FirstRegularFish(object fishByScene, string sceneId)
        {
            object fishList = fishByScene.GetType().GetProperty("Item").GetValue(fishByScene, new object[] { sceneId });
            foreach (object fish in (IEnumerable)fishList)
            {
                bool isHidden = (bool)fish.GetType().GetProperty("IsHidden").GetValue(fish, null);
                if (!isHidden)
                {
                    return fish;
                }
            }
            throw new Exception("no regular fish for " + sceneId);
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }
}
