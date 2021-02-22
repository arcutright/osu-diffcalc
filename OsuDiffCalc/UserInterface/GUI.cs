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
		const int LABEL_FONT_SIZE = 12;

		private bool _isVisible = true;
		private bool _isOsuPresent = false;
		private int? _osuPid = null;
		private bool _isInGame = false;
		private string _inGameWindowTitle = null;
		private string _prevMapsetDirectory = null, _currentMapsetDirectory = null;
		//background event timers
		private Task _autoBeatmapAnalyzer;
		private CancellationTokenSource _autoBeatmapCancellation = new CancellationTokenSource();
		private Task _autoWindowUpdater;
		private CancellationTokenSource _autoWindowCancellation = new CancellationTokenSource();

		//display variables
		private Beatmap _chartedBeatmap;
		private Mapset _displayedMapset;
		private bool _pauseGUI = false;

		public GUI() {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "GUIThread";
			InitializeComponent();
		}

		private const int INITIAL_TIMEOUT_MS =
#if DEBUG
			-1;
#else
			2000;
#endif

		public int MinUpdateDelayMs { get; set; } = 600;
		public int AutoBeatmapAnalyzerTimeoutMs { get; set; } = INITIAL_TIMEOUT_MS;
		public int AutoWindowUpdaterTimeoutMs { get; set; } = INITIAL_TIMEOUT_MS;

#region Public methods

		public void GUI_Load(object sender, EventArgs eArgs) {
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "GUIThread";
			double xPadding = 0;
			int x = Screen.PrimaryScreen.Bounds.Right - Width - (int)(Screen.PrimaryScreen.Bounds.Width * xPadding + 0.5);
			int y = Screen.PrimaryScreen.Bounds.Y + (Screen.PrimaryScreen.Bounds.Height - Height) / 2;
			Location = new Point(x, y);
		}

		public void SetTime1(string timeString) {
			SetLabel(timeDisplay1, timeString);
		}

		public void SetTime2(string timeString) {
			SetLabel(timeDisplay2, timeString);
		}

#endregion

#region Controls

		private void ScaleRatings_CheckedChanged(object sender, EventArgs e) {
		}

		private void OpenFromFile_Click(object sender, EventArgs e) {
			Task.Run(ManualBeatmapAnalyzer);
		}

		private void ClearButton_Click(object sender, EventArgs e) {
			MapsetManager.Clear();
			SavefileXMLManager.ClearXML();
		}

		//starts and stops all background threads
		protected override void OnResize(EventArgs e) {
			if (WindowState == FormWindowState.Minimized) {
				StopAutoWindowUpdater();
				StopAutoBeatmapAnalyzer();
				_isVisible = false;
				_pauseGUI = true;
			}
			else {
				StartAutoWindowUpdater();
				StartAutoBeatmapAnalyzer();
				_isVisible = true;
				_pauseGUI = false;
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e) {
			if (e.CloseReason == CloseReason.UserClosing) {
				StopAutoBeatmapAnalyzer();
				StopAutoWindowUpdater();
			}
			base.OnFormClosing(e);
		}

		private void AutoBeatmapCheckbox_CheckedChanged(object sender, EventArgs e) {
			if (autoBeatmapCheckbox.Checked)
				StartAutoBeatmapAnalyzer();
			else
				StopAutoBeatmapAnalyzer();
		}

		private void SeriesSelect_SelectedIndexChanged(object sender, EventArgs e) {
			if (_chartedBeatmap is not null) {
				Invoke((MethodInvoker)delegate { _pauseGUI = true; });
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
					else if (text == "Couplets")
						AddChartSeries(_chartedBeatmap.DiffRating.Couplets);
					else if (text == "Sliders")
						AddChartSeries(_chartedBeatmap.DiffRating.Sliders);
					else if (text == "Jumps")
						AddChartSeries(_chartedBeatmap.DiffRating.Jumps);
				}
			}
			Invoke((MethodInvoker)delegate { _pauseGUI = false; });
		}

		private void ChartedMapChoice_SelectedIndexChanged(object sender, EventArgs e) {
			if (_displayedMapset is not null) {
				Invoke((MethodInvoker)delegate { _pauseGUI = false; });
				string choice = chartedMapChoice.SelectedItem.ToString();
				Beatmap displayedMap = _displayedMapset.Beatmaps.FirstOrDefault(map => map.Version == choice);
				if (displayedMap is not null)
					_chartedBeatmap = displayedMap;
				SeriesSelect_SelectedIndexChanged(null, null);
			}
		}

		private void ChartedMapChoice_DropDown(object sender, EventArgs e) {
			Invoke((MethodInvoker)delegate { _pauseGUI = true; });
		}

