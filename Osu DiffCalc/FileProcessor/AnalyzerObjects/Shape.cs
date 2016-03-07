using Osu_DiffCalc.FileProcessor.BeatmapObjects;
using Osu_DiffCalc.FileProcessor.FileParserHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Osu_DiffCalc.FileProcessor.AnalyzerObjects
{
    class Shape
    {
        protected List<BeatmapObject> hitObjects;
        public int numObjects = 0;
        protected double effective1_4bpm = -1; //bpm when mapped at 1/4 time-spacing for 4/4 timing
        public double avgTimeGapMs = 0;
        public double totalDistancePx = 0;
        public double avgDistancePx = 0;
        public double minDistancePx = -1, maxDistancePx = -1;
        public Type type;

        public int startTime = -1, endTime = -1;
        public Shape previous;

        public Shape()
        {
            hitObjects = new List<BeatmapObject>();
            previous = null;
            type = Type.UNDEFINED;
        }

        public Shape(params BeatmapObject[] objs): this()
        {
            foreach (BeatmapObject obj in objs)
                Add(obj);
        }

        public enum Type
        {
            COUPLET,
            TRIPLET,
            BURST,
            STREAM,
            LINE,
            TRIANGLE,
            SQUARE,
            REGULAR_POLYGON,
            POLYGON,
            UNDEFINED
        }

        public void Add(BeatmapObject obj)
        {
            hitObjects.Add(obj);
            numObjects++;
            UpdateAvgMsPerBeat();
            UpdateDistances();
            endTime = obj.endTime;
            if (startTime < 0)
                startTime = obj.startTime;
        }

        public double GetEffectiveBPM()
        {
            effective1_4bpm = TimingParser.getBPM(4*avgTimeGapMs);
            return effective1_4bpm;
        }

        //check if next object has constant timing with the current stream
        public int CompareTiming(int nextObjectStartTime)
        {
            if(numObjects > 0)
            {
                int timeGap = nextObjectStartTime - hitObjects.Last().startTime;
                double difference = timeGap - avgTimeGapMs;
                if (Math.Abs(difference) <= 20) //20ms margin of error = spacing for 3000bpm stream at 1/4 mapping
                    return 0;
                else if (difference < 0)
                    return -1;
                else
                    return 1;
            }
            return -2;
        }

        public void Clear()
        {
            hitObjects.Clear();
        }

        public void PrintDebug(string prepend="", string append="", bool printType=false)
        {
            Console.Write(prepend);
            if (printType)
            {
                Console.Write("{0}({1}):  {2}  {3:0.0}ms  {4:0.0}bpm  {5:0.0}px  {6:0.0}px", type, numObjects, 
                    TimingParser.getTimeStamp(hitObjects[0].startTime), avgTimeGapMs,
                    GetEffectiveBPM(), totalDistancePx, avgDistancePx);
            }
            else
            {
                Console.Write("({0}):  {1}  {2:0.0}ms  {3:0.0}bpm  {4:0.0}px  {5:0.0}px", numObjects, 
                    TimingParser.getTimeStamp(hitObjects[0].startTime), avgTimeGapMs, 
                    GetEffectiveBPM(), totalDistancePx, avgDistancePx);
            }
            Console.WriteLine(append);
        }

        //private helpers

        void UpdateAvgMsPerBeat()
        {
            if (hitObjects.Count >= 2)
            {
                //if the second to last uses endTime instead of startTime, stream detection will consider ends of sliders as continuing the stream
                int lastTimeGapMs = hitObjects[numObjects - 1].startTime - hitObjects[numObjects - 2].startTime;
                avgTimeGapMs = (avgTimeGapMs * (numObjects - 2) + lastTimeGapMs) / (numObjects - 1);
            }
        }

        void UpdateDistances()
        {
            if (hitObjects.Count >= 2)
            {
                int lastDistanceX = hitObjects[numObjects - 1].x - hitObjects[numObjects - 2].x;
                int lastDistanceY = hitObjects[numObjects - 1].y - hitObjects[numObjects - 2].y;
                double lastDistance = Math.Sqrt((lastDistanceX * lastDistanceX) + (lastDistanceY * lastDistanceY));
                avgDistancePx = (avgDistancePx * (numObjects - 2) + lastDistance) / (numObjects - 1);
                totalDistancePx += lastDistance;

                if (minDistancePx < 0 || lastDistance < minDistancePx)
                    minDistancePx = lastDistance;
                if (maxDistancePx < 0 || lastDistance > maxDistancePx)
                    maxDistancePx = lastDistance;
            }
        }

    }
}
