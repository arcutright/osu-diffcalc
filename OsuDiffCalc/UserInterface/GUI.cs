namespace OsuDiffCalc.UserInterface {
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using System.Windows.Forms.DataVisualization.Charting;
	using FileFinder;
	using FileProcessor;
	using UserInterface.Controls;
	using Utility;

	public partial class GUI : Form {
		private const int LABEL_FONT_SIZE = 12;
		/// <summary> Process for the ui thread </summary>
		private readonly Process _guiProcess;
		/// <summary> System-wide pid for the ui thread </summary>
		private readonly int _guiPid;

		// osu state variables
		private Process _osuProcess = null;
		private bool _isInGame = false;
		private string _inGameWindowTitle = null;
		private Beatmap _inGameBeatmap = null;
		private string _prevMapsetDirectory = null, _currentMapsetDirectory = null;

		//background event timers
		private Task _autoBeatmapAnalyzer;
		private CancellationTokenSource _autoBeatmapCancellation = new();
		/// <summary>
		/// Task to minimize this program when osu is in-game, move to other monitors if osu is full screen, etc.
		/// </summary>
		private Task _autoWindowUpdater;
		private CancellationTokenSource _autoWindowCancellation = new();

		//display variables
		private Beatmap _chartedBeatmap;
		private Mapset _displayedMapset;
		private TabPage _prevTab;
		private bool _isChangingTab = false;
		private bool _isLoaded = false;
		private bool _isMinimized = false;
		private bool _isOsuPresent = false;
		private bool _isOnSameScreen = true;
		private bool _didMinimize = false;
		private bool _pauseAllTasks = false;

		private const int INITIAL_TIMEOUT_MS =
#if DEBUG
			-1;
#else
			3000;
#endif

		public GUI() {
			_guiProcess = Process.GetCurrentProcess();
			_guiPid = _guiProcess?.IdSafe() ?? (int)NativeMethods.GetCurrentThreadId();

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "GUIThread";

			InitializeComponent();
			WindowHelper.TrySetUseImmersiveDarkMode(Handle, true);
		}

		private void GUI_Load(object sender, EventArgs eArgs) {
			double xPadding = 0;
			int x = Screen.PrimaryScreen.Bounds.Right - Width - (int)(Screen.PrimaryScreen.Bounds.Width * xPadding + 0.5);
			int y = Screen.PrimaryScreen.Bounds.Y + (Screen.PrimaryScreen.Bounds.Height - Height) / 2;
			Location = new Point(x, y);
			TopMost = IsAlwaysOnTop;

			// initialize text box text
			SetText(Settings_StarTargetMinTextbox, $"{Settings.FamiliarStarTargetMinimum:f2}");
			SetText(Settings_StarTargetMaxTextbox, $"{Settings.FamiliarStarTargetMaximum:f2}");
			SetText(Settings_UpdateIntervalNormalTextbox, $"{Settings.UpdateIntervalNormalMs}");
			SetText(Settings_UpdateIntervalMinimizedTextbox, $"{Settings.UpdateIntervalMinimizedMs}");
			SetText(Settings_UpdateIntervalOsuNotFoundTextbox, $"{Settings.UpdateIntervalOsuNotFoundMs}");

			// attach event handlers for text box updates
			Settings_StarTargetMinTextbox.TextChanged += SettingsTextbox_TextChanged;
			Settings_StarTargetMinTextbox.LostFocus += SettingsTextbox_TextChanged;
			Settings_StarTargetMaxTextbox.TextChanged += SettingsTextbox_TextChanged;
			Settings_StarTargetMaxTextbox.LostFocus += SettingsTextbox_TextChanged;
			Settings_UpdateIntervalNormalTextbox.TextChanged += SettingsTextbox_TextChanged;
			Settings_UpdateIntervalNormalTextbox.LostFocus += SettingsTextbox_TextChanged;
			Settings_UpdateIntervalMinimizedTextbox.TextChanged += SettingsTextbox_TextChanged;
			Settings_UpdateIntervalMinimizedTextbox.LostFocus += SettingsTextbox_TextChanged;
			Settings_UpdateIntervalOsuNotFoundTextbox.TextChanged += SettingsTextbox_TextChanged;
			Settings_UpdateIntervalOsuNotFoundTextbox.LostFocus += SettingsTextbox_TextChanged;

			ChartStyleDropdown.Items.Clear();
			ChartStyleDropdown.Items.AddRange(new object[] {
				SeriesChartType.Column,
				SeriesChartType.RangeColumn,
				SeriesChartType.StackedColumn,
				SeriesChartType.StackedColumn100,
				SeriesChartType.Point,
			});
			ChartStyleDropdown.SelectedItem = SeriesChartType;
			ChartStyleDropdown.Update();
			RefreshChart();

			_isLoaded = true;
			StartTasksIfNeeded();

			// attach extra event handlers
			Chart.MouseMove += Chart_MouseMove;
			MainTabControl.SelectedIndexChanged += MainTabControl_TabChanged;

			_prevTab = MainTabControl.SelectedTab;
			Refresh();
		}

#pragma warning disable IDE1006 // Naming Styles

		// hack to prevent stealing focus when activating this window

		protected override CreateParams CreateParams {
			get {
				var createParams = base.CreateParams;

				const int WS_EX_NOACTIVATE = 0x08000000;
				//const int WS_EX_TOOLWINDOW = 0x00000080;
				createParams.ExStyle |= WS_EX_NOACTIVATE;

				return createParams;
			}
		}

		protected override bool ShowWithoutActivation => true;

#pragma warning restore IDE1006 // Naming Styles

		private void MainTabControl_TabChanged(object sender, EventArgs e) {
			if (_isChangingTab)
				_isChangingTab = false;
			else
				_prevTab = MainTabControl.SelectedTab;
		}

		public Properties.Settings Settings => Properties.Settings.Default;

		public int AutoBeatmapAnalyzerTimeoutMs { get; set; } = INITIAL_TIMEOUT_MS;
		public int AutoWindowUpdaterTimeoutMs { get; set; } = INITIAL_TIMEOUT_MS;

		#region Results page checkbox backing properties

		public bool ShowFamiliarRating {
			get => Settings.ShowFamiliarStarRating;
			set {
				if (ShowFamiliarRating == value) return;
				Settings.ShowFamiliarStarRating = value;
				scaleRatings.Checked = value;
				Settings.Save();
				RefreshMapset();
			}
		}

		public bool EnableXmlCache {
			get => Settings.EnableXmlCache;
			set {
				if (EnableXmlCache == value) return;
				Settings.EnableXmlCache = value;
				EnableXmlCheckbox.Checked = value;
				Settings.Save();
			}
		}

		public bool IsAlwaysOnTop {
			get => Settings.AlwaysOnTop;
			set {
				if (IsAlwaysOnTop == value) return;
				Settings.AlwaysOnTop = value;
				AlwaysOnTopCheckbox.Checked = value;
				Settings.Save();
			}
		}

		public bool EnableAutoBeatmapAnalyzer {
			get => Settings.EnableAutoBeatmapAnalyzer;
			set {
				if (EnableAutoBeatmapAnalyzer == value) return;
				Settings.EnableAutoBeatmapAnalyzer = value;
				AutoBeatmapCheckbox.Checked = value;
				Settings.Save();
				if (value && _isLoaded)
					StartAutoBeatmapAnalyzer();
				else
					Task.Run(StopAutoBeatmapAnalyzer);
			}
		}

		public SeriesChartType SeriesChartType {
			get => Settings.SeriesChartType;
			set {
				if (SeriesChartType == value) return;
				Settings.SeriesChartType = value;
				RefreshChart();
			}
		}

		#endregion

		#region Settings page input box and checkbox backing properties

		public double FamiliarStarTargetMininum {
			get => Settings.FamiliarStarTargetMinimum;
			set {
				if (FamiliarStarTargetMininum == value) return;
				Settings.FamiliarStarTargetMinimum = value;
				SetText(Settings_StarTargetMinTextbox, $"{value:f2}");
				Settings.Save();
			}
		}

		public double FamiliarStarTargetMaximum {
			get => Settings.FamiliarStarTargetMaximum;
			set {
				if (FamiliarStarTargetMaximum == value) return;
				Settings.FamiliarStarTargetMaximum = value;
				SetText(Settings_StarTargetMaxTextbox, $"{value:f2}");
				Settings.Save();
			}
		}

		public int UpdateIntervalNormalMs {
			get => Settings.UpdateIntervalNormalMs;
			set {
				if (UpdateIntervalNormalMs == value) return;
				Settings.UpdateIntervalNormalMs = value;
				SetText(Settings_UpdateIntervalNormalTextbox, $"{value}");
				Settings.Save();
			}
		}

		public int UpdateIntervalMinimizedMs {
			get => Settings.UpdateIntervalMinimizedMs;
			set {
				if (UpdateIntervalMinimizedMs == value) return;
				Settings.UpdateIntervalMinimizedMs = value;
				SetText(Settings_UpdateIntervalMinimizedTextbox, $"{value}");
				Settings.Save();
			}
		}

		public int UpdateIntervalOsuNotFoundMs {
			get => Settings.UpdateIntervalOsuNotFoundMs;
			set {
				if (UpdateIntervalOsuNotFoundMs == value) return;
				Settings.UpdateIntervalOsuNotFoundMs = value;
				SetText(Settings_UpdateIntervalOsuNotFoundTextbox, $"{value}");
				Settings.Save();
			}
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Update the "parse + analyze" time text in the UI
		/// </summary>
		public void SetAnalyzeTime(string timeString) {
			SetText(timeDisplay2, timeString);
		}

		#endregion

		//starts and stops all background threads
		protected override async void OnResize(EventArgs e) {
			try {
				base.OnResize(e);
				if (WindowState == FormWindowState.Minimized) {
					_isMinimized = true;
					_pauseAllTasks = true;
					await Task.WhenAll(StopAutoWindowUpdater(), StopAutoBeatmapAnalyzer());
				}
				else {
					_isMinimized = false;
					_pauseAllTasks = false;
					StartTasksIfNeeded();
				}
			}
			catch { }
		}

		protected override void OnParentVisibleChanged(EventArgs e) {
			base.OnParentVisibleChanged(e);
			StartTasksIfNeeded();
		}

		protected override void OnVisibleChanged(EventArgs e) {
			base.OnVisibleChanged(e);
			StartTasksIfNeeded();
		}

		protected override async void OnFormClosing(FormClosingEventArgs e) {
			if (e?.CloseReason == CloseReason.UserClosing) {
				try {
					// detach heavy event handlers
					Chart.MouseMove -= Chart_MouseMove;
					// wait for work to end
					await Task.WhenAll(StopAutoWindowUpdater(), StopAutoBeatmapAnalyzer());
				}
				catch { }
			}
			base.OnFormClosing(e);
		}

		#region Controls

		private void OpenFromFile_Click(object sender, EventArgs e) {
			Task.Run(ManualBeatmapAnalyzer);
		}

		private void ClearButton_Click(object sender, EventArgs e) {
			// var displayedMapset = _displayedMapset;
			MapsetManager.Clear();
			SavefileXMLManager.ClearXML();
			// MapsetManager.SaveMapset(displayedMapset, true, EnableXmlCache);
		}

		private void ScaleRatings_CheckedChanged(object sender, EventArgs e) {
			ShowFamiliarRating = scaleRatings.Checked;
		}

		private void AutoBeatmapCheckbox_CheckedChanged(object sender, EventArgs e) {
			EnableAutoBeatmapAnalyzer = AutoBeatmapCheckbox.Checked;
		}

		private void AlwaysOnTop_CheckedChanged(object sender, EventArgs e) {
			IsAlwaysOnTop = AlwaysOnTopCheckbox.Checked;
		}

		private void EnableXmlCheckbox_CheckedChanged(object sender, EventArgs e) {
			EnableXmlCache = EnableXmlCheckbox.Checked;
		}

		private void ChartedMapDropdown_SelectedValueChanged(object sender, EventArgs e) {
			int index = ChartedMapDropdown.SelectedIndex;
			var displayedMap = _displayedMapset is not null && index >= 0 && index < _displayedMapset.Count
				? _displayedMapset[index]
				: null;
			if (displayedMap is not null) {
				if (_chartedBeatmap != displayedMap) {
					Chart.Series.Clear();
					_chartedBeatmap = displayedMap;
				}
				RefreshChart();
			}
			if (_chartedBeatmap is not null)
				StatusStripLabel.Text = $"{_chartedBeatmap.Artist} - {_chartedBeatmap.Title} [{_chartedBeatmap.Version}] ({_chartedBeatmap.Creator})";
			else
				StatusStripLabel.Text = string.Empty;
		}

		private void SettingsTextbox_TextChanged(object sender, System.EventArgs e) {
			if (!_isLoaded || _isTextChanging || sender is not TextBox box) return;
			if (!box.IsHandleCreated || box.Focused) return;
			try {
				_isTextChanging = true;

				if (box == Settings_StarTargetMinTextbox)
					FamiliarStarTargetMininum = Settings_StarTargetMinTextbox.Value;
				else if (box == Settings_StarTargetMaxTextbox)
					FamiliarStarTargetMaximum = Settings_StarTargetMaxTextbox.Value;
				else if (box == Settings_UpdateIntervalNormalTextbox)
					UpdateIntervalNormalMs = Settings_UpdateIntervalNormalTextbox.Value;
				else if (box == Settings_UpdateIntervalMinimizedTextbox)
					UpdateIntervalMinimizedMs = Settings_UpdateIntervalMinimizedTextbox.Value;
#if DEBUG
				else
					throw new NotImplementedException($"No logic defined for sender '{sender}'");
#endif
			}
			finally {
				_isTextChanging = false;
			}
		}

		private void ChartStyleDropdown_SelectedIndexChanged(object sender, EventArgs e) {
			SeriesChartType = (SeriesChartType?)ChartStyleDropdown.SelectedItem ?? this.SeriesChartType;
			RefreshChart();
		}

		private readonly CustomToolTip _customChartToolTip = new() { Font = new Font("Consolas", 9) };
		private readonly ToolTip _basicChartToolTip = new();
		private readonly StringBuilder _chartToolTipSb = new();
		private Point? _chartMousePrevPosition = null;

		private void Chart_MouseMove(object sender, MouseEventArgs e) {
			if (sender is not Chart chart || e is null)
				return;
			var pos = e.Location;
			if (_chartMousePrevPosition.HasValue && pos == _chartMousePrevPosition.Value)
				return;
			// reset tooltip
			_basicChartToolTip.RemoveAll();
			_customChartToolTip.RemoveAll();
			_chartToolTipSb.Clear();
			_chartMousePrevPosition = pos;

			const double marginX = 1.5; // time in seconds

			// find nearest series x-value to our cursor (that we have data for) and build a tooltip from each of the series' points
			var results = chart.HitTest(pos.X, pos.Y, false, ChartElementType.PlottingArea);
			foreach (var result in results) {
				if (result.Object is null) continue;
				double pointX = result.ChartArea.AxisX.PixelPositionToValue(pos.X);

				int guessIndex = -1; // used to cache the point index that we are "nearest". will only be calculated for the first series
				foreach (var series in chart.Series) {
					if (series is null || !series.Enabled) continue;
					int n = series.Points?.Count ?? 0;
					if (n == 0) continue;
					if (guessIndex == -1) {
						double bestDelta = double.MaxValue;
						// TODO: could probably make a better starting guess than 0, but x-spacing of points is not guaranteed
						for (int i = 0; i < n; ++i) {
							var point = series.Points[i];
							double delta = Math.Abs(pointX - point.XValue);
							if (point.XValue < pointX || delta < bestDelta) {
								if (delta < bestDelta) {
									bestDelta = delta;
									guessIndex = i;
								}
							}
							else if (delta > bestDelta) {
								break;
							}
						}
						// don't show anything when no points within margin
						if (bestDelta > marginX)
							return;
					}
					if (guessIndex != -1) {
						// find the closest non-zero rating within our margin
						int i = guessIndex;
						bool found = false;

						// try to find first non-zero rating at or to the right of guessIndex, within our margin
						for (; i < n; ++i) {
							var pt = series.Points[i];
							double delta = Math.Abs(pt.XValue - pointX);
							if (delta > marginX) {
								// don't search outside our margin
								break;
							}
							else if (pt.YValues[0] != 0) {
								// found non-zero point i >= guessIndex
								found = true;
								break;
							}
						}
						if (!found) {
							// if we didn't find a non-zero point to the right of guessIndex, check to the left
							for (i = guessIndex - 1; i >= 0; --i) {
								var pt = series.Points[i];
								double delta = Math.Abs(pointX - pt.XValue);
								if (delta > marginX) {
									// don't search outside our margin
									break;
								}
								else if (pt.YValues[0] != 0) {
									// found non-zero point i < guessIndex
									break;
								}
							}
						}
						double xValue = pointX;
						double yValue = 0;
						if (i >= 0 && i < n) {
							var point = series.Points[i];
							xValue = point.XValue;
							yValue = point.YValues[0];
						}
						if (_chartToolTipSb.Length == 0)
							_chartToolTipSb.AppendLine($"Time: {xValue:0.#}");
						if (yValue == 0)
							_chartToolTipSb.AppendLine($"{series.Name,8}:");
						else
							_chartToolTipSb.AppendLine($"{series.Name,8}: {yValue,4:0.0}");
					}
				}
			}
			// show tooltip
			var tooltip = SeriesChartType is SeriesChartType.SplineArea or SeriesChartType.SplineRange
				? _basicChartToolTip
				: _customChartToolTip;
			if (_chartToolTipSb.Length != 0)
				tooltip.Show(_chartToolTipSb.ToString(), chart, pos.X + 8, pos.Y - 15);
		}

		#endregion

		#region Private Helpers

		private void ClearBeatmapDisplay() {
			difficultyDisplayPanel.Controls.Clear();
		}

		private void AddBeatmapToDisplay(Beatmap beatmap) {
			Label diff;
			if (ShowFamiliarRating)
				diff = MakeLabel(beatmap.GetFamiliarizedDisplayString(), LABEL_FONT_SIZE, beatmap.GetFamiliarizedDetailString());
			else
				diff = MakeLabel(beatmap.GetDiffDisplayString(), LABEL_FONT_SIZE, beatmap.GetDiffDetailString());
			difficultyDisplayPanel.Controls.Add(diff);
		}

		private void RefreshMapset() {
			DisplayMapset(_displayedMapset);
		}

		private void DisplayMapset(Mapset set) {
			var prevFgWindow = WindowHelper.GetForegroundWindow();
			try {
				if (set is null) {
					Invoke((MethodInvoker)delegate {
						ClearBeatmapDisplay();
						ChartedMapDropdown.Items.Clear();
						_displayedMapset = null;
						_chartedBeatmap = null;
						StatusStripLabel.Text = string.Empty;
					});
					return;
				}

				// sort by difficulty
				set.Sort(false);

				Invoke((MethodInvoker)delegate {
					// display all maps
					ClearBeatmapDisplay();
					foreach (Beatmap map in set) {
						AddBeatmapToDisplay(map);
					}
					_displayedMapset = set;

					var inGameBeatmap = GetInGameBeatmap(set, _inGameWindowTitle);
					if (inGameBeatmap != _inGameBeatmap)
						Console.WriteLine($"  => beatmap: {inGameBeatmap}");
					_inGameBeatmap = inGameBeatmap;

					_chartedBeatmap = inGameBeatmap ?? set.FirstOrDefault();
					UpdateChartOptions(_chartedBeatmap);
					RefreshChart();
				});
			}
			finally {
				WindowHelper.MakeForegroundWindow(prevFgWindow);
			}
		}

		private Label MakeLabel(string text, int fontSize, string toolTipStr = "") {
			var label = new Label {
				Text = text,
				Font = difficultyDisplayPanel.Font,
				AutoSize = true,
				Dock = DockStyle.Fill,
				Margin = Padding.Empty,
				Padding = Padding.Empty,
			};
			//label.Font = new Font(label.Font.FontFamily, fontSize);
			if (!string.IsNullOrEmpty(toolTipStr)) {
				var tip = new ToolTip {
					ShowAlways = true
				};
				tip.SetToolTip(label, toolTipStr);
			}
			return label;
		}

		private bool _isTextChanging = false;
		private void SetText(Control control, string text) {
			string currentText = control?.Text;
			if (currentText == text)
				return;
			Invoke((MethodInvoker)delegate {
				bool prevIsTextChanging = _isTextChanging;
				try {
					_isTextChanging = true;
					control.Text = text;
					//if (control is Label) {
					//	control.AutoSize = true;
					//	control.Font = new Font(control.Font.FontFamily, 9);
					//}
				}
				catch { }
				finally {
					_isTextChanging = prevIsTextChanging;
				}
			});
		}

		private void ClearChart() {
			Invoke((MethodInvoker)delegate {
				_chartedBeatmap = null;
				_displayedMapset = null;
				SetText(StreamBpmLabel, string.Empty);
				SetText(BurstBpmLabel, string.Empty);
				SetText(StatusStripLabel, string.Empty);
				Chart.Series.Clear();
				//if (Chart.ChartAreas.Count != 0) {
				//	Chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
				//	Chart.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
				//	Chart.ChartAreas[0].AxisX.LabelStyle.Format = "#";
				//	Chart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
				//	Chart.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
				//	Chart.ChartAreas[0].AxisY.LabelStyle.Format = "#";
				//}
				ChartedMapDropdown.Items.Clear();
				ChartedMapDropdown.Update();
				Chart.Update();
				RebuildCustomChartLegend();
			});
		}

		private void RefreshChart() {
			if (_chartedBeatmap is null) {
				ClearChart();
				return;
			}
			bool prevPauseAllTasks = _pauseAllTasks;
			try {
				_pauseAllTasks = true;
				Invoke((MethodInvoker)delegate {
					SetText(StreamBpmLabel, $"{_chartedBeatmap.DiffRating.StreamsMaxBPM:f0}");
					SetText(BurstBpmLabel, $"{_chartedBeatmap.DiffRating.BurstsMaxBPM:f0}");
					SetText(StatusStripLabel, $"{_chartedBeatmap.Artist} - {_chartedBeatmap.Title} ({_chartedBeatmap.Creator})");
					// remove any unexpected series
					Series[] allSeries = _chartedBeatmap.DiffRating.GetAllSeries();
					var toRemove = Chart.Series.Where(series => Array.IndexOf(allSeries, series) == -1).ToArray(); // .ToArray() to avoid collection-was-modified
					foreach (var series in toRemove) {
						Chart.Series.Remove(series);
					}
					// add series if needed, update visibility + chart type of each series
					foreach (var series in allSeries) {
						if (_seriesEnabled.TryGetValue(series.Name, out var isEnabled))
							series.Enabled = isEnabled;
						else
							_seriesEnabled[series.Name] = series.Enabled;
						if (!Chart.Series.Contains(series))
							Chart.Series.Add(series);
						series.ChartType = SeriesChartType;
					}
					// refresh the chart
					Chart.Visible = true;
					Chart.Update();

					RebuildCustomChartLegend();
				});
			}
			finally {
				_pauseAllTasks = prevPauseAllTasks;
			}
		}

		private int[] _seriesIndexToLegendIndex = null;
		private int[] _legendIndexToSeriesIndex = null;
		private (string seriesName, SeriesChartType seriesType, string legendName)[] _prevSeries = null;
		private readonly Dictionary<string, bool> _seriesEnabled = new();

		/// <summary>
		/// Update and rebuild the chart's custom legend if needed.
		/// </summary>
		/// <param name="force">
		/// If <see langword="false"/>, will check if the (series name, series type, legend name)
		/// for each series has changed since last time we built the legend. If they haven't, will
		/// only update the strikethrough visibilities. <br/>
		/// If <see langword="true"/>, will always rebuild.
		/// </param>
		private void RebuildCustomChartLegend(bool force = false) {
			int n = Chart.Series.Count;
			bool needsRebuild = force || _prevSeries is null || _prevSeries.Length != n;
			if (!needsRebuild) {
				Chart.ApplyPaletteColors();
				for (int i = 0; i < n; i++) {
					var series = Chart.Series[i];
					if ((series.Name, series.ChartType, series.Legend) != _prevSeries[i]) {
						needsRebuild = true;
						break;
					}
				}
				if (!needsRebuild) {
					bool anyChanged = false;
					for (int i = 0; i < n; i++) {
						var series = Chart.Series[i];
						var colorPanel = ChartLegendPanel.Controls[2 * i] as Panel;
						var label = ChartLegendPanel.Controls[(2 * i) + 1] as Label;
						var colorPanelStrikeThrough = colorPanel.Controls[0];
						bool isEnabledInLegend = colorPanelStrikeThrough.Visible;
						if (colorPanel.BackColor != series.Color) {
							colorPanel.BackColor = series.Color;
							anyChanged = true;
						}
						if (series.Enabled != isEnabledInLegend) {
							label.Font = new Font(label.Font, series.Enabled ? FontStyle.Regular : FontStyle.Strikeout);
							colorPanelStrikeThrough.Visible = !series.Enabled;
							anyChanged = true;
						}
					}
					if (anyChanged)
						ChartLegendPanel.Update();

					return;
				}
			}

			// dispose any controls currently attached to legend before clearing the collection
			foreach (var obj in ChartLegendPanel.Controls) {
				dispose(obj);
			}
			ChartLegendPanel.Controls.Clear();
			ChartLegendPanel.RowStyles.Clear();

			// reset cached values
			_seriesIndexToLegendIndex = new int[n];
			_legendIndexToSeriesIndex = new int[n];
			_prevSeries = new(string, SeriesChartType, string)[n];

			if (n > 0) {
				Chart.ApplyPaletteColors(); // ensures the Color property is calculated for each Series
				var baseFont = Font;
				const int rowHeight = 11;

				// all series for a given legend and a 'reversed' type are in reversed order
				// figure out the order of all the series by index
				var seriesStacks = new Dictionary<string, Stack<int>>(); // stack to hold reversed-order series(s)
				var stackPositions = new Dictionary<string, int>(); // starting index in the legend for each stack
				for (int i = 0; i < n; i++) {
					var series = Chart.Series[i];
					bool isReversed = (series.ChartType is SeriesChartType.StackedArea or SeriesChartType.StackedArea100 or SeriesChartType.Pyramid or SeriesChartType.StackedColumn or SeriesChartType.StackedColumn100);
					if (isReversed) {
						if (!stackPositions.ContainsKey(series.Legend))
							stackPositions[series.Legend] = i;
						if (!seriesStacks.TryGetValue(series.Legend, out var stack)) {
							stack = new Stack<int>();
							seriesStacks.Add(series.Legend, stack);
						}
						stack.Push(i);
					}
					else
						_seriesIndexToLegendIndex[i] = i;
				}
				foreach (var (legend, startingIndex) in stackPositions) {
					var stack = seriesStacks[legend];
					int m = startingIndex + stack.Count;
					for (int i = startingIndex; i < m; i++) {
						_seriesIndexToLegendIndex[i] = stack.Pop();
					}
				}

				// build legend panel + update cache
				for (int i = 0; i < n; i++) {
					int legendRowIndex = _seriesIndexToLegendIndex[i];
					var series = Chart.Series[i];

					var colorPanel = new Panel() {
						BackColor = series.Color,
						Anchor = AnchorStyles.None,
						Dock = DockStyle.Fill,
						Padding = Padding.Empty,
						Margin = new Padding(0, 2, 0, 2),
						Height = rowHeight - 2,
					};
					var colorPanelStrikeThrough = new Panel() {
						BackColor = Color.Black,
						Anchor = AnchorStyles.Top | AnchorStyles.Left,
						Dock = DockStyle.None,
						Visible = !series.Enabled,
						Left = 0,
						Top = rowHeight / 2,
						Height = 1,
					};
					colorPanel.Controls.Add(colorPanelStrikeThrough);
					var label = new Label() {
						Text = series.Name,
						AutoSize = true,
						TextAlign = ContentAlignment.MiddleLeft,
						ImageAlign = ContentAlignment.MiddleLeft,
						Anchor = AnchorStyles.Left,
						Margin = Padding.Empty,
						Padding = Padding.Empty,
						Font = new Font(baseFont, series.Enabled ? FontStyle.Regular : FontStyle.Strikeout),
					};
					// add to legend panel
					colorPanel.Click += ChartLegendRowCellClick;
					label.Click += ChartLegendRowCellClick;
					ChartLegendPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
					ChartLegendPanel.Controls.Add(colorPanel, 0, legendRowIndex);
					ChartLegendPanel.Controls.Add(label, 1, legendRowIndex);

					// update cache
					_prevSeries[i] = (series.Name, series.ChartType, series.Legend);
					_legendIndexToSeriesIndex[legendRowIndex] = i;
				}
				ChartLegendPanel.RowCount = n;
			}
			else {
				ChartLegendPanel.RowCount = 0;
			}
			ChartLegendPanel.Update();


			// local recursive dispose method to ensure our events are removed
			void dispose(object obj) {
				if (obj is null) return;
				if (obj is Control parent) {
					if (parent.Controls is not null) {
						foreach (var child in parent.Controls) {
							dispose(child);
						}
						parent.Controls.Clear();
					}

					parent.Click -= ChartLegendRowCellClick;
					parent.Dispose();
				}
				else if (obj is IDisposable disposable)
					disposable.Dispose();
			}
		}

		private void ChartLegendRowCellClick(object sender, EventArgs e) {
			if (sender is not Control control) return;
			if (control.Parent is not TableLayoutPanel table) return;
			int legendRowIndex = table.GetRow(control);
			int nColumns = table.ColumnCount;
			var seriesIndex = _legendIndexToSeriesIndex[legendRowIndex];
			var series = Chart.Series[seriesIndex];

			//var colorPanel = table.Controls[legendRowIndex * nColumns] as Panel;
			//var label = table.Controls[legendRowIndex * nColumns + 1] as Label;
			//var colorPanelStrikeThrough = colorPanel.Controls[0];
			var colorPanel = table.Controls[seriesIndex * nColumns] as Panel;
			var label = table.Controls[seriesIndex * nColumns + 1] as Label;
			var colorPanelStrikeThrough = colorPanel.Controls[0];

			bool shouldEnable = !series.Enabled;

			// make the series appear enabled / disabled
			label.Font = new Font(label.Font, shouldEnable ? FontStyle.Regular : FontStyle.Strikeout);
			colorPanelStrikeThrough.Visible = !shouldEnable;
			series.Enabled = shouldEnable;

			// save enabled state so it's preserved between maps
			_seriesEnabled[series.Name] = shouldEnable;

			// TODO: get chart Y axis to rescale itself when changing what is visible. This doesn't work...
			//Chart.Invalidate();
			//Chart.ChartAreas.Invalidate();
		}

		private void UpdateChartOptions(Beatmap selectedBeatmap, bool fullSet = true) {
			int selectedIndex = -1;
			if (selectedBeatmap is not null)
				selectedIndex = _displayedMapset.IndexOf(selectedBeatmap);

			//if fullSet = false, the only option should be manually chosen map(s)
			if (fullSet && _displayedMapset is not null) {
				bool showFamiliarRating = ShowFamiliarRating;
				var newMapOptions = _displayedMapset.Select(
					map => showFamiliarRating ? map.GetFamiliarizedDisplayString() : map.GetDiffDisplayString()
				).ToArray();
				if (newMapOptions.Length == ChartedMapDropdown.Items.Count) {
					if (newMapOptions.Length == 0)
						return;
					int i = 0;
					bool equal = true;
					foreach (var item in ChartedMapDropdown.Items) {
						if (newMapOptions[i] != item?.ToString()) {
							equal = false;
							break;
						}
						++i;
					}
					if (equal) {
						int currentIndex = ChartedMapDropdown.SelectedIndex;
						if (selectedIndex != -1 && currentIndex != selectedIndex) {
							Invoke(() => {
								ChartedMapDropdown.SelectedIndex = selectedIndex;
							});
						}
						return;
					}
				}

				Invoke((MethodInvoker)delegate {
					ChartedMapDropdown.Items.Clear();
					foreach (Beatmap map in _displayedMapset) {
						string displayString = showFamiliarRating ? map.GetFamiliarizedDisplayString() : map.GetDiffDisplayString();
						ChartedMapDropdown.Items.Add(displayString);
					}

					if (selectedIndex >= 0)
						ChartedMapDropdown.SelectedIndex = selectedIndex;
					else {

						// pick the most appropriate initial map according to the settings
						int numMaps = _displayedMapset.Count;
						if (numMaps > 1) {
							var diffs = new double[numMaps];
							for (int i = 0; i < numMaps; ++i) {
								diffs[i] = FileProcessor.AnalyzerObjects.DifficultyRating.FamiliarizeRating(_displayedMapset[i].DiffRating.TotalDifficulty);
							}

							var (minStars, maxStars) = (FamiliarStarTargetMininum, FamiliarStarTargetMaximum);
							double minDiffSeen = double.MinValue;
							double maxDiffSeen = double.MaxValue;
							int minDiffSeenIndex = 0;
							int maxDiffSeenIndex = 0;
							for (int i = 0; i < numMaps; ++i) {
								double diff = diffs[i];
								minDiffSeen = Math.Min(minDiffSeen, diff);
								maxDiffSeen = Math.Max(maxDiffSeen, diff);
								if (diff == minDiffSeen)
									minDiffSeenIndex = i;
								if (diff == maxDiffSeen)
									maxDiffSeenIndex = i;
								if (diff >= minStars && diff <= maxStars) {
									selectedIndex = i;
									break;
								}
							}
							if (selectedIndex == -1) {
								if (minDiffSeen > maxStars)
									selectedIndex = minDiffSeenIndex;
								else
									selectedIndex = maxDiffSeenIndex;
							}
							ChartedMapDropdown.SelectedIndex = selectedIndex;
						}
						else if (numMaps == 1)
							ChartedMapDropdown.SelectedIndex = 0;
					}
				});
			}
			else {
				Invoke((MethodInvoker)delegate {
					ChartedMapDropdown.Items.Clear();
					if (_chartedBeatmap is not null) {
						var map = _chartedBeatmap;
						string displayString = ShowFamiliarRating ? map.GetFamiliarizedDisplayString() : map.GetDiffDisplayString();
						ChartedMapDropdown.Items.Add(displayString);
					}
					if (ChartedMapDropdown.Items.Count != 0)
						ChartedMapDropdown.SelectedIndex = 0;
				});
			}
		}

		#endregion

		private void StartTasksIfNeeded() {
			if (_isLoaded && !_isMinimized && Visible) {
				StartAutoWindowUpdater();
				if (EnableAutoBeatmapAnalyzer)
					StartAutoBeatmapAnalyzer();
			}
		}

		#region Manual Beatmap Analyzer

		private void ManualBeatmapAnalyzer() {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "ManualBeatmapAnalyzerWorkerThread";
			bool prevPauseAllTasks = _pauseAllTasks;
			try {
				_pauseAllTasks = true;
				var selectedFileNames = (string[])Invoke(() => UX.GetFilenamesFromDialog());
				Mapset set = MapsetManager.BuildSet(selectedFileNames);
				if (set is null || set.Count == 0) {
					set?.Dispose();
					return;
				}
				SetText(timeDisplay1, $"0 ms");
				Console.WriteLine("set built");

				var sw = Stopwatch.StartNew();
				var errorsLock = new object();
				Parallel.ForEach(set, beatmap => {
					try {
						MapsetManager.AnalyzeMap(beatmap);
					}
					catch (Exception ex) {
						lock (errorsLock) {
							Console.WriteLine($"[ERROR] Failed to parse beatmap: \"{beatmap.Artist} - {beatmap.Title} [{beatmap.Version}]\"");
							Console.WriteLine(ex.ToString());
						}
					}
				});
				var toRemove = set.Where(map => !map.IsAnalyzed).ToArray();
				foreach (var beatmap in toRemove) {
					set.Remove(beatmap);
					beatmap.Dispose();
				}
				if (EnableXmlCache) {
					foreach (var beatmap in set) {
						MapsetManager.SaveMapToXML(beatmap);
					}
				}
				sw.Stop();
				SetAnalyzeTime($"{sw.ElapsedMilliseconds} ms");

				if (set.Count == 0) {
					// if we got here, that means all the selected maps were invalid, so clearing the display seems reasonable
					Invoke((MethodInvoker)delegate {
						ClearBeatmapDisplay();
						_displayedMapset = null;
						ChartedMapDropdown.Items.Clear();
						ClearChart();
						SetText(timeDisplay1, string.Empty);
						SetText(timeDisplay2, string.Empty);
					});
					return;
				}

				Invoke((MethodInvoker)delegate {
					ClearBeatmapDisplay();
					ClearChart();
					// add to text results
					foreach (var beatmap in set) {
						AddBeatmapToDisplay(beatmap);
					}
					_displayedMapset = set;


					var inGameBeatmap = GetInGameBeatmap(set, _inGameWindowTitle); 
					if (inGameBeatmap != _inGameBeatmap)
						Console.WriteLine($"  => beatmap: {inGameBeatmap}");
					_inGameBeatmap = inGameBeatmap;

					// display graph results
					_chartedBeatmap = inGameBeatmap ?? set.First();
					UpdateChartOptions(inGameBeatmap);
					RefreshChart();
				});
			}
			catch (OperationCanceledException) { throw; }
			catch { }
			finally {
				_pauseAllTasks = prevPauseAllTasks;
			}
		}
		#endregion

		#region Automatic Window Updater - auto show/hide window

		private void StartAutoWindowUpdater() {
			if (_autoWindowUpdater is null) {
				if (_autoWindowCancellation.IsCancellationRequested)
					_autoWindowCancellation = new CancellationTokenSource();
				_autoWindowUpdater = Task.Run(() => AutoWindowUpdaterBegin(_autoWindowCancellation.Token, AutoWindowUpdaterTimeoutMs));
			}
		}

		private async Task StopAutoWindowUpdater() {
			if (_autoWindowUpdater is not null) {
				try { _autoWindowCancellation.Cancel(true); } catch { }
				try { await _autoWindowUpdater; } catch { }
				try { _autoWindowUpdater.Dispose(); } catch { }
			}
			_autoWindowUpdater = null;
		}

		private async Task AutoWindowUpdaterBegin(CancellationToken cancelToken, int timeoutMs) {
			try {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoWindowUpdaterThread";
				var sw = new Stopwatch();
				while (!cancelToken.IsCancellationRequested) {
					sw.Restart();
					if (!_pauseAllTasks)
						await AutoWindowUpdaterThreadTick(cancelToken, timeoutMs);
					sw.Stop();

					int updateInterval;
					if (!_isOsuPresent)
						updateInterval = UpdateIntervalOsuNotFoundMs;
					else if (!_isInGame && !_isMinimized && Visible)
						updateInterval = UpdateIntervalNormalMs;
					else
						updateInterval = UpdateIntervalMinimizedMs;
					await Task.Delay(Math.Max(updateInterval, timeoutMs - (int)sw.ElapsedMilliseconds), cancelToken);
				}
				cancelToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException) { throw; }
			catch { }
		}

		private async Task AutoWindowUpdaterThreadTick(CancellationToken cancelToken, int timeoutMs) {
			//Console.Write("auto window  ");
			try {
				//var t = new Thread(ts) {
				//	IsBackground = true,
				//	Priority = ThreadPriority.Lowest
				//};
				//t.Start();
				//var joinTask = Task.Run(t.Join, cancelToken);
				//await Task.WhenAny(joinTask, Task.Delay(timeoutMs, cancelToken));
				//if (t.IsAlive) {
				//	t.Interrupt();
				//	t.Abort();
				//}
				await Task.Run(ts, cancelToken);
			}
			catch (OperationCanceledException) { throw; }
			catch { }

			void ts() {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoWindowTickThread";

				// ensure we have the correct process for osu (in case player restarted osu, etc)
				_osuProcess = Finder.GetOsuProcess(_guiPid, _osuProcess);
				cancelToken.ThrowIfCancellationRequested();
				string windowTitle =  _osuProcess?.MainWindowTitle?.Trim();

				// update visibility
				if (_osuProcess is null || _osuProcess.HasExitedSafe()) {
					// osu not found
					if (TopMost) {
						Invoke((MethodInvoker)delegate {
							TopMost = false;
							if (MainTabControl.SelectedTab != _prevTab) {
								var prevFgWindow = WindowHelper.GetForegroundWindow();
								MainTabControl.SelectTab(_prevTab);
								WindowHelper.MakeForegroundWindow(prevFgWindow);
							}
						});
					}
					_isOsuPresent = false;
					_isInGame = false;
					_didMinimize = false;
				}
				else {
					// osu found and we may be in-game
					_isOsuPresent = true;

					if (_isInGame && windowTitle == _inGameWindowTitle) {
						// definitely in-game
						return;
					}

					Invoke((MethodInvoker)delegate {
						// crappy way to check if osu is in-game
						_isInGame = windowTitle.IndexOf('[') != -1;
						if (_isInGame)
							_inGameWindowTitle = windowTitle;
						else
							_inGameWindowTitle = null;

						// find the screen bounds of each process
						Rectangle? osuScreenBounds = null, thisScreenBounds = null;
						try {
							thisScreenBounds = Screen.FromHandle(Handle)?.Bounds;
							if (_osuProcess is not null)
								osuScreenBounds = Screen.FromHandle(_osuProcess.MainWindowHandle)?.Bounds;
							_isOnSameScreen = thisScreenBounds == osuScreenBounds;
						}
						catch { }
						
						if (!_isInGame) {
							// not in game
							// unminimize if it was auto-minimized, change back to prev user tab if we auto-changed to the charts tab
							if (_didMinimize && !Visible) Visible = true;
							if (MainTabControl.SelectedTab != _prevTab) {
								var prevFgWindow = WindowHelper.GetForegroundWindow();
								MainTabControl.SelectTab(_prevTab);
								WindowHelper.MakeForegroundWindow(prevFgWindow);
							}
							_didMinimize = false;
						}

						if (!TopMost) TopMost = IsAlwaysOnTop;

						// try to move to a secondary screen
						try {
							if (!_isOnSameScreen) {
								// if we are not in game and osu is on a different screen, do nothing
							}
							else {
								// if we are not in game and we are on the same screen
								int numScreens = Screen.AllScreens.Length;
								bool isOsuFullScreen = WindowHelper.IsFullScreen(_osuProcess);
								if (numScreens > 1 && (isOsuFullScreen || _isInGame)) {
									// move to a different screen if osu is full screen
									// (we can't act as an overlay with WinForms UI, would require a rewrite in DirectX or something)
									var otherScreen = Screen.AllScreens.First(s => s.Bounds != osuScreenBounds);
									Console.WriteLine($"Try move window from ({thisScreenBounds?.X}, {thisScreenBounds?.Y}) to ({otherScreen?.Bounds.X}, {otherScreen?.Bounds.Y})");
									WindowHelper.TryMoveToScreen(Program.ConsoleWindowHandle, otherScreen);
									WindowHelper.TryMoveToScreen(Handle, otherScreen);
									//TopMost = false;
								}
								else if (_isInGame) {
									if (!_isMinimized) _didMinimize = _isOnSameScreen;
									if (TopMost) TopMost = false;
									if (Visible) Visible = false;
								}
								else if (numScreens == 1 && isOsuFullScreen) {
									// osu is full screen, not in game, and the player only has 1 screen. no good way to resolve this, see DirectX note above

									// TODO: find way to forward mouse/kb inputs to osu process when using this technique
									WindowHelper.ForceForegroundWindow(_guiProcess, _osuProcess);

									// seizure warning
									//WindowHelper.MakeForegroundWindow(_guiProcess);
									//WindowHelper.MakeForegroundWindow2(_guiProcess);
									//WindowHelper.MakeForegroundWindow3(_guiProcess);
								}
							}
						}
						catch { }

						// if we are in-game, switch to charts tab if we aren't on it
						if (_isInGame) {
							_prevTab = MainTabControl.SelectedTab;
							if (_prevTab != chartsTab) {
								_isChangingTab = true;
								var prevFgWindow = WindowHelper.GetForegroundWindow();
								MainTabControl.SelectTab(chartsTab);
								WindowHelper.MakeForegroundWindow(prevFgWindow);
							}

							// try to figure out what map is being played. This work will only happen once
							_inGameBeatmap = GetInGameBeatmap(_displayedMapset, windowTitle);

							// switch charted beatmap to in-game map
							if (_chartedBeatmap != _inGameBeatmap || _prevTab != chartsTab) {
								UpdateChartOptions(_inGameBeatmap);
								RefreshChart();
							}

							Console.WriteLine($"In game, window title: '{windowTitle}'");
							Console.WriteLine($"  => beatmap: {_inGameBeatmap}");
						}
					});
				}
			}
		}

		Beatmap GetInGameBeatmap(Mapset set, string windowTitle) {
			if (set is not null && !string.IsNullOrEmpty(windowTitle)) {
				var inGameBeatmap = set.FirstOrDefault(map => windowTitle.EndsWith($"[{map.Version}]", StringComparison.Ordinal));
				inGameBeatmap ??= set.FirstOrDefault(map => windowTitle.Contains($"[{map.Version}]") && windowTitle.Contains($"{map.Title}"));
				return inGameBeatmap;
			}
			else
				return null;
		}

		#endregion

		#region Automatic Beatmap Analyzer - polls handles, gets beatmaps, analyzes, updates display

		private void StartAutoBeatmapAnalyzer() {
			if (_autoBeatmapAnalyzer is null) {
				if (_autoBeatmapCancellation.IsCancellationRequested)
					_autoBeatmapCancellation = new CancellationTokenSource();
				_autoBeatmapAnalyzer = Task.Run(() => AutoBeatmapAnalyzerBegin(_autoBeatmapCancellation.Token, AutoBeatmapAnalyzerTimeoutMs));
			}
		}

		private async Task StopAutoBeatmapAnalyzer() {
			if (_autoBeatmapAnalyzer is not null) {
				try { _autoBeatmapCancellation.Cancel(true); } catch { }
				try { await _autoBeatmapAnalyzer; } catch { }
				try { _autoBeatmapAnalyzer.Dispose(); } catch { }
			}
			_autoBeatmapAnalyzer = null;
		}

		private async Task AutoBeatmapAnalyzerBegin(CancellationToken cancelToken, int timeoutMs) {
			try {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoBeatmapAnalyzerThread";
				var sw = new Stopwatch();
				while (!cancelToken.IsCancellationRequested) {
					bool needsAnalyze = _isOsuPresent && !_isMinimized
						&& ((Visible && !_isInGame) || (_isInGame && _prevMapsetDirectory is null));
					if (needsAnalyze && !_pauseAllTasks) {
						sw.Restart();
						await AutoBeatmapAnalyzerThreadTick(cancelToken, timeoutMs);
						sw.Stop();
					}

					int updateInterval;
					if (!_isOsuPresent)
						updateInterval = UpdateIntervalOsuNotFoundMs;
					else if (!_isInGame && !_isMinimized && Visible)
						updateInterval = UpdateIntervalNormalMs;
					else
						updateInterval = UpdateIntervalMinimizedMs;
					await Task.Delay(Math.Max(updateInterval, timeoutMs - (int)sw.ElapsedMilliseconds), cancelToken);
				}
				cancelToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException) { throw; }
			catch { }
		}

		private async Task AutoBeatmapAnalyzerThreadTick(CancellationToken cancelToken, int timeoutMs) {
			try {
				//var t = new Thread(ts) {
				//	IsBackground = true,
				//	Priority = ThreadPriority.BelowNormal
				//};
				//t.Start();
				//var joinTask = Task.Run(t.Join, cancelToken);
				//await Task.WhenAny(joinTask, Task.Delay(timeoutMs, cancelToken));
				//if (t.IsAlive) {
				//	t.Interrupt();
				//	t.Abort();
				//}
				await Task.Run(ts, cancelToken);
			}
			catch (OperationCanceledException) { throw; }
			catch { }

			void ts() {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoBeatmapTickThread";

				var sw = Stopwatch.StartNew();

				_osuProcess ??= Finder.GetOsuProcess(_guiPid, _osuProcess); // secondary HOT PATH
				cancelToken.ThrowIfCancellationRequested();
				_currentMapsetDirectory = Finder.GetActiveBeatmapDirectory(_osuProcess?.Id); // true HOT PATH
				if (_currentMapsetDirectory is null && _isInGame)
					_currentMapsetDirectory = MapsetManager.GetCurrentMapsetDirectory(_osuProcess, _inGameWindowTitle, _prevMapsetDirectory);

				sw.Stop();
				SetText(timeDisplay1, $"{sw.ElapsedMilliseconds} ms");

				if (_currentMapsetDirectory != _prevMapsetDirectory && Directory.Exists(_currentMapsetDirectory)) {
					//analyze the mapset
					Mapset set = MapsetManager.AnalyzeMapset(_currentMapsetDirectory, this, true, EnableXmlCache);
					// show on GUI
					var prevSet = _displayedMapset;
					DisplayMapset(set);
					_prevMapsetDirectory = _currentMapsetDirectory;

					// clear previously cached Series
					if (set != prevSet && prevSet is not null) {
						Invoke(() => {
							foreach (var map in prevSet) {
								map.DiffRating.ClearCachedSeries();
							}
						});
					}
				}
			}
		}

		#endregion

	}
}
