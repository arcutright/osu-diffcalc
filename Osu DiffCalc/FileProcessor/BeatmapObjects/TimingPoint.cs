using Osu_DiffCalc.FileProcessor.FileParserHelpers;
using System;

namespace Osu_DiffCalc.FileProcessor.BeatmapObjects
{
    class TimingPoint : BeatmapObject
    {
        public int offset = -1;
        public int inheritedOffset = -1;
        public double bpm = -1;
        public double effectiveSliderBPM = -1;
        public double msPerBeat = -1;

        public TimingPoint(TimingPoint lastRootPoint, int offset, double msPerBeat)
        {
            this.offset = offset;
            type = Type.TIMING_POINT;

            //new timing point
            if (msPerBeat >= 0)
            {
                this.msPerBeat = msPerBeat;
                effectiveSliderBPM = TimingParser.getBPM(msPerBeat);
                bpm = effectiveSliderBPM;
                inheritedOffset = offset;
            }
            //inherited timing point
            else if (lastRootPoint != null)
            {
                bpm = lastRootPoint.bpm;
                inheritedOffset = lastRootPoint.inheritedOffset;
                effectiveSliderBPM = TimingParser.getEffectiveBPM(bpm, msPerBeat);
                this.msPerBeat = lastRootPoint.msPerBeat;
            }
        }

        public TimingPoint(int offset, double msPerBeat) : this(null, offset, msPerBeat)
        { }

        public new void PrintDebug(string prepend = "", string append = "")
        {
            Console.Write(prepend);
            Console.Write("{0}   msPerBeat:{1:0.00}   bpm:{2:0.0}   effectiveSliderBPM:{3:0.0}", 
                TimingParser.getTimeStamp(offset), msPerBeat, bpm, effectiveSliderBPM);
            Console.WriteLine(append);
        }
    }
}
