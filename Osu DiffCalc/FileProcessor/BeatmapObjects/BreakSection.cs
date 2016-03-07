
namespace Osu_DiffCalc.FileProcessor.BeatmapObjects
{
    class BreakSection : BeatmapObject
    {
        public BreakSection(int startTime, int endTime) : base()
        {
            this.startTime = startTime;
            this.endTime = endTime;
            type = Type.BREAK_SECTION;
        }
    }
}
