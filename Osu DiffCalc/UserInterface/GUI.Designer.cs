namespace Osu_DiffCalc.UserInterface
{
    partial class GUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Jumps",
            ""}, -1);
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("Streams");
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("Bursts");
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("Couplets");
            System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem("Sliders");
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
            this.autoBeatmapCheckbox = new System.Windows.Forms.CheckBox();
            this.tabs = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.chartedMapChoice = new System.Windows.Forms.ComboBox();
            this.seriesSelect = new System.Windows.Forms.ListView();
            this.column = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabs.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart)).BeginInit();
            this.SuspendLayout();
            // 
            // openFromFile
            // 
            this.openFromFile.Location = new System.Drawing.Point(5, 158);
            this.openFromFile.Name = "openFromFile";
            this.openFromFile.Size = new System.Drawing.Size(94, 23);
            this.openFromFile.TabIndex = 3;
            this.openFromFile.Text = "Open From File";
            this.openFromFile.UseVisualStyleBackColor = true;
            this.openFromFile.Click += new System.EventHandler(this.OpenFromFile_Click);
            // 
            // scaleRatings
            // 
            this.scaleRatings.AutoSize = true;
            this.scaleRatings.Checked = true;
            this.scaleRatings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.scaleRatings.Location = new System.Drawing.Point(117, 162);
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
            this.difficultyDisplayPanel.Location = new System.Drawing.Point(3, 3);
            this.difficultyDisplayPanel.Name = "difficultyDisplayPanel";
            this.difficultyDisplayPanel.Size = new System.Drawing.Size(266, 153);
            this.difficultyDisplayPanel.TabIndex = 7;
            // 
            // timeDisplay2
            // 
            this.timeDisplay2.BackColor = System.Drawing.SystemColors.Control;
            this.timeDisplay2.Location = new System.Drawing.Point(276, 137);
            this.timeDisplay2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.timeDisplay2.Name = "timeDisplay2";
            this.timeDisplay2.Size = new System.Drawing.Size(36, 19);
            this.timeDisplay2.TabIndex = 11;
            // 
            // timeDescriptionLabel
            // 
            this.timeDescriptionLabel.AutoSize = true;
            this.timeDescriptionLabel.Location = new System.Drawing.Point(276, 116);
            this.timeDescriptionLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.timeDescriptionLabel.Name = "timeDescriptionLabel";
            this.timeDescriptionLabel.Size = new System.Drawing.Size(80, 13);
            this.timeDescriptionLabel.TabIndex = 10;
            this.timeDescriptionLabel.Text = "Parse+Analyze:";
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(278, 162);
            this.clearButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(40, 19);
            this.clearButton.TabIndex = 0;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // timeDisplay1
            // 
            this.timeDisplay1.BackColor = System.Drawing.SystemColors.Control;
            this.timeDisplay1.Location = new System.Drawing.Point(276, 98);
            this.timeDisplay1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.timeDisplay1.Name = "timeDisplay1";
            this.timeDisplay1.Size = new System.Drawing.Size(36, 19);
            this.timeDisplay1.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(274, 76);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "getSetDirectory:";
            // 
            // autoBeatmapCheckbox
            // 
            this.autoBeatmapCheckbox.AutoSize = true;
            this.autoBeatmapCheckbox.Checked = true;
            this.autoBeatmapCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoBeatmapCheckbox.Location = new System.Drawing.Point(276, 5);
            this.autoBeatmapCheckbox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.autoBeatmapCheckbox.Name = "autoBeatmapCheckbox";
            this.autoBeatmapCheckbox.Size = new System.Drawing.Size(93, 17);
            this.autoBeatmapCheckbox.TabIndex = 14;
            this.autoBeatmapCheckbox.Text = "Auto Beatmap";
            this.autoBeatmapCheckbox.UseVisualStyleBackColor = true;
            this.autoBeatmapCheckbox.CheckedChanged += new System.EventHandler(this.AutoBeatmapCheckbox_CheckedChanged);
            // 
            // tabs
            // 
            this.tabs.Controls.Add(this.tabPage1);
            this.tabs.Controls.Add(this.tabPage2);
            this.tabs.Location = new System.Drawing.Point(2, 2);
            this.tabs.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(383, 216);
            this.tabs.TabIndex = 15;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.difficultyDisplayPanel);
            this.tabPage1.Controls.Add(this.autoBeatmapCheckbox);
            this.tabPage1.Controls.Add(this.openFromFile);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.scaleRatings);
            this.tabPage1.Controls.Add(this.timeDisplay1);
            this.tabPage1.Controls.Add(this.clearButton);
            this.tabPage1.Controls.Add(this.timeDisplay2);
            this.tabPage1.Controls.Add(this.timeDescriptionLabel);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage1.Size = new System.Drawing.Size(375, 190);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Results";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.chartedMapChoice);
            this.tabPage2.Controls.Add(this.seriesSelect);
            this.tabPage2.Controls.Add(this.chart);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage2.Size = new System.Drawing.Size(375, 190);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Graoh";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // chartedMapChoice
            // 
            this.chartedMapChoice.CausesValidation = false;
            this.chartedMapChoice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.chartedMapChoice.FormattingEnabled = true;
            this.chartedMapChoice.Items.AddRange(new object[] {
            "test",
            "test2",
            "test3"});
            this.chartedMapChoice.Location = new System.Drawing.Point(286, 89);
            this.chartedMapChoice.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.chartedMapChoice.Name = "chartedMapChoice";
            this.chartedMapChoice.Size = new System.Drawing.Size(92, 21);
            this.chartedMapChoice.TabIndex = 2;
            this.chartedMapChoice.DropDown += new System.EventHandler(this.ChartedMapChoice_DropDown);
            this.chartedMapChoice.SelectedIndexChanged += new System.EventHandler(this.ChartedMapChoice_SelectedIndexChanged);
            // 
            // seriesSelect
            // 
            this.seriesSelect.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.seriesSelect.CheckBoxes = true;
            this.seriesSelect.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.column});
            listViewItem1.Checked = true;
            listViewItem1.StateImageIndex = 1;
            listViewItem2.Checked = true;
            listViewItem2.StateImageIndex = 1;
            listViewItem3.Checked = true;
            listViewItem3.StateImageIndex = 1;
            listViewItem4.StateImageIndex = 0;
            listViewItem5.Checked = true;
            listViewItem5.StateImageIndex = 1;
            this.seriesSelect.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5});
            this.seriesSelect.Location = new System.Drawing.Point(317, 114);
            this.seriesSelect.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.seriesSelect.Name = "seriesSelect";
            this.seriesSelect.Scrollable = false;
            this.seriesSelect.Size = new System.Drawing.Size(63, 97);
            this.seriesSelect.TabIndex = 1;
            this.seriesSelect.UseCompatibleStateImageBehavior = false;
            this.seriesSelect.View = System.Windows.Forms.View.List;
            this.seriesSelect.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.SeriesSelect_SelectedIndexChanged);
            this.seriesSelect.SelectedIndexChanged += new System.EventHandler(this.SeriesSelect_SelectedIndexChanged);
            // 
            // column
            // 
            this.column.Text = "Column";
            this.column.Width = 100;
            // 
            // chart
            // 
            chartArea1.Name = "ChartArea";
            this.chart.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart.Legends.Add(legend1);
            this.chart.Location = new System.Drawing.Point(-24, -7);
            this.chart.Margin = new System.Windows.Forms.Padding(0);
            this.chart.Name = "chart";
            series1.ChartArea = "ChartArea";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            series1.YValuesPerPoint = 6;
            this.chart.Series.Add(series1);
            this.chart.Size = new System.Drawing.Size(421, 209);
            this.chart.TabIndex = 0;
            this.chart.Text = "Graph";
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(386, 216);
            this.Controls.Add(this.tabs);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "GUI";
            this.Text = "Diff Analyzer";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Fuchsia;
            this.Load += new System.EventHandler(this.GUI_Load);
            this.tabs.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart)).EndInit();
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
        private System.Windows.Forms.CheckBox autoBeatmapCheckbox;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart;
        private System.Windows.Forms.ListView seriesSelect;
        private System.Windows.Forms.ColumnHeader column;
        private System.Windows.Forms.ComboBox chartedMapChoice;
    }
}