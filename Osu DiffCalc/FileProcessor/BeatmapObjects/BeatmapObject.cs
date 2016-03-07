
using System;

namespace Osu_DiffCalc.FileProcessor.BeatmapObjects
{
    class BeatmapObject
    {
        public Type type;
        public int x, y;
        public int startTime, endTime;

        public BeatmapObject()
        {
            x = 0;
            y = 0;
            startTime = 0;
            endTime = 0;
            type = Type.UNDEFINED;
        }

        public BeatmapObject(int x, int y, int startTime, Type type)
        {
            this.x = x;
            this.y = y;
            this.startTime = startTime;
            this.type = type;
        }

        public bool IsCircle()
        {
            return (type == Type.CIRCLE);
        }

        public bool IsSlider()
        {
            return (type == Type.SLIDER);
        }

        public bool IsSpinner()
        {
            return (type == Type.SPINNER);
        }

        public bool IsBreakSection()
        {
            return (type == Type.BREAK_SECTION);
        }

        public bool IsTimingPoint()
        {
            return (type == Type.TIMING_POINT);
        }

        public enum Type
        {
            CIRCLE,
            SLIDER,
            SPINNER,
            TIMING_POINT,
            BREAK_SECTION,
            UNDEFINED
        }
        
        public void PrintDebug(string prepend="", string append="")
        {
            Console.Write(prepend);
            Console.Write("{0}:  xy({1} {2})  time({3} {4})", type.ToString(), x, y, 
                FileParserHelpers.TimingParser.getTimeStamp(startTime), FileParserHelpers.TimingParser.getTimeStamp(endTime));
            Console.WriteLine(append);
        }
    }
}
