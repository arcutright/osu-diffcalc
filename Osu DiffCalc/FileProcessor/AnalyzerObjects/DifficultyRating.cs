using System;
using System.Windows.Forms.DataVisualization.Charting;

namespace Osu_DiffCalc.FileProcessor.AnalyzerObjects
{
    class DifficultyRating
    {
        public const SeriesChartType DEFAULT_CHART_TYPE = SeriesChartType.Column;

        public double jumpDifficulty, streamDifficulty, burstDifficulty, coupletDifficulty, sliderDifficulty;
        public double totalDifficulty;
        public Series jumps, streams, bursts, couplets, sliders;

        public DifficultyRating()
        {
            InitSeries(ref jumps, "Jumps");
            InitSeries(ref streams, "Streams");
            InitSeries(ref bursts, "Bursts");
            InitSeries(ref couplets, "Couplets");
            InitSeries(ref sliders, "Sliders");
        }

        private void InitSeries(ref Series s, string legend)
        {
            s = new Series();
            s.LegendText = legend;
            s.Name = legend;
            s.ChartType = DEFAULT_CHART_TYPE;
        }

        public static double FamiliarizeRating(double rating)
        {
            //return 1.1 * Math.Pow(rating, 0.25);
            return 0.5 * Math.Pow(rating, 0.4);
        }

        public void AddJump(double time, double difficulty)
        {
            Add(time, difficulty, jumps);
        }

        public void AddStream(double time, double difficulty)
        {
            Add(time, difficulty, streams);
        }

        public void AddBurst(double time, double difficulty)
        {
            Add(time, difficulty, bursts);
        }

        public void AddCouplet(double time, double difficulty)
        {
            Add(time, difficulty, couplets);
        }

        public void AddSlider(double time, double difficulty)
        {
            Add(time, difficulty, sliders);
        }

        private void Add(double ms, double diff, Series dest)
        {
            dest.Points.AddXY(ms/1000, diff);
        }

    }
}
