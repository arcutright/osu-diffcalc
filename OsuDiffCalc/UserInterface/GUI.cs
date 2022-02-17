namespace OsuDiffCalc.UserInterface {
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Forms;
	using System.Windows.Forms.DataVisualization.Charting;
	using FileFinder;
	using FileProcessor;

	public partial class GUI : Form {
		private const int LABEL_FONT_SIZE = 12;
		/// <summary> Process for the ui thread </summary>
		private readonly Process _guiProcess;
		/// <summary> Process for the console thread </summary>
		private readonly Process _consoleProcess;
		/// <summary> System-wide pid for the ui thread </summary>
		private readonly int _guiPid;

		private bool _isLoaded = false;
		private bool _isVisible = true;
		private bool _isOsuPresent = false;
		private int? _osuPid = null;
		private bool _isInGame = false;
		private string _inGameWindowTitle = null;
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
		private bool _pauseGUI = false;

		private const int INITIAL_TIMEOUT_MS =
#if DEBUG
			-1;
#else
			3000;
#endif

		public GUI() {
			_guiProcess = Process.GetCurrentProcess();
			_guiPid = _guiProcess?.Id ?? (int)NativeMethods.GetCurrentThreadId();

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "GUIThread";
			InitializeComponent();
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
			SetText(Settings_UpdateIntervalNormalTextbox, $"{Settings.AutoUpdateIntervalNormalMs}");
			SetText(Settings_UpdateIntervalMinimizedTextbox, $"{Settings.AutoUpdateIntervalMinimizedMs}");

			_isLoaded = true;

			if (_isVisible) {
				StartAutoWindowUpdater();
				if (EnableAutoBeatmapAnalyzer)
					StartAutoBeatmapAnalyzer();
			}
		}

		public Properties.Settings Settings { get; } = Properties.Settings.Default;

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
				if (value && _isVisible)
					StartAutoBeatmapAnalyzer();
				else
					_ = StopAutoBeatmapAnalyzer();
			}
		}

		#endregion

		#region Settings page input box and checkbox backing properties

		public int UpdateIntervalNormalMs {
			get => Settings.AutoUpdateIntervalNormalMs;
			set {
				if (UpdateIntervalNormalMs == value) return;
				Settings.AutoUpdateIntervalNormalMs = value;
				SetText(Settings_UpdateIntervalNormalTextbox, $"{value}");
				Settings.Save();
			}
		}

		public int UpdateIntervalMinimizedMs {
			get => Settings.AutoUpdateIntervalMinimizedMs;
			set {
				if (UpdateIntervalMinimizedMs == value) return;
				Settings.AutoUpdateIntervalMinimizedMs = value;
				SetText(Settings_UpdateIntervalMinimizedTextbox, $"{value}");
				Settings.Save();
			}
		}

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
			if (WindowState == FormWindowState.Minimized) {
				_isVisible = false;
				_pauseGUI = true;
				await StopAutoWindowUpdater();
				await StopAutoBeatmapAnalyzer();
			}
			else {
				_isVisible = true;
				_pauseGUI = false;
				if (_isLoaded) {
					StartAutoWindowUpdater();
					if (EnableAutoBeatmapAnalyzer)
						StartAutoBeatmapAnalyzer();
				}
			}
		}

		protected override async void OnFormClosing(FormClosingEventArgs e) {
			if (e.CloseReason == CloseReason.UserClosing) {
				await StopAutoBeatmapAnalyzer();
				await StopAutoWindowUpdater();
			}
			base.OnFormClosing(e);
		}

