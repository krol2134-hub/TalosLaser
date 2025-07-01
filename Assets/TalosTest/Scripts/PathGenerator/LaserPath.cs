using System.Collections.Generic;
using TalosTest.Tool;

namespace TalosTest.PathGenerator
{
    public class LaserPath
    {
        public Generator SourceGenerator { get; }
        public List<LaserSegment> Segments { get; }
        public PathStatus Status { get; private set; }
        public BlockReason BlockReason { get; private set; }

        public LaserPath(List<LaserSegment> segments, Generator sourceGenerator, PathStatus status,
            BlockReason blockReason)
        {
            SourceGenerator = sourceGenerator;
            Segments = segments;
            Status = status;
            BlockReason = blockReason;
        }
    
        public void UpdateBlockStatus(BlockReason reason)
        {
            Status = PathStatus.Blocked;
            BlockReason = reason;
        }
    }
}