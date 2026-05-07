using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using FishingGame.Core;

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
                Type waterPanelType = app.GetType("FishingGame.WinForms.WaterPanel", true);
                PropertyInfo sceneProperty = waterPanelType.GetProperty("Scene");
                MethodInfo onPaint = waterPanelType.GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic);

                foreach (SceneInfo scene in GameData.Scenes)
                {
                    using (Control panel = (Control)Activator.CreateInstance(waterPanelType, true))
                    using (Bitmap bitmap = new Bitmap(640, 360))
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    using (PaintEventArgs paintArgs = new PaintEventArgs(graphics, new Rectangle(0, 0, bitmap.Width, bitmap.Height)))
                    {
                        panel.Size = new Size(640, 360);
                        sceneProperty.SetValue(panel, scene, null);
                        onPaint.Invoke(panel, new object[] { paintArgs });
                        AssertNotRedErrorCross(bitmap, scene.Name);
                    }
                }

                Type roundButtonType = app.GetType("FishingGame.WinForms.RoundButton", true);
                PropertyInfo symbolProperty = roundButtonType.GetProperty("Symbol");
                using (Button button = (Button)Activator.CreateInstance(roundButtonType, true))
                {
                    symbolProperty.SetValue(button, "↑", null);
                    button.Text = "拉线";
                    button.Size = new Size(78, 78);
                    AssertTrue((string)symbolProperty.GetValue(button, null) == "↑", "round button keeps visible symbol");
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

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }
    }
}
