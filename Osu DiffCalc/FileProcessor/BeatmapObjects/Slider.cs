using System.Collections.Generic;
using System.Linq;

namespace Osu_DiffCalc.FileProcessor.BeatmapObjects
{
    class Slider : BeatmapObject
    {
        public string sliderType;
        public float pixelLength;
        public double totalLength;
        public double msPerBeat;
        public float pxPerSecond;
        public int repeat, x2, y2;
        public List<Point> points = new List<Point>();

        public Slider(int x, int y, int startTime, string sliderType, int repeat, float pixelLength) : base(x, y, startTime, Type.SLIDER)
        {
            this.sliderType = sliderType;
            this.pixelLength = pixelLength;
            this.repeat = repeat;
            totalLength = pixelLength * repeat;
            points = new List<Point>();
        }

        public void AddPoint(int x, int y)
        {
            points.Add(new Point(x, y));
        }

        public void GetInfo(TimingPoint timingPoint, float sliderMultiplier)
        {
            //get endtime of slider
            double sliderVelocityMultiplier = timingPoint.effectiveSliderBPM / timingPoint.bpm;
            float calculatedEndTime = (float)(totalLength / (100 * sliderMultiplier * sliderVelocityMultiplier) * timingPoint.msPerBeat + startTime);
            endTime = (int)(calculatedEndTime + 0.5);
            
            //calculate the speed in px/s
            pxPerSecond = (float)(totalLength * 1000.0 / (endTime - startTime));
            //get x2, y2
            if (repeat % 2 == 0)
            {
                x2 = points.Last().x; //these are approximations
                y2 = points.Last().y;
            }
            else
            {
                x2 = points[0].x; //these are approximations
                y2 = points[0].y;
            }
            //get timing parameter
            msPerBeat = timingPoint.msPerBeat;
        }

    }

    public class Point
    {
        public int x, y;

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
