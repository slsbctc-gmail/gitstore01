using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FishingGame.Core;

namespace FishingGame.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal class MainForm : Form
    {
        private readonly Random _random;
        private readonly string _savePath;
        private GameState _state;
        private string _currentSceneId;

        private Label _coinLabel;
        private Label _rodLabel;
        private Label _aquariumLabel;
        private Label _statusLabel;
        private Label _sceneInfoLabel;
        private Label _catchLabel;
        private ListBox _sceneList;
        private ListBox _bagList;
        private ListBox _aquariumList;
        private ListBox _collectionList;
        private ListBox _rodList;
        private ProgressBar _tensionBar;
        private ProgressBar _progressBar;
        private Button _castButton;
        private Button _pullButton;
        private Button _releaseButton;
        private Button _signButton;
        private Button _sellButton;
        private Button _toAquariumButton;
        private Button _toBagButton;
        private Button _buyRodButton;
        private Button _equipRodButton;
        private Button _buyAquariumButton;
        private Button _ticketsButton;
        private WaterPanel _waterPanel;
        private Timer _fishingTimer;

        private FishSpecies _activeFish;
        private HiddenItem _activeItem;
        private CatchRecord _pendingCatch;
        private TensionActionProfile _actionProfile;
        private double _tension;
        private double _catchProgress;
        private int _safeLow;
        private int _safeHigh;
        private bool _isFishing;

        public MainForm()
        {
            _random = new Random();
            _savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fishing-save.json");
            _state = SaveStore.Load(_savePath);
            GameRules.NormalizeState(_state);
            _currentSceneId = _state.UnlockedSceneIds.Contains(GameData.Scenes[0].Id)
                ? _state.UnlockedSceneIds[_state.UnlockedSceneIds.Count - 1]
                : GameData.Scenes[0].Id;

            Text = "溪海钓手";
            MinimumSize = new Size(1180, 760);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            KeyPreview = true;

            BuildLayout();
            BuildFishingTimer();
            RefreshAll();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Save();
            base.OnFormClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_isFishing && e.KeyCode == Keys.Space)
            {
                PullLine();
                e.Handled = true;
            }
            if (_isFishing && (e.KeyCode == Keys.Down || e.KeyCode == Keys.S))
            {
                ReleaseLine();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
            Controls.Add(root);

            root.Controls.Add(BuildTopBar(), 0, 0);
            root.Controls.Add(BuildMainArea(), 0, 1);
            root.Controls.Add(BuildBottomTabs(), 0, 2);
        }

        private Control BuildTopBar()
        {
            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Fill;
            top.BackColor = Color.FromArgb(30, 56, 72);
            top.ColumnCount = 6;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

            _coinLabel = TopLabel();
            _rodLabel = TopLabel();
            _aquariumLabel = TopLabel();
            _signButton = new Button();
            _signButton.Text = "每日签到";
            _signButton.Dock = DockStyle.Fill;
            _signButton.Click += delegate { SignIn(); };
            _statusLabel = TopLabel();
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _ticketsButton = new Button();
            _ticketsButton.Text = "收取门票";
            _ticketsButton.Dock = DockStyle.Fill;
            _ticketsButton.Click += delegate { CollectTickets(); };

            top.Controls.Add(_coinLabel, 0, 0);
            top.Controls.Add(_rodLabel, 1, 0);
            top.Controls.Add(_aquariumLabel, 2, 0);
            top.Controls.Add(_signButton, 3, 0);
            top.Controls.Add(_statusLabel, 4, 0);
            top.Controls.Add(_ticketsButton, 5, 0);
            return top;
        }

        private Control BuildMainArea()
        {
            TableLayoutPanel main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.ColumnCount = 3;
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));

            Panel left = PaddedPanel();
            Label sceneTitle = SectionLabel("钓点场景");
            _sceneList = new ListBox();
            _sceneList.Dock = DockStyle.Fill;
            _sceneList.SelectedIndexChanged += delegate { SelectSceneFromList(); };
            left.Controls.Add(_sceneList);
            left.Controls.Add(sceneTitle);

            Panel center = PaddedPanel();
            _waterPanel = new WaterPanel();
            _waterPanel.Dock = DockStyle.Fill;
            _waterPanel.Margin = new Padding(6);
            _catchLabel = new Label();
            _catchLabel.Dock = DockStyle.Bottom;
            _catchLabel.Height = 38;
            _catchLabel.TextAlign = ContentAlignment.MiddleCenter;
            _catchLabel.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
            center.Controls.Add(_waterPanel);
            center.Controls.Add(_catchLabel);

            Panel right = PaddedPanel();
            _sceneInfoLabel = new Label();
            _sceneInfoLabel.Dock = DockStyle.Top;
            _sceneInfoLabel.Height = 120;
            _sceneInfoLabel.Padding = new Padding(4);
            _sceneInfoLabel.BackColor = Color.FromArgb(235, 244, 246);

            _tensionBar = new ProgressBar();
            _tensionBar.Dock = DockStyle.Top;
            _tensionBar.Height = 28;
            _progressBar = new ProgressBar();
            _progressBar.Dock = DockStyle.Top;
            _progressBar.Height = 28;

            _castButton = ActionButton("抛竿");
            _castButton.Click += delegate { CastLine(); };

            FlowLayoutPanel lineControls = new FlowLayoutPanel();
            lineControls.Dock = DockStyle.Top;
            lineControls.Height = 96;
            lineControls.FlowDirection = FlowDirection.LeftToRight;
            lineControls.WrapContents = false;
            lineControls.Padding = new Padding(18, 8, 8, 8);
            lineControls.BackColor = Color.FromArgb(248, 250, 249);
            _pullButton = CircleButton("拉线\nSpace", Color.FromArgb(229, 91, 73));
            _pullButton.Click += delegate { PullLine(); };
            _releaseButton = CircleButton("放线\nS/↓", Color.FromArgb(62, 139, 210));
            _releaseButton.Click += delegate { ReleaseLine(); };
            lineControls.Controls.Add(_pullButton);
            lineControls.Controls.Add(_releaseButton);

            right.Controls.Add(_castButton);
            right.Controls.Add(lineControls);
            right.Controls.Add(_progressBar);
            right.Controls.Add(_tensionBar);
            right.Controls.Add(_sceneInfoLabel);

            main.Controls.Add(left, 0, 0);
            main.Controls.Add(center, 1, 0);
            main.Controls.Add(right, 2, 0);
            return main;
        }

        private Control BuildBottomTabs()
        {
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;

            TabPage bagPage = new TabPage("背包/出售");
            _bagList = new ListBox();
            _bagList.Dock = DockStyle.Fill;
            _sellButton = ActionButton("出售背包全部鱼");
            _sellButton.Click += delegate { SellBag(); };
            _toAquariumButton = ActionButton("放入鱼缸");
            _toAquariumButton.Click += delegate { MoveSelectedBagFishToAquarium(); };
            bagPage.Controls.Add(_bagList);
            bagPage.Controls.Add(_toAquariumButton);
            bagPage.Controls.Add(_sellButton);

            TabPage aquariumPage = new TabPage("鱼缸/水族馆");
            _aquariumList = new ListBox();
            _aquariumList.Dock = DockStyle.Fill;
            _toBagButton = ActionButton("移回背包");
            _toBagButton.Click += delegate { MoveSelectedAquariumFishToBag(); };
            _buyAquariumButton = ActionButton("购买更大鱼缸");
            _buyAquariumButton.Click += delegate { BuyAquarium(); };
            aquariumPage.Controls.Add(_aquariumList);
            aquariumPage.Controls.Add(_toBagButton);
            aquariumPage.Controls.Add(_buyAquariumButton);

            TabPage collectionPage = new TabPage("图鉴");
            _collectionList = new ListBox();
            _collectionList.Dock = DockStyle.Fill;
            collectionPage.Controls.Add(_collectionList);

            TabPage rodPage = new TabPage("鱼竿商店");
            _rodList = new ListBox();
            _rodList.Dock = DockStyle.Fill;
            _buyRodButton = ActionButton("购买鱼竿");
            _buyRodButton.Click += delegate { BuySelectedRod(); };
            _equipRodButton = ActionButton("装备鱼竿");
            _equipRodButton.Click += delegate { EquipSelectedRod(); };
            rodPage.Controls.Add(_rodList);
            rodPage.Controls.Add(_equipRodButton);
            rodPage.Controls.Add(_buyRodButton);

            tabs.TabPages.Add(bagPage);
            tabs.TabPages.Add(aquariumPage);
            tabs.TabPages.Add(collectionPage);
            tabs.TabPages.Add(rodPage);
            return tabs;
        }

        private void BuildFishingTimer()
        {
            _fishingTimer = new Timer();
            _fishingTimer.Interval = 100;
            _fishingTimer.Tick += delegate { FishingTick(); };
        }

        private void RefreshAll()
        {
            GameRules.NormalizeState(_state);
            RefreshTopBar();
            RefreshSceneList();
            RefreshSceneInfo();
            RefreshBag();
            RefreshAquarium();
            RefreshCollection();
            RefreshRods();
            RefreshFishingControls();
            Save();
        }

        private void RefreshTopBar()
        {
            Rod rod = GameData.FindRod(_state.EquippedRodId);
            AquariumTier tier = GameData.AquariumTiers[_state.AquariumTierIndex];
            _coinLabel.Text = "金币 " + _state.Coins;
            _rodLabel.Text = "鱼竿 " + rod.Name + "  幸运 " + rod.Luck;
            _aquariumLabel.Text = tier.Name + " " + _state.Aquarium.Count + "/" + tier.Capacity;
            _ticketsButton.Enabled = tier.TicketEnabled && _state.Aquarium.Count > 0;
        }

        private void RefreshSceneList()
        {
            object selected = _sceneList.SelectedItem;
            _sceneList.BeginUpdate();
            _sceneList.Items.Clear();
            foreach (SceneInfo scene in GameData.Scenes)
            {
                bool unlocked = _state.UnlockedSceneIds.Contains(scene.Id);
                string text = (unlocked ? "开放 " : "锁定 ") + scene.Name + "  难度 " + scene.Difficulty;
                _sceneList.Items.Add(new DisplayItem<SceneInfo>(text, scene));
            }
            _sceneList.EndUpdate();

            for (int i = 0; i < _sceneList.Items.Count; i++)
            {
                DisplayItem<SceneInfo> item = (DisplayItem<SceneInfo>)_sceneList.Items[i];
                if (item.Value.Id == _currentSceneId)
                {
                    _sceneList.SelectedIndex = i;
                    break;
                }
            }
        }

        private void RefreshSceneInfo()
        {
            SceneInfo scene = GameData.FindScene(_currentSceneId);
            List<FishSpecies> fish = GameData.FishByScene[scene.Id];
            int regular = fish.Count(f => !f.IsHidden);
            int caughtRegular = fish.Count(f => !f.IsHidden && _state.CollectionSpeciesIds.Contains(f.Id));
            int hidden = fish.Count(f => f.IsHidden);
            int caughtHidden = fish.Count(f => f.IsHidden && _state.CollectionSpeciesIds.Contains(f.Id));
            _sceneInfoLabel.Text = scene.Name
                + Environment.NewLine + "难度：" + scene.Difficulty
                + Environment.NewLine + "非隐藏图鉴：" + caughtRegular + "/" + regular
                + Environment.NewLine + "隐藏鱼：" + caughtHidden + "/" + hidden
                + Environment.NewLine + "收集完非隐藏鱼后解锁下一场景";
            _waterPanel.Scene = scene;
            _waterPanel.Invalidate();
        }

        private void RefreshBag()
        {
            _bagList.BeginUpdate();
            _bagList.Items.Clear();
            foreach (CatchRecord item in _state.Bag)
            {
                _bagList.Items.Add(new DisplayItem<CatchRecord>(FormatCatch(item), item));
            }
            _bagList.EndUpdate();
        }

        private void RefreshAquarium()
        {
            AquariumTier tier = GameData.AquariumTiers[_state.AquariumTierIndex];
            _aquariumList.BeginUpdate();
            _aquariumList.Items.Clear();
            _aquariumList.Items.Add("容量：" + _state.Aquarium.Count + "/" + tier.Capacity + "  当前：" + tier.Name);
            foreach (CatchRecord item in _state.Aquarium)
            {
                _aquariumList.Items.Add(new DisplayItem<CatchRecord>(FormatCatch(item), item));
            }
            _aquariumList.EndUpdate();

            int next = _state.AquariumTierIndex + 1;
            if (next < GameData.AquariumTiers.Count)
            {
                AquariumTier nextTier = GameData.AquariumTiers[next];
                _buyAquariumButton.Text = "升级到" + nextTier.Name + "（" + nextTier.Price + "金币）";
                _buyAquariumButton.Enabled = true;
            }
            else
            {
                _buyAquariumButton.Text = "已是最大水族馆";
                _buyAquariumButton.Enabled = false;
            }
        }

        private void RefreshCollection()
        {
            SceneInfo scene = GameData.FindScene(_currentSceneId);
            _collectionList.BeginUpdate();
            _collectionList.Items.Clear();
            foreach (FishSpecies fish in GameData.FishByScene[scene.Id].OrderBy(f => f.IsHidden).ThenBy(f => f.Rarity).ThenBy(f => f.Name))
            {
                bool caught = _state.CollectionSpeciesIds.Contains(fish.Id);
                string name = fish.IsHidden && !caught ? "未知隐藏鱼" : fish.Name;
                string marker = caught ? "√ " : "· ";
                string hidden = fish.IsHidden ? "【隐藏】" : "";
                string icon = fish.IsHidden ? "秘" : fish.IconSymbol;
                _collectionList.Items.Add(marker + icon + " " + name + hidden + "  " + fish.Rarity + "  " + fish.MinWeight + "-" + fish.MaxWeight + "kg");
            }
            _collectionList.Items.Add("── 隐藏物品 ──");
            foreach (HiddenItem item in GameData.HiddenItemsByScene[scene.Id])
            {
                bool caught = _state.ItemCollectionIds.Contains(item.Id);
                string marker = caught ? "√ " : "· ";
                string name = caught ? item.Name : "未知隐藏物品";
                _collectionList.Items.Add(marker + item.IconSymbol + " " + name + "  价值约" + item.BasePrice);
            }
            _collectionList.EndUpdate();
        }

        private void RefreshRods()
        {
            _rodList.BeginUpdate();
            _rodList.Items.Clear();
            foreach (Rod rod in GameData.Rods)
            {
                bool owned = _state.OwnedRodIds.Contains(rod.Id);
                bool equipped = _state.EquippedRodId == rod.Id;
                string text = (equipped ? "已装备 " : owned ? "已拥有 " : "可购买 ")
                    + rod.Name + " [" + rod.Quality + "]  价" + rod.Price
                    + "  力" + rod.Power + " 控" + rod.Control + " 运" + rod.Luck;
                _rodList.Items.Add(new DisplayItem<Rod>(text, rod));
            }
            _rodList.EndUpdate();
        }

        private void RefreshFishingControls()
        {
            _pullButton.Enabled = _isFishing;
            _releaseButton.Enabled = _isFishing;
            _castButton.Enabled = !_isFishing;
            _tensionBar.Value = ClampToProgress(_tension);
            _progressBar.Value = ClampToProgress(_catchProgress);
            _waterPanel.Tension = _tension;
            _waterPanel.Progress = _catchProgress;
            _waterPanel.SafeLow = _safeLow;
            _waterPanel.SafeHigh = _safeHigh;
            _waterPanel.ActiveFish = _activeFish;
            _waterPanel.IsFishing = _isFishing;
            _waterPanel.Invalidate();
        }

        private void SelectSceneFromList()
        {
            if (_sceneList.SelectedItem == null) return;
            DisplayItem<SceneInfo> item = _sceneList.SelectedItem as DisplayItem<SceneInfo>;
            if (item == null) return;
            if (!_state.UnlockedSceneIds.Contains(item.Value.Id))
            {
                SetStatus("这个钓点还没解锁。完成前一个场景的非隐藏鱼图鉴即可开放。");
                RefreshSceneList();
                return;
            }
            _currentSceneId = item.Value.Id;
            SetStatus("来到 " + item.Value.Name + "。");
            RefreshSceneInfo();
            RefreshCollection();
            _waterPanel.Scene = item.Value;
        }

        private void CastLine()
        {
            if (_isFishing) return;
            if (!_state.UnlockedSceneIds.Contains(_currentSceneId))
            {
                SetStatus("这个场景还没有解锁。");
                return;
            }

            Rod rod = GameData.FindRod(_state.EquippedRodId);
            _activeItem = GameRules.ChooseHiddenItem(_state, _currentSceneId, _random);
            if (_activeItem != null)
            {
                CatchRecord itemCatch = GameRules.CreateItemCatch(_activeItem);
                int bonus = GameRules.RegisterCatch(_state, itemCatch);
                _catchLabel.Text = "钩到了隐藏物品：" + itemCatch.IconSymbol + " " + itemCatch.SpeciesName;
                SetStatus("隐藏物品进入背包，可出售获得 " + itemCatch.SellPrice + " 金币。首次发现奖励 " + bonus + " 金币。");
                _activeItem = null;
                RefreshAll();
                return;
            }

            _activeFish = GameRules.ChooseFish(_state, _currentSceneId, _random);
            _pendingCatch = GameRules.CreateCatch(_activeFish, _random);
            _actionProfile = GameRules.CalculateActionProfile(_activeFish, rod);
            TensionWindow window = GameRules.CalculateTensionWindow(_activeFish, rod);
            _tension = 50;
            _catchProgress = 0;
            _safeLow = window.Low;
            _safeHigh = window.High;
            _isFishing = true;
            _catchLabel.Text = "鱼咬钩了！保持张力在 " + _safeLow + "-" + _safeHigh + "。";
            SetStatus("正在钓鱼：Space 拉线，S 或 ↓ 放线。");
            _fishingTimer.Start();
            RefreshFishingControls();
        }

        private void FishingTick()
        {
            if (!_isFishing || _activeFish == null) return;
            Rod rod = GameData.FindRod(_state.EquippedRodId);
            if (_actionProfile == null)
            {
                _actionProfile = GameRules.CalculateActionProfile(_activeFish, rod);
            }
            double fishPull = _activeFish.RunStrength / 15.0;
            double control = rod.Control / 95.0;
            double drift = (_random.NextDouble() - 0.43) * _actionProfile.DriftAmount + fishPull - control;
            _tension += drift;

            if (_tension >= _safeLow && _tension <= _safeHigh)
            {
                _catchProgress += 1.8 + rod.Power / 42.0 + Math.Max(0, _safeHigh - _safeLow) / 55.0;
            }
            else
            {
                _catchProgress -= 2.4 + _activeFish.TensionVolatility / 9.0;
            }

            if (_catchProgress < 0) _catchProgress = 0;
            if (_tension <= 2 || _tension >= 98)
            {
                FinishFishing(false);
                return;
            }
            if (_catchProgress >= 100)
            {
                FinishFishing(true);
                return;
            }
            RefreshFishingControls();
        }

        private void PullLine()
        {
            if (!_isFishing || _activeFish == null) return;
            Rod rod = GameData.FindRod(_state.EquippedRodId);
            TensionActionProfile profile = _actionProfile ?? GameRules.CalculateActionProfile(_activeFish, rod);
            AdjustTension(profile.PullAmount);
        }

        private void ReleaseLine()
        {
            if (!_isFishing || _activeFish == null) return;
            Rod rod = GameData.FindRod(_state.EquippedRodId);
            TensionActionProfile profile = _actionProfile ?? GameRules.CalculateActionProfile(_activeFish, rod);
            AdjustTension(-profile.ReleaseAmount);
        }

        private void AdjustTension(double amount)
        {
            if (!_isFishing) return;
            _tension += amount;
            if (_tension < 0) _tension = 0;
            if (_tension > 100) _tension = 100;
            RefreshFishingControls();
        }

        private void FinishFishing(bool success)
        {
            _fishingTimer.Stop();
            _isFishing = false;
            if (success)
            {
                int bonus = GameRules.RegisterCatch(_state, _pendingCatch);
                string hidden = _pendingCatch.IsHidden ? "隐藏鱼！" : "";
                if (!_pendingCatch.IsSellable)
                {
                    _catchLabel.Text = "钓到了偏小的 " + _pendingCatch.SpeciesName + "，已放生。";
                    SetStatus("小鱼不能售卖，已直接放掉。首次图鉴奖励 " + bonus + " 金币。");
                }
                else
                {
                    _catchLabel.Text = "钓到了 " + _pendingCatch.WeightGrade + " " + _pendingCatch.SpeciesName + " " + hidden + " 重 " + _pendingCatch.Weight + "kg";
                    SetStatus("鱼进入背包，售价 " + _pendingCatch.SellPrice + " 金币。首次图鉴奖励 " + bonus + " 金币。");
                }
                Save();
            }
            else
            {
                _catchLabel.Text = "鱼逃脱了。";
                SetStatus("张力失控，鱼跑掉了。换更高控制的鱼竿会轻松些。");
            }
            _activeFish = null;
            _activeItem = null;
            _pendingCatch = null;
            _actionProfile = null;
            _tension = 0;
            _catchProgress = 0;
            RefreshAll();
        }

        private void SignIn()
        {
            int reward = GameRules.SignIn(_state, DateTime.Today);
            SetStatus(reward > 0 ? "签到获得 " + reward + " 金币。" : "今天已经签到过了。");
            RefreshAll();
        }

        private void SellBag()
        {
            int total = GameRules.SellAllBag(_state);
            SetStatus(total > 0 ? "出售背包鱼获得 " + total + " 金币。" : "背包里没有鱼可出售。");
            RefreshAll();
        }

        private void MoveSelectedBagFishToAquarium()
        {
            DisplayItem<CatchRecord> item = _bagList.SelectedItem as DisplayItem<CatchRecord>;
            if (item == null)
            {
                SetStatus("先在背包中选择一条鱼。");
                return;
            }
            bool moved = GameRules.TryMoveCatchToAquarium(_state, item.Value.Id);
            SetStatus(moved ? "已放入鱼缸：" + item.Value.SpeciesName : "鱼缸容量不足，需要升级。");
            RefreshAll();
        }

        private void MoveSelectedAquariumFishToBag()
        {
            DisplayItem<CatchRecord> item = _aquariumList.SelectedItem as DisplayItem<CatchRecord>;
            if (item == null)
            {
                SetStatus("先在鱼缸中选择一条鱼。");
                return;
            }
            bool moved = GameRules.TryMoveCatchToBag(_state, item.Value.Id);
            SetStatus(moved ? "已移回背包：" + item.Value.SpeciesName : "无法移动这条鱼。");
            RefreshAll();
        }

        private void BuySelectedRod()
        {
            DisplayItem<Rod> item = _rodList.SelectedItem as DisplayItem<Rod>;
            if (item == null)
            {
                SetStatus("先选择一根鱼竿。");
                return;
            }
            bool bought = GameRules.BuyRod(_state, item.Value.Id);
            SetStatus(bought ? "已拥有鱼竿：" + item.Value.Name : "金币不足，买不起这根鱼竿。");
            RefreshAll();
        }

        private void EquipSelectedRod()
        {
            DisplayItem<Rod> item = _rodList.SelectedItem as DisplayItem<Rod>;
            if (item == null)
            {
                SetStatus("先选择一根鱼竿。");
                return;
            }
            bool equipped = GameRules.EquipRod(_state, item.Value.Id);
            SetStatus(equipped ? "已装备：" + item.Value.Name : "还没有购买这根鱼竿。");
            RefreshAll();
        }

        private void BuyAquarium()
        {
            int next = _state.AquariumTierIndex + 1;
            if (next >= GameData.AquariumTiers.Count)
            {
                SetStatus("已经是最大水族馆。");
                return;
            }
            string name = GameData.AquariumTiers[next].Name;
            bool bought = GameRules.BuyNextAquarium(_state);
            SetStatus(bought ? "鱼缸升级为：" + name : "金币不足，暂时买不起 " + name + "。");
            RefreshAll();
        }

        private void CollectTickets()
        {
            int income = GameRules.CollectTicketIncome(_state, DateTime.Today);
            SetStatus(income > 0 ? "收取门票获得 " + income + " 金币。" : "今天没有可收取的门票。");
            RefreshAll();
        }

        private void Save()
        {
            SaveStore.Save(_savePath, _state);
        }

        private void SetStatus(string text)
        {
            _statusLabel.Text = text;
        }

        private string FormatCatch(CatchRecord item)
        {
            if (!item.IsFish)
            {
                return item.IconSymbol + " " + item.SpeciesName + "  隐藏物品  售价" + item.SellPrice;
            }
            string hidden = item.IsHidden ? "【隐藏】" : "";
            return item.IconSymbol + " " + item.SpeciesName + hidden + "  " + item.Rarity + "  " + item.WeightGrade + "  " + item.Weight + "kg  售价" + item.SellPrice;
        }

        private static int ClampToProgress(double value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return (int)Math.Round(value);
        }

        private Label TopLabel()
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.ForeColor = Color.White;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
            return label;
        }

        private Panel PaddedPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(8);
            panel.BackColor = Color.FromArgb(248, 250, 249);
            return panel;
        }

        private Label SectionLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Top;
            label.Height = 28;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
            return label;
        }

        private Button ActionButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Bottom;
            button.Height = 36;
            button.Margin = new Padding(4);
            return button;
        }

        private Button CircleButton(string text, Color color)
        {
            RoundButton button = new RoundButton();
            button.Text = text;
            button.CircleColor = color;
            button.ForeColor = Color.White;
            button.Width = 78;
            button.Height = 78;
            button.Margin = new Padding(8, 0, 8, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            return button;
        }
    }

    internal class DisplayItem<T>
    {
        public string Text { get; private set; }
        public T Value { get; private set; }

        public DisplayItem(string text, T value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    internal class RoundButton : Button
    {
        public Color CircleColor { get; set; }

        public RoundButton()
        {
            CircleColor = Color.FromArgb(70, 130, 180);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(2, 2, Width - 4, Height - 4);
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(3, 3, Width - 6, Height - 6);
            Color fill = Enabled ? CircleColor : Color.FromArgb(150, 150, 150);
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, ControlPaint.Light(fill), ControlPaint.Dark(fill), LinearGradientMode.ForwardDiagonal))
            using (Pen pen = new Pen(Color.FromArgb(90, Color.White), 2F))
            using (Brush text = new SolidBrush(ForeColor))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.FillEllipse(brush, rect);
                g.DrawEllipse(pen, rect);
                g.DrawString(Text, Font, text, rect, format);
            }
        }
    }

    internal class WaterPanel : Panel
    {
        public SceneInfo Scene { get; set; }
        public FishSpecies ActiveFish { get; set; }
        public bool IsFishing { get; set; }
        public double Tension { get; set; }
        public double Progress { get; set; }
        public int SafeLow { get; set; }
        public int SafeHigh { get; set; }

        public WaterPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(77, 148, 180);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            Color top = Scene == null ? Color.FromArgb(147, 213, 255) : ParseColor(Scene.PaletteTop);
            Color bottom = Scene == null ? Color.FromArgb(47, 155, 216) : ParseColor(Scene.PaletteBottom);
            using (LinearGradientBrush brush = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, bounds);
            }

            DrawScenery(g, bounds);
            DrawWaves(g, bounds);
            DrawFishShadows(g, bounds);
            DrawDock(g, bounds);
            DrawFishingState(g, bounds);
        }

        private void DrawScenery(Graphics g, Rectangle bounds)
        {
            int difficulty = Scene == null ? 1 : Scene.Difficulty;
            DrawSkyLight(g, bounds, difficulty);

            if (difficulty <= 3)
            {
                DrawHills(g, bounds, Color.FromArgb(120, 67, 142, 95), Color.FromArgb(150, 39, 111, 76));
                DrawReeds(g, bounds, difficulty == 2 ? 30 : 12);
            }
            else if (difficulty <= 5)
            {
                DrawHills(g, bounds, Color.FromArgb(120, 95, 115, 126), Color.FromArgb(150, 74, 93, 96));
                DrawMist(g, bounds);
                DrawReeds(g, bounds, 34);
            }
            else if (difficulty == 6)
            {
                DrawPier(g, bounds);
                DrawBoats(g, bounds);
            }
            else if (difficulty == 7)
            {
                DrawMoon(g, bounds);
                DrawHills(g, bounds, Color.FromArgb(130, 28, 40, 98), Color.FromArgb(160, 22, 31, 73));
            }
            else if (difficulty == 8)
            {
                DrawCoral(g, bounds);
            }
            else if (difficulty == 9)
            {
                DrawIce(g, bounds);
            }
            else if (difficulty == 10)
            {
                DrawStorm(g, bounds);
            }
            else if (difficulty == 11)
            {
                DrawAbyss(g, bounds);
            }
            else
            {
                DrawDragonTide(g, bounds);
            }
        }

        private void DrawSkyLight(Graphics g, Rectangle bounds, int difficulty)
        {
            if (difficulty == 7 || difficulty >= 10)
            {
                using (Brush star = new SolidBrush(Color.FromArgb(160, Color.White)))
                {
                    for (int i = 0; i < 24; i++)
                    {
                        int x = 28 + (i * 83) % Math.Max(80, bounds.Width - 56);
                        int y = 22 + (i * 47) % Math.Max(70, bounds.Height / 2);
                        g.FillEllipse(star, x, y, 2, 2);
                    }
                }
                return;
            }

            using (GraphicsPath path = new GraphicsPath())
            using (PathGradientBrush glow = new PathGradientBrush(path))
            {
                path.AddEllipse(bounds.Width - 150, 28, 96, 96);
                glow.CenterColor = Color.FromArgb(210, 255, 232, 138);
                glow.SurroundColors = new[] { Color.FromArgb(0, 255, 232, 138) };
                g.FillPath(glow, path);
            }
        }

        private void DrawHills(Graphics g, Rectangle bounds, Color back, Color front)
        {
            using (Brush b1 = new SolidBrush(back))
            using (Brush b2 = new SolidBrush(front))
            {
                Point[] far = {
                    new Point(0, bounds.Height / 3),
                    new Point(bounds.Width / 5, bounds.Height / 5),
                    new Point(bounds.Width / 2, bounds.Height / 3),
                    new Point(bounds.Width * 3 / 4, bounds.Height / 6),
                    new Point(bounds.Width, bounds.Height / 3),
                    new Point(bounds.Width, bounds.Height),
                    new Point(0, bounds.Height)
                };
                Point[] near = {
                    new Point(0, bounds.Height / 2),
                    new Point(bounds.Width / 4, bounds.Height / 3),
                    new Point(bounds.Width / 2, bounds.Height / 2),
                    new Point(bounds.Width * 4 / 5, bounds.Height / 3),
                    new Point(bounds.Width, bounds.Height / 2),
                    new Point(bounds.Width, bounds.Height),
                    new Point(0, bounds.Height)
                };
                g.FillPolygon(b1, far);
                g.FillPolygon(b2, near);
            }
        }

        private void DrawReeds(Graphics g, Rectangle bounds, int count)
        {
            using (Pen reed = new Pen(Color.FromArgb(160, 64, 94, 45), 3F))
            using (Brush tip = new SolidBrush(Color.FromArgb(180, 116, 82, 42)))
            {
                for (int i = 0; i < count; i++)
                {
                    int x = 10 + (i * 37) % Math.Max(40, bounds.Width - 20);
                    int y = bounds.Height - 78 - (i % 5) * 7;
                    g.DrawLine(reed, x, bounds.Height - 38, x + (i % 3 - 1) * 8, y);
                    g.FillEllipse(tip, x - 4, y - 8, 8, 16);
                }
            }
        }

        private void DrawMist(Graphics g, Rectangle bounds)
        {
            using (Brush mist = new SolidBrush(Color.FromArgb(75, Color.White)))
            {
                for (int i = 0; i < 5; i++)
                {
                    g.FillEllipse(mist, -80 + i * 180, 70 + i % 2 * 34, 240, 42);
                }
            }
        }

        private void DrawPier(Graphics g, Rectangle bounds)
        {
            using (Brush wood = new SolidBrush(Color.FromArgb(170, 125, 85, 49)))
            using (Pen edge = new Pen(Color.FromArgb(180, 82, 50, 31), 4F))
            {
                Point[] pier = {
                    new Point(bounds.Width / 2 - 90, bounds.Height / 2),
                    new Point(bounds.Width / 2 + 90, bounds.Height / 2),
                    new Point(bounds.Width / 2 + 190, bounds.Height - 58),
                    new Point(bounds.Width / 2 - 190, bounds.Height - 58)
                };
                g.FillPolygon(wood, pier);
                g.DrawPolygon(edge, pier);
            }
        }

        private void DrawBoats(Graphics g, Rectangle bounds)
        {
            using (Brush hull = new SolidBrush(Color.FromArgb(170, 70, 48, 38)))
            using (Brush sail = new SolidBrush(Color.FromArgb(200, 246, 242, 220)))
            {
                g.FillPie(hull, 58, bounds.Height / 2 + 24, 110, 44, 0, 180);
                Point[] sailShape = { new Point(112, bounds.Height / 2 - 24), new Point(112, bounds.Height / 2 + 28), new Point(154, bounds.Height / 2 + 18) };
                g.FillPolygon(sail, sailShape);
            }
        }

        private void DrawMoon(Graphics g, Rectangle bounds)
        {
            using (Brush moon = new SolidBrush(Color.FromArgb(230, 246, 240, 190)))
            using (Brush cut = new SolidBrush(Color.FromArgb(98, 116, 176)))
            {
                g.FillEllipse(moon, bounds.Width - 140, 42, 70, 70);
                g.FillEllipse(cut, bounds.Width - 118, 34, 70, 70);
            }
        }

        private void DrawCoral(Graphics g, Rectangle bounds)
        {
            Color[] colors = { Color.Coral, Color.DeepPink, Color.Gold, Color.MediumPurple, Color.LightSeaGreen };
            for (int i = 0; i < 16; i++)
            {
                using (Pen pen = new Pen(Color.FromArgb(185, colors[i % colors.Length]), 5F))
                {
                    int x = 24 + i * bounds.Width / 16;
                    int y = bounds.Height - 78;
                    g.DrawLine(pen, x, y, x + (i % 3 - 1) * 12, y - 36 - i % 4 * 8);
                    g.DrawLine(pen, x, y - 18, x + 18, y - 42);
                    g.DrawLine(pen, x, y - 16, x - 16, y - 38);
                }
            }
        }

        private void DrawIce(Graphics g, Rectangle bounds)
        {
            using (Brush ice = new SolidBrush(Color.FromArgb(150, 230, 250, 255)))
            using (Pen edge = new Pen(Color.FromArgb(180, 172, 218, 239), 2F))
            {
                for (int i = 0; i < 7; i++)
                {
                    Point[] floe = {
                        new Point(30 + i * 120, bounds.Height - 126),
                        new Point(86 + i * 120, bounds.Height - 146),
                        new Point(134 + i * 120, bounds.Height - 118),
                        new Point(98 + i * 120, bounds.Height - 96),
                        new Point(44 + i * 120, bounds.Height - 102)
                    };
                    g.FillPolygon(ice, floe);
                    g.DrawPolygon(edge, floe);
                }
            }
        }

        private void DrawStorm(Graphics g, Rectangle bounds)
        {
            using (Pen rain = new Pen(Color.FromArgb(120, Color.White), 2F))
            using (Pen lightning = new Pen(Color.FromArgb(230, 255, 232, 80), 4F))
            {
                for (int i = 0; i < 42; i++)
                {
                    int x = (i * 47) % Math.Max(80, bounds.Width);
                    int y = 20 + (i * 29) % Math.Max(100, bounds.Height - 100);
                    g.DrawLine(rain, x, y, x - 18, y + 34);
                }
                Point[] bolt = {
                    new Point(bounds.Width / 2 + 80, 40),
                    new Point(bounds.Width / 2 + 42, 108),
                    new Point(bounds.Width / 2 + 74, 108),
                    new Point(bounds.Width / 2 + 28, 188)
                };
                g.DrawLines(lightning, bolt);
            }
        }

        private void DrawAbyss(Graphics g, Rectangle bounds)
        {
            using (Brush glow = new SolidBrush(Color.FromArgb(110, 60, 220, 210)))
            using (Pen vent = new Pen(Color.FromArgb(130, 200, 240, 255), 3F))
            {
                for (int i = 0; i < 9; i++)
                {
                    int x = 42 + i * bounds.Width / 9;
                    int y = bounds.Height - 105 - (i % 3) * 22;
                    g.FillEllipse(glow, x, y, 20, 20);
                    g.DrawLine(vent, x + 10, y + 14, x + 10, y - 30);
                }
            }
        }

        private void DrawDragonTide(Graphics g, Rectangle bounds)
        {
            using (Pen dragon = new Pen(Color.FromArgb(170, 255, 205, 92), 8F))
            using (Brush eye = new SolidBrush(Color.FromArgb(230, 255, 80, 60)))
            {
                Point[] body = new Point[7];
                for (int i = 0; i < body.Length; i++)
                {
                    body[i] = new Point(70 + i * bounds.Width / 8, 130 + (i % 2 == 0 ? 28 : -22));
                }
                g.DrawCurve(dragon, body);
                g.FillEllipse(eye, body[body.Length - 1].X + 18, body[body.Length - 1].Y - 12, 10, 10);
            }
        }

        private void DrawWaves(Graphics g, Rectangle bounds)
        {
            using (Pen pen = new Pen(Color.FromArgb(90, Color.White), 2F))
            {
                for (int y = 42; y < bounds.Height - 30; y += 44)
                {
                    Point[] points = new Point[9];
                    for (int i = 0; i < points.Length; i++)
                    {
                        int x = i * bounds.Width / (points.Length - 1);
                        int wave = (i % 2 == 0) ? 6 : -6;
                        points[i] = new Point(x, y + wave);
                    }
                    g.DrawCurve(pen, points);
                }
            }
        }

        private void DrawFishShadows(Graphics g, Rectangle bounds)
        {
            using (Brush shadow = new SolidBrush(Color.FromArgb(55, 20, 48, 58)))
            {
                for (int i = 0; i < 8; i++)
                {
                    int x = 50 + (i * 97) % Math.Max(100, bounds.Width - 120);
                    int y = 80 + (i * 53) % Math.Max(100, bounds.Height - 160);
                    int w = 42 + (i % 3) * 16;
                    g.FillEllipse(shadow, x, y, w, 14);
                    Point[] tail = {
                        new Point(x + w - 4, y + 7),
                        new Point(x + w + 14, y - 2),
                        new Point(x + w + 14, y + 16)
                    };
                    g.FillPolygon(shadow, tail);
                }
            }
        }

        private void DrawDock(Graphics g, Rectangle bounds)
        {
            using (Brush dock = new SolidBrush(Color.FromArgb(160, 105, 73, 44)))
            using (Pen line = new Pen(Color.FromArgb(130, 70, 45, 25), 2F))
            {
                Rectangle dockRect = new Rectangle(0, bounds.Height - 58, bounds.Width, 58);
                g.FillRectangle(dock, dockRect);
                for (int x = 0; x < bounds.Width; x += 46)
                {
                    g.DrawLine(line, x, bounds.Height - 58, x + 18, bounds.Height);
                }
            }
        }

        private void DrawFishingState(Graphics g, Rectangle bounds)
        {
            string sceneName = Scene == null ? "水域" : Scene.Name;
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Font title = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold))
            using (Font small = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular))
            {
                g.DrawString(sceneName, title, textBrush, 24, 20);
                string line = IsFishing && ActiveFish != null
                    ? "目标鱼影：" + (ActiveFish.IsHidden ? "异常稀有" : ActiveFish.Rarity) + "  张力安全区 " + SafeLow + "-" + SafeHigh
                    : "选择钓点后抛竿，等待水面动静。";
                g.DrawString(line, small, textBrush, 28, 58);
            }

            if (IsFishing)
            {
                Rectangle meter = new Rectangle(40, bounds.Height - 112, bounds.Width - 80, 20);
                using (Brush back = new SolidBrush(Color.FromArgb(160, 255, 255, 255)))
                using (Brush safe = new SolidBrush(Color.FromArgb(180, 82, 190, 121)))
                using (Brush pointer = new SolidBrush(Color.FromArgb(230, 255, 210, 76)))
                {
                    g.FillRectangle(back, meter);
                    int safeX = meter.Left + SafeLow * meter.Width / 100;
                    int safeW = Math.Max(4, (SafeHigh - SafeLow) * meter.Width / 100);
                    g.FillRectangle(safe, safeX, meter.Top, safeW, meter.Height);
                    int pointerX = meter.Left + (int)(Tension * meter.Width / 100.0);
                    g.FillRectangle(pointer, pointerX - 3, meter.Top - 6, 6, meter.Height + 12);
                }
            }
        }

        private static Color ParseColor(string hex)
        {
            try
            {
                return ColorTranslator.FromHtml(hex);
            }
            catch
            {
                return Color.FromArgb(77, 148, 180);
            }
        }
    }
}