#region Controls

		private void OpenFromFile_Click(object sender, EventArgs e) {
			Task.Run(ManualBeatmapAnalyzer);
		}

		private void ClearButton_Click(object sender, EventArgs e) {
			MapsetManager.Clear();
			SavefileXMLManager.ClearXML();
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

		private void SeriesSelect_SelectedIndexChanged(object sender, EventArgs e) {
			if (_chartedBeatmap is not null) {
				_pauseGUI = true;
				var selectedItemsText = new List<string>();
				foreach (ListViewItem sel in seriesSelect.SelectedItems) {
					sel.Checked = true;
					sel.Selected = false;
				}
				foreach (ListViewItem sel in seriesSelect.CheckedItems) {
					if (!selectedItemsText.Contains(sel.Text))
						selectedItemsText.Add(sel.Text);
					sel.Selected = false;
				}
				ClearChart();
				foreach (string text in selectedItemsText) {
					if (text == "Streams")
						AddChartSeries(_chartedBeatmap.DiffRating.Streams);
					else if (text == "Bursts")
						AddChartSeries(_chartedBeatmap.DiffRating.Bursts);
					else if (text == "Doubles")
						AddChartSeries(_chartedBeatmap.DiffRating.Doubles);
					else if (text == "Sliders")
						AddChartSeries(_chartedBeatmap.DiffRating.Sliders);
					else if (text == "Jumps")
						AddChartSeries(_chartedBeatmap.DiffRating.Jumps);
				}
			}
			_pauseGUI = false;
		}

		private void ChartedMapChoice_SelectedIndexChanged(object sender, EventArgs e) {
			if (_displayedMapset is not null) {
				_pauseGUI = false;
				string choice = chartedMapChoice.SelectedItem.ToString();
				Beatmap displayedMap = _displayedMapset.Beatmaps.FirstOrDefault(map => map.Version == choice);
				if (displayedMap is not null)
					_chartedBeatmap = displayedMap;
				SeriesSelect_SelectedIndexChanged(null, null);
			}
		}

		private void ChartedMapChoice_DropDown(object sender, EventArgs e) {
			_pauseGUI = true;
		}

		private bool _isTextChanging = false;
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
			difficultyDisplayPanel.SetFlowBreak(diff, true);
		}

		private void RefreshMapset() {
			DisplayMapset(_displayedMapset);
		}

		private void DisplayMapset(Mapset set) {
			// sort by difficulty
			set.Sort(false);

			Invoke((MethodInvoker)delegate {
				// display all maps
				ClearBeatmapDisplay();
				foreach (Beatmap map in set.Beatmaps) {
					AddBeatmapToDisplay(map);
				}
				_displayedMapset = set;
				_chartedBeatmap = set.Beatmaps.FirstOrDefault();
				UpdateChartOptions(true);
				SeriesSelect_SelectedIndexChanged(null, null);
			});
		}

		private Label MakeLabel(string text, int fontSize, string toolTipStr = "") {
			var label = new Label {
				Text = text,
				AutoSize = true,
			};
			label.Font = new Font(label.Font.FontFamily, fontSize);
			if (!string.IsNullOrEmpty(toolTipStr)) {
				var tip = new ToolTip {
					ShowAlways = true
				};
				tip.SetToolTip(label, toolTipStr);
			}
			return label;
		}

		private void SetText(Control control, string text) {
			bool prevIsTextChanging = _isTextChanging;
			try {
				_isTextChanging = true;
				Invoke((MethodInvoker)delegate {
					control.Text = text;
					//if (control is Label) {
					//	control.AutoSize = true;
					//	control.Font = new Font(control.Font.FontFamily, 9);
					//}
				});
			}
			catch { }
			finally {
				_isTextChanging = prevIsTextChanging;
			}
		}

		public void AddChartPoint(double x, double y) {
			Invoke((MethodInvoker)delegate {
				Series last = Chart.Series.Last();
				if (last is null) {
					last = new Series();
					Chart.Series.Add(last);
					last = Chart.Series.Last();
				}
				Chart.Series.Last().Points.AddXY(x, y);
				//chart.ChartAreas[0].RecalculateAxesScale();
				Chart.Visible = true;
				Chart.Update();
			});
		}

		private void ClearChart() {
			Invoke((MethodInvoker)delegate {
				Chart.Series.Clear();
				if (Chart.ChartAreas.Count != 0) {
					Chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
					Chart.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
					Chart.ChartAreas[0].AxisX.LabelStyle.Format = "#";
					Chart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
					Chart.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
					Chart.ChartAreas[0].AxisY.LabelStyle.Format = "#";
				}
				Chart.Update();
			});
		}

		private void AddChartSeries(Series series) {
			Invoke((MethodInvoker)delegate {
				if (Chart.Series.IsUniqueName(series.Name)) {
					Chart.Series.Add(series);
					Chart.Visible = true;
					Chart.Update();
				}
			});
		}

		private void UpdateChartOptions(bool fullSet = true) {
			//if fullSet = false, the only option should be manually chosen map(s)
			if (fullSet) {
				Invoke((MethodInvoker)delegate {
					chartedMapChoice.Items.Clear();
					bool showFamiliarRating = ShowFamiliarRating;
					foreach (Beatmap map in _displayedMapset.Beatmaps) {
						string displayString = showFamiliarRating ? map.GetFamiliarizedDisplayString() : map.GetDiffDisplayString();
						chartedMapChoice.Items.Add(displayString);
					}

					// pick the most appropriate initial map according to the settings
					int selectedIndex = -1;
					int numMaps = _displayedMapset.Beatmaps.Count;
					if (numMaps > 1) {
						var diffs = new double[numMaps];
						for (int i = 0; i < numMaps; ++i) {
							diffs[i] = FileProcessor.AnalyzerObjects.DifficultyRating.FamiliarizeRating(_displayedMapset.Beatmaps[i].DiffRating.TotalDifficulty);
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
					}
					else {
						selectedIndex = 0;
					}
					chartedMapChoice.SelectedIndex = selectedIndex;
				});
			}
			else {
				Invoke((MethodInvoker)delegate {
					chartedMapChoice.Items.Clear();
					chartedMapChoice.Items.Add(_chartedBeatmap.Version);
					chartedMapChoice.SelectedIndex = 0;
				});
			}
		}

#endregion

#region Manual Beatmap Analyzer

		private void ManualBeatmapAnalyzer() {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "ManualBeatmapAnalyzerWorkerThread";
			try {
				Mapset set = MapsetManager.BuildSet(UX.GetFilenamesFromDialog(this));
				SetText(timeDisplay1, $"0 ms");
				Console.WriteLine("set built");
				MapsetManager.Clear();
				if (set.Beatmaps.Count == 0) {
					Invoke((MethodInvoker)ClearBeatmapDisplay);
					return;
				}

				var sw = Stopwatch.StartNew();
				foreach (var beatmap in set.Beatmaps) {
					MapsetManager.AnalyzeMap(beatmap);
					MapsetManager.SaveMap(beatmap, EnableXmlCache);
				}
				sw.Stop();
				SetAnalyzeTime($"{sw.ElapsedMilliseconds} ms");

				Invoke((MethodInvoker)delegate {
					ClearBeatmapDisplay();
					// add to text results
					_displayedMapset = set;
					foreach (var beatmap in set.Beatmaps) {
						AddBeatmapToDisplay(beatmap);
					}
					// display graph results
					_chartedBeatmap = set.Beatmaps.First();
					SeriesSelect_SelectedIndexChanged(null, null);
					UpdateChartOptions(true);
				});
			}
			catch (OperationCanceledException) { throw; }
			catch { }
		}
#endregion

#region Automatic Window Updater - auto show/hide window

		private void StartAutoWindowUpdater() {
			if (_autoWindowUpdater is null) {
				if (_autoWindowCancellation.IsCancellationRequested)
					_autoWindowCancellation = new CancellationTokenSource();
				_autoWindowUpdater = AutoWindowUpdaterBegin(_autoWindowCancellation.Token, AutoWindowUpdaterTimeoutMs);
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
					if (!_pauseGUI)
						await AutoWindowUpdaterThreadTick(cancelToken, timeoutMs);
					sw.Stop();
					cancelToken.ThrowIfCancellationRequested();
					int updateInterval = _isVisible ? UpdateIntervalNormalMs : UpdateIntervalMinimizedMs;
					await Task.Delay(Math.Max(updateInterval, timeoutMs - (int)sw.ElapsedMilliseconds));
				}
				cancelToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException) { throw; }
			catch { }
		}

		private async Task AutoWindowUpdaterThreadTick(CancellationToken cancelToken, int timeoutMs) {
			//Console.Write("auto window  ");
			try {
				var t = new Thread(ts) {
					IsBackground = true,
					Priority = ThreadPriority.Lowest
				};
				t.Start();
				var joinTask = Task.Run(t.Join, cancelToken);
				await Task.WhenAny(joinTask, Task.Delay(timeoutMs, cancelToken));
				if (t.IsAlive) {
					t.Interrupt();
					t.Abort();
				}
			}
			catch (OperationCanceledException) { throw; }
			catch { }

			void ts() {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoWindowTickThread";

				var osuProcess = Finder.GetOsuProcess(_guiPid, _osuPid);
				string windowTitle =  osuProcess?.MainWindowTitle;
				_osuPid = osuProcess?.Id;

				//update visibility
				if (string.IsNullOrEmpty(windowTitle)) {
					Invoke((MethodInvoker)delegate {
						if (TopMost) TopMost = false;
						_isOsuPresent = false;
						_isInGame = false;
					});
				}
				else if (windowTitle.IndexOf('[') != -1) {
					Invoke((MethodInvoker)delegate {
						if (Visible) Visible = false;
						if (TopMost) TopMost = false;
						_isVisible = false;
						_isOsuPresent = true;
						_isInGame = true;
						_inGameWindowTitle = windowTitle;
					});
				}
				else {
					Invoke((MethodInvoker)delegate {
						if (!TopMost) TopMost = IsAlwaysOnTop;
						if (!Visible) Visible = true;
						_isVisible = true;
						_isOsuPresent = true;
						_isInGame = false;

						var osuScreenBounds = Screen.FromHandle(osuProcess.MainWindowHandle)?.Bounds;
						var thisScreenBounds = Screen.FromHandle(_guiProcess.MainWindowHandle)?.Bounds;
						if (thisScreenBounds == osuScreenBounds) {
							int numScreens = Screen.AllScreens.Length;
							bool isOsuFullScreen = NativeMethods.IsFullScreen(osuProcess);
							if (numScreens > 1 && isOsuFullScreen) {
								var otherScreen = Screen.AllScreens.First(s => s.Bounds != osuScreenBounds);
								NativeMethods.TryMoveToScreen(Program.ConsoleWindowHandle, otherScreen);
								NativeMethods.TryMoveToScreen(_guiProcess, otherScreen);
								//TopMost = false;
							}
							else if (!isOsuFullScreen) {
								//TopMost = IsAlwaysOnTop;
							}
							else {
								// TODO: find way to forward mouse/kb inputs to osu process when using this technique
								NativeMethods.ForceForegroundWindow(_guiProcess, osuProcess);

								// seizure warning
								//NativeMethods.MakeForegroundWindow(_guiProcess);
								//NativeMethods.MakeForegroundWindow2(_guiProcess);
								//NativeMethods.MakeForegroundWindow3(_guiProcess);
							}
						}
						else {
							//TopMost = false;
						}
					});
				}
			}
		}

