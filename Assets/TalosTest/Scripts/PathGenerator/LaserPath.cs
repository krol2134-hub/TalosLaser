using System.Collections.Generic;
using TalosTest.Tool;

namespace TalosTest.PathGenerator
{
    public class LaserPath
    {
        public List<LaserSegment> Segments { get; }
        public PathType Type { get; set; }
        public BlockReason BlockReason { get; set; }
        public LaserInteractable BlockConnector { get; set; }
        public Generator SourceGenerator { get; }

        public LaserPath(List<LaserSegment> segments, PathType type, Generator sourceGenerator,
            BlockReason blockReason = BlockReason.None)
        {
            Segments = segments;
            Type = type;
            SourceGenerator = sourceGenerator;
            BlockReason = blockReason;
        }

        public void UpdateBlockConnector(LaserInteractable laserInteractable)
        {
            Type = PathType.Blocked;
            BlockReason = BlockReason.LogicalBlock;
            BlockConnector = laserInteractable;
        }
    
        public void UpdateBlockStatus(BlockReason reason)
        {
            Type = PathType.Blocked;
            BlockReason = reason;
        }
    }
}