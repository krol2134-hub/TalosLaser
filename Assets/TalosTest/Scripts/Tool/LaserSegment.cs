namespace TalosTest.Tool
{
    public class LaserSegment
    {
        public LaserInteractable Start { get; }
        public LaserInteractable End { get; }
        public ColorType ColorType { get; }

        public LaserSegment(LaserInteractable start, LaserInteractable end, ColorType colorType)
        {
            Start = start;
            End = end;
            ColorType = colorType;
        }

        public bool CheckMatchingBySides(LaserSegment otherSegment)
        { 
            var otherStart = otherSegment.Start;
            var otherEnd = otherSegment.End;
            
            return CheckMatchingBySides(otherStart, otherEnd);
        }
        
        public bool CheckMatchingBySides(LaserInteractable otherStart, LaserInteractable otherEnd)
        { 
            var isOneSideMatch = Start == otherStart && End == otherEnd;
            var isOtherSideMatch = Start == otherEnd && End == otherStart;
            
            return isOneSideMatch || isOtherSideMatch;
        }

        public override string ToString() => $"{ColorType} => {Start} → {End}";
    }
}