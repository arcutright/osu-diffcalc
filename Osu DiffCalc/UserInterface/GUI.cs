using Osu_DiffCalc.FileFinder;
using Osu_DiffCalc.FileProcessor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Osu_DiffCalc.UserInterface
{
    public partial class GUI : Form
    {
        const int LABEL_FONT_SIZE = 12;
        const int AUTO_BEATMAP_ANALYZER_FREQUENCY = 1000; //in ms
        const int AUTO_WINDOW_UPDATER_FREQUENCY = 1000; //in ms

        bool visible = true;
        bool osuPresent = false;
        bool ingame = false;
        string ingameWindowTitle = null;
        string prevMapsetDirectory = null, currentMapsetDirectory = null;
        //background event timers
        Task autoBeatmapAnalyzer;
        CancellationTokenSource autoBeatmapCancellation = new CancellationTokenSource();
        Task autoWindowUpdater;
        CancellationTokenSource autoWindowCancellation = new CancellationTokenSource();
        
        //display variables
        Beatmap chartedBeatmap;
        Mapset displayedMapset;
        bool pauseGUI = false;

        public GUI()
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "GUIThread";
            InitializeComponent();
        }

        public void GUI_Load(object sender, EventArgs eArgs)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "GUIThread";
            double xPadding = 0;
            int x = Screen.PrimaryScreen.Bounds.Right - Width - (int)(Screen.PrimaryScreen.Bounds.Width * xPadding + 0.5);
            int y = Screen.PrimaryScreen.Bounds.Y + (Screen.PrimaryScreen.Bounds.Height - Height) / 2;
            Location = new Point(x, y);
        }

        #region Public methods

        public void SetTime1(string timeString)
        {
            SetLabel(timeDisplay1, timeString);
        }

        public void SetTime2(string timeString)
        {
            SetLabel(timeDisplay2, timeString);
        }

        #endregion

        #region Controls

        private void ScaleRatings_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void OpenFromFile_Click(object sender, EventArgs e)
        {
            Task.Run(() => ManualBeatmapAnalyzer());
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            MapsetManager.clear();
            SavefileXMLManager.ClearXML();
        }
        
        //starts and stops all background threads
        protected override void OnResize(EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                StopAutoWindowUpdater();
                StopAutoBeatmapAnalyzer();
                visible = false;
                pauseGUI = true;
            }
            else
            {
                StartAutoWindowUpdater();
                StartAutoBeatmapAnalyzer();
                visible = true;
                pauseGUI = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                StopAutoBeatmapAnalyzer();
                StopAutoWindowUpdater();
            }
            base.OnFormClosing(e);
        }

        private void AutoBeatmapCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoBeatmapCheckbox.Checked)
                StartAutoBeatmapAnalyzer();
            else
                StopAutoBeatmapAnalyzer();
        }

        private void SeriesSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chartedBeatmap != null)
            {
                Invoke((MethodInvoker)delegate { pauseGUI = true; });
                List<string> selectedItemsText = new List<string>();
                foreach (ListViewItem sel in seriesSelect.SelectedItems)
                {
                    sel.Checked = true;
                    sel.Selected = false;
                }
                foreach (ListViewItem sel in seriesSelect.CheckedItems)
                {
                    if (!selectedItemsText.Contains(sel.Text))
                        selectedItemsText.Add(sel.Text);
                    sel.Selected = false;
                }
                ClearChart();
                Action<string> AddToChart = (string text) =>
                {
                    if (text == "Streams")
                        AddChartSeries(chartedBeatmap.diffRating.streams);
                    else if (text == "Bursts")
                        AddChartSeries(chartedBeatmap.diffRating.bursts);
                    else if (text == "Couplets")
                        AddChartSeries(chartedBeatmap.diffRating.couplets);
                    else if (text == "Sliders")
                        AddChartSeries(chartedBeatmap.diffRating.sliders);
                    else if (text == "Jumps")
                        AddChartSeries(chartedBeatmap.diffRating.jumps);
                };
                foreach (string sel in selectedItemsText)
                {
                    AddToChart(sel);
                }
            }
            Invoke((MethodInvoker)delegate { pauseGUI = false; });
        }

        private void ChartedMapChoice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (displayedMapset != null)
            {
                Invoke((MethodInvoker)delegate { pauseGUI = false; });
                string choice = chartedMapChoice.SelectedItem.ToString();
                IEnumerable<Beatmap> find = displayedMapset.beatmaps.Where(map => map.version == choice);
                if (find != null && find.Count() > 0)
                    chartedBeatmap = find.First();
                SeriesSelect_SelectedIndexChanged(null, null);
            }
        }

        private void ChartedMapChoice_DropDown(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate{ pauseGUI = true; });
        }

        #endregion

        #region Private Helpers

        private void ClearBeatmapDisplay()
        {
            difficultyDisplayPanel.Controls.Clear();
        }

        private void AddBeatmapToDisplay(Beatmap beatmap)
        {
            Label diff;
            if (scaleRatings.Checked)
                diff = MakeLabel(beatmap.getFamiliarizedDisplayString(), LABEL_FONT_SIZE, beatmap.getFamiliarizedDetailString());
            else
                diff = MakeLabel(beatmap.getDiffDisplayString(), LABEL_FONT_SIZE, beatmap.getDiffDetailString());
            difficultyDisplayPanel.Controls.Add(diff);
            difficultyDisplayPanel.SetFlowBreak(diff, true);
        }

        private void DisplayMapset(Mapset set)
        {
            if (set.beatmaps.Count > 0)
            {
                //sort by difficulty
                set.sort(false);
                //display all maps
                foreach (Beatmap map in set.beatmaps)
                {
                    AddBeatmapToDisplay(map);
                }
                displayedMapset = set;
                chartedBeatmap = set.beatmaps.First();
                UpdateChartOptions(true);
            }
        }

        private Label MakeLabel(string text, int fontSize, string toolTipStr="")
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font(label.Font.FontFamily, fontSize);
            label.AutoSize = true;
            if (!toolTipStr.Equals(""))
            {
                ToolTip tip = new ToolTip();
                tip.ShowAlways = true;
                tip.SetToolTip(label, toolTipStr);
            }
            return label;
        }

        private void SetLabel(Label label, string labelString)
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    label.Text = labelString;
                    label.AutoSize = true;
                    label.Font = new Font(label.Font.FontFamily, 9);
                });
            }
            catch
            { }
        }

        public void AddChartPoint(double x, double y)
        {
            Invoke((MethodInvoker)delegate
            {
                Series last = chart.Series.Last();
                if (last == null)
                {
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

        private void ClearChart()
        {
            Invoke((MethodInvoker)delegate
            {
                chart.Series.Clear();
                if (chart.ChartAreas.Count > 0)
                {
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

        private void AddChartSeries(Series series)
        {
            Invoke((MethodInvoker)delegate
            {
                if (chart.Series.IsUniqueName(series.Name))
                {
                    chart.Series.Add(series);
                    chart.Visible = true;
                    chart.Update();
                }
            });
        }

        private void UpdateChartOptions(bool fullSet = true)
        {
            //if fullSet = false, the only option should be manually chosen map(s)
            if (fullSet)
            {
                Invoke((MethodInvoker)delegate
                {
                    chartedMapChoice.Items.Clear();
                    foreach (Beatmap map in displayedMapset.beatmaps)
                    {
                        chartedMapChoice.Items.Add(map.version);
                    }
                    chartedMapChoice.SelectedIndex = 0;
                });
            }
            else
            {
                Invoke((MethodInvoker)delegate
                {
                    chartedMapChoice.Items.Clear();
                    chartedMapChoice.Items.Add(chartedBeatmap.version);
                    chartedMapChoice.SelectedIndex = 0;
                });
            }
        }

        #endregion

        #region Manual Beatmap Analyzer

        private void ManualBeatmapAnalyzer()
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "ManualBeatmapAnalyzerWorkerThread";
            try
            {
                string filename = UX.getFilenameFromDialog(this);
                if (filename != null)
                {
                    Beatmap beatmap = new Beatmap(filename);
                    MapsetManager.analyzeMap(beatmap, true);
                    MapsetManager.saveMap(beatmap);

                    Invoke((MethodInvoker)delegate
                    {
                        ClearBeatmapDisplay();
                        //display text results
                        AddBeatmapToDisplay(beatmap);
                        //display graph results
                        chartedBeatmap = beatmap;
                        SeriesSelect_SelectedIndexChanged(null, null);
                        UpdateChartOptions(false);
                    });
                }
            }
            catch { }
        }
        #endregion

        #region Automatic Window Updater - auto show/hide window

        private void StartAutoWindowUpdater()
        {
            if (autoWindowUpdater == null)
            {
                if (autoWindowCancellation.IsCancellationRequested)
                    autoWindowCancellation = new CancellationTokenSource();
                autoWindowUpdater = Task.Run(() => AutoWindowUpdaterBegin(autoWindowCancellation.Token, AUTO_WINDOW_UPDATER_FREQUENCY));
            }
        }

        private void StopAutoWindowUpdater()
        {
            if (autoWindowUpdater != null)
            {
                autoWindowCancellation.Cancel();
                autoWindowUpdater.Wait();
                autoWindowUpdater.Dispose();
            }
            autoWindowUpdater = null;
        }

        private void AutoWindowUpdaterBegin(CancellationToken cancelToken, int timeout)
        {
            try
            {
                if (Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "AutoWindowUpdaterThread";
                Stopwatch watch = new Stopwatch();

                while (!cancelToken.IsCancellationRequested)
                {
                    watch.Restart();
                    if(!pauseGUI)
                        AutoWindowUpdaterThreadTick(timeout);
                    cancelToken.ThrowIfCancellationRequested();
                    watch.Stop();
                    int wait = timeout - (int)watch.ElapsedMilliseconds;
                    Thread.Sleep(wait > 0 ? wait : 0);
                }
                cancelToken.ThrowIfCancellationRequested();
            }
            catch { }
        }

        private void AutoWindowUpdaterThreadTick(int timeout)
        {
            //Console.Write("auto window  ");

            ThreadStart ts = delegate
            {
                //the try-catch is needed for cancellation/disposal
                try
                {
                    if (Thread.CurrentThread.Name == null)
                        Thread.CurrentThread.Name = "AutoWindowTickThread";
                    #region Tick
                    string windowTitle = Finder.GetWindowTitle("osu!");
                    //update visibility
                    if (windowTitle.Contains("["))
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            Visible = false;
                            visible = false;
                            osuPresent = true;
                            ingame = true;
                            ingameWindowTitle = windowTitle;
                        });
                    }
                    else if (windowTitle.Length > 0)
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            Visible = true;
                            TopMost = true;
                            visible = true;
                            osuPresent = true;
                            ingame = false;
                        });
                    }
                    else
                        Invoke((MethodInvoker)delegate
                        {
                            osuPresent = false;
                            ingame = false;
                        });
                    #endregion
                }
                catch { }
            };

            try
            {
                Thread t = new Thread(ts);
                t.IsBackground = true;
                t.Priority = ThreadPriority.Lowest;
                t.Start();
                if (!t.Join(timeout))
                {
                    try
                    {
                        t.Interrupt();
                        t.Abort();
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        #endregion

        #region Automatic Beatmap Analyzer - polls handles, gets beatmaps, analyzes, updates display
        
        private void StartAutoBeatmapAnalyzer()
        {
            if (autoBeatmapAnalyzer == null)
            {
                if(autoBeatmapCancellation.IsCancellationRequested)
                    autoBeatmapCancellation = new CancellationTokenSource();
                autoBeatmapAnalyzer = Task.Run(() => AutoBeatmapAnalyzerBegin(autoBeatmapCancellation.Token, AUTO_BEATMAP_ANALYZER_FREQUENCY));
            }
        }

        private void StopAutoBeatmapAnalyzer()
        {
            if (autoBeatmapAnalyzer != null)
            {
                autoBeatmapCancellation.Cancel();
                autoBeatmapAnalyzer.Wait();
                autoBeatmapAnalyzer.Dispose();
            }
            autoBeatmapAnalyzer = null;
        }

        private void AutoBeatmapAnalyzerBegin(CancellationToken cancelToken, int timeout)
        {
            try
            {
                if (Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "AutoBeatmapAnalyzerThread";
                Stopwatch watch = new Stopwatch();

                while (!cancelToken.IsCancellationRequested)
                {
                    watch.Restart();
                    if(!pauseGUI)
                        AutoBeatmapAnalyzerThreadTick(timeout);
                    cancelToken.ThrowIfCancellationRequested();
                    watch.Stop();
                    int wait = timeout - (int)watch.ElapsedMilliseconds;
                    Thread.Sleep(wait > 0 ? wait : 0);
                }
                cancelToken.ThrowIfCancellationRequested();
            }
            catch { }
        }

        private void AutoBeatmapAnalyzerThreadTick(int timeout)
        {
            bool useWindowTitleForDirectory = false;
            ThreadStart ts = delegate
            {
                try
                {
                    if (Thread.CurrentThread.Name == null)
                        Thread.CurrentThread.Name = "AutoBeatmapTickThread";
                    #region Tick
                    //timing
                    var localwatch = Stopwatch.StartNew();
                    //the try-catch is needed for cancellation/disposal
                    try
                    {
                        if (osuPresent && visible)
                        {
                            localwatch.Restart();
                            if(useWindowTitleForDirectory)
                            {
                                currentMapsetDirectory = MapsetManager.getCurrentMapsetDirectory(ingameWindowTitle, prevMapsetDirectory);
                                useWindowTitleForDirectory = false;
                            }
                            else
                                currentMapsetDirectory = MapsetManager.getCurrentMapsetDirectory();   //native calls lie ahead
                            localwatch.Stop();
                            SetTime1(string.Format("{0} ms", localwatch.ElapsedMilliseconds));

                            if (currentMapsetDirectory != prevMapsetDirectory &&
                                System.IO.Directory.Exists(currentMapsetDirectory))
                            {
                                //analyze the mapset
                                Mapset set = MapsetManager.analyzeMapset(currentMapsetDirectory, this);
                                if (set != null)
                                {
                                    //show info on GUI
                                    Invoke((MethodInvoker)delegate
                                    {
                                        //display text results
                                        ClearBeatmapDisplay();
                                        DisplayMapset(set);
                                        //display graph results
                                        SeriesSelect_SelectedIndexChanged(null, null);
                                        prevMapsetDirectory = currentMapsetDirectory;
                                    });
                                }
                            }
                        }
                        else if(ingame)
                        {
                            useWindowTitleForDirectory = true;
                        }
                    }
                    catch { }
                    #endregion
                }
                catch { }
            };
            
            try
            {
                Thread t = new Thread(ts);
                t.IsBackground = true;
                t.Priority = ThreadPriority.BelowNormal;
                t.Start();
                if (!t.Join(timeout))
                {
                    try
                    {
                        t.Interrupt();
                        t.Abort();
                    }
                    catch { }
                }
            }
            catch { }
        }

        #endregion

    }
}
