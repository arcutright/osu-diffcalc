
namespace Osu_DiffCalc.FileProcessor.BeatmapObjects
{
    class Spinner : BeatmapObject
    {
        public Spinner(int startTime, int endTime) : base(Beatmap.maxX/2, Beatmap.maxY/2, startTime, Type.SPINNER)
        {
            this.endTime = endTime;
        }
    }
}
