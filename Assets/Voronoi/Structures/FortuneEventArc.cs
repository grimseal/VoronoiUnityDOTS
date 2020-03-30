namespace Voronoi.Structures
{
    public struct FortuneEventArc
    {
        public readonly int Id;
        public readonly float X;
        public readonly float Y;

        public bool Exists => Id > 0;

        public FortuneEventArc(FortuneEvent fortuneEvent)
        {
            Id = fortuneEvent.Id;
            X = fortuneEvent.X;
            Y = fortuneEvent.Y;
        }

        private FortuneEventArc(int id)
        {
            Id = id;
            X = 0;
            Y = 0;
        }
        
        
        public static readonly FortuneEventArc Null = new FortuneEventArc(-1);
    }
}