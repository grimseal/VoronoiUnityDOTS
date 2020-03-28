using Unity.Collections;
using Unity.Mathematics;

namespace Voronoi.Structures
{
	public struct VEdge
	{

		public readonly int Index;
		public readonly float2 Start;
		public readonly float2 End;
		public readonly int Left;
		public readonly int Right;
		
		// internal float SlopeRise;
		// internal float SlopeRun;
		//
		// internal float? Slope;
		// internal float? Intercept;

		public readonly int Neighbor;

		public VEdge(int index, float2 startPoint, int leftSite, int rightSite, NativeHashMap<int, int> sitesMap)
		{
			Index = index;
			Start = startPoint;
			End = float2.zero;
			Left = sitesMap[leftSite];
			Right = sitesMap[rightSite];
			Neighbor = -1;
		}

		public VEdge(int index, float2 startPoint, int leftSite, int rightSite, int neighbor, NativeHashMap<int, int> sitesMap)
		{
			Index = index;
			Start = startPoint;
			End = float2.zero;
			Left = sitesMap[leftSite];
			Right = sitesMap[rightSite];
			Neighbor = neighbor;
		}

		public VEdge(int index, float2 start, float2 end, int left, int right)
		{
			Index = index;
			Start = start;
			End = end;
			Left = left;
			Right = right;
			Neighbor = -1;
		}

		public VEdge(int index, VEdge edge)
		{
			Index = index;
			Start = edge.Start;
			End = edge.End;
			Left = edge.Left;
			Right = edge.Right;
			Neighbor = edge.Neighbor;
		}
		
		public static bool operator ==(VEdge a, VEdge b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(VEdge a, VEdge b)
		{
			return !a.Equals(b);
		}

		private bool Equals(VEdge other)
		{
			return Left == other.Left && Right == other.Right;
		}

		public override bool Equals(object obj)
		{
			return obj is VEdge other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Left * 397) ^ Right;
			}
		}
		
		public static VEdge Null = new VEdge(-1, float2.zero, float2.zero, -1, -1);
	}
}