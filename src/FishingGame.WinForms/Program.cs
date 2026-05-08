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
        private Button _castButton;
        private Button _signButton;
        private Button _sellButton;
        private Button _toAquariumButton;
        private Button _toBagButton;
        private Button _buyRodButton;
        private Button _equipRodButton;
        private Button _buyAquariumButton;
        private Button _ticketsButton;
        private WaterPanel _waterPanel;
        private ReelControl _reelControl;
        private Timer _fishingTimer;

        private FishSpecies _activeFish;
        private HiddenItem _activeItem;
        private CatchRecord _pendingCatch;
        private CatchRecord _lastCatch;
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

            _castButton = ActionButton("抛竿");
            _castButton.Click += delegate { CastLine(); };

            _reelControl = new ReelControl();
            _reelControl.Dock = DockStyle.Top;
            _reelControl.Height = 300;
            _reelControl.PullRequested += delegate { PullLineFromReel(); };
            _reelControl.ReleaseRequested += delegate { ReleaseLineFromReel(); };

            right.Controls.Add(_castButton);
            right.Controls.Add(_reelControl);
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
            _castButton.Enabled = !_isFishing;
            _reelControl.IsFishing = _isFishing;
            _reelControl.Tension = _tension;
            _reelControl.Progress = _catchProgress;
            _reelControl.SafeLow = _safeLow;
            _reelControl.SafeHigh = _safeHigh;
            _reelControl.ActiveFish = _activeFish;
            _reelControl.Invalidate();
            _waterPanel.Tension = _tension;
            _waterPanel.Progress = _catchProgress;
            _waterPanel.SafeLow = _safeLow;
            _waterPanel.SafeHigh = _safeHigh;
            _waterPanel.ActiveFish = _activeFish;
            _waterPanel.LastCatch = _lastCatch;
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
                _lastCatch = itemCatch;
                _catchLabel.Text = "钩到了隐藏物品：" + itemCatch.IconSymbol + " " + itemCatch.SpeciesName;
                SetStatus("隐藏物品进入背包，可出售获得 " + itemCatch.SellPrice + " 金币。首次发现奖励 " + bonus + " 金币。");
                _activeItem = null;
                RefreshAll();
                return;
            }

            _activeFish = GameRules.ChooseFish(_state, _currentSceneId, _random);
            _pendingCatch = GameRules.CreateCatch(_activeFish, _random);
            _lastCatch = null;
            _actionProfile = GameRules.CalculateActionProfile(_activeFish, rod);
            TensionWindow window = GameRules.CalculateTensionWindow(_activeFish, rod);
            _tension = 50;
            _catchProgress = 0;
            _safeLow = window.Low;
            _safeHigh = window.High;
            _isFishing = true;
            _catchLabel.Text = "鱼咬钩了！保持张力在 " + _safeLow + "-" + _safeHigh + "。";
            SetStatus("正在钓鱼：顺时针转动卷线器拉线，逆时针放线。Space / S 也可操作。");
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
            ApplyLineAction(true, true);
        }

        private void ReleaseLine()
        {
            ApplyLineAction(false, true);
        }

        private void PullLineFromReel()
        {
            ApplyLineAction(true, false);
        }

        private void ReleaseLineFromReel()
        {
            ApplyLineAction(false, false);
        }

        private void ApplyLineAction(bool pull, bool spinReel)
        {
            if (!_isFishing || _activeFish == null) return;
            Rod rod = GameData.FindRod(_state.EquippedRodId);
            TensionActionProfile profile = _actionProfile ?? GameRules.CalculateActionProfile(_activeFish, rod);
            AdjustTension(pull ? profile.PullAmount : -profile.ReleaseAmount);
            if (spinReel)
            {
                _reelControl.Spin(pull ? 18 : -18);
            }
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
                _lastCatch = _pendingCatch;
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

    internal class ReelControl : Control
    {
        private bool _dragging;
        private double _lastAngle;
        private double _dragAccumulator;
        private double _reelAngle;

        public event EventHandler PullRequested;
        public event EventHandler ReleaseRequested;
        public bool IsFishing { get; set; }
        public double Tension { get; set; }
        public double Progress { get; set; }
        public int SafeLow { get; set; }
        public int SafeHigh { get; set; }
        public FishSpecies ActiveFish { get; set; }

        public ReelControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(245, 248, 247);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        }

        public void Spin(double degrees)
        {
            _reelAngle += degrees;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (!IsFishing || !PointInsideReel(e.Location)) return;
            _dragging = true;
            Capture = true;
            _lastAngle = AngleFromReelCenter(e.Location);
            _dragAccumulator = 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging || !IsFishing) return;
            double angle = AngleFromReelCenter(e.Location);
            double delta = NormalizeRadians(angle - _lastAngle);
            _lastAngle = angle;
            if (Math.Abs(delta) < 0.015) return;

            _reelAngle += delta * 180.0 / Math.PI;
            _dragAccumulator += delta;
            while (_dragAccumulator >= 0.18)
            {
                OnPullRequested();
                _dragAccumulator -= 0.18;
            }
            while (_dragAccumulator <= -0.18)
            {
                OnReleaseRequested();
                _dragAccumulator += 0.18;
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            Capture = false;
            _dragAccumulator = 0;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!IsFishing) return;
            if (e.Delta > 0)
            {
                Spin(16);
                OnPullRequested();
            }
            else if (e.Delta < 0)
            {
                Spin(-16);
                OnReleaseRequested();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            using (LinearGradientBrush back = new LinearGradientBrush(bounds, Color.FromArgb(252, 254, 253), Color.FromArgb(226, 234, 233), LinearGradientMode.Vertical))
            using (Pen edge = new Pen(Color.FromArgb(210, 217, 216)))
            {
                g.FillRectangle(back, bounds);
                g.DrawRectangle(edge, 0, 0, bounds.Width - 1, bounds.Height - 1);
            }

            DrawGauge(g, bounds);
            DrawReel(g, bounds);
        }

        private void DrawGauge(Graphics g, Rectangle bounds)
        {
            float radius = Math.Min(bounds.Width - 52, 230) / 2F;
            radius = Math.Max(70F, radius);
            PointF center = new PointF(bounds.Width / 2F, 126F);
            int low = SafeHigh > SafeLow ? SafeLow : 36;
            int high = SafeHigh > SafeLow ? SafeHigh : 64;
            Color inactive = Color.FromArgb(135, 155, 162);
            Color gaugeBack = IsFishing ? Color.FromArgb(210, 201, 82, 72) : inactive;
            Color safeColor = IsFishing ? Color.FromArgb(225, 50, 160, 105) : Color.FromArgb(150, 170, 180, 176);
            Color needleColor = IsFishing ? Color.FromArgb(240, 246, 174, 55) : Color.FromArgb(160, 120, 128, 132);

            using (Pen backPen = new Pen(gaugeBack, 12F))
            using (Pen safePen = new Pen(safeColor, 13F))
            using (Pen needlePen = new Pen(needleColor, 4F))
            using (Brush labelBrush = new SolidBrush(Color.FromArgb(42, 55, 59)))
            using (Brush mutedBrush = new SolidBrush(Color.FromArgb(92, 105, 108)))
            using (Font title = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold))
            using (Font small = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular))
            using (StringFormat centerText = new StringFormat())
            {
                backPen.StartCap = LineCap.Round;
                backPen.EndCap = LineCap.Round;
                safePen.StartCap = LineCap.Round;
                safePen.EndCap = LineCap.Round;
                DrawGaugeArc(g, backPen, center, radius, 0, 100);
                DrawGaugeArc(g, safePen, center, radius, low, high);

                PointF needle = PointOnGauge(center, radius - 10, Tension);
                using (Brush needleBrush = new SolidBrush(needleColor))
                {
                    g.DrawLine(needlePen, center, needle);
                    g.FillEllipse(needleBrush, center.X - 5, center.Y - 5, 10, 10);
                }

                centerText.Alignment = StringAlignment.Center;
                centerText.LineAlignment = StringAlignment.Center;
                string value = IsFishing ? ((int)Math.Round(Tension)).ToString() : "--";
                g.DrawString("张力 " + value, title, labelBrush, new RectangleF(0, 18, bounds.Width, 24), centerText);
                g.DrawString("安全区 " + low + "-" + high + "    进度 " + (int)Math.Round(Progress) + "%", small, mutedBrush, new RectangleF(0, 42, bounds.Width, 22), centerText);
            }
        }

        private void DrawReel(Graphics g, Rectangle bounds)
        {
            PointF center = ReelCenter();
            float radius = ReelRadius();
            RectangleF outer = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            Color rim = IsFishing ? Color.FromArgb(76, 91, 99) : Color.FromArgb(136, 144, 146);
            Color inner = IsFishing ? Color.FromArgb(54, 65, 72) : Color.FromArgb(170, 176, 178);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(outer);
                using (PathGradientBrush glow = new PathGradientBrush(path))
                using (Pen rimPen = new Pen(Color.FromArgb(235, 248, 250, 248), 3F))
                using (Pen darkPen = new Pen(Color.FromArgb(130, 32, 42, 47), 2F))
                {
                    glow.CenterColor = Color.FromArgb(245, 230, 236, 235);
                    glow.SurroundColors = new[] { rim };
                    g.FillPath(glow, path);
                    g.DrawEllipse(rimPen, outer);
                    g.DrawEllipse(darkPen, outer);
                }
            }

            using (Pen spoke = new Pen(Color.FromArgb(210, 238, 242, 240), 5F))
            using (Brush hub = new SolidBrush(inner))
            using (Brush knob = new SolidBrush(IsFishing ? Color.FromArgb(231, 111, 81) : Color.FromArgb(150, 150, 150)))
            using (Brush labelBrush = new SolidBrush(Color.FromArgb(42, 55, 59)))
            using (Brush pullBrush = new SolidBrush(Color.FromArgb(194, 73, 58)))
            using (Brush releaseBrush = new SolidBrush(Color.FromArgb(48, 115, 176)))
            using (Font labelFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            using (Font fishFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular))
            using (StringFormat centerText = new StringFormat())
            {
                spoke.StartCap = LineCap.Round;
                spoke.EndCap = LineCap.Round;
                for (int i = 0; i < 5; i++)
                {
                    double a = DegreesToRadians(_reelAngle + i * 72);
                    PointF p = new PointF(center.X + (float)Math.Cos(a) * radius * 0.72F, center.Y + (float)Math.Sin(a) * radius * 0.72F);
                    g.DrawLine(spoke, center, p);
                }

                g.FillEllipse(hub, center.X - 18, center.Y - 18, 36, 36);
                double handleAngle = DegreesToRadians(_reelAngle + 38);
                PointF handle = new PointF(center.X + (float)Math.Cos(handleAngle) * radius * 0.86F, center.Y + (float)Math.Sin(handleAngle) * radius * 0.86F);
                g.FillEllipse(knob, handle.X - 10, handle.Y - 10, 20, 20);

                centerText.Alignment = StringAlignment.Center;
                centerText.LineAlignment = StringAlignment.Center;
                g.DrawString("放线", labelFont, releaseBrush, new RectangleF(8, center.Y - 12, 64, 24), centerText);
                g.DrawString("拉线", labelFont, pullBrush, new RectangleF(bounds.Width - 72, center.Y - 12, 64, 24), centerText);
                string target = ActiveFish == null ? "等待咬钩" : ActiveFish.Rarity + "  " + ActiveFish.Name;
                g.DrawString(target, fishFont, labelBrush, new RectangleF(12, bounds.Height - 30, bounds.Width - 24, 20), centerText);
            }
        }

        private void DrawGaugeArc(Graphics g, Pen pen, PointF center, float radius, double start, double end)
        {
            if (end < start)
            {
                double temp = start;
                start = end;
                end = temp;
            }

            int steps = Math.Max(6, (int)Math.Ceiling((end - start) / 2.0));
            PointF[] points = new PointF[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                double value = start + (end - start) * i / steps;
                points[i] = PointOnGauge(center, radius, value);
            }
            g.DrawLines(pen, points);
        }

        private PointF PointOnGauge(PointF center, float radius, double value)
        {
            value = ClampDouble(value, 0, 100);
            double angle = Math.PI * (1.0 - value / 100.0);
            return new PointF(center.X + (float)Math.Cos(angle) * radius, center.Y - (float)Math.Sin(angle) * radius);
        }

        private bool PointInsideReel(Point point)
        {
            PointF center = ReelCenter();
            float radius = ReelRadius() + 22;
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            return dx * dx + dy * dy <= radius * radius;
        }

        private double AngleFromReelCenter(Point point)
        {
            PointF center = ReelCenter();
            return Math.Atan2(point.Y - center.Y, point.X - center.X);
        }

        private PointF ReelCenter()
        {
            float radius = ReelRadius();
            return new PointF(Width / 2F, Math.Max(190F, Height - radius - 38F));
        }

        private float ReelRadius()
        {
            return Math.Min(68F, Math.Max(48F, Math.Min(Width, Height) / 4F));
        }

        private static double NormalizeRadians(double value)
        {
            while (value > Math.PI) value -= Math.PI * 2;
            while (value < -Math.PI) value += Math.PI * 2;
            return value;
        }

        private static double DegreesToRadians(double value)
        {
            return value * Math.PI / 180.0;
        }

        private static double ClampDouble(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void OnPullRequested()
        {
            if (PullRequested != null) PullRequested(this, EventArgs.Empty);
        }

        private void OnReleaseRequested()
        {
            if (ReleaseRequested != null) ReleaseRequested(this, EventArgs.Empty);
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
        public CatchRecord LastCatch { get; set; }

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

            DrawImageLighting(g, bounds);
            DrawScenery(g, bounds);
            DrawWaves(g, bounds);
            DrawFishShadows(g, bounds);
            DrawDock(g, bounds);
            DrawFishingState(g, bounds);
            if (!IsFishing && LastCatch != null)
            {
                DrawCatchShowcase(g, bounds, LastCatch);
            }
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

        private void DrawImageLighting(Graphics g, Rectangle bounds)
        {
            int difficulty = Scene == null ? 1 : Scene.Difficulty;
            Color rayColor = difficulty >= 8 ? Color.FromArgb(42, 130, 245, 230) : Color.FromArgb(44, 255, 240, 172);
            using (Brush depth = new LinearGradientBrush(bounds, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(120, 8, 28, 45), LinearGradientMode.Vertical))
            using (Brush ray = new SolidBrush(rayColor))
            using (Brush particle = new SolidBrush(difficulty >= 8 ? Color.FromArgb(120, 140, 255, 235) : Color.FromArgb(105, 255, 250, 205)))
            {
                g.FillRectangle(depth, bounds);
                for (int i = 0; i < 4; i++)
                {
                    int startX = difficulty >= 7 ? bounds.Width - 120 - i * 70 : 80 + i * 78;
                    Point[] beam = {
                        new Point(startX, -10),
                        new Point(startX + 40, -10),
                        new Point(startX + 150 + i * 26, bounds.Height),
                        new Point(startX - 80 + i * 16, bounds.Height)
                    };
                    g.FillPolygon(ray, beam);
                }

                for (int i = 0; i < 46; i++)
                {
                    int x = 16 + (i * 67 + difficulty * 23) % Math.Max(60, bounds.Width - 32);
                    int y = 36 + (i * 43 + difficulty * 19) % Math.Max(80, bounds.Height - 82);
                    int size = 2 + i % 3;
                    g.FillEllipse(particle, x, y, size, size);
                }
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
            {
                path.AddEllipse(bounds.Width - 150, 28, 96, 96);
                using (PathGradientBrush glow = new PathGradientBrush(path))
                {
                    glow.CenterColor = Color.FromArgb(210, 255, 232, 138);
                    glow.SurroundColors = new[] { Color.FromArgb(0, 255, 232, 138) };
                    g.FillPath(glow, path);
                }
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

            using (Pen bubble = new Pen(Color.FromArgb(125, Color.White), 2F))
            {
                for (int i = 0; i < 18; i++)
                {
                    int size = 5 + i % 4 * 3;
                    int x = 34 + (i * 61) % Math.Max(90, bounds.Width - 70);
                    int y = 78 + (i * 47) % Math.Max(110, bounds.Height - 150);
                    g.DrawEllipse(bubble, x, y, size, size);
                }
            }
        }

        private void DrawIce(Graphics g, Rectangle bounds)
        {
            using (Pen auroraA = new Pen(Color.FromArgb(115, 120, 255, 210), 7F))
            using (Pen auroraB = new Pen(Color.FromArgb(95, 205, 150, 255), 5F))
            {
                Point[] ribbonA = {
                    new Point(0, 74),
                    new Point(bounds.Width / 4, 34),
                    new Point(bounds.Width / 2, 68),
                    new Point(bounds.Width * 3 / 4, 28),
                    new Point(bounds.Width, 58)
                };
                Point[] ribbonB = {
                    new Point(0, 118),
                    new Point(bounds.Width / 3, 84),
                    new Point(bounds.Width * 2 / 3, 116),
                    new Point(bounds.Width, 76)
                };
                g.DrawCurve(auroraA, ribbonA);
                g.DrawCurve(auroraB, ribbonB);
            }

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
            using (Brush cloud = new SolidBrush(Color.FromArgb(135, 30, 39, 54)))
            {
                for (int i = 0; i < 6; i++)
                {
                    int x = -80 + i * bounds.Width / 5;
                    int y = 22 + (i % 2) * 20;
                    g.FillEllipse(cloud, x, y, 180, 48);
                    g.FillEllipse(cloud, x + 52, y - 18, 170, 58);
                }
            }

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
            using (Pen beam = new Pen(Color.FromArgb(45, 100, 230, 255), 10F))
            {
                for (int i = 0; i < 4; i++)
                {
                    int x = 80 + i * bounds.Width / 4;
                    g.DrawLine(beam, x, 0, x - 70, bounds.Height);
                }
            }

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
            using (Pen vortex = new Pen(Color.FromArgb(95, 110, 250, 220), 4F))
            {
                Rectangle ring = new Rectangle(bounds.Width / 2 - 150, bounds.Height / 2 - 72, 300, 128);
                for (int i = 0; i < 4; i++)
                {
                    Rectangle r = Rectangle.Inflate(ring, -i * 28, -i * 12);
                    g.DrawArc(vortex, r, 190 + i * 12, 250);
                }
            }

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
                    ? "目标鱼影：" + (ActiveFish.IsHidden ? "异常稀有" : ActiveFish.Rarity) + "  " + ActiveFish.Name
                    : "选择钓点后抛竿，等待水面动静。";
                g.DrawString(line, small, textBrush, 28, 58);
            }
        }

        private void DrawCatchShowcase(Graphics g, Rectangle bounds, CatchRecord item)
        {
            int cardWidth = Math.Min(380, Math.Max(240, bounds.Width - 72));
            int cardHeight = 142;
            Rectangle card = new Rectangle(bounds.Right - cardWidth - 28, Math.Max(92, bounds.Top + 86), cardWidth, cardHeight);
            if (card.Bottom > bounds.Bottom - 72)
            {
                card.Y = Math.Max(76, bounds.Bottom - cardHeight - 82);
            }

            using (GraphicsPath cardPath = RoundedRectangle(card, 16))
            using (Brush shadow = new SolidBrush(Color.FromArgb(85, 0, 0, 0)))
            using (LinearGradientBrush fill = new LinearGradientBrush(card, Color.FromArgb(232, 255, 255, 250), Color.FromArgb(218, 218, 238, 232), LinearGradientMode.ForwardDiagonal))
            using (Pen border = new Pen(Color.FromArgb(180, Color.White), 2F))
            {
                Rectangle shadowRect = new Rectangle(card.X + 8, card.Y + 10, card.Width, card.Height);
                using (GraphicsPath shadowPath = RoundedRectangle(shadowRect, 16))
                {
                    g.FillPath(shadow, shadowPath);
                }
                g.FillPath(fill, cardPath);
                g.DrawPath(border, cardPath);
            }

            Rectangle icon = new Rectangle(card.Left + 16, card.Top + 18, 108, 96);
            if (item.IsFish)
            {
                DrawCatchFishIcon(g, icon, item);
            }
            else
            {
                DrawCatchItemIcon(g, icon, item);
            }

            using (Brush titleBrush = new SolidBrush(Color.FromArgb(32, 42, 46)))
            using (Brush smallBrush = new SolidBrush(Color.FromArgb(72, 86, 88)))
            using (Font title = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold))
            using (Font meta = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular))
            {
                Rectangle text = new Rectangle(card.Left + 136, card.Top + 22, card.Width - 152, 30);
                g.DrawString(item.SpeciesName, title, titleBrush, text);
                string second = item.IsFish
                    ? item.Rarity + "  " + item.WeightGrade + "  " + item.Weight + "kg"
                    : item.Rarity;
                g.DrawString(second, meta, smallBrush, new Rectangle(card.Left + 138, card.Top + 58, card.Width - 152, 24));
                string price = item.IsSellable ? "售价 " + item.SellPrice + " 金币" : "偏小放生";
                g.DrawString(price, meta, smallBrush, new Rectangle(card.Left + 138, card.Top + 88, card.Width - 152, 24));
            }
        }

        private void DrawCatchFishIcon(Graphics g, Rectangle icon, CatchRecord item)
        {
            Color body = RarityColor(item.Rarity, item.IsHidden);
            Rectangle bodyRect = new Rectangle(icon.Left + 18, icon.Top + 30, icon.Width - 42, 38);
            Point[] tail = {
                new Point(bodyRect.Right - 2, bodyRect.Top + bodyRect.Height / 2),
                new Point(icon.Right - 8, bodyRect.Top + 4),
                new Point(icon.Right - 8, bodyRect.Bottom - 4)
            };
            Point[] fin = {
                new Point(bodyRect.Left + 32, bodyRect.Top + 2),
                new Point(bodyRect.Left + 54, bodyRect.Top - 18),
                new Point(bodyRect.Left + 68, bodyRect.Top + 6)
            };

            if (item.IsHidden)
            {
                using (GraphicsPath glowPath = new GraphicsPath())
                {
                    glowPath.AddEllipse(icon.Left + 2, icon.Top + 8, icon.Width - 4, icon.Height - 16);
                    using (PathGradientBrush glow = new PathGradientBrush(glowPath))
                    {
                        glow.CenterColor = Color.FromArgb(150, body);
                        glow.SurroundColors = new[] { Color.FromArgb(0, body) };
                        g.FillPath(glow, glowPath);
                    }
                }
            }

            using (Brush bodyBrush = new SolidBrush(body))
            using (Brush finBrush = new SolidBrush(ControlPaint.Light(body)))
            using (Brush eye = new SolidBrush(Color.FromArgb(35, 42, 46)))
            using (Pen shine = new Pen(Color.FromArgb(180, Color.White), 2F))
            {
                g.FillPolygon(finBrush, fin);
                g.FillEllipse(bodyBrush, bodyRect);
                g.FillPolygon(bodyBrush, tail);
                g.DrawArc(shine, bodyRect.Left + 10, bodyRect.Top + 6, bodyRect.Width - 20, bodyRect.Height - 12, 195, 70);
                g.FillEllipse(eye, bodyRect.Left + 14, bodyRect.Top + 13, 6, 6);
            }
        }

        private void DrawCatchItemIcon(Graphics g, Rectangle icon, CatchRecord item)
        {
            using (GraphicsPath badge = RoundedRectangle(icon, 18))
            using (LinearGradientBrush fill = new LinearGradientBrush(icon, Color.FromArgb(255, 243, 216, 111), Color.FromArgb(255, 81, 148, 192), LinearGradientMode.ForwardDiagonal))
            using (Pen border = new Pen(Color.FromArgb(220, Color.White), 2F))
            {
                g.FillPath(fill, badge);
                g.DrawPath(border, badge);
            }

            string symbol = item.IconSymbol ?? "";
            using (Brush dark = new SolidBrush(Color.FromArgb(45, 58, 64)))
            using (Pen darkPen = new Pen(Color.FromArgb(45, 58, 64), 4F))
            using (Pen lightPen = new Pen(Color.FromArgb(220, Color.White), 2F))
            using (Font font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                if (symbol.Contains("机"))
                {
                    Rectangle phone = new Rectangle(icon.Left + 38, icon.Top + 17, 34, 62);
                    using (GraphicsPath phonePath = RoundedRectangle(phone, 8))
                    {
                        g.DrawPath(darkPen, phonePath);
                        g.DrawLine(lightPen, phone.Left + 10, phone.Bottom - 10, phone.Right - 10, phone.Bottom - 10);
                    }
                }
                else if (symbol.Contains("宝") || symbol.Contains("晶"))
                {
                    Point[] gem = {
                        new Point(icon.Left + icon.Width / 2, icon.Top + 16),
                        new Point(icon.Right - 18, icon.Top + 42),
                        new Point(icon.Left + icon.Width / 2, icon.Bottom - 14),
                        new Point(icon.Left + 18, icon.Top + 42)
                    };
                    g.FillPolygon(dark, gem);
                    g.DrawPolygon(lightPen, gem);
                }
                else if (symbol.Contains("表"))
                {
                    g.DrawLine(darkPen, icon.Left + icon.Width / 2, icon.Top + 12, icon.Left + icon.Width / 2, icon.Top + 30);
                    g.DrawLine(darkPen, icon.Left + icon.Width / 2, icon.Bottom - 12, icon.Left + icon.Width / 2, icon.Bottom - 30);
                    g.DrawEllipse(darkPen, icon.Left + 31, icon.Top + 30, 46, 46);
                    g.DrawLine(lightPen, icon.Left + icon.Width / 2, icon.Top + 52, icon.Left + icon.Width / 2 + 11, icon.Top + 45);
                }
                else
                {
                    g.DrawString(symbol, font, dark, icon, format);
                }
            }
        }

        private static Color RarityColor(string rarity, bool hidden)
        {
            if (hidden) return Color.FromArgb(236, 194, 75);
            if (rarity == "优秀") return Color.FromArgb(74, 169, 118);
            if (rarity == "稀有") return Color.FromArgb(70, 131, 214);
            if (rarity == "史诗") return Color.FromArgb(159, 96, 214);
            if (rarity == "传说") return Color.FromArgb(232, 151, 54);
            return Color.FromArgb(87, 158, 190);
        }

        private static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
        {
            int d = Math.Max(2, radius * 2);
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
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