#endregion

#region Automatic Beatmap Analyzer - polls handles, gets beatmaps, analyzes, updates display

		private void StartAutoBeatmapAnalyzer() {
			if (_autoBeatmapAnalyzer is null) {
				if (_autoBeatmapCancellation.IsCancellationRequested)
					_autoBeatmapCancellation = new CancellationTokenSource();
				_autoBeatmapAnalyzer = AutoBeatmapAnalyzerBegin(_autoBeatmapCancellation.Token, AutoBeatmapAnalyzerTimeoutMs);
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
					sw.Restart();
					if (!_pauseGUI)
						await AutoBeatmapAnalyzerThreadTick(cancelToken, timeoutMs);
					sw.Stop();
					cancelToken.ThrowIfCancellationRequested();
					int updateInterval = _isVisible ? UpdateIntervalNormalMs : UpdateIntervalMinimizedMs;
					await Task.Delay(Math.Max(updateInterval, timeoutMs - (int)sw.ElapsedMilliseconds));
				}
				cancelToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException) { throw; }
			catch { }
		}

		private async Task AutoBeatmapAnalyzerThreadTick(CancellationToken cancelToken, int timeoutMs) {
			bool useWindowTitleForDirectory = false;
			if (!_isOsuPresent || _isInGame || !_isVisible)
				return;
			try {
				var t = new Thread(ts) {
					IsBackground = true,
					Priority = ThreadPriority.BelowNormal
				};
				t.Start();
				var joinTask = Task.Run(t.Join, cancelToken);
				await Task.WhenAny(joinTask, Task.Delay(timeoutMs, cancelToken));
				if (t.IsAlive) {
					t.Interrupt();
					t.Abort();
				}
			}
			catch (OperationCanceledException) { throw; }
			catch { }

			void ts() {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoBeatmapTickThread";
					
				//timing
				var sw = Stopwatch.StartNew();
				if (useWindowTitleForDirectory) {
					// TODO: this branch is unused since the program is always minimized when in-game
					_currentMapsetDirectory = MapsetManager.GetCurrentMapsetDirectory(_inGameWindowTitle, _prevMapsetDirectory);
					useWindowTitleForDirectory = false;
				}
				else {
					var osuProcess = Finder.GetOsuProcess(_guiPid, _osuPid);
					_osuPid = osuProcess?.Id;
					_currentMapsetDirectory = Finder.GetOsuBeatmapDirectory(_osuPid);
				}
				sw.Stop();
				SetText(timeDisplay1, $"{sw.ElapsedMilliseconds} ms");

				if (_currentMapsetDirectory != _prevMapsetDirectory && Directory.Exists(_currentMapsetDirectory)) {
					//analyze the mapset
					Mapset set = MapsetManager.AnalyzeMapset(_currentMapsetDirectory, this, true, EnableXmlCache);
					if (set is not null) {
						//show info on GUI
						Invoke((MethodInvoker)delegate {
							DisplayMapset(set);
							_prevMapsetDirectory = _currentMapsetDirectory;
						});
					}
				}
			}
		}

#endregion

	}
}
