namespace TalosTest.Tool
{
    public class LaserSegment
    {
        public LaserInteractable Start { get; }
        public LaserInteractable End { get; }

        public LaserSegment(LaserInteractable start, LaserInteractable end)
        {
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start} → {End}";
    }
}