using ListDiff;﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuaMergeDriver.Structures;
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
            
            string mergePath = args[4];
            
            int blockSize = Int32.Parse(args[3]);
            
            int mergeConflicts = Merge(ancestorPath, ourPath, theirPath, blockSize, mergePath);
            Console.WriteLine(mergeConflicts);
            return mergeConflicts;
        }
        
        private static int Merge(string ancestorPath, string ourPath, string theirPath, int blockSize = 1000, string mergePath = null)
        {
            // hit objects, timing points, scroll velocities, preview points, editor layers
            // key counts, diff names
            // audio file, background file, banner file
            // map id, mapset id
            // song title, song artist, song source
            // tags, creator, description, genre
            // initial sv, sv mode, scratch key
            // custom audio samples, sound effects
            Qua ancestor = Qua.Parse(ancestorPath);
            Qua ours = Qua.Parse(ourPath);
            Qua theirs = Qua.Parse(theirPath);
            
            ancestor.Sort();
            ours.Sort();
            theirs.Sort();
            
            int mergeConflicts = 0;
            
            Console.WriteLine("Merging Layers...");
            
            // merge editor layers
            // what if a layer is removed from one branch but not other?
            // what happens to the notes?
            // how to treat layers that are hidden?
            // how to treat layers with different colors?
            // if any change is detected to a layer, ask user what layer to put notes in
            List<EditorLayerInfo> mergeLayers = new List<EditorLayerInfo>();
            if (ours.EditorLayers.SequenceEqual(theirs.EditorLayers, EditorLayerInfo.ByValueComparer))
                mergeLayers = ours.EditorLayers;
            else
            {
                if (ours.EditorLayers.SequenceEqual(ancestor.EditorLayers, EditorLayerInfo.ByValueComparer))
                {
                    mergeLayers = theirs.EditorLayers;
                    var layerMoves = new Dictionary<int, int>();
                    var diff = new ListDiff<EditorLayerInfo, EditorLayerInfo>(ours.EditorLayers, theirs.EditorLayers, EditorLayerInfo.ByValueComparer.Equals);
                    foreach (var action in diff.Actions)
                    {
                        if (action.ActionType != ListDiffActionType.Add)
                        {
                            int sourceLayer = ours.EditorLayers.FindIndex(x => x == action.SourceItem) + 1;
                            Console.WriteLine($"Which layer should our layer {sourceLayer} notes merge to?");
                            int destLayer = Convert.ToInt32(Console.ReadLine());
                            layerMoves.Add(sourceLayer, destLayer);
                        }
                    }
                    foreach (var hitObject in ours.HitObjects)
                    {
                        if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                            hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                    }
                    foreach (var hitObject in ancestor.HitObjects)
                    {
                        if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                            hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                    }
                }
                else if (theirs.EditorLayers.SequenceEqual(ancestor.EditorLayers, EditorLayerInfo.ByValueComparer))
                {
                    mergeLayers = ours.EditorLayers;
                    var layerMoves = new Dictionary<int, int>();
                    var diff = new ListDiff<EditorLayerInfo, EditorLayerInfo>(theirs.EditorLayers, ours.EditorLayers, EditorLayerInfo.ByValueComparer.Equals);
                    foreach (var action in diff.Actions)
                    {
                        if (action.ActionType != ListDiffActionType.Add)
                        {
                            int sourceLayer = theirs.EditorLayers.FindIndex(x => x == action.SourceItem) + 1;
                            Console.WriteLine($"Which layer should their layer {sourceLayer} notes merge to?");
                            int destLayer = Convert.ToInt32(Console.ReadLine());
                            layerMoves.Add(sourceLayer, destLayer);
                        }
                    }
                    foreach (var hitObject in theirs.HitObjects)
                    {
                        if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                            hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                    }
                    foreach (var hitObject in ancestor.HitObjects)
                    {
                        if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                            hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                    }
                }
                else
                {
                    // probably just take the union of the lists
                    // ask user which layer(s) to go before others? (does that work in a merge driver?)
                    // what if user wants to combine the different layers into one layer?
                    // after that need to change notes' layers accordingly
                    // TODO: figure out what to do here
                    
                    Console.WriteLine("Merge Conflict in Layers");
                    Console.WriteLine("How should it be resolved?");
                    Console.WriteLine("1. merge theirs into ours (1, 2, +3, +4 + 1, 2, +3 = 1, 2, +3(ours), +4(ours))");
                    Console.WriteLine("2. merge ours into theirs (1, 2, +3, +4 + 1, 2, +3 = 1, 2, +3(theirs), +4(ours))");
                    Console.WriteLine("3. ours then theirs (1, 2, +3, +4 + 1, 2, +3 = 1, 2, +3(ours), +4(ours), +3(theirs)");
                    Console.WriteLine("4. theirs then ours (1, 2, +3, +4 + 1, 2, +3 = 1, 2, +3(theirs), +3(ours), +4(ours)");
                    Console.WriteLine("5. only ours");
                    Console.WriteLine("6. only theirs");
                    
                    // from there edit layers and notes' layers accordingly
                    int input = Convert.ToInt32(Console.ReadLine());
                    switch (input)
                    {
                        // cases 1 and 2 don't need notes to be moved around
                        case 1:
                        {
                            mergeLayers = ours.EditorLayers;
                            int layerCountDiff = theirs.EditorLayers.Count - mergeLayers.Count;
                            if (layerCountDiff > 0)
                                mergeLayers.AddRange(theirs.EditorLayers.GetRange(mergeLayers.Count, layerCountDiff));
                            break;
                        }
                        case 2:
                        {
                            mergeLayers = theirs.EditorLayers;
                            int layerCountDiff = ours.EditorLayers.Count - mergeLayers.Count;
                            if (layerCountDiff > 0)
                                mergeLayers.AddRange(ours.EditorLayers.GetRange(mergeLayers.Count, layerCountDiff));
                            break;
                        }
                        // cases 3 and 4 need notes to be moved around
                        case 3:
                        {
                            mergeLayers = ours.EditorLayers.Union(theirs.EditorLayers).ToList();
                            // resulting layers from merge should be all of ours and then extra from theirs
                            // so their notes need to be moved
                            var layerMoves = new Dictionary<int, int>();
                            for (int oldLayerIndex = 0; oldLayerIndex < theirs.EditorLayers.Count; oldLayerIndex++)
                            {
                                int newLayerIndex = mergeLayers.FindIndex(x => x == theirs.EditorLayers[oldLayerIndex]);
                                if (newLayerIndex != oldLayerIndex)
                                    layerMoves.Add(oldLayerIndex, newLayerIndex);
                            }
                            foreach (var hitObject in theirs.HitObjects)
                            {
                                if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                                    hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                            }
                            break;
                        }
                        case 4:
                        {
                            mergeLayers = theirs.EditorLayers.Union(ours.EditorLayers).ToList();
                            var layerMoves = new Dictionary<int, int>();
                            for (int oldLayerIndex = 0; oldLayerIndex < ours.EditorLayers.Count; oldLayerIndex++)
                            {
                                int newLayerIndex = mergeLayers.FindIndex(x => x == ours.EditorLayers[oldLayerIndex]);
                                if (newLayerIndex != oldLayerIndex)
                                    layerMoves.Add(oldLayerIndex, newLayerIndex);
                            }
                            foreach (var hitObject in ours.HitObjects)
                            {
                                if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                                    hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                            }
                            break;
                        }
                        case 5:
                        {
                            mergeLayers = ours.EditorLayers;
                            var layerMoves = new Dictionary<int, int>();
                            for (int oldLayerIndex = 1; oldLayerIndex < theirs.EditorLayers.Count + 1; oldLayerIndex++)
                            {
                                Console.Write($"Which layer should their layer {oldLayerIndex} notes go to? ");
                                int newLayerIndex = Convert.ToInt32(Console.ReadLine());
                                layerMoves.Add(oldLayerIndex, newLayerIndex);
                            }
                            foreach (var hitObject in theirs.HitObjects)
                            {
                                if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                                    hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                            }
                            foreach (var hitObject in ancestor.HitObjects)
                            {
                                if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                                    hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                            }
                            break;
                        }
                        case 6:
                        {
                            mergeLayers = theirs.EditorLayers;
                            var layerMoves = new Dictionary<int, int>();
                            for (int oldLayerIndex = 1; oldLayerIndex < ours.EditorLayers.Count + 1; oldLayerIndex++)
                            {
                                Console.Write($"Which layer should our layer {oldLayerIndex} notes go to? ");
                                int newLayerIndex = Convert.ToInt32(Console.ReadLine());
                                layerMoves.Add(oldLayerIndex, newLayerIndex);
                            }
                            foreach (var hitObject in ours.HitObjects)
                            {
                                if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                                    hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                            }
                            foreach (var hitObject in ancestor.HitObjects)
                            {
                                if (layerMoves.Keys.Contains(hitObject.EditorLayer))
                                    hitObject.EditorLayer = layerMoves[hitObject.EditorLayer];
                            }
                            break;
                        }
                    }
                }
            }
            
            int minTime = Math.Min(ancestor.HitObjects[0].StartTime, 
                          Math.Min((int)ancestor.TimingPoints[0].StartTime,
                          Math.Min((int)ancestor.SliderVelocities[0].StartTime,
                          Math.Min(ours.HitObjects[0].StartTime,
                          Math.Min((int)ours.TimingPoints[0].StartTime,
                          Math.Min((int)ours.SliderVelocities[0].StartTime,
                          Math.Min(theirs.HitObjects[0].StartTime,
                          Math.Min((int)theirs.TimingPoints[0].StartTime,
                                   (int)theirs.SliderVelocities[0].StartTime))))))));
                                   
            Console.WriteLine("minTime: " + minTime);
                          
            int maxTime = Math.Max(ancestor.HitObjects.Last().StartTime, 
                          Math.Max((int)ancestor.TimingPoints.Last().StartTime,
                          Math.Max((int)ancestor.SliderVelocities.Last().StartTime,
                          Math.Max(ours.HitObjects.Last().StartTime,
                          Math.Max((int)ours.TimingPoints.Last().StartTime,
                          Math.Max((int)ours.SliderVelocities.Last().StartTime,
                          Math.Max(theirs.HitObjects.Last().StartTime,
                          Math.Max((int)theirs.TimingPoints.Last().StartTime,
                                   (int)theirs.SliderVelocities.Last().StartTime))))))));
                                   
            Console.WriteLine("maxTime: " + maxTime);
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
                    // TODO: figure out wtf to do here
                    else
                    {
                        mergeConflicts++;
                        Console.WriteLine($"Merge Conflict when merging block {i}");
                    }
                }
            }
            
            Console.WriteLine("Generating Merged Map");
            
            // TODO: update
            Qua mergeQua = new Qua
            {
                AudioFile = ancestor.AudioFile,
                SongPreviewTime = ancestor.SongPreviewTime,
                BackgroundFile = ancestor.BackgroundFile,
                BannerFile = ancestor.BannerFile,
                MapId = ancestor.MapId,
                MapSetId = ancestor.MapSetId,
                Mode = ancestor.Mode, // why would anyone merge 4k and 7k together
                Title = ancestor.Title,
                Artist = ancestor.Artist,
                Source = ancestor.Source,
                Tags = ancestor.Tags,
                Creator = ancestor.Creator,
                DifficultyName = ours.DifficultyName + " + " + theirs.DifficultyName, //temp
                Description = "merge result", //temp
                Genre = ancestor.Genre,
                InitialScrollVelocity = ancestor.InitialScrollVelocity,
                HasScratchKey = ancestor.HasScratchKey,
                CustomAudioSamples = ancestor.CustomAudioSamples // I suspect will have to work like layer merging
            };
            mergeQua.EditorLayers.AddRange(mergeLayers);
            mergeQua.SoundEffects.AddRange(ancestor.SoundEffects);
            
            /*if (ancestor.BPMDoesNotAffectScrollVelocity)
                mergeQua.NormalizeSVs();*/
            var objects = GenerateListsFromBlocks(mergeBlocks);
            mergeQua.HitObjects.AddRange(objects.HitObjects);
            mergeQua.TimingPoints.AddRange(objects.TimingPoints);
            mergeQua.SliderVelocities.AddRange(objects.ScrollVelocities);
            
            Console.WriteLine("Writing .qua File at " + mergePath);
            
            // git expected behavior: overwrite our file
            if (mergePath == null)
                mergeQua.Save(ourPath);
            // if path is specified, write new file/overwrite existing file
            else
                mergeQua.Save(mergePath);
                
            return mergeConflicts;
        }
        
        private static List<Block> GenerateBlocks(Qua map, int blockSize, int minTime, int maxTime)
        {
            List<Block> blocks = new List<Block>();
            
            int hitObjectIndex = 0;
            int timingPointIndex = 0;
            int scrollVelocityIndex = 0;
            
            for (int i = minTime; i < maxTime; i += blockSize)
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
    }
}