#endregion

#region Private Helpers

		private void ClearBeatmapDisplay() {
			difficultyDisplayPanel.Controls.Clear();
		}

		private void AddBeatmapToDisplay(Beatmap beatmap) {
			Label diff;
			if (scaleRatings.Checked)
				diff = MakeLabel(beatmap.GetFamiliarizedDisplayString(), LABEL_FONT_SIZE, beatmap.GetFamiliarizedDetailString());
			else
				diff = MakeLabel(beatmap.GetDiffDisplayString(), LABEL_FONT_SIZE, beatmap.GetDiffDetailString());
			difficultyDisplayPanel.Controls.Add(diff);
			difficultyDisplayPanel.SetFlowBreak(diff, true);
		}

		private void DisplayMapset(Mapset set) {
			if (set.Beatmaps.Count != 0) {
				// sort by difficulty
				set.Sort(false);
				// display all maps
				foreach (Beatmap map in set.Beatmaps) {
					AddBeatmapToDisplay(map);
				}
				_displayedMapset = set;
				_chartedBeatmap = set.Beatmaps[0];
				UpdateChartOptions(true);
			}
		}

		private Label MakeLabel(string text, int fontSize, string toolTipStr = "") {
			var label = new Label {
				Text = text,
				AutoSize = true
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

		private void SetLabel(Label label, string labelString) {
			try {
				Invoke((MethodInvoker)delegate {
					label.Text = labelString;
					label.AutoSize = true;
					label.Font = new Font(label.Font.FontFamily, 9);
				});
			}
			catch { }
		}

		public void AddChartPoint(double x, double y) {
			Invoke((MethodInvoker)delegate {
				Series last = chart.Series.Last();
				if (last is null) {
					last = new Series();
					chart.Series.Add(last);
					last = chart.Series.Last();
				}
				chart.Series.Last().Points.AddXY(x, y);
				//chart.ChartAreas[0].RecalculateAxesScale();
				chart.Visible = true;
				chart.Update();
			});
		}

		private void ClearChart() {
			Invoke((MethodInvoker)delegate {
				chart.Series.Clear();
				if (chart.ChartAreas.Count != 0) {
					chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
					chart.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
					chart.ChartAreas[0].AxisX.LabelStyle.Format = "#";
					chart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
					chart.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
					chart.ChartAreas[0].AxisY.LabelStyle.Format = "#";
				}
				chart.Update();
			});
		}

		private void AddChartSeries(Series series) {
			Invoke((MethodInvoker)delegate {
				if (chart.Series.IsUniqueName(series.Name)) {
					chart.Series.Add(series);
					chart.Visible = true;
					chart.Update();
				}
			});
		}

		private void UpdateChartOptions(bool fullSet = true) {
			//if fullSet = false, the only option should be manually chosen map(s)
			if (fullSet) {
				Invoke((MethodInvoker)delegate {
					chartedMapChoice.Items.Clear();
					foreach (Beatmap map in _displayedMapset.Beatmaps) {
						chartedMapChoice.Items.Add(map.Version);
					}
					chartedMapChoice.SelectedIndex = 0;
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
				SetTime1($"0 ms");
				Console.WriteLine("set built");
				MapsetManager.Clear();
				if (set.Beatmaps.Count == 0) {
					Invoke((MethodInvoker)ClearBeatmapDisplay);
					return;
				}

				var sw = Stopwatch.StartNew();
				foreach (var beatmap in set.Beatmaps) {
					MapsetManager.AnalyzeMap(beatmap);
					MapsetManager.SaveMap(beatmap);
				}
				sw.Stop();
				SetTime2($"{sw.ElapsedMilliseconds} ms");

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
				_autoWindowUpdater = Task.Run(() => AutoWindowUpdaterBegin(_autoWindowCancellation.Token, AutoWindowUpdaterTimeoutMs));
			}
		}

		private void StopAutoWindowUpdater() {
			if (_autoWindowUpdater is not null) {
				try { _autoWindowCancellation.Cancel(); } catch { }
				try { _autoWindowUpdater.GetAwaiter().GetResult(); } catch { }
				try { _autoWindowUpdater.Dispose(); } catch { }
			}
			_autoWindowUpdater = null;
		}

		private void AutoWindowUpdaterBegin(CancellationToken cancelToken, int timeoutMs) {
			try {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoWindowUpdaterThread";
				var sw = new Stopwatch();
				while (!cancelToken.IsCancellationRequested) {
					sw.Restart();
					if (!_pauseGUI)
						AutoWindowUpdaterThreadTick(cancelToken, timeoutMs);
					sw.Stop();
					cancelToken.ThrowIfCancellationRequested();
					Thread.Sleep(Math.Max(MinUpdateDelayMs, timeoutMs - (int)sw.ElapsedMilliseconds));
				}
				cancelToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException) { throw; }
			catch { }
		}

		private void AutoWindowUpdaterThreadTick(CancellationToken cancelToken, int timeoutMs) {
			//Console.Write("auto window  ");
			try {
				var t = new Thread(ts) {
					IsBackground = true,
					Priority = ThreadPriority.Lowest
				};
				t.Start();
				var joinTask = Task.Run(t.Join, cancelToken);
				Task.WaitAny(joinTask, Task.Delay(timeoutMs, cancelToken));
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

				var osuProcess = Finder.GetOsuProcess(_osuPid);
				string windowTitle =  osuProcess?.MainWindowTitle;
				_osuPid = osuProcess?.Id;

				//update visibility
				if (string.IsNullOrEmpty(windowTitle)) {
					Invoke((MethodInvoker)delegate {
						_isOsuPresent = false;
						_isInGame = false;
					});
				}
				else if (windowTitle.Contains("[")) {
					Invoke((MethodInvoker)delegate {
						Visible = false;
						_isVisible = false;
						_isOsuPresent = true;
						_isInGame = true;
						_inGameWindowTitle = windowTitle;
					});
				}
				else {
					Invoke((MethodInvoker)delegate {
						Visible = true;
						TopMost = true;
						_isVisible = true;
						_isOsuPresent = true;
						_isInGame = false;
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
				_autoBeatmapAnalyzer = Task.Run(() => AutoBeatmapAnalyzerBegin(_autoBeatmapCancellation.Token, AutoBeatmapAnalyzerTimeoutMs));
			}
		}

		private void StopAutoBeatmapAnalyzer() {
			if (_autoBeatmapAnalyzer is not null) {
				try { _autoBeatmapCancellation.Cancel(); } catch { }
				try { _autoBeatmapAnalyzer.GetAwaiter().GetResult(); } catch { }
				try { _autoBeatmapAnalyzer.Dispose(); } catch { }
			}
			_autoBeatmapAnalyzer = null;
		}

		private void AutoBeatmapAnalyzerBegin(CancellationToken cancelToken, int timeoutMs) {
			try {
				if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
					Thread.CurrentThread.Name = "AutoBeatmapAnalyzerThread";
				var sw = new Stopwatch();
				while (!cancelToken.IsCancellationRequested) {
					sw.Restart();
					if (!_pauseGUI)
						AutoBeatmapAnalyzerThreadTick(cancelToken, timeoutMs);
					sw.Stop();
					cancelToken.ThrowIfCancellationRequested();
					Thread.Sleep(Math.Max(MinUpdateDelayMs, timeoutMs - (int)sw.ElapsedMilliseconds));
				}
				cancelToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException) { throw; }
			catch { }
		}

		private void AutoBeatmapAnalyzerThreadTick(CancellationToken cancelToken, int timeoutMs) {
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
				Task.WaitAny(joinTask, Task.Delay(timeoutMs, cancelToken));
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
					var osuProcess = Finder.GetOsuProcess(_osuPid);
					_osuPid = osuProcess?.Id;
					_currentMapsetDirectory = _osuPid.HasValue ? Finder.GetOsuBeatmapDirectory(_osuPid) : null;
				}
				sw.Stop();
				SetTime1($"{sw.ElapsedMilliseconds} ms");

				if (_currentMapsetDirectory != _prevMapsetDirectory && Directory.Exists(_currentMapsetDirectory)) {
					//analyze the mapset
					Mapset set = MapsetManager.AnalyzeMapset(_currentMapsetDirectory, this);
					if (set is not null) {
						//show info on GUI
						Invoke((MethodInvoker)delegate {
							//display text results
							ClearBeatmapDisplay();
							DisplayMapset(set);
							//display graph results
							SeriesSelect_SelectedIndexChanged(null, null);
							_prevMapsetDirectory = _currentMapsetDirectory;
						});
					}
				}
			}
		}

#endregion

	}
}
