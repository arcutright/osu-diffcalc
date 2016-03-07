using Osu_DiffCalc.FileProcessor.BeatmapObjects;
using System.IO;

namespace Osu_DiffCalc.FileProcessor.FileParserHelpers
{
    class HitObjectsParser
    {
        public static bool parse(Beatmap beatmap, ref StreamReader reader)
        {
            GeneralHelper.skipTo(ref reader, "[HitObjects]", false);
            string line;
            string[] data;
            int x, y, time, type, repeat, endTime;
            float pixelLength;
            string slidertype;

            int breakSectionIndex = 0;
            bool breakSectionsUsed = false;
            
            int timingPointIndex = 0;
            bool timingPointsUsed = false;
            if (beatmap.numTimingPoints <= 0)
                return false;
            TimingPoint curTiming = beatmap.timingPoints[0];

            while ((line = reader.ReadLine()) != null)
            {
                data = line.Split(',');
                x = (int)double.Parse(data[0]);
                y = (int)double.Parse(data[1]);
                time = (int)double.Parse(data[2]);
                type = (int)double.Parse(data[3]) % 4;
                if (beatmap.numBreakSections > 0)
                {
                    if (!breakSectionsUsed && time >= beatmap.breakSections[breakSectionIndex].startTime)
                    {
                        beatmap.add(beatmap.breakSections[breakSectionIndex]);
                        if (breakSectionIndex >= beatmap.numBreakSections - 1)
                            breakSectionsUsed = true;
                        breakSectionIndex++;
                    }
                }

                if(!timingPointsUsed && time >= beatmap.timingPoints[timingPointIndex].offset)
                {
                    while (timingPointIndex < beatmap.numTimingPoints - 1 && time >= beatmap.timingPoints[timingPointIndex].offset)
                        timingPointIndex++;
                    if (time < beatmap.timingPoints[timingPointIndex].offset)
                        timingPointIndex--;
                    curTiming = beatmap.timingPoints[timingPointIndex];
                    if (timingPointIndex >= beatmap.numTimingPoints - 1)
                        timingPointsUsed = true;
                    timingPointIndex++;
                }

                //spinner
                if (type == 0)
                {
                    //spinner : x,y,time,type,hit-sound,end_spinner,addition
                    endTime = (int)double.Parse(data[5]);
                    beatmap.add(new Spinner(time, endTime));
                }
                //circle
                else if (type == 1)
                {
                    //circle : x,y,time,type,hit-sound,addition
                    beatmap.add(new Hitcircle(x, y, time));
                }
                //slider
                else if(type == 2)
                {
                    /* slider : x,y,time,type,hit-sound,slidertype|points,repeat,length,edge_hitsound,edge_addition (v10 addition param),addition
                     * slidertype: b/c/l/p: [B]ezier/[C]atmull/[L]inear/ [P] is a mystery ?[P]olynomial ?[P]assthrough */

                    string[] pointChunkArray, point;
                    pointChunkArray = data[5].Split('|');
                    slidertype = pointChunkArray[0];
                    repeat = (int)double.Parse(data[6]);
                    pixelLength = float.Parse(data[7]);

                    Slider slider = new Slider(x, y, time, slidertype, repeat, pixelLength);

                    foreach(string pointChunk in pointChunkArray)
                    {
                        point = pointChunk.Split(':');
                        if(point.Length == 2)
                        {
                            slider.AddPoint((int)double.Parse(point[0]), (int)double.Parse(point[1]));
                        }
                    }
                    //get the endtime, speed, x2, y2, etc
                    slider.GetInfo(curTiming, beatmap.sliderMultiplier);
                    beatmap.add(slider);
                }
                //???
                else
                {
                    System.Console.WriteLine("fuckery:: {0}:  xy({1} {2})  type:{3}", TimingParser.getTimeStamp(time), x, y, type);
                }
            }
            if (beatmap.numHitObjects > 0)
                return true;
            else
                return false;
        }

    }
}
