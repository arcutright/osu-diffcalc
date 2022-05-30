namespace OsuDiffCalc.UserInterface {
	partial class GUI {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				components?.Dispose();
			}
			base.Dispose(disposing);
		}
		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("Jumps");
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("Streams");
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("Bursts");
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("Sliders");
            System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem("Doubles");
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.openFromFile = new System.Windows.Forms.Button();
            this.scaleRatings = new System.Windows.Forms.CheckBox();
            this.difficultyDisplayPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.timeDisplay2 = new System.Windows.Forms.Label();
            this.timeDescriptionLabel = new System.Windows.Forms.Label();
            this.clearButton = new System.Windows.Forms.Button();
            this.timeDisplay1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.AutoBeatmapCheckbox = new System.Windows.Forms.CheckBox();
            this.MainTabControl = new System.Windows.Forms.TabControl();
            this.resultsTab = new System.Windows.Forms.TabPage();
            this.EnableXmlCheckbox = new System.Windows.Forms.CheckBox();
            this.AlwaysOnTopCheckbox = new System.Windows.Forms.CheckBox();
            this.chartsTab = new System.Windows.Forms.TabPage();
            this.StreamBpmLabel = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.ChartStyleDropdown = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ChartedMapDropdown = new System.Windows.Forms.ComboBox();
            this.seriesSelect = new System.Windows.Forms.ListView();
            this.column = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.settingsTab = new System.Windows.Forms.TabPage();
            this.label6 = new System.Windows.Forms.Label();
            this.Settings_UpdateIntervalOsuNotFoundTextbox = new OsuDiffCalc.UserInterface.IntTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.Settings_StarTargetMinTextbox = new OsuDiffCalc.UserInterface.DoubleTextBox();
            this.Settings_StarTargetMaxTextbox = new OsuDiffCalc.UserInterface.DoubleTextBox();
            this.Settings_UpdateIntervalNormalTextbox = new OsuDiffCalc.UserInterface.IntTextBox();
            this.Settings_UpdateIntervalMinimizedTextbox = new OsuDiffCalc.UserInterface.IntTextBox();
            this.MainTabControl.SuspendLayout();
            this.resultsTab.SuspendLayout();
            this.chartsTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Chart)).BeginInit();
            this.settingsTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFromFile
            // 
            this.openFromFile.BackColor = System.Drawing.SystemColors.Control;
            this.openFromFile.Location = new System.Drawing.Point(0, 187);
            this.openFromFile.Name = "openFromFile";
            this.openFromFile.Size = new System.Drawing.Size(94, 23);
            this.openFromFile.TabIndex = 3;
            this.openFromFile.Text = "Open From File";
            this.openFromFile.UseVisualStyleBackColor = false;
            this.openFromFile.Click += new System.EventHandler(this.OpenFromFile_Click);
            // 
            // scaleRatings
            // 
            this.scaleRatings.AutoSize = true;
            this.scaleRatings.Checked = true;
            this.scaleRatings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.scaleRatings.Location = new System.Drawing.Point(100, 191);
            this.scaleRatings.Name = "scaleRatings";
            this.scaleRatings.Size = new System.Drawing.Size(125, 17);
            this.scaleRatings.TabIndex = 4;
            this.scaleRatings.Text = "Familiar Rating Scale";
            this.scaleRatings.UseVisualStyleBackColor = true;
            this.scaleRatings.CheckedChanged += new System.EventHandler(this.ScaleRatings_CheckedChanged);
            // 
            // difficultyDisplayPanel
            // 
            this.difficultyDisplayPanel.AutoScroll = true;
            this.difficultyDisplayPanel.BackColor = System.Drawing.Color.Gainsboro;
            this.difficultyDisplayPanel.Font = new System.Drawing.Font("Consolas", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.difficultyDisplayPanel.Location = new System.Drawing.Point(-4, 3);
            this.difficultyDisplayPanel.Name = "difficultyDisplayPanel";
            this.difficultyDisplayPanel.Size = new System.Drawing.Size(305, 180);
            this.difficultyDisplayPanel.TabIndex = 7;
            // 
            // timeDisplay2
            // 
            this.timeDisplay2.BackColor = System.Drawing.SystemColors.Control;
            this.timeDisplay2.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.timeDisplay2.Location = new System.Drawing.Point(343, 164);
            this.timeDisplay2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.timeDisplay2.Name = "timeDisplay2";
            this.timeDisplay2.Size = new System.Drawing.Size(51, 19);
            this.timeDisplay2.TabIndex = 11;
            this.timeDisplay2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // timeDescriptionLabel
            // 
            this.timeDescriptionLabel.AutoSize = true;
            this.timeDescriptionLabel.Location = new System.Drawing.Point(315, 150);
            this.timeDescriptionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.timeDescriptionLabel.Name = "timeDescriptionLabel";
            this.timeDescriptionLabel.Size = new System.Drawing.Size(80, 13);
            this.timeDescriptionLabel.TabIndex = 10;
            this.timeDescriptionLabel.Text = "Parse+Analyze:";
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(349, 188);
            this.clearButton.Margin = new System.Windows.Forms.Padding(2);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(45, 20);
            this.clearButton.TabIndex = 0;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // timeDisplay1
            // 
            this.timeDisplay1.BackColor = System.Drawing.SystemColors.Control;
            this.timeDisplay1.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.timeDisplay1.Location = new System.Drawing.Point(340, 127);
            this.timeDisplay1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.timeDisplay1.Name = "timeDisplay1";
            this.timeDisplay1.Size = new System.Drawing.Size(54, 19);
            this.timeDisplay1.TabIndex = 12;
            this.timeDisplay1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(313, 112);
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
            this.AutoBeatmapCheckbox.Location = new System.Drawing.Point(306, 4);
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
            this.MainTabControl.Location = new System.Drawing.Point(-1, 2);
            this.MainTabControl.Margin = new System.Windows.Forms.Padding(2);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(403, 237);
            this.MainTabControl.TabIndex = 15;
            // 
            // resultsTab
            // 
            this.resultsTab.BackColor = System.Drawing.Color.Silver;
            this.resultsTab.Controls.Add(this.EnableXmlCheckbox);
            this.resultsTab.Controls.Add(this.AlwaysOnTopCheckbox);
            this.resultsTab.Controls.Add(this.difficultyDisplayPanel);
            this.resultsTab.Controls.Add(this.AutoBeatmapCheckbox);
            this.resultsTab.Controls.Add(this.openFromFile);
            this.resultsTab.Controls.Add(this.label2);
            this.resultsTab.Controls.Add(this.scaleRatings);
            this.resultsTab.Controls.Add(this.timeDisplay1);
            this.resultsTab.Controls.Add(this.clearButton);
            this.resultsTab.Controls.Add(this.timeDisplay2);
            this.resultsTab.Controls.Add(this.timeDescriptionLabel);
            this.resultsTab.Location = new System.Drawing.Point(4, 22);
            this.resultsTab.Margin = new System.Windows.Forms.Padding(2);
            this.resultsTab.Name = "resultsTab";
            this.resultsTab.Padding = new System.Windows.Forms.Padding(2);
            this.resultsTab.Size = new System.Drawing.Size(395, 211);
            this.resultsTab.TabIndex = 0;
            this.resultsTab.Text = "Results";
            // 
            // EnableXmlCheckbox
            // 
            this.EnableXmlCheckbox.AutoSize = true;
            this.EnableXmlCheckbox.Location = new System.Drawing.Point(306, 46);
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
            this.AlwaysOnTopCheckbox.Location = new System.Drawing.Point(306, 25);
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
            this.chartsTab.Controls.Add(this.StreamBpmLabel);
            this.chartsTab.Controls.Add(this.label7);
            this.chartsTab.Controls.Add(this.ChartStyleDropdown);
            this.chartsTab.Controls.Add(this.label1);
            this.chartsTab.Controls.Add(this.ChartedMapDropdown);
            this.chartsTab.Controls.Add(this.seriesSelect);
            this.chartsTab.Controls.Add(this.Chart);
            this.chartsTab.Location = new System.Drawing.Point(4, 22);
            this.chartsTab.Margin = new System.Windows.Forms.Padding(2);
            this.chartsTab.Name = "chartsTab";
            this.chartsTab.Padding = new System.Windows.Forms.Padding(2);
            this.chartsTab.Size = new System.Drawing.Size(395, 211);
            this.chartsTab.TabIndex = 1;
            this.chartsTab.Text = "Charts";
            // 
            // StreamBpmLabel
            // 
            this.StreamBpmLabel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.StreamBpmLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.StreamBpmLabel.Location = new System.Drawing.Point(359, 74);
            this.StreamBpmLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.StreamBpmLabel.Name = "StreamBpmLabel";
            this.StreamBpmLabel.Size = new System.Drawing.Size(34, 18);
            this.StreamBpmLabel.TabIndex = 12;
            this.StreamBpmLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(292, 77);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(69, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "Stream BPM:";
            // 
            // ChartStyleDropdown
            // 
            this.ChartStyleDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ChartStyleDropdown.FormattingEnabled = true;
            this.ChartStyleDropdown.Location = new System.Drawing.Point(296, 168);
            this.ChartStyleDropdown.Name = "ChartStyleDropdown";
            this.ChartStyleDropdown.Size = new System.Drawing.Size(98, 21);
            this.ChartStyleDropdown.TabIndex = 4;
            this.ChartStyleDropdown.SelectedIndexChanged += new System.EventHandler(this.ChartStyleDropdown_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(154, 194);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Version:";
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
            this.ChartedMapDropdown.Location = new System.Drawing.Point(198, 189);
            this.ChartedMapDropdown.Margin = new System.Windows.Forms.Padding(2);
            this.ChartedMapDropdown.Name = "ChartedMapDropdown";
            this.ChartedMapDropdown.Size = new System.Drawing.Size(196, 21);
            this.ChartedMapDropdown.TabIndex = 2;
            this.ChartedMapDropdown.SelectedIndexChanged += new System.EventHandler(this.ChartedMapDropdown_SelectedIndexChanged);
            // 
            // seriesSelect
            // 
            this.seriesSelect.BackColor = System.Drawing.Color.Silver;
            this.seriesSelect.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.seriesSelect.CheckBoxes = true;
            this.seriesSelect.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.column});
            this.seriesSelect.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.seriesSelect.HideSelection = false;
            listViewItem1.Checked = true;
            listViewItem1.StateImageIndex = 1;
            listViewItem2.Checked = true;
            listViewItem2.StateImageIndex = 1;
            listViewItem3.Checked = true;
            listViewItem3.StateImageIndex = 1;
            listViewItem4.Checked = true;
            listViewItem4.StateImageIndex = 1;
            listViewItem5.StateImageIndex = 0;
            this.seriesSelect.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5});
            this.seriesSelect.LabelWrap = false;
            this.seriesSelect.Location = new System.Drawing.Point(326, 95);
            this.seriesSelect.Margin = new System.Windows.Forms.Padding(2);
            this.seriesSelect.MultiSelect = false;
            this.seriesSelect.Name = "seriesSelect";
            this.seriesSelect.Scrollable = false;
            this.seriesSelect.Size = new System.Drawing.Size(65, 142);
            this.seriesSelect.TabIndex = 1;
            this.seriesSelect.UseCompatibleStateImageBehavior = false;
            this.seriesSelect.View = System.Windows.Forms.View.List;
            this.seriesSelect.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.SeriesSelect_ColumnClick);
            this.seriesSelect.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.SeriesSelect_ItemChecked);
            // 
            // column
            // 
            this.column.Text = "Column";
            this.column.Width = 120;
            // 
            // Chart
            // 
            this.Chart.BackColor = System.Drawing.Color.Transparent;
            this.Chart.BorderlineColor = System.Drawing.Color.LightGray;
            chartArea1.AxisX.LabelStyle.Format = "#";
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.DimGray;
            chartArea1.AxisX.Title = "X=Time, Y=Diff";
            chartArea1.AxisX.TitleAlignment = System.Drawing.StringAlignment.Near;
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.DimGray;
            chartArea1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            chartArea1.Name = "ChartArea";
            this.Chart.ChartAreas.Add(chartArea1);
            legend1.BackColor = System.Drawing.Color.Gainsboro;
            legend1.Name = "Legend1";
            this.Chart.Legends.Add(legend1);
            this.Chart.Location = new System.Drawing.Point(-16, -4);
            this.Chart.Margin = new System.Windows.Forms.Padding(0);
            this.Chart.Name = "Chart";
            this.Chart.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Pastel;
            series1.ChartArea = "ChartArea";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            series1.YValuesPerPoint = 6;
            this.Chart.Series.Add(series1);
            this.Chart.Size = new System.Drawing.Size(427, 219);
            this.Chart.TabIndex = 0;
            this.Chart.Text = "Chart";
            // 
            // settingsTab
            // 
            this.settingsTab.BackColor = System.Drawing.Color.Silver;
            this.settingsTab.Controls.Add(this.label6);
            this.settingsTab.Controls.Add(this.Settings_UpdateIntervalOsuNotFoundTextbox);
            this.settingsTab.Controls.Add(this.label3);
            this.settingsTab.Controls.Add(this.label4);
            this.settingsTab.Controls.Add(this.label5);
            this.settingsTab.Controls.Add(this.Settings_StarTargetMinTextbox);
            this.settingsTab.Controls.Add(this.Settings_StarTargetMaxTextbox);
            this.settingsTab.Controls.Add(this.Settings_UpdateIntervalNormalTextbox);
            this.settingsTab.Controls.Add(this.Settings_UpdateIntervalMinimizedTextbox);
            this.settingsTab.Location = new System.Drawing.Point(4, 22);
            this.settingsTab.Name = "settingsTab";
            this.settingsTab.Padding = new System.Windows.Forms.Padding(3);
            this.settingsTab.Size = new System.Drawing.Size(395, 211);
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
            // Settings_UpdateIntervalOsuNotFoundTextbox
            // 
            this.Settings_UpdateIntervalOsuNotFoundTextbox.AllowNegative = true;
            this.Settings_UpdateIntervalOsuNotFoundTextbox.Location = new System.Drawing.Point(217, 85);
            this.Settings_UpdateIntervalOsuNotFoundTextbox.Name = "Settings_UpdateIntervalOsuNotFoundTextbox";
            this.Settings_UpdateIntervalOsuNotFoundTextbox.Size = new System.Drawing.Size(44, 20);
            this.Settings_UpdateIntervalOsuNotFoundTextbox.TabIndex = 6;
            this.Settings_UpdateIntervalOsuNotFoundTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(398, 235);
            this.Controls.Add(this.MainTabControl);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "GUI";
            this.Text = "osu! Difficulty Calculator";
            this.TransparencyKey = System.Drawing.Color.Fuchsia;
            this.Load += new System.EventHandler(this.GUI_Load);
            this.MainTabControl.ResumeLayout(false);
            this.resultsTab.ResumeLayout(false);
            this.resultsTab.PerformLayout();
            this.chartsTab.ResumeLayout(false);
            this.chartsTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Chart)).EndInit();
            this.settingsTab.ResumeLayout(false);
            this.settingsTab.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button openFromFile;
		private System.Windows.Forms.CheckBox scaleRatings;
		private System.Windows.Forms.FlowLayoutPanel difficultyDisplayPanel;
		private System.Windows.Forms.Label timeDisplay2;
		private System.Windows.Forms.Label timeDescriptionLabel;
		private System.Windows.Forms.Button clearButton;
		private System.Windows.Forms.Label timeDisplay1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox AutoBeatmapCheckbox;
		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage resultsTab;
		private System.Windows.Forms.TabPage chartsTab;
		private System.Windows.Forms.DataVisualization.Charting.Chart Chart;
		private System.Windows.Forms.ListView seriesSelect;
		private System.Windows.Forms.ColumnHeader column;
		private System.Windows.Forms.ComboBox ChartedMapDropdown;
		private System.Windows.Forms.TabPage settingsTab;
		private System.Windows.Forms.CheckBox AlwaysOnTopCheckbox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox EnableXmlCheckbox;
		private System.Windows.Forms.Label label3;
		private DoubleTextBox Settings_StarTargetMinTextbox;
		private System.Windows.Forms.Label label5;
		private IntTextBox Settings_UpdateIntervalMinimizedTextbox;
		private System.Windows.Forms.Label label4;
		private IntTextBox Settings_UpdateIntervalNormalTextbox;
		private DoubleTextBox Settings_StarTargetMaxTextbox;
		private System.Windows.Forms.ComboBox ChartStyleDropdown;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label StreamBpmLabel;
		private System.Windows.Forms.Label label6;
		private IntTextBox Settings_UpdateIntervalOsuNotFoundTextbox;
	}
}