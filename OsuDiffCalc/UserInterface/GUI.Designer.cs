namespace OsuDiffCalc.UserInterface {
	partial class GUI {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
			System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
			this.openFromFile = new System.Windows.Forms.Button();
			this.scaleRatings = new System.Windows.Forms.CheckBox();
			this.difficultyDisplayPanel = new System.Windows.Forms.TableLayoutPanel();
			this.timeDisplay2 = new System.Windows.Forms.Label();
			this.timeDescriptionLabel = new System.Windows.Forms.Label();
			this.clearButton = new System.Windows.Forms.Button();
			this.timeDisplay1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.AutoBeatmapCheckbox = new System.Windows.Forms.CheckBox();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.resultsTab = new System.Windows.Forms.TabPage();
			this.PrimaryResultsPanel = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.openFromFilePanel = new System.Windows.Forms.Panel();
			this.autoBeatmapCheckboxPanel = new System.Windows.Forms.Panel();
			this.timeAndClearPanel = new System.Windows.Forms.Panel();
			this.EnableXmlCheckbox = new System.Windows.Forms.CheckBox();
			this.AlwaysOnTopCheckbox = new System.Windows.Forms.CheckBox();
			this.chartsTab = new System.Windows.Forms.TabPage();
			this.panel1 = new System.Windows.Forms.Panel();
			this.chartCheckboxPanel = new System.Windows.Forms.Panel();
			this.ChartStyleDropdown = new System.Windows.Forms.ComboBox();
			this.chartVersionPanel = new System.Windows.Forms.Panel();
			this.ChartedMapDropdown = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.panel6 = new System.Windows.Forms.Panel();
			this.Chart = new OsuDiffCalc.UserInterface.Controls.CustomChart();
			this.panel5 = new System.Windows.Forms.Panel();
			this.BurstBpmLabel = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.StreamBpmLabel = new System.Windows.Forms.Label();
			this.ChartLegendPanel = new System.Windows.Forms.TableLayoutPanel();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.settingsTab = new System.Windows.Forms.TabPage();
			this.label6 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.Settings_UpdateIntervalOsuNotFoundTextbox = new OsuDiffCalc.UserInterface.Controls.IntTextBox();
			this.Settings_StarTargetMinTextbox = new OsuDiffCalc.UserInterface.Controls.DoubleTextBox();
			this.Settings_StarTargetMaxTextbox = new OsuDiffCalc.UserInterface.Controls.DoubleTextBox();
			this.Settings_UpdateIntervalNormalTextbox = new OsuDiffCalc.UserInterface.Controls.IntTextBox();
			this.Settings_UpdateIntervalMinimizedTextbox = new OsuDiffCalc.UserInterface.Controls.IntTextBox();
			this.MainWindowPanel = new System.Windows.Forms.Panel();
			this.panel3 = new System.Windows.Forms.Panel();
			this.StatusStripLabel = new System.Windows.Forms.Label();
			this.panel4 = new System.Windows.Forms.Panel();
			this.MainTabControl.SuspendLayout();
			this.resultsTab.SuspendLayout();
			this.PrimaryResultsPanel.SuspendLayout();
			this.panel2.SuspendLayout();
			this.openFromFilePanel.SuspendLayout();
			this.autoBeatmapCheckboxPanel.SuspendLayout();
			this.timeAndClearPanel.SuspendLayout();
			this.chartsTab.SuspendLayout();
			this.panel1.SuspendLayout();
			this.chartCheckboxPanel.SuspendLayout();
			this.chartVersionPanel.SuspendLayout();
			this.panel6.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.Chart)).BeginInit();
			this.panel5.SuspendLayout();
			this.ChartLegendPanel.SuspendLayout();
			this.settingsTab.SuspendLayout();
			this.MainWindowPanel.SuspendLayout();
			this.panel3.SuspendLayout();
			this.panel4.SuspendLayout();
			this.SuspendLayout();
			// 
			// openFromFile
			// 
			this.openFromFile.BackColor = System.Drawing.SystemColors.Control;
			this.openFromFile.Location = new System.Drawing.Point(-1, 1);
			this.openFromFile.Name = "openFromFile";
			this.openFromFile.Size = new System.Drawing.Size(94, 23);
			this.openFromFile.TabIndex = 3;
			this.openFromFile.Text = "Open .osu files";
			this.openFromFile.UseVisualStyleBackColor = false;
			this.openFromFile.Click += new System.EventHandler(this.OpenFromFile_Click);
			// 
			// scaleRatings
			// 
			this.scaleRatings.AutoSize = true;
			this.scaleRatings.Checked = true;
			this.scaleRatings.CheckState = System.Windows.Forms.CheckState.Checked;
			this.scaleRatings.Location = new System.Drawing.Point(100, 5);
			this.scaleRatings.Name = "scaleRatings";
			this.scaleRatings.Size = new System.Drawing.Size(118, 17);
			this.scaleRatings.TabIndex = 4;
			this.scaleRatings.Text = "Familiar rating scale";
			this.scaleRatings.UseVisualStyleBackColor = true;
			this.scaleRatings.CheckedChanged += new System.EventHandler(this.ScaleRatings_CheckedChanged);
			// 
			// difficultyDisplayPanel
			// 
			this.difficultyDisplayPanel.AutoScroll = true;
			this.difficultyDisplayPanel.BackColor = System.Drawing.Color.Gainsboro;
			this.difficultyDisplayPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.difficultyDisplayPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.difficultyDisplayPanel.Font = new System.Drawing.Font("Consolas", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.difficultyDisplayPanel.Location = new System.Drawing.Point(0, 0);
			this.difficultyDisplayPanel.Name = "difficultyDisplayPanel";
			this.difficultyDisplayPanel.Size = new System.Drawing.Size(314, 188);
			this.difficultyDisplayPanel.TabIndex = 7;
			// 
			// timeDisplay2
			// 
			this.timeDisplay2.BackColor = System.Drawing.Color.Gainsboro;
			this.timeDisplay2.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.timeDisplay2.Location = new System.Drawing.Point(34, 56);
			this.timeDisplay2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.timeDisplay2.Name = "timeDisplay2";
			this.timeDisplay2.Size = new System.Drawing.Size(51, 19);
			this.timeDisplay2.TabIndex = 11;
			this.timeDisplay2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// timeDescriptionLabel
			// 
			this.timeDescriptionLabel.AutoSize = true;
			this.timeDescriptionLabel.Location = new System.Drawing.Point(7, 42);
			this.timeDescriptionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.timeDescriptionLabel.Name = "timeDescriptionLabel";
			this.timeDescriptionLabel.Size = new System.Drawing.Size(80, 13);
			this.timeDescriptionLabel.TabIndex = 10;
			this.timeDescriptionLabel.Text = "Parse+Analyze:";
			// 
			// clearButton
			// 
			this.clearButton.BackColor = System.Drawing.SystemColors.Control;
			this.clearButton.Location = new System.Drawing.Point(10, 80);
			this.clearButton.Margin = new System.Windows.Forms.Padding(2);
			this.clearButton.Name = "clearButton";
			this.clearButton.Size = new System.Drawing.Size(77, 23);
			this.clearButton.TabIndex = 0;
			this.clearButton.Text = "Clear cache";
			this.clearButton.UseVisualStyleBackColor = false;
			this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);
			// 
			// timeDisplay1
			// 
			this.timeDisplay1.BackColor = System.Drawing.Color.Gainsboro;
			this.timeDisplay1.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.timeDisplay1.Location = new System.Drawing.Point(31, 20);
			this.timeDisplay1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.timeDisplay1.Name = "timeDisplay1";
			this.timeDisplay1.Size = new System.Drawing.Size(54, 19);
			this.timeDisplay1.TabIndex = 12;
			this.timeDisplay1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(5, 5);
			this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(83, 13);
			this.label2.TabIndex = 13;
			this.label2.Text = "getSetDirectory:";
			// 
			// AutoBeatmapCheckbox
			// 
			this.AutoBeatmapCheckbox.AutoSize = true;
			this.AutoBeatmapCheckbox.Checked = true;
			this.AutoBeatmapCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.AutoBeatmapCheckbox.Location = new System.Drawing.Point(1, 2);
			this.AutoBeatmapCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.AutoBeatmapCheckbox.Name = "AutoBeatmapCheckbox";
			this.AutoBeatmapCheckbox.Size = new System.Drawing.Size(92, 17);
			this.AutoBeatmapCheckbox.TabIndex = 14;
			this.AutoBeatmapCheckbox.Text = "Auto beatmap";
			this.AutoBeatmapCheckbox.UseVisualStyleBackColor = true;
			this.AutoBeatmapCheckbox.CheckedChanged += new System.EventHandler(this.AutoBeatmapCheckbox_CheckedChanged);
			// 
			// MainTabControl
			// 
			this.MainTabControl.Controls.Add(this.resultsTab);
			this.MainTabControl.Controls.Add(this.chartsTab);
			this.MainTabControl.Controls.Add(this.settingsTab);
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.Location = new System.Drawing.Point(0, 0);
			this.MainTabControl.Margin = new System.Windows.Forms.Padding(2);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(410, 237);
			this.MainTabControl.TabIndex = 15;
			// 
			// resultsTab
			// 
			this.resultsTab.BackColor = System.Drawing.Color.Silver;
			this.resultsTab.Controls.Add(this.PrimaryResultsPanel);
			this.resultsTab.Controls.Add(this.autoBeatmapCheckboxPanel);
			this.resultsTab.Location = new System.Drawing.Point(4, 22);
			this.resultsTab.Margin = new System.Windows.Forms.Padding(0);
			this.resultsTab.Name = "resultsTab";
			this.resultsTab.Size = new System.Drawing.Size(402, 211);
			this.resultsTab.TabIndex = 0;
			this.resultsTab.Text = "Results";
			// 
			// PrimaryResultsPanel
			// 
			this.PrimaryResultsPanel.Controls.Add(this.panel2);
			this.PrimaryResultsPanel.Controls.Add(this.openFromFilePanel);
			this.PrimaryResultsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PrimaryResultsPanel.Location = new System.Drawing.Point(0, 0);
			this.PrimaryResultsPanel.Name = "PrimaryResultsPanel";
			this.PrimaryResultsPanel.Size = new System.Drawing.Size(314, 211);
			this.PrimaryResultsPanel.TabIndex = 18;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.difficultyDisplayPanel);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(314, 188);
			this.panel2.TabIndex = 9;
			// 
			// openFromFilePanel
			// 
			this.openFromFilePanel.Controls.Add(this.openFromFile);
			this.openFromFilePanel.Controls.Add(this.scaleRatings);
			this.openFromFilePanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.openFromFilePanel.Location = new System.Drawing.Point(0, 188);
			this.openFromFilePanel.Name = "openFromFilePanel";
			this.openFromFilePanel.Size = new System.Drawing.Size(314, 23);
			this.openFromFilePanel.TabIndex = 8;
			// 
			// autoBeatmapCheckboxPanel
			// 
			this.autoBeatmapCheckboxPanel.Controls.Add(this.timeAndClearPanel);
			this.autoBeatmapCheckboxPanel.Controls.Add(this.EnableXmlCheckbox);
			this.autoBeatmapCheckboxPanel.Controls.Add(this.AlwaysOnTopCheckbox);
			this.autoBeatmapCheckboxPanel.Controls.Add(this.AutoBeatmapCheckbox);
			this.autoBeatmapCheckboxPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.autoBeatmapCheckboxPanel.Location = new System.Drawing.Point(314, 0);
			this.autoBeatmapCheckboxPanel.Name = "autoBeatmapCheckboxPanel";
			this.autoBeatmapCheckboxPanel.Size = new System.Drawing.Size(88, 211);
			this.autoBeatmapCheckboxPanel.TabIndex = 17;
			// 
			// timeAndClearPanel
			// 
			this.timeAndClearPanel.Controls.Add(this.timeDisplay1);
			this.timeAndClearPanel.Controls.Add(this.timeDisplay2);
			this.timeAndClearPanel.Controls.Add(this.clearButton);
			this.timeAndClearPanel.Controls.Add(this.timeDescriptionLabel);
			this.timeAndClearPanel.Controls.Add(this.label2);
			this.timeAndClearPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.timeAndClearPanel.Location = new System.Drawing.Point(0, 107);
			this.timeAndClearPanel.Name = "timeAndClearPanel";
			this.timeAndClearPanel.Size = new System.Drawing.Size(88, 104);
			this.timeAndClearPanel.TabIndex = 17;
			// 
			// EnableXmlCheckbox
			// 
			this.EnableXmlCheckbox.AutoSize = true;
			this.EnableXmlCheckbox.Location = new System.Drawing.Point(1, 36);
			this.EnableXmlCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.EnableXmlCheckbox.Name = "EnableXmlCheckbox";
			this.EnableXmlCheckbox.Size = new System.Drawing.Size(84, 17);
			this.EnableXmlCheckbox.TabIndex = 16;
			this.EnableXmlCheckbox.Text = "Enable XML";
			this.EnableXmlCheckbox.UseVisualStyleBackColor = true;
			this.EnableXmlCheckbox.CheckedChanged += new System.EventHandler(this.EnableXmlCheckbox_CheckedChanged);
			// 
			// AlwaysOnTopCheckbox
			// 
			this.AlwaysOnTopCheckbox.AutoSize = true;
			this.AlwaysOnTopCheckbox.Checked = true;
			this.AlwaysOnTopCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.AlwaysOnTopCheckbox.Location = new System.Drawing.Point(1, 18);
			this.AlwaysOnTopCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.AlwaysOnTopCheckbox.Name = "AlwaysOnTopCheckbox";
			this.AlwaysOnTopCheckbox.Size = new System.Drawing.Size(92, 17);
			this.AlwaysOnTopCheckbox.TabIndex = 15;
			this.AlwaysOnTopCheckbox.Text = "Always on top";
			this.AlwaysOnTopCheckbox.UseVisualStyleBackColor = true;
			this.AlwaysOnTopCheckbox.CheckedChanged += new System.EventHandler(this.AlwaysOnTop_CheckedChanged);
			// 
			// chartsTab
			// 
			this.chartsTab.BackColor = System.Drawing.Color.Silver;
			this.chartsTab.Controls.Add(this.panel1);
			this.chartsTab.Location = new System.Drawing.Point(4, 22);
			this.chartsTab.Margin = new System.Windows.Forms.Padding(0);
			this.chartsTab.Name = "chartsTab";
			this.chartsTab.Size = new System.Drawing.Size(402, 211);
			this.chartsTab.TabIndex = 1;
			this.chartsTab.Text = "Charts";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.chartCheckboxPanel);
			this.panel1.Controls.Add(this.chartVersionPanel);
			this.panel1.Controls.Add(this.panel6);
			this.panel1.Controls.Add(this.panel5);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(402, 211);
			this.panel1.TabIndex = 17;
			// 
			// chartCheckboxPanel
			// 
			this.chartCheckboxPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.chartCheckboxPanel.BackColor = System.Drawing.Color.Transparent;
			this.chartCheckboxPanel.Controls.Add(this.ChartStyleDropdown);
			this.chartCheckboxPanel.Location = new System.Drawing.Point(301, 164);
			this.chartCheckboxPanel.Name = "chartCheckboxPanel";
			this.chartCheckboxPanel.Size = new System.Drawing.Size(101, 27);
			this.chartCheckboxPanel.TabIndex = 15;
			// 
			// ChartStyleDropdown
			// 
			this.ChartStyleDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ChartStyleDropdown.FormattingEnabled = true;
			this.ChartStyleDropdown.Location = new System.Drawing.Point(3, 5);
			this.ChartStyleDropdown.Name = "ChartStyleDropdown";
			this.ChartStyleDropdown.Size = new System.Drawing.Size(98, 21);
			this.ChartStyleDropdown.TabIndex = 4;
			this.ChartStyleDropdown.SelectedIndexChanged += new System.EventHandler(this.ChartStyleDropdown_SelectedIndexChanged);
			// 
			// chartVersionPanel
			// 
			this.chartVersionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.chartVersionPanel.Controls.Add(this.ChartedMapDropdown);
			this.chartVersionPanel.Controls.Add(this.label1);
			this.chartVersionPanel.Location = new System.Drawing.Point(160, 189);
			this.chartVersionPanel.Name = "chartVersionPanel";
			this.chartVersionPanel.Size = new System.Drawing.Size(242, 20);
			this.chartVersionPanel.TabIndex = 16;
			// 
			// ChartedMapDropdown
			// 
			this.ChartedMapDropdown.BackColor = System.Drawing.SystemColors.Window;
			this.ChartedMapDropdown.CausesValidation = false;
			this.ChartedMapDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ChartedMapDropdown.FormattingEnabled = true;
			this.ChartedMapDropdown.Items.AddRange(new object[] {
            "test",
            "test2",
            "test3"});
			this.ChartedMapDropdown.Location = new System.Drawing.Point(46, 1);
			this.ChartedMapDropdown.Margin = new System.Windows.Forms.Padding(2);
			this.ChartedMapDropdown.Name = "ChartedMapDropdown";
			this.ChartedMapDropdown.Size = new System.Drawing.Size(196, 21);
			this.ChartedMapDropdown.TabIndex = 2;
			this.ChartedMapDropdown.SelectedValueChanged += new System.EventHandler(this.ChartedMapDropdown_SelectedValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(2, 5);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(45, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Version:";
			// 
			// panel6
			// 
			this.panel6.Controls.Add(this.Chart);
			this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel6.Location = new System.Drawing.Point(0, 0);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(308, 211);
			this.panel6.TabIndex = 19;
			// 
			// Chart
			// 
			this.Chart.BackColor = System.Drawing.Color.Silver;
			this.Chart.BorderlineColor = System.Drawing.Color.LightGray;
			chartArea1.AxisX.LabelStyle.Format = "#";
			chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.DimGray;
			chartArea1.AxisX.Title = "X=Time, Y=Diff";
			chartArea1.AxisX.TitleAlignment = System.Drawing.StringAlignment.Near;
			chartArea1.AxisY.LabelAutoFitStyle = ((System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles)(((System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.IncreaseFont | System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.StaggeredLabels) 
            | System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.WordWrap)));
			chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.DimGray;
			chartArea1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
			chartArea1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
			chartArea1.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
			chartArea1.Name = "ChartArea";
			chartArea1.ShadowColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
			this.Chart.ChartAreas.Add(chartArea1);
			this.Chart.Dock = System.Windows.Forms.DockStyle.Fill;
			legend1.BackColor = System.Drawing.Color.Gainsboro;
			legend1.Enabled = false;
			legend1.IsDockedInsideChartArea = false;
			legend1.Name = "Legend1";
			legend1.TableStyle = System.Windows.Forms.DataVisualization.Charting.LegendTableStyle.Tall;
			this.Chart.Legends.Add(legend1);
			this.Chart.Location = new System.Drawing.Point(-13, -4);
			this.Chart.Margin = new System.Windows.Forms.Padding(0);
			this.Chart.Name = "Chart";
			this.Chart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Pastel;
			series1.ChartArea = "ChartArea";
			series1.Legend = "Legend1";
			series1.Name = "Series1";
			series1.YValuesPerPoint = 6;
			this.Chart.Series.Add(series1);
			this.Chart.Size = new System.Drawing.Size(322, 217);
			this.Chart.TabIndex = 0;
			this.Chart.Text = "Chart";
			// 
			// panel5
			// 
			this.panel5.Controls.Add(this.BurstBpmLabel);
			this.panel5.Controls.Add(this.label14);
			this.panel5.Controls.Add(this.StreamBpmLabel);
			this.panel5.Controls.Add(this.ChartLegendPanel);
			this.panel5.Controls.Add(this.label7);
			this.panel5.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel5.Location = new System.Drawing.Point(308, 0);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(94, 211);
			this.panel5.TabIndex = 18;
			// 
			// BurstBpmLabel
			// 
			this.BurstBpmLabel.BackColor = System.Drawing.Color.WhiteSmoke;
			this.BurstBpmLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.BurstBpmLabel.Location = new System.Drawing.Point(67, 22);
			this.BurstBpmLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.BurstBpmLabel.Name = "BurstBpmLabel";
			this.BurstBpmLabel.Size = new System.Drawing.Size(25, 18);
			this.BurstBpmLabel.TabIndex = 18;
			this.BurstBpmLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(10, 24);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(60, 13);
			this.label14.TabIndex = 19;
			this.label14.Text = "Burst BPM:";
			// 
			// StreamBpmLabel
			// 
			this.StreamBpmLabel.BackColor = System.Drawing.Color.WhiteSmoke;
			this.StreamBpmLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.StreamBpmLabel.Location = new System.Drawing.Point(67, 2);
			this.StreamBpmLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.StreamBpmLabel.Name = "StreamBpmLabel";
			this.StreamBpmLabel.Size = new System.Drawing.Size(25, 18);
			this.StreamBpmLabel.TabIndex = 12;
			this.StreamBpmLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// ChartLegendPanel
			// 
			this.ChartLegendPanel.AutoSize = true;
			this.ChartLegendPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ChartLegendPanel.BackColor = System.Drawing.Color.Gainsboro;
			this.ChartLegendPanel.ColumnCount = 2;
			this.ChartLegendPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
			this.ChartLegendPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ChartLegendPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.ChartLegendPanel.Controls.Add(this.label8, 1, 2);
			this.ChartLegendPanel.Controls.Add(this.label9, 1, 1);
			this.ChartLegendPanel.Controls.Add(this.label10, 1, 3);
			this.ChartLegendPanel.Controls.Add(this.label11, 1, 4);
			this.ChartLegendPanel.Controls.Add(this.label12, 1, 5);
			this.ChartLegendPanel.Location = new System.Drawing.Point(0, 42);
			this.ChartLegendPanel.Name = "ChartLegendPanel";
			this.ChartLegendPanel.Padding = new System.Windows.Forms.Padding(5, 3, 8, 3);
			this.ChartLegendPanel.RowCount = 6;
			this.ChartLegendPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ChartLegendPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.ChartLegendPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.ChartLegendPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.ChartLegendPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.ChartLegendPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.ChartLegendPanel.Size = new System.Drawing.Size(89, 106);
			this.ChartLegendPanel.TabIndex = 17;
			// 
			// label8
			// 
			this.label8.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(32, 26);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(45, 13);
			this.label8.TabIndex = 0;
			this.label8.Text = "Streams";
			// 
			// label9
			// 
			this.label9.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(32, 6);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(37, 13);
			this.label9.TabIndex = 1;
			this.label9.Text = "Jumps";
			// 
			// label10
			// 
			this.label10.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(32, 46);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(36, 13);
			this.label10.TabIndex = 2;
			this.label10.Text = "Bursts";
			// 
			// label11
			// 
			this.label11.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(32, 66);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(46, 13);
			this.label11.TabIndex = 3;
			this.label11.Text = "Doubles";
			// 
			// label12
			// 
			this.label12.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(32, 86);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(38, 13);
			this.label12.TabIndex = 4;
			this.label12.Text = "Sliders";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(1, 4);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(69, 13);
			this.label7.TabIndex = 13;
			this.label7.Text = "Stream BPM:";
			// 
			// settingsTab
			// 
			this.settingsTab.BackColor = System.Drawing.Color.Silver;
			this.settingsTab.Controls.Add(this.label6);
			this.settingsTab.Controls.Add(this.label3);
			this.settingsTab.Controls.Add(this.label4);
			this.settingsTab.Controls.Add(this.label5);
			this.settingsTab.Controls.Add(this.Settings_UpdateIntervalOsuNotFoundTextbox);
			this.settingsTab.Controls.Add(this.Settings_StarTargetMinTextbox);
			this.settingsTab.Controls.Add(this.Settings_StarTargetMaxTextbox);
			this.settingsTab.Controls.Add(this.Settings_UpdateIntervalNormalTextbox);
			this.settingsTab.Controls.Add(this.Settings_UpdateIntervalMinimizedTextbox);
			this.settingsTab.Location = new System.Drawing.Point(4, 22);
			this.settingsTab.Name = "settingsTab";
			this.settingsTab.Padding = new System.Windows.Forms.Padding(3);
			this.settingsTab.Size = new System.Drawing.Size(402, 211);
			this.settingsTab.TabIndex = 2;
			this.settingsTab.Text = "Settings";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(9, 88);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(175, 13);
			this.label6.TabIndex = 7;
			this.label6.Text = "Update interval, osu! not found (ms)";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 14);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(153, 13);
			this.label3.TabIndex = 1;
			this.label3.Text = "Chart default friendly star range";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 40);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(138, 13);
			this.label4.TabIndex = 3;
			this.label4.Text = "Update interval, normal (ms)";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(9, 64);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(194, 13);
			this.label5.TabIndex = 5;
			this.label5.Text = "Update interval, in game/minimized (ms)";
			// 
			// Settings_UpdateIntervalOsuNotFoundTextbox
			// 
			this.Settings_UpdateIntervalOsuNotFoundTextbox.AllowNegative = true;
			this.Settings_UpdateIntervalOsuNotFoundTextbox.Location = new System.Drawing.Point(217, 85);
			this.Settings_UpdateIntervalOsuNotFoundTextbox.Name = "Settings_UpdateIntervalOsuNotFoundTextbox";
			this.Settings_UpdateIntervalOsuNotFoundTextbox.Size = new System.Drawing.Size(44, 20);
			this.Settings_UpdateIntervalOsuNotFoundTextbox.TabIndex = 6;
			this.Settings_UpdateIntervalOsuNotFoundTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// Settings_StarTargetMinTextbox
			// 
			this.Settings_StarTargetMinTextbox.AllowNegative = true;
			this.Settings_StarTargetMinTextbox.Location = new System.Drawing.Point(167, 11);
			this.Settings_StarTargetMinTextbox.Name = "Settings_StarTargetMinTextbox";
			this.Settings_StarTargetMinTextbox.Size = new System.Drawing.Size(44, 20);
			this.Settings_StarTargetMinTextbox.TabIndex = 0;
			this.Settings_StarTargetMinTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// Settings_StarTargetMaxTextbox
			// 
			this.Settings_StarTargetMaxTextbox.AllowNegative = true;
			this.Settings_StarTargetMaxTextbox.Location = new System.Drawing.Point(217, 11);
			this.Settings_StarTargetMaxTextbox.Name = "Settings_StarTargetMaxTextbox";
			this.Settings_StarTargetMaxTextbox.Size = new System.Drawing.Size(44, 20);
			this.Settings_StarTargetMaxTextbox.TabIndex = 1;
			this.Settings_StarTargetMaxTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// Settings_UpdateIntervalNormalTextbox
			// 
			this.Settings_UpdateIntervalNormalTextbox.AllowNegative = true;
			this.Settings_UpdateIntervalNormalTextbox.Location = new System.Drawing.Point(217, 37);
			this.Settings_UpdateIntervalNormalTextbox.Name = "Settings_UpdateIntervalNormalTextbox";
			this.Settings_UpdateIntervalNormalTextbox.Size = new System.Drawing.Size(44, 20);
			this.Settings_UpdateIntervalNormalTextbox.TabIndex = 2;
			this.Settings_UpdateIntervalNormalTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// Settings_UpdateIntervalMinimizedTextbox
			// 
			this.Settings_UpdateIntervalMinimizedTextbox.AllowNegative = true;
			this.Settings_UpdateIntervalMinimizedTextbox.Location = new System.Drawing.Point(217, 61);
			this.Settings_UpdateIntervalMinimizedTextbox.Name = "Settings_UpdateIntervalMinimizedTextbox";
			this.Settings_UpdateIntervalMinimizedTextbox.Size = new System.Drawing.Size(44, 20);
			this.Settings_UpdateIntervalMinimizedTextbox.TabIndex = 3;
			this.Settings_UpdateIntervalMinimizedTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// MainWindowPanel
			// 
			this.MainWindowPanel.Controls.Add(this.MainTabControl);
			this.MainWindowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainWindowPanel.Location = new System.Drawing.Point(0, 1);
			this.MainWindowPanel.Name = "MainWindowPanel";
			this.MainWindowPanel.Size = new System.Drawing.Size(410, 237);
			this.MainWindowPanel.TabIndex = 17;
			// 
			// panel3
			// 
			this.panel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
			this.panel3.Controls.Add(this.StatusStripLabel);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel3.Location = new System.Drawing.Point(0, 238);
			this.panel3.Name = "panel3";
			this.panel3.Padding = new System.Windows.Forms.Padding(5, 1, 5, 2);
			this.panel3.Size = new System.Drawing.Size(410, 18);
			this.panel3.TabIndex = 18;
			// 
			// StatusStripLabel
			// 
			this.StatusStripLabel.AutoSize = true;
			this.StatusStripLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.StatusStripLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StatusStripLabel.ForeColor = System.Drawing.SystemColors.ControlLight;
			this.StatusStripLabel.Location = new System.Drawing.Point(5, 1);
			this.StatusStripLabel.Name = "StatusStripLabel";
			this.StatusStripLabel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 1);
			this.StatusStripLabel.Size = new System.Drawing.Size(175, 17);
			this.StatusStripLabel.TabIndex = 0;
			this.StatusStripLabel.Text = "Artist - Song name (Creator)";
			// 
			// panel4
			// 
			this.panel4.BackColor = System.Drawing.Color.Silver;
			this.panel4.Controls.Add(this.MainWindowPanel);
			this.panel4.Controls.Add(this.panel3);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel4.Location = new System.Drawing.Point(0, 0);
			this.panel4.Margin = new System.Windows.Forms.Padding(0);
			this.panel4.Name = "panel4";
			this.panel4.Padding = new System.Windows.Forms.Padding(0, 1, 0, 1);
			this.panel4.Size = new System.Drawing.Size(410, 257);
			this.panel4.TabIndex = 19;
			// 
			// GUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.BackColor = System.Drawing.Color.Silver;
			this.ClientSize = new System.Drawing.Size(410, 257);
			this.Controls.Add(this.panel4);
			this.ForeColor = System.Drawing.SystemColors.ControlText;
			this.Name = "GUI";
			this.Text = "osu! Difficulty Calculator";
			this.TransparencyKey = System.Drawing.Color.Fuchsia;
			this.Load += new System.EventHandler(this.GUI_Load);
			this.MainTabControl.ResumeLayout(false);
			this.resultsTab.ResumeLayout(false);
			this.PrimaryResultsPanel.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.openFromFilePanel.ResumeLayout(false);
			this.openFromFilePanel.PerformLayout();
			this.autoBeatmapCheckboxPanel.ResumeLayout(false);
			this.autoBeatmapCheckboxPanel.PerformLayout();
			this.timeAndClearPanel.ResumeLayout(false);
			this.timeAndClearPanel.PerformLayout();
			this.chartsTab.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.chartCheckboxPanel.ResumeLayout(false);
			this.chartVersionPanel.ResumeLayout(false);
			this.chartVersionPanel.PerformLayout();
			this.panel6.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.Chart)).EndInit();
			this.panel5.ResumeLayout(false);
			this.panel5.PerformLayout();
			this.ChartLegendPanel.ResumeLayout(false);
			this.ChartLegendPanel.PerformLayout();
			this.settingsTab.ResumeLayout(false);
			this.settingsTab.PerformLayout();
			this.MainWindowPanel.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button openFromFile;
		private System.Windows.Forms.CheckBox scaleRatings;
		private System.Windows.Forms.TableLayoutPanel difficultyDisplayPanel;
		private System.Windows.Forms.Label timeDisplay2;
		private System.Windows.Forms.Label timeDescriptionLabel;
		private System.Windows.Forms.Button clearButton;
		private System.Windows.Forms.Label timeDisplay1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox AutoBeatmapCheckbox;
		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage resultsTab;
		private System.Windows.Forms.TabPage chartsTab;
		private Controls.CustomChart Chart;
		private System.Windows.Forms.ComboBox ChartedMapDropdown;
		private System.Windows.Forms.TabPage settingsTab;
		private System.Windows.Forms.CheckBox AlwaysOnTopCheckbox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox EnableXmlCheckbox;
		private System.Windows.Forms.Label label3;
		private Controls.DoubleTextBox Settings_StarTargetMinTextbox;
		private System.Windows.Forms.Label label5;
		private Controls.IntTextBox Settings_UpdateIntervalMinimizedTextbox;
		private System.Windows.Forms.Label label4;
		private Controls.IntTextBox Settings_UpdateIntervalNormalTextbox;
		private Controls.DoubleTextBox Settings_StarTargetMaxTextbox;
		private System.Windows.Forms.ComboBox ChartStyleDropdown;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label StreamBpmLabel;
		private System.Windows.Forms.Label label6;
		private Controls.IntTextBox Settings_UpdateIntervalOsuNotFoundTextbox;
		private System.Windows.Forms.Panel PrimaryResultsPanel;
		private System.Windows.Forms.Panel openFromFilePanel;
		private System.Windows.Forms.Panel autoBeatmapCheckboxPanel;
		private System.Windows.Forms.Panel timeAndClearPanel;
		private System.Windows.Forms.Panel MainWindowPanel;
		private System.Windows.Forms.Panel chartVersionPanel;
		private System.Windows.Forms.Panel chartCheckboxPanel;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Label StatusStripLabel;
		private System.Windows.Forms.TableLayoutPanel ChartLegendPanel;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.Label BurstBpmLabel;
		private System.Windows.Forms.Label label14;
	}
}
