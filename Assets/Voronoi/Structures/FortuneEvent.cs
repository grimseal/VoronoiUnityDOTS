using Unity.Mathematics;

namespace Voronoi.Structures
{
	public struct FortuneEvent
	{
		public enum EventType
		{
			Site,
			Circle
		}

		public readonly bool Exists;
		
		public readonly int Id;

		public readonly EventType Type;

		public readonly float X;

		public readonly float Y;

		public readonly int Site;

		public readonly float YCenter;

		public readonly int Node;

		public FortuneEvent(ref int eventIdSeq, int siteIndex, float siteX, float siteY)
		{
			Exists = true;
			Type = EventType.Site;
			Id = eventIdSeq++;
			Site = siteIndex;
			X = siteX;
			Y = siteY;
			YCenter = float.MaxValue;
			Node = -1;
		}

		public FortuneEvent(ref int eventIdSeq, ref float2 point, float yCenter, int nodeIndex)
		{
			Exists = true;
			Type = EventType.Circle;
			Id = eventIdSeq++;
			X = point.x;
			Y = point.y;
			YCenter = yCenter;
			Node = nodeIndex;
			Site = -1;
		}

		// public int CompareTo(object obj)
		// {
		// 	var other = (FortuneEvent) obj;
		// 	var c = Y.CompareTo(other.Y);
		// 	return c == 0 ? X.CompareTo(other.X) : c;
		// }
	}
}