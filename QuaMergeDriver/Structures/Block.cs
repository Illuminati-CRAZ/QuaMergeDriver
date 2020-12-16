using System;
using System.Collections.Generic;
using System.Linq;
using Quaver.API.Maps.Structures;

namespace QuaMergeDriver.Structures
{
    public class Block
    {
        public List<HitObjectInfo> HitObjects { get; set; }
        
        public List<TimingPointInfo> TimingPoints { get; set; }
        
        public List<SliderVelocityInfo> ScrollVelocities { get; set; }
        
        public Block() {}
        
        public Block(List<HitObjectInfo> hitObjects, List<TimingPointInfo> timingPoints, List<SliderVelocityInfo> scrollVelocities)
        {
            HitObjects = hitObjects;
            TimingPoints = timingPoints;
            ScrollVelocities = scrollVelocities;
        }
        
        // based off of Quaver.API.Maps.Qua.EqualByValue()
        public bool Equals(Block other)
        {
            return HitObjects.SequenceEqual(other.HitObjects, HitObjectInfo.ByValueComparer)
                   && TimingPoints.SequenceEqual(other.TimingPoints, TimingPointInfo.ByValueComparer)
                   && ScrollVelocities.SequenceEqual(other.ScrollVelocities, SliderVelocityInfo.ByValueComparer);
        }
    }
}