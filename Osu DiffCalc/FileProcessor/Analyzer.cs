using System.Collections.Generic;
using Osu_DiffCalc.FileProcessor.BeatmapObjects;
using Osu_DiffCalc.FileProcessor.AnalyzerObjects;
using System;
using System.Threading;

namespace Osu_DiffCalc.FileProcessor
{
    class Analyzer
    {
        const int minBurstLength = 3;
        const int minStreamLength = 6;
        const double burstMultiplier = 0.75;
        static double maxDistPx = Math.Sqrt((Beatmap.maxX * Beatmap.maxX) + (Beatmap.maxY * Beatmap.maxY));

        public static void analyze(Beatmap beatmap, bool clearLists = true)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = string.Format("analyze[{0}]", beatmap.version);
            
            //analysis variables
            List<Shape> streams = new List<Shape>();
            double minStreamAvgMs = -1;

            List<Shape> couplets = new List<Shape>();
            double minCoupletAvgMs = -1;

            List<Shape> triplets = new List<Shape>();
            double minTripletAvgMs = -1;

            List<Shape> bursts = new List<Shape>();
            double minBurstAvgMs = -1;

            Shape shape = new Shape();
            Shape lastShape = new Shape();
            BeatmapObject lastHitObject = null;
            BeatmapObject lastObject = null;
            bool shapeAdded = false;

            List<double> jumpDifficultyList = new List<double>();
            List<double> sliderDifficultyList = new List<double>();

            //macro to make life easier
            Action addShapeToAppropriateList = () =>
            {
                shape.previous = lastShape;
                if (shape.numObjects == 2)
                {
                    shape.type = Shape.Type.COUPLET;
                    couplets.Add(shape);
                    updateMin(ref minCoupletAvgMs, shape.avgTimeGapMs);
                }
                else if (shape.numObjects < minBurstLength)
                {
                    shape.type = Shape.Type.TRIPLET;
                    triplets.Add(shape);
                    updateMin(ref minTripletAvgMs, shape.avgTimeGapMs);
                }
                else if (shape.numObjects < minStreamLength)
                {
                    shape.type = Shape.Type.BURST;
                    bursts.Add(shape);
                    updateMin(ref minBurstAvgMs, shape.avgTimeGapMs);
                }
                else if (shape.numObjects >= minStreamLength)
                {
                    shape.type = Shape.Type.STREAM;
                    streams.Add(shape);
                    updateMin(ref minStreamAvgMs, shape.avgTimeGapMs);
                }
                lastShape = shape;
                shapeAdded = true;
            };

            foreach (BeatmapObject obj in beatmap.beatmapObjects)
            {
                shapeAdded = false;

                if (obj.IsCircle() || obj.IsSlider())
                {
                    if (lastHitObject != null)
                    {
                        //second object
                        if (shape.numObjects == 1)
                            shape.Add(obj);
                        //third, fourth, etc. 
                        else if (shape.numObjects >= 2)
                        {
                            //continuing stream
                            if (shape.CompareTiming(obj.startTime) == 0)
                                shape.Add(obj);
                            //end of stream, start of new stream
                            else
                            {
                                addShapeToAppropriateList();
                                shape = new Shape(lastHitObject, obj);
                            }
                        }
                    }
                    //first hit object
                    else
                        shape.Add(obj);
                    //calculate jump and slider diffs
                    if (obj.IsSlider())
                        sliderDifficultyList.Add(getSliderDifficulty(obj, beatmap));
                    if (lastHitObject != null)
                        jumpDifficultyList.Add(getJumpDifficulty(obj, lastObject, beatmap));

                    lastHitObject = obj;
                }
                //obj is not a hit object
                else
                     addShapeToAppropriateList();
                lastObject = obj;
            }
            if (!shapeAdded)
                addShapeToAppropriateList();

            double streamsDifficulty = getStreamsDifficulty(streams, minStreamAvgMs, beatmap);
            double coupletsDifficulty = getCoupletsDifficulty(couplets, minCoupletAvgMs, beatmap);
            double burstsDifficulty = getStreamsDifficulty(bursts, minBurstAvgMs, beatmap);
            //Console.Write("jump ");
            double jumpsDifficulty = getWeightedSumOfList(jumpDifficultyList, 1.5);
            //Console.Write("slider ");
            double slidersDifficulty = getWeightedSumOfList(sliderDifficultyList, 1.5);
            double totalDifficulty = streamsDifficulty + burstsDifficulty + coupletsDifficulty + jumpsDifficulty + slidersDifficulty;

            beatmap.diffRating.jumpDifficulty = jumpsDifficulty;
            beatmap.diffRating.streamDifficulty = streamsDifficulty;
            beatmap.diffRating.burstDifficulty = burstsDifficulty;
            beatmap.diffRating.coupletDifficulty = coupletsDifficulty;
            beatmap.diffRating.sliderDifficulty = slidersDifficulty;
            beatmap.diffRating.totalDifficulty = totalDifficulty;
            beatmap.analyzed = true;
            /*
            Console.WriteLine("\n{1:000} jumps diff    = {0:0.0}", jumpsDifficulty, jumpDifficultyList.Count);
            Console.WriteLine("{1:000} streams diff  = {0:0.0}", streamsDifficulty, streams.Count);
            Console.WriteLine("{1:000} bursts diff   = {0:0.0}", burstsDifficulty, bursts.Count);
            Console.WriteLine("{1:000} couplets diff = {0:0.0}", coupletsDifficulty, couplets.Count);
            Console.WriteLine("{1:000} sliders diff  = {0:0.0}", slidersDifficulty, sliderDifficultyList.Count);
            Console.WriteLine("* total diff  = {0:0.0}", beatmap.diffRating.totalDifficulty);
            //printDebug(couplets, "couplets");
            //printDebug(triplets, "triplets");
            //printDebug(streams, "streams");
            */
            

            if (clearLists)
            {
                beatmap.beatmapObjects.Clear();
                beatmap.timingPoints.Clear();
                beatmap.breakSections.Clear();
            }
        }

        #region Difficulty calculations

        static double getStreamsDifficulty(List<Shape> streams, double minStreamMs, Beatmap map)
        {
            double difficulty = 0;
            double avgObjects = 0;
            double avgBPM = 0;
            double avgDistancePx = 0;
            int numShapes = 0;
            double localDiff;

            List<double> diffs = new List<double>();

            foreach (Shape stream in streams)
            {
                localDiff = 0;
                if (stream.numObjects >= minBurstLength && stream.avgTimeGapMs > 0)
                {
                    double streamBPM = 15000 / stream.avgTimeGapMs;
                    //calculate the difficulty of the stream
                    localDiff = Math.Pow(stream.numObjects, 0.8) * Math.Pow(streamBPM, 3.4) / 1000000000 * (stream.avgDistancePx + 1);
                    //addition for OD. Maxes ~10%
                    localDiff += localDiff * 1000 / (stream.avgTimeGapMs * map.marginOfErrorMs300);
                    //multiplier to represent that bursts are significantly easier
                    if (stream.numObjects < minStreamLength)
                    {
                        localDiff *= burstMultiplier;
                        map.diffRating.AddBurst(stream.startTime, localDiff);
                    }
                    else //a stream, not a burst
                    {
                        map.diffRating.AddStream(stream.startTime, localDiff);
                    }
                    diffs.Add(localDiff);

                    //Console.WriteLine("s({0}) {1}  bpm:{2:0.0}   avgDist:{3:0.0}  localDiff:{4:0.00}", 
                    //   stream.numObjects, FileParserHelpers.TimingParser.getTimeStamp(stream.startTime), streamBPM, stream.avgDistancePx, localDiff);
                    avgObjects = rollingAverage(avgObjects, stream.numObjects, numShapes);
                    avgBPM = rollingAverage(avgBPM, streamBPM, numShapes);
                    avgDistancePx = rollingAverage(avgDistancePx, stream.avgDistancePx, numShapes);
                    numShapes++;
                }
            }
            //Console.WriteLine("\ntypical stream len:{0:0.0}  bpm:{1:0.0}  dist:{2:0.0}  diff:{3:0.0}", avgObjects, avgBPM, avgDistancePx,
             //   Math.Pow(avgObjects, 0.6) * Math.Pow(avgBPM, 3.2) / 1000000000 * Math.Pow(avgDistancePx, 1.6));

            //Console.Write("stream ");
            if(diffs.Count > 0)
                difficulty += getWeightedSumOfList(diffs, 1.5);

            return difficulty;
        }

        static double getCoupletsDifficulty(List<Shape> couplets, double minCoupletsMs, Beatmap map)
        {
            double difficulty = 0;
            //are dependent on the spacing between couplets... ie, closely timed couplets are harder to hit than far-timed ones
            //track with a shape.previous
            int timeGapMs;
            float timeDifferenceForTransition; //to measure how abrubt changing between shapes is
            float timeDifferenceForSpeeds; //similar to difference in BPM between streams, but in ms/tick
            double coupleBPM, localDiff = 0;
            foreach(Shape couple in couplets)
            {
                localDiff = 0;
                timeGapMs = couple.startTime - couple.previous.endTime;
                timeDifferenceForTransition = (float)Math.Abs(couple.avgTimeGapMs*2 - timeGapMs);
                timeDifferenceForSpeeds = (float)Math.Abs(couple.avgTimeGapMs - couple.previous.avgTimeGapMs);
                //let's define these parameters to make a couplet difiicult
                if (timeGapMs > 0)
                {
                    if (timeDifferenceForTransition <= 20 && timeDifferenceForSpeeds <= 1.5 * couple.avgTimeGapMs + 20)
                    {
                        coupleBPM = 15000 / couple.avgTimeGapMs;
                        localDiff = Math.Pow(coupleBPM, 3.2) / 1000000000 * Math.Pow(couple.avgDistancePx + 1, 1.6);
                    }
                    map.diffRating.AddCouplet(couple.startTime, localDiff);
                }
            }
            return difficulty;
        }

        static double getJumpDifficulty(BeatmapObject hitObject, BeatmapObject prevObject, Beatmap map)
        {
            double difficulty = 0;
            if (prevObject.IsCircle() || prevObject.IsSlider())
            {
                int dx = hitObject.x;
                int dy = hitObject.y;
                if (prevObject.IsSlider())
                {
                    dx -= ((Slider)prevObject).x2;
                    dy -= ((Slider)prevObject).y2;
                }
                else
                {
                    dx -= prevObject.x;
                    dy -= prevObject.y;
                }
                double distPx = Math.Sqrt((dx * dx) + (dy * dy));
                double distMs = hitObject.startTime - prevObject.endTime;
                if (distMs > 0)
                {
                    //adjust distance based on CS
                    distPx -= map.circleSizePx / 2;
                    if (distPx < 0)
                        distPx = 0;
                    else if (distPx > 0)
                    {
                        //adjust time based on OD
                        if (hitObject.IsSlider())
                            distMs += map.marginOfErrorMs50 / 2;
                        else
                            distMs += map.marginOfErrorMs300 / 2;
                        if (prevObject.IsSlider())
                            distMs += map.marginOfErrorMs50 / 2;
                        else
                            distMs += map.marginOfErrorMs300 / 2;

                        //difficulty due to the speed of the jump
                        double speedDiff = Math.Pow(10 * distPx / distMs, 2);
                        //difficulty due to the distance
                        double distDiff = speedDiff * 0.4 * distPx / (maxDistPx - map.circleSizePx / 2);
                        difficulty = speedDiff + distDiff;
                        //adjustment to make jumps vs streams more reasonable
                        difficulty *= 0.1;
                        map.diffRating.AddJump(prevObject.endTime, difficulty);

                        //Console.WriteLine("jump {0}  {1}  spDiff:{2:0.0}  dstDiff:{3:0.0}  dist:{5:0.0}  bpm:{6:0.0}  ={4:0.00}",  FileParserHelpers.TimingParser.getTimeStamp(prevObject.endTime), FileParserHelpers.TimingParser.getTimeStamp(hitObject.startTime), speedDiff, distDiff, difficulty, distPx, 15000 / distMs);
                    }
                }
            }
            return difficulty;
        }

        static double getSliderDifficulty(BeatmapObject sliderObject, Beatmap map)
        {
            double difficulty = 0;
            
            //check to make sure timegapms > 0
            if (sliderObject.IsSlider())
            {
                Slider slider = (Slider)sliderObject;
                double px = slider.totalLength;
                //adjust slider length to consider the margin allowed by od
                px -= ((double)slider.repeat * (map.circleSizePx + map.accuracy));     //FIX ME (OD MARGIN)
                
                if (px > 0)
                {
                    difficulty += Math.Pow(px / (slider.endTime - slider.startTime), 1.5) * Math.Pow(px, 0.6) / 2;

                    //Console.WriteLine("slider {0} => {1}", FileParserHelpers.TimingParser.getTimeStamp(slider.startTime), difficulty);
                   /*
                   //adjust for tickrate
                   double msPerTick = map.sliderTickRate * slider.msPerBeat; //may have to correct this (what are units for tickrate?)
                   double pxPerTick = slider.pxPerSecond / 1000.0 * msPerTick;
                   //difficulty *= (precisionDelta - 1);
                   */
                    map.diffRating.AddSlider(slider.startTime, difficulty);
                }
            }

            return difficulty;
        }

        #endregion

        #region Helper functions

        static void updateMin(ref double min, double toCompare)
        {
            if (min < 0 || toCompare < min)
                min = toCompare;
        }

        static double getWeightedSumOfList(List<double> list, double addMeanAndDeviations = 0, double weightFactor= 0.9)
        {
            double difficulty = 0;
            double localDiff = 0;
            int numItems = list.Count;
            if (numItems > 1)
            {
                list.Sort();
                //Console.WriteLine("max: {0:0.0}", list[numItems - 1]);

                //weight the highest difficulties the greatest, decrease by weightFactor each time
                double weight = 1;
                double average = 0;
                for (int i = numItems - 1; i >= 0; i--)
                {
                    localDiff = list[i] * weight;
                    difficulty += localDiff;
                    if (addMeanAndDeviations > 0)
                        average = rollingAverage(average, localDiff, numItems - i);
                    weight *= weightFactor;

                }
                if (addMeanAndDeviations > 0)
                {
                    double bonus = average + addMeanAndDeviations * getStandardDeviation(list, average);
                    difficulty += bonus;
                    //Console.WriteLine("bonus: {0:0.0}", bonus);
                }
            }
            else if (numItems == 1)
                difficulty = list[0];
            else
                difficulty = 0;
            return difficulty;
        }

        static double getStandardDeviation(List<double> list, double avg)
        {
            double variance = 0;
            foreach(double value in list)
            {
                //variance += Math.Pow(value - avg, 2);
                variance += value * value;
            }
            variance -= list.Count * avg * avg; //
            variance /= list.Count;
            return Math.Sqrt(variance);
        }

        static double rollingAverage(double currentAvg, double toAdd, int currentNumValues)
        {
            return (currentAvg * currentNumValues + toAdd) / (currentNumValues + 1);
        }

        #endregion

        //DEBUG
        static void printDebug(List<Shape> shapeList, string phrase = "streams")
        {
            Console.WriteLine("\n---------------{0}-------------------", phrase);
            Console.WriteLine("       (time)               (bpm)  (dist)  (avgdist)");
            foreach (Shape shape in shapeList)
            {
                shape.PrintDebug();
            }
        }

    }
}
