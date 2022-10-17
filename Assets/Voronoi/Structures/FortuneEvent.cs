using Unity.Mathematics;

namespace Voronoi.Structures
{
	internal struct FortuneEvent
	{
		public readonly int Id;

		public readonly float X;

		public readonly float Y;

		public readonly ushort Site;

		public readonly float YCenter;

		public readonly int Node;

		/// <summary>
		/// Site/Circle event type flag
		/// </summary>
		public readonly bool IsSiteEvent;

		/// <summary>
		/// Site event constructor
		/// </summary>
		/// <param name="eventIdSeq"></param>
		/// <param name="siteIndex"></param>
		/// <param name="siteX"></param>
		/// <param name="siteY"></param>
		public FortuneEvent(ref int eventIdSeq, int siteIndex, float siteX, float siteY)
		{
			IsSiteEvent = true;
			Id = eventIdSeq++;
			Site = (ushort) siteIndex;
			X = siteX;
			Y = siteY;
			YCenter = float.MaxValue;
			Node = -1;
		}

		/// <summary>
		/// Circle event constructor
		/// </summary>
		/// <param name="eventIdSeq"></param>
		/// <param name="point"></param>
		/// <param name="yCenter"></param>
		/// <param name="nodeIndex"></param>
		public FortuneEvent(ref int eventIdSeq, ref float2 point, float yCenter, int nodeIndex)
		{
			IsSiteEvent = false;
			Id = eventIdSeq++;
			X = point.x;
			Y = point.y;
			YCenter = yCenter;
			Node = nodeIndex;
			Site = ushort.MaxValue;
		}
	}
}