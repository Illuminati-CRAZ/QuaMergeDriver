using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using QuaMergeDriver.Structures;
using Quaver.API.Enums;
using Quaver.API.Maps;
using Quaver.API.Maps.Structures;

namespace QuaMergeDriver
{
    public class Program
    {
        public static int Main(string[] args)
        {
            string ancestorPath = args[0];
            string ourPath = args[1];
            string theirPath = args[2];
            
            int blockSize = Int32.Parse(args[3]);
            
            int mergeConflicts = Merge(ancestorPath, ourPath, theirPath, blockSize);
            Console.WriteLine(mergeConflicts);
            return mergeConflicts;
        }
        
        private static int Merge(string ancestorPath, string ourPath, string theirPath, int blockSize = 1000)
        {
            // hitobjects, timing points, scroll velocities, preview points, editor layers
            // key counts, diff names
            // audio file, background file, banner file
            // map id, mapset id
            // song title, song artist, song source
            // tags, creator, description, genre
            // initial sv, sv mode, scratch key
            // custom audio samples, sound effects
            Qua ancestor = Qua.Parse(ancestorPath, false);
            Qua ours = Qua.Parse(ourPath, false);
            Qua theirs = Qua.Parse(theirPath, false);
            
            ancestor.Sort();
            ours.Sort();
            theirs.Sort();
            
            int mergeConflicts = 0;
            
            List<EditorLayerInfo> mergeLayers = GenerateIndexBasedMergeObjects<EditorLayerInfo>(ancestor.EditorLayers, ours.EditorLayers, theirs.EditorLayers,
                                                                                                ancestor.HitObjects, ours.HitObjects, theirs.HitObjects,
                                                                                                EditorLayerInfo.ByValueComparer,
                                                                                                typeof(HitObjectInfo).GetProperty("EditorLayer"));
            
            float? minTime = new float?[9]
            {
                ancestor.HitObjects.Count > 0 ? (float)ancestor.HitObjects[0].StartTime : (float?)null,
                ancestor.TimingPoints.Count > 0 ? ancestor.TimingPoints[0].StartTime : (float?)null,
                ancestor.SliderVelocities.Count > 0 ? ancestor.SliderVelocities[0].StartTime : (float?)null,
                ours.HitObjects.Count > 0 ? (float)ours.HitObjects[0].StartTime : (float?)null,
                ours.TimingPoints.Count > 0 ? ours.TimingPoints[0].StartTime : (float?)null,
                ours.SliderVelocities.Count > 0 ? ours.SliderVelocities[0].StartTime : (float?)null,
                theirs.HitObjects.Count > 0 ? (float)theirs.HitObjects[0].StartTime : (float?)null,
                theirs.TimingPoints.Count > 0 ? theirs.TimingPoints[0].StartTime : (float?)null,
                theirs.SliderVelocities.Count > 0 ? theirs.SliderVelocities[0].StartTime : (float?)null
            }.Min();
                                   
            Console.WriteLine("minTime: " + minTime);
                          
            float? maxTime = new float?[9]
            {
                ancestor.HitObjects.Count > 0 ? (float)ancestor.HitObjects.Last().StartTime : (float?)null,
                ancestor.TimingPoints.Count > 0 ? ancestor.TimingPoints.Last().StartTime : (float?)null,
                ancestor.SliderVelocities.Count > 0 ? ancestor.SliderVelocities.Last().StartTime : (float?)null,
                ours.HitObjects.Count > 0 ? (float)ours.HitObjects.Last().StartTime : (float?)null,
                ours.TimingPoints.Count > 0 ? ours.TimingPoints.Last().StartTime : (float?)null,
                ours.SliderVelocities.Count > 0 ? ours.SliderVelocities.Last().StartTime : (float?)null,
                theirs.HitObjects.Count > 0 ? (float)theirs.HitObjects.Last().StartTime : (float?)null,
                theirs.TimingPoints.Count > 0 ? theirs.TimingPoints.Last().StartTime : (float?)null,
                theirs.SliderVelocities.Count > 0 ? theirs.SliderVelocities.Last().StartTime : (float?)null
            }.Max();
                                   
            Console.WriteLine("maxTime: " + maxTime);
            
            List<Block> mergeBlocks;
            if (minTime != null && maxTime != null)
                mergeBlocks = GenerateMergeBlocks(ancestor, ours, theirs, (float)minTime, (float)maxTime, blockSize, ref mergeConflicts);
            else
                mergeBlocks = new List<Block>();
            
            Console.WriteLine("Generating Merged Map");
            
            Qua mergeQua = new Qua
            {
                AudioFile = MergeMetadata<string>(ancestor.AudioFile, ours.AudioFile, theirs.AudioFile, "MERGE CONFLICT", ref mergeConflicts),
                SongPreviewTime = MergeMetadata<int>(ancestor.SongPreviewTime, ours.SongPreviewTime, theirs.SongPreviewTime, -1, ref mergeConflicts),
                BackgroundFile = MergeMetadata<string>(ancestor.BackgroundFile, ours.BackgroundFile, theirs.BackgroundFile, "MERGE CONFLICT", ref mergeConflicts),
                BannerFile = MergeMetadata<string>(ancestor.BannerFile, ours.BannerFile, theirs.BannerFile, "MERGE CONFLICT", ref mergeConflicts),
                MapId = MergeMetadata<int>(ancestor.MapId, ours.MapId, theirs.MapId, -1, ref mergeConflicts),
                MapSetId = MergeMetadata<int>(ancestor.MapSetId, ours.MapSetId, theirs.MapSetId, -1, ref mergeConflicts),
                // why would anyone merge 4k and 7k together
                Mode = MergeMetadata<GameMode>(ancestor.Mode, ours.Mode, theirs.Mode, GameMode.Keys7, ref mergeConflicts),
                Title = MergeMetadata<string>(ancestor.Title, ours.Title, theirs.Title, "MERGE CONFLICT", ref mergeConflicts),
                Artist = MergeMetadata<string>(ancestor.Artist, ours.Artist, theirs.Artist, "MERGE CONFLICT", ref mergeConflicts),
                Source = MergeMetadata<string>(ancestor.Source, ours.Source, theirs.Source, "MERGE CONFLICT", ref mergeConflicts),
                Tags = MergeMetadata<string>(ancestor.Tags, ours.Tags, theirs.Tags, "MERGE CONFLICT", ref mergeConflicts),
                Creator = MergeMetadata<string>(ancestor.Creator, ours.Creator, theirs.Creator, "MERGE CONFLICT", ref mergeConflicts),
                DifficultyName = MergeMetadata<string>(ancestor.DifficultyName, ours.DifficultyName, theirs.DifficultyName, "MERGE CONFLICT", ref mergeConflicts),
                Description = MergeMetadata<string>(ancestor.Description, ours.Description, theirs.Description, "MERGE CONFLICT", ref mergeConflicts),
                Genre = MergeMetadata<string>(ancestor.Genre, ours.Genre, theirs.Genre, "MERGE CONFLICT", ref mergeConflicts),
                BPMDoesNotAffectScrollVelocity = MergeMetadata<bool>(ancestor.BPMDoesNotAffectScrollVelocity, ours.BPMDoesNotAffectScrollVelocity, theirs.BPMDoesNotAffectScrollVelocity, true, ref mergeConflicts),
                InitialScrollVelocity = MergeMetadata<float>(ancestor.InitialScrollVelocity, ours.InitialScrollVelocity, theirs.InitialScrollVelocity, -1, ref mergeConflicts),
                HasScratchKey = MergeMetadata<bool>(ancestor.HasScratchKey, ours.HasScratchKey, theirs.HasScratchKey, false, ref mergeConflicts),
                CustomAudioSamples = ours.CustomAudioSamples // I suspect will have to work like layer merging
            };
            mergeQua.EditorLayers.AddRange(mergeLayers);
            mergeQua.SoundEffects.AddRange(ours.SoundEffects);
            
            var objects = GenerateListsFromBlocks(mergeBlocks);
            mergeQua.HitObjects.AddRange(objects.HitObjects);
            mergeQua.TimingPoints.AddRange(objects.TimingPoints);
            mergeQua.SliderVelocities.AddRange(objects.ScrollVelocities);
            
            Console.WriteLine("Writing .qua File at " + ourPath);
            
            // git expected behavior: overwrite our file
            mergeQua.Save(ourPath);
                
            return mergeConflicts;
        }
        
        private static List<T> GenerateIndexBasedMergeObjects<T>(List<T> ancestor, List<T> ours, List<T> theirs,
                                                                 List<HitObjectInfo> ancestorHitObjects,
                                                                 List<HitObjectInfo> ourHitObjects,
                                                                 List<HitObjectInfo> theirHitObjects,
                                                                 IEqualityComparer<T> byValueComparer,
                                                                 PropertyInfo propertyInfo)
        {
            List<T> mergeResult;
            
            if (ours.SequenceEqual(theirs, byValueComparer))
            {
                mergeResult = ours;
                var changes = GenerateIndexChanges(ancestor, mergeResult, byValueComparer);
                ApplyIndexChanges(ancestorHitObjects, propertyInfo, changes);
            }
            else if (ours.SequenceEqual(ancestor, byValueComparer))
            {
                mergeResult = theirs;
                var changes = GenerateIndexChanges(ancestor, mergeResult, byValueComparer);
                ApplyIndexChanges(ancestorHitObjects, propertyInfo, changes);
                ApplyIndexChanges(ourHitObjects, propertyInfo, changes);
            }
            else if (theirs.SequenceEqual(ancestor, byValueComparer))
            {
                mergeResult = ours;
                var changes = GenerateIndexChanges(ancestor, mergeResult, byValueComparer);
                ApplyIndexChanges(ancestorHitObjects, propertyInfo, changes);
                ApplyIndexChanges(theirHitObjects, propertyInfo, changes);
            }
            else
            {
                Console.WriteLine("A Merge Conflict Occurred");
                Console.WriteLine("How should it be handled?");
                Console.WriteLine("1. Only ours");
                Console.WriteLine("2. Only theirs");
                Console.WriteLine("3. Ours then theirs");
                Console.WriteLine("4. Theirs then ours");
                Console.WriteLine("5. Merge theirs into ours");
                Console.WriteLine("6. Merge ours into theirs");
                int input = Convert.ToInt32(Console.ReadLine());
                switch (input)
                {
                    // only ours
                    case 1:
                    {
                        mergeResult = ours;
                        var changes = GenerateIndexChanges(theirs, mergeResult, byValueComparer);
                        ApplyIndexChanges(theirHitObjects, propertyInfo, changes);
                        break;
                    }
                    
                    // only theirs
                    case 2:
                    {
                        mergeResult = theirs;
                        var changes = GenerateIndexChanges(ours, mergeResult, byValueComparer);
                        ApplyIndexChanges(ourHitObjects, propertyInfo, changes);
                        break;
                    }
                    
                    // ours then theirs
                    case 3:
                    {
                        int layerCountDiff = theirs.Count - ours.Count;
                        mergeResult = ours;
                        if (layerCountDiff > 0)
                            mergeResult.AddRange(theirs.GetRange(ours.Count, layerCountDiff));
                            
                        var changes = GenerateIndexChanges(theirs, mergeResult, byValueComparer);
                        ApplyIndexChanges(theirHitObjects, propertyInfo, changes);
                        break;
                    }
                    
                    // theirs then ours
                    case 4:
                    {
                        int layerCountDiff = ours.Count - theirs.Count;
                        mergeResult = theirs;
                        if (layerCountDiff > 0)
                            mergeResult.AddRange(ours.GetRange(theirs.Count, layerCountDiff));
                            
                        var changes = GenerateIndexChanges(ours, mergeResult, byValueComparer);
                        ApplyIndexChanges(ourHitObjects, propertyInfo, changes);
                        break;
                    }
                    
                    // merge theirs into ours
                    case 5:
                    {
                        mergeResult = ours.Union(theirs, byValueComparer).ToList();
                        var changes = GenerateIndexChanges(theirs, mergeResult, byValueComparer);
                        ApplyIndexChanges(theirHitObjects, propertyInfo, changes);
                        break;
                    }
                    
                    // merge ours into theirs
                    case 6:
                    {
                        mergeResult = theirs.Union(ours, byValueComparer).ToList();
                        var changes = GenerateIndexChanges(ours, mergeResult, byValueComparer);
                        ApplyIndexChanges(ourHitObjects, propertyInfo, changes);
                        break;
                    }
                    
                    // merge theirs into ours
                    default:
                    {
                        mergeResult = ours.Union(theirs, byValueComparer).ToList();
                        var changes = GenerateIndexChanges(theirs, mergeResult, byValueComparer);
                        ApplyIndexChanges(theirHitObjects, propertyInfo, changes);
                        break;
                    }
                }
                
                // C# is dumb
                var ancestorChanges = GenerateIndexChanges(ancestor, mergeResult, byValueComparer);
                ApplyIndexChanges(ancestorHitObjects, propertyInfo, ancestorChanges);
            }
            
            return mergeResult;
        }
        
        private static Dictionary<int, int> GenerateIndexChanges<T>(List<T> source, List<T> dest, IEqualityComparer<T> byValueComparer)
        {
            var changes = new Dictionary<int, int>();
            for (int oldIndex = 0; oldIndex < source.Count; oldIndex++)
            {
                int newIndex = dest.FindIndex(x => byValueComparer.Equals(x, source[oldIndex]));
                if (oldIndex != newIndex)
                    changes.Add(oldIndex + 1, newIndex + 1);
            }
            
            return changes;
        }
        
        private static void ApplyIndexChanges(List<HitObjectInfo> hitObjects, PropertyInfo propertyInfo, Dictionary<int, int> changes)
        {
            foreach (var hitObject in hitObjects)
            {
                int oldIndex = (int)propertyInfo.GetValue(hitObject);
                if (changes.Keys.Contains(oldIndex))
                    propertyInfo.SetValue(hitObject, changes[oldIndex]);
            }
        }
        
        private static List<Block> GenerateMergeBlocks(Qua ancestor, Qua ours, Qua theirs, float minTime, float maxTime, int blockSize, ref int mergeConflicts)
        {
            Console.WriteLine("Generating Blocks...");
            
            List<Block> ancestorBlocks = GenerateBlocks(ancestor, blockSize, minTime, maxTime);
            List<Block> ourBlocks = GenerateBlocks(ours, blockSize, minTime, maxTime);
            List<Block> theirBlocks = GenerateBlocks(theirs, blockSize, minTime, maxTime);
            
            List<Block> mergeBlocks = new List<Block>();
            
            Console.WriteLine("Merging Blocks...");
            
            // merge blocks
            for (int i = 0; i < ancestorBlocks.Count; i++)
            {
                // if our and their block are the same, using either block for the merge works
                if (ourBlocks[i].Equals(theirBlocks[i]))
                    mergeBlocks.Add(ourBlocks[i]);
                // if they aren't the same, check if one of the two blocks is uniquely different from ancestor's
                else
                {
                    // our block hasn't changed, so use their block
                    if (ourBlocks[i].Equals(ancestorBlocks[i]))
                        mergeBlocks.Add(theirBlocks[i]);
                    // their block hasn't changed, so use ours
                    else if (theirBlocks[i].Equals(ancestorBlocks[i]))
                        mergeBlocks.Add(ourBlocks[i]);
                    // both have changed, requires manual editting by user
                    // this could be rewritten to just check each object type's block instead of whole blocks
                    // alternatively, check blocks, and then handle each type when merge conflict
                    // because maybe the SV relies on hitobjects or something
                    else
                    {
                        List<HitObjectInfo> hitObjects;
                        List<TimingPointInfo> timingPoints;
                        List<SliderVelocityInfo> scrollVelocities;
                        
                        if (!ourBlocks[i].HitObjects.SequenceEqual(theirBlocks[i].HitObjects, HitObjectInfo.ByValueComparer))
                        {
                            Console.WriteLine($"Hitobject merge conflict in block {i}");
                            hitObjects = HandleBlockObjectMergeConflict<HitObjectInfo>(ourBlocks[i].HitObjects, theirBlocks[i].HitObjects, HitObjectInfo.ByValueComparer, ref mergeConflicts);
                        }
                        else
                            hitObjects = ourBlocks[i].HitObjects;
                            
                        if (!ourBlocks[i].TimingPoints.SequenceEqual(theirBlocks[i].TimingPoints, TimingPointInfo.ByValueComparer))
                        {
                            Console.WriteLine($"Timing point merge conflict in block {i}");
                            timingPoints = HandleBlockObjectMergeConflict<TimingPointInfo>(ourBlocks[i].TimingPoints, theirBlocks[i].TimingPoints, TimingPointInfo.ByValueComparer, ref mergeConflicts);
                        }
                        else
                            timingPoints = ourBlocks[i].TimingPoints;
                            
                        if (!ourBlocks[i].ScrollVelocities.SequenceEqual(theirBlocks[i].ScrollVelocities, SliderVelocityInfo.ByValueComparer))
                        {
                            Console.WriteLine($"Scroll velocity merge conflict in block {i}");
                            scrollVelocities = HandleBlockObjectMergeConflict<SliderVelocityInfo>(ourBlocks[i].ScrollVelocities, theirBlocks[i].ScrollVelocities, SliderVelocityInfo.ByValueComparer, ref mergeConflicts);
                        }
                        else
                            scrollVelocities = ourBlocks[i].ScrollVelocities;
                            
                        mergeBlocks.Add(new Block(hitObjects, timingPoints, scrollVelocities));
                    }
                }
            }
            
            return mergeBlocks;
        }
        
        private static List<Block> GenerateBlocks(Qua map, int blockSize, float minTime, float maxTime)
        {
            List<Block> blocks = new List<Block>();
            
            int hitObjectIndex = 0;
            int timingPointIndex = 0;
            int scrollVelocityIndex = 0;
            
            for (float i = minTime; i <= maxTime; i += blockSize)
            {
                var block = new Block()
                {
                    HitObjects = new List<HitObjectInfo>(),
                    TimingPoints = new List<TimingPointInfo>(),
                    ScrollVelocities = new List<SliderVelocityInfo>()
                };
                while (hitObjectIndex < map.HitObjects.Count && map.HitObjects[hitObjectIndex].StartTime < i + blockSize)
                {
                    block.HitObjects.Add(map.HitObjects[hitObjectIndex]);
                    hitObjectIndex++;
                }
                while (timingPointIndex < map.TimingPoints.Count && map.TimingPoints[timingPointIndex].StartTime < i + blockSize)
                {
                    block.TimingPoints.Add(map.TimingPoints[timingPointIndex]);
                    timingPointIndex++;
                }
                while (scrollVelocityIndex < map.SliderVelocities.Count && map.SliderVelocities[scrollVelocityIndex].StartTime < i + blockSize)
                {
                    block.ScrollVelocities.Add(map.SliderVelocities[scrollVelocityIndex]);
                    scrollVelocityIndex++;
                }
                blocks.Add(block);
            }
            
            return blocks;
        }
        
        private static (List<HitObjectInfo> HitObjects, List<TimingPointInfo> TimingPoints, List<SliderVelocityInfo> ScrollVelocities) GenerateListsFromBlocks(List<Block> blocks)
        {
            List<HitObjectInfo> hitObjects = new List<HitObjectInfo>();
            List<TimingPointInfo> timingPoints = new List<TimingPointInfo>();
            List<SliderVelocityInfo> scrollVelocities = new List<SliderVelocityInfo>();
            
            foreach (Block block in blocks)
            {
                hitObjects.AddRange(block.HitObjects);
                timingPoints.AddRange(block.TimingPoints);
                scrollVelocities.AddRange(block.ScrollVelocities);
            }
            
            return (hitObjects, timingPoints, scrollVelocities);
        }
        
        private static List<T> HandleBlockObjectMergeConflict<T>(List<T> ours, List<T> theirs, IEqualityComparer<T> byValueComparer, ref int mergeConflicts)
        {
            Console.WriteLine("How should the conflict be handled?");
            Console.WriteLine("1. Use ours");
            Console.WriteLine("2. Use theirs");
            Console.WriteLine("3. Use both");
            Console.WriteLine("4. Use neither (manually handle)");
            
            int input = Convert.ToInt32(Console.ReadLine());
            switch (input)
            {
                case 1:
                {
                    return ours;
                }
                case 2:
                {
                    return theirs;
                }
                case 3:
                {
                    // should mean no stacked notes that result from merging
                    return ours.Union(theirs, byValueComparer).ToList();
                }
                default:
                {
                    mergeConflicts++;
                    return new List<T>();
                }
            }
        }
        
        public static T MergeMetadata<T>(T ancestor, T ours, T theirs, T conflict, ref int mergeConflicts)
        {
            if (Equals(ours, theirs))
                return ours;
            else if (Equals(ours, ancestor))
                return theirs;
            else if (Equals(theirs, ancestor))
                return ours;
            else
            {
                mergeConflicts++;
                return conflict;
            }
        }
    }
}
