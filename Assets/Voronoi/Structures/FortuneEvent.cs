using Unity.Mathematics;

namespace Voronoi.Structures
{
	public struct FortuneEvent
	{
		public static int MaxId;
		
		public readonly int Id;

		public readonly byte Type;

		public readonly float X;

		public readonly float Y;

		public readonly ushort Site;

		public readonly float YCenter;

		public readonly int Node;

		public bool Exists => Type != 0;

		public bool IsSiteEvent => Type == SiteEventType;

		public bool IsCircleEvent => Type == CircleEventType;

		public FortuneEvent(ref int eventIdSeq, int siteIndex, float siteX, float siteY)
		{
			// Exists = true;
			Type = SiteEventType;
			Id = eventIdSeq++;
			Site = (ushort) siteIndex;
			X = siteX;
			Y = siteY;
			YCenter = float.MaxValue;
			Node = -1;
		}

		public FortuneEvent(ref int eventIdSeq, ref float2 point, float yCenter, int nodeIndex)
		{
			// Exists = true;
			Type = CircleEventType;
			Id = eventIdSeq++;
			X = point.x;
			Y = point.y;
			YCenter = yCenter;
			Node = nodeIndex;
			Site = ushort.MaxValue;
		}

		// public int CompareTo(object obj)
		// {
		// 	var other = (FortuneEvent) obj;
		// 	var c = Y.CompareTo(other.Y);
		// 	return c == 0 ? X.CompareTo(other.X) : c;
		// }
		
		

		private const byte SiteEventType = 1;

		private const byte CircleEventType = 2;
	}
}