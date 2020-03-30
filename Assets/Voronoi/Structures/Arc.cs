namespace Voronoi.Structures
{
	public struct Arc
	{
		public readonly int Site;

		public int Edge;

		public FortuneEvent Event;

		public Arc(int siteIndex)
		{
			Site = siteIndex;
			Edge = -1;
			Event = new FortuneEvent();
		}
	}
}