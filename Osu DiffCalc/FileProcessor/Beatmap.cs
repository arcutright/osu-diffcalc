using Osu_DiffCalc.FileProcessor.AnalyzerObjects;
using Osu_DiffCalc.FileProcessor.BeatmapObjects;
using System;
using System.Collections.Generic;

namespace Osu_DiffCalc.FileProcessor
{
    class Beatmap
    {
        public const int maxX = 512;
        public const int maxY = 384;

        public string title, artist, creator, version;
        public string filepath, mp3FileName;
        public float approachRate, circleSize, hpDrain, sliderMultiplier, sliderTickRate, accuracy;
        public float marginOfErrorMs300; //time window for a 300
        public float marginOfErrorMs50; //time window for a 50
        public float circleSizePx; //actual circle size
        public int format;
        public bool analyzed;
        public bool parsed;
        public bool isOfficiallySupported;
        
        public DifficultyRating diffRating = new DifficultyRating();

        public List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();
        public List<TimingPoint> timingPoints = new List<TimingPoint>();
        public List<BreakSection> breakSections = new List<BreakSection>();
        public int numHitObjects, numBreakSections, numTimingPoints;
        public int numCircles, numSliders, numSpinners;

        public Beatmap()
        {
            beatmapObjects.Clear();
            timingPoints.Clear();
            breakSections.Clear();
            numHitObjects = 0;
            numBreakSections = 0;
            numTimingPoints = 0;
            numCircles = 0;
            numSpinners = 0;
            numSliders = 0;
            format = -1;
            parsed = false;
            analyzed = false;
            isOfficiallySupported = false;
        }

        public Beatmap(string filepath) : this()
        {
            this.filepath = filepath;
        }
        
        public Beatmap(ref Mapset set, string version) : this()
        {
            title = set.title;
            artist = set.artist;
            creator = set.creator;
            this.version = version;
        }

        public void add(BeatmapObject obj)
        {
            beatmapObjects.Add(obj);
            if (!obj.IsBreakSection() && !obj.IsTimingPoint())
            {
                if (obj.IsCircle())
                    numCircles++;
                else if (obj.IsSlider())
                    numSliders++;
                else if (obj.IsSpinner())
                    numSpinners++;
                numHitObjects++;
            }
        }

        public void addTiming(TimingPoint timingPoint)
        {
            timingPoints.Add(timingPoint);
            numTimingPoints++;
        }

        public void addBreak(BreakSection breakSection)
        {
            breakSections.Add(breakSection);
            numBreakSections++;
        }

        public TimingPoint getTiming(BeatmapObject obj, bool startTime = true)
        {
            int time;
            if (startTime)
                time = obj.startTime;
            else
                time = obj.endTime;
            int maxIndex = -1;
            int numTimingPoints = timingPoints.Count;
            for(int i=0; i < numTimingPoints; i++)
            {
                if (time >= timingPoints[i].offset)
                    maxIndex = i;
                else
                    break;
            }
            if (maxIndex >= 0)
            {
                return timingPoints[maxIndex];
            }
            else
                return null;
        }

        public string getFamiliarizedDisplayString()
        {
            return getDiffDisplayString(DifficultyRating.FamiliarizeRating);
        }

        public string getDiffDisplayString(Func<double, double> diffFunction)
        {
            return string.Format("[{0}]  {1:0.###}", version, diffFunction(diffRating.totalDifficulty));
        }

        public string getDiffDisplayString(double scalingFactor = 1)
        {
            return string.Format("[{0}]  {1:0.###}", version, diffRating.totalDifficulty / scalingFactor);
        }

        public string getFamiliarizedDetailString()
        {
            double scaling =  diffRating.totalDifficulty / DifficultyRating.FamiliarizeRating(diffRating.totalDifficulty);
            return getDiffDetailString(scaling);
        }

        public string getDiffDetailString(double scalingFactor = 1)
        {
            return string.Format("jump:{0:0.####}  stream:{3:0.####}  \ncouplet:{1:0.####}  burst:{2:0.####}  slider:{4:0.####}",
                diffRating.jumpDifficulty / scalingFactor, diffRating.coupletDifficulty / scalingFactor, 
                diffRating.burstDifficulty / scalingFactor, diffRating.streamDifficulty / scalingFactor, 
                diffRating.sliderDifficulty / scalingFactor);
        }

        public string getDiffDetailString(Func<double, double> diffFunction)
        {
            return string.Format("jump:{0:0.####}  stream:{3:0.####}  \ncouplet:{1:0.####}  burst:{2:0.####}  slider:{4:0.####}",
                diffFunction(diffRating.jumpDifficulty), diffFunction(diffRating.coupletDifficulty),
                diffFunction(diffRating.burstDifficulty), diffFunction(diffRating.streamDifficulty),
                diffFunction(diffRating.sliderDifficulty));
        }

        public void printDebug()
        {
            Console.WriteLine("\n--------------Beatmap---------------");
            Console.WriteLine("format: v{0}  official support:{1}", format, isOfficiallySupported);
            Console.WriteLine("title: {0} [{1}]", title, version);
            Console.WriteLine("artist: {0}", artist);
            Console.WriteLine("creator: {0}\n", creator);
            
            Console.WriteLine("ar: {0}  hp: {1}  cs: {2}  od: {3}", approachRate, hpDrain, circleSize, accuracy);
            Console.WriteLine("slidermult: {0}  tickrate: {1}", sliderMultiplier, sliderTickRate);
            Console.WriteLine("difficulty rating: {0:0.0}\n", diffRating.totalDifficulty);

            Console.WriteLine("hitObjects: {0}  circles: {1}  sliders: {2}  spinners: {3}", numHitObjects, numCircles, numSliders, numSpinners);
        }
    }
}
