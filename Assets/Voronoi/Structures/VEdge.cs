using Unity.Collections;
using Unity.Mathematics;

namespace Voronoi.Structures
{
	public struct VEdge
	{
		public readonly float2 Start;
		public readonly float2 End;
		public readonly int Left;
		public readonly int Right;
		public readonly int Neighbor;

		public VEdge(float2 startPoint, int leftSite, int rightSite, ref NativeHashMap<int, int> sitesMap)
		{
			Start = startPoint;
			End = float2.zero;
			Left = sitesMap[leftSite];
			Right = sitesMap[rightSite];
			Neighbor = -1;
		}

		public VEdge(float2 startPoint, int leftSite, int rightSite, int neighbor, ref NativeHashMap<int, int> sitesMap)
		{
			Start = startPoint;
			End = float2.zero;
			Left = sitesMap[leftSite];
			Right = sitesMap[rightSite];
			Neighbor = neighbor;
		}

		public VEdge(float2 start, float2 end, int left, int right)
		{
			Start = start;
			End = end;
			Left = left;
			Right = right;
			Neighbor = -1;
		}
		

		public VEdge(float2 start, float2 end, int left, int right, int neighbor)
		{
			Start = start;
			End = end;
			Left = left;
			Right = right;
			Neighbor = neighbor;
		}
		

		public VEdge(double2 start, double2 end, int left, int right)
		{
			Start = (float2) start;
			End = (float2) end;
			Left = left;
			Right = right;
			Neighbor = -1;
		}
		public VEdge(float2 start, double2 end, int left, int right)
		{
			Start = start;
			End = (float2) end;
			Left = left;
			Right = right;
			Neighbor = -1;
		}

		public VEdge(double2 start, float2 end, int left, int right)
		{
			Start = (float2) start;
			End = end;
			Left = left;
			Right = right;
			Neighbor = -1;
		}

		public bool Equals(VEdge other)
		{
			return Left == other.Left && Right == other.Right;
		}

		public static readonly VEdge Null = new VEdge(float2.zero, float2.zero, -1, -1);

		public bool IsRay => float.IsNaN(End.x);

		public VEdge GetFlip() => new VEdge(End, Start, Right, Left);
	}
}