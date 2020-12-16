using System;
using System.IO;
using Quaver.API.Maps;

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
            
            int mergeConflict = Merge(ancestorPath, ourPath, theirPath, blockSize);
            return mergeConflict;
        }
        
        private static int Merge(string ancestorPath, string ourPath, string theirPath, int blockSize = 1000)
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
            
            int mergeConflict = 0;
                
            return mergeConflict;
        }
    }
}
