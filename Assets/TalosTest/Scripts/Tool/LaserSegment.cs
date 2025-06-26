namespace TalosTest.Tool
{
    public class LaserSegment
    {
        public LaserInteractable From { get; }
        public LaserInteractable To { get; }

        public LaserSegment(LaserInteractable from, LaserInteractable to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => $"{From} → {To}";
    }
}