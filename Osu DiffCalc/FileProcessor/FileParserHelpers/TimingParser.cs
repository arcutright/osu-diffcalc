using System;
using System.IO;
using System.Linq;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class TimingParser
    {
        /* the general form of timing points are x,x,x,x,x,x,x,x
        1st x means the offset
        2nd x (positive) means the BPM but in the other format, the BPM in Edit is equal to 60,000/x
        2nd x (negative) means the BPM multiplier - kind of, BPM multiplier in Edit is equal to -100/x
        3rd x is related to metronome, 3 means 3/4 beat, 4 means 4/4 beats
        4th x is about hitsounds set: soft/normal...etc
        5th x is custom hitsounds: 0 means no custom hitsound, 1 means set 1, 2 means set 2
        6th x is volume
        7th x means if it's inherit timing point
        8th x is kiai time */

        public static double getEffectiveBPM(double bpm, double msPerBeat)
        {
            //inherited timing point
            if(msPerBeat < 0)
                return bpm * (-100.0) / msPerBeat;
            //independent timing point
            else
                return bpm;
        }

        public static double getBPM(double msPerBeat)
        {
            return 60000.0 / msPerBeat;
        }

        public static double getMsPerBeat(double bpm)
        {
            return 60000.0 / bpm;
        }

        public static string getTimeStamp(long ms, bool hours = false)
        {
            TimeSpan timespan = TimeSpan.FromMilliseconds(ms);
            string timeString;
            if(hours)
                timeString = string.Format("{0:00}:{1:00}:{2:00}:{3:000}", 
                    timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);
            else
                timeString = string.Format("{0:00}:{1:00}:{2:000}", 
                    timespan.Minutes, timespan.Seconds, timespan.Milliseconds);
            return timeString;
        }

        public static bool parse(Beatmap beatmap, ref StreamReader reader)
        {
            string line;
            string[] data;
            int offset;
            double msPerBeat;
            GeneralHelper.skipTo(ref reader, @"[TimingPoints]", false);

            while ((line = reader.ReadLine()) != null && line.Length > 0)
            {
                line = line.Trim();
                data = line.Split(',');

                if (data.Length >= 2)
                {
                    //Offset, Milliseconds per Beat, Meter, Sample Type, Sample Set, Volume, Inherited, Kiai Mode
                    //old maps only have   offset,msPerBeat
                    offset = (int)double.Parse(data[0]);
                    msPerBeat = double.Parse(data[1]);
                    if(beatmap.timingPoints.Count <= 0)
                        beatmap.addTiming(new BeatmapObjects.TimingPoint(offset, msPerBeat));
                    else
                        beatmap.addTiming(new BeatmapObjects.TimingPoint(beatmap.timingPoints.Last(), offset, msPerBeat));
                }
                if (reader.Peek() == '[')
                    break;
            }
            if (beatmap.numTimingPoints > 0)
                return true;
            else
                return false;
        }

    }
}
