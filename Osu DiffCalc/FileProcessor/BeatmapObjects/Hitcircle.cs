
namespace Osu_DiffCalc.FileProcessor.BeatmapObjects
{
    class Hitcircle : BeatmapObject
    {
        public Hitcircle(int x, int y, int startTime) : base(x, y, startTime, Type.CIRCLE)
        {
            endTime = startTime;
        }
    }
}
