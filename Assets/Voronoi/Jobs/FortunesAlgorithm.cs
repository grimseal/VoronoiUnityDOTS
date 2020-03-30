using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Voronoi.Helpers;
using Voronoi.Structures;
using static Voronoi.Handlers.BeachLine;
using static Voronoi.Handlers.MinHeap;

namespace Voronoi.Jobs
{
	[BurstCompile]
	public struct FortunesAlgorithm : IJob
	{
		public NativeArray<VSite> Sites;
		public NativeList<VEdge> Edges;
		public NativeMultiHashMap<int, VEdge> Regions;
		public NativeHashMap<int, int> SiteIdIndexes;
		public NativeHashMap<int, int> SiteIndexIds;

		public void Execute()
		{
			var eventsCount = 0;
			int eventIdSeq = 0;

			var edgesEnds = new NativeList<float2>(Edges.Capacity, Allocator.Temp);
			var treeCount = 0;
			var rbTreeRoot = -1;
			var capacity = Sites.Length * 2;
			var treeArc = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeLeft = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeRight = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeParent = new NativeArray<int>(capacity, Allocator.Temp); 
			var treePrevious = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeNext = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeRed = new NativeArray<bool>(capacity, Allocator.Temp);
			var eventsLength = (int) (Sites.Length * 1.05f);
			var events = new NativeArray<FortuneEvent>(eventsLength, Allocator.Temp);
			var beachSections = new NativeList<Arc>(capacity, Allocator.Temp);
			var deletedCapacity = math.ceilpow2((int) (Sites.Length * 0.1f));
			var deleted = new NativeHashMap<int, byte>(deletedCapacity, Allocator.Temp);

			for (var i = 0; i < Sites.Length; i++)
			{
				var site = Sites[i];
				SiteIdIndexes[site.Id] = i;
				SiteIndexIds[i] = site.Id;
				EventInsert(new FortuneEvent(ref eventIdSeq, i, site.X, site.Y), ref events, ref eventsCount);
			}

			for (var i = 0; i < capacity; i++)
			{
				treeArc[i] = -1;
				treeLeft[i] = -1;
				treeRight[i] = -1;
				treeParent[i] = -1;
				treePrevious[i] = -1;
				treeNext[i] = -1;
			}

			while (eventsCount != 0)
			{
				var fEvent = EventPop(ref events, ref eventsCount);
				if (fEvent.IsSiteEvent)
					AddBeachArc(fEvent, ref Sites, ref SiteIndexIds, ref Edges, ref edgesEnds, ref beachSections,
						ref events, ref deleted, ref eventsCount, ref eventIdSeq, 
						ref treeArc, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeRed, 
						ref treeCount, ref rbTreeRoot);
				else
				{
					if (deleted.ContainsKey(fEvent.Id)) deleted.Remove(fEvent.Id);
					else RemoveBeachArc(fEvent, ref Sites, ref SiteIndexIds, ref Edges, ref edgesEnds, ref beachSections,
						ref events, ref deleted, ref eventsCount, ref eventIdSeq, 
						ref treeArc, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeRed, 
						ref treeCount, ref rbTreeRoot);
				}
			}

			var newIndex = 0;
			var newEdges = new NativeList<VEdge>(Edges.Capacity, Allocator.Temp);
			var temp = new NativeList<float2>(4, Allocator.Temp);
			for (var i = 0; i < Edges.Length; i++)
			{
				VEdge edge;
				var n = Edges[i].Neighbor;
				if (n < 0)
				{
					edge = IsNotSet(edgesEnds[i]) ?
						new VEdge(newIndex, Edges[i].Start, BuildRayEnd(i, ref temp), Edges[i].Left, Edges[i].Right) :
						new VEdge(newIndex, Edges[i].Start, edgesEnds[i], Edges[i].Left, Edges[i].Right);
				}
				else
				{
					if (IsNotSet(edgesEnds[i]))
						edge = new VEdge(newIndex, edgesEnds[n], BuildRayEnd(i, ref temp), Edges[i].Left, Edges[i].Right);
					else if (IsNotSet(edgesEnds[n]))
						edge = new VEdge(newIndex, edgesEnds[i], BuildRayEnd(n, ref temp), Edges[i].Left, Edges[i].Right);
					else
						edge = new VEdge(newIndex, edgesEnds[i], edgesEnds[n], Edges[i].Left, Edges[i].Right);
					i++;
				}
				newEdges.Add(edge);
				Regions.Add(edge.Left, edge);
				Regions.Add(edge.Right, edge);
				newIndex++;
			}
			Debug.Log(Edges.Length);
			Edges.Clear();
			Edges.AddRange(newEdges);
			Debug.Log(Edges.Length);
		}

		public void Dispose()
		{
			Sites.Dispose();
			Edges.Dispose();
			Regions.Dispose();
			SiteIndexIds.Dispose();
			SiteIdIndexes.Dispose();
		}
		
		private static readonly float Max = math.sqrt(math.sqrt(float.MaxValue));

		private float2 BuildRayEnd(int index, ref NativeList<float2> candidates)
		{
			var l = SiteIdIndexes[Edges[index].Left];
			var r = SiteIdIndexes[Edges[index].Right];
			var left = new float2(Sites[l].X, Sites[l].Y);
			var right = new float2(Sites[r].X, Sites[r].Y);
			var start = Edges[index].Start;

			float minX = -Max;
			float minY = -Max;
			float maxX = Max;
			float maxY = Max;

	        var slopeRise = left.x - right.x;
	        var slopeRun = -(left.y - right.y);
	        var slope = slopeRise / slopeRun;
	        var intercept = start.x - slope*start.x;
	        
	        //horizontal ray
	        if (VMath.ApproxEqual(slopeRise, 0))
		        return slopeRun > 0 ? new float2(maxX, start.y) : new float2(minX, start.y);

	        //vertical ray
	        if (VMath.ApproxEqual(slopeRun, 0))
		        return slopeRise > 0 ? new float2(start.x, maxY) : new float2(start.x, minY);

	        var topX = new float2(CalcX(slope, maxY, intercept), maxY);
            var bottomX = new float2(CalcX(slope, minY, intercept), minY);
            var leftY = new float2(minX, CalcY(slope, minX, intercept));
            var rightY = new float2(maxX, CalcY(slope, maxX, intercept));

            candidates.Clear();

            if (Within(topX.x, minX, maxX))
	            candidates.Add(topX);
            if (Within(bottomX.x, minX, maxX))
	            candidates.Add(bottomX);
            if (Within(leftY.y, minY, maxY))
	            candidates.Add(leftY);
            if (Within(rightY.y, minY, maxY))
	            candidates.Add(rightY);

            //reject candidates which don't align with the slope
            for (var i = candidates.Length - 1; i > -1; i--)
            {
                var candidate = candidates[i];
                //grab vector representing the edge
                var ax = candidate.x - start.x;
                var ay = candidate.y - start.y;
                if (slopeRun*ax + slopeRise*ay < 0) candidates.RemoveAtSwapBack(i);
            }

            switch (candidates.Length)
            {
	            //if there are two candidates we are outside the closer one is start
	            //the further one is the end
	            case 2:
	            {
		            var ax = candidates[0].x - start.x;
		            var ay = candidates[0].y - start.y;
		            var bx = candidates[1].x - start.x;
		            var by = candidates[1].y - start.y;
		            return ax*ax + ay*ay > bx*bx + @by*@by ? candidates[0] : candidates[1];
	            }
	            //if there is one candidate we are inside
	            case 1:
		            return candidates[0];
	            default:
		            //there were no candidates
		            return new float2(float.MinValue, float.MinValue);
            }
		}

		private static float CalcY(float m, float x, float b)
		{
			return m * x + b;
		}

		private static float CalcX(float m, float y, float b)
		{
			return (y - b) / m;
		}
		
		private static bool Within(float x, float a, float b)
		{
			return VMath.ApproxGreaterThanOrEqualTo(x, a) && VMath.ApproxLessThanOrEqualTo(x, b);
		}
		
		public static bool IsNotSet(float2 v)
		{
			return v.x <= float.MinValue || v.y <= float.MinValue;
		}


		public static FortunesAlgorithm CreateJob(NativeArray<VSite> sites)
		{
			var initialCapacity = math.ceilpow2(sites.Length * 4);
			var edges = new NativeList<VEdge>(initialCapacity, Allocator.Persistent);
			const int regionsCapacity = 1 << 4;

			return new FortunesAlgorithm
			{
				Sites = sites,
				Edges = edges,
				Regions = new NativeMultiHashMap<int, VEdge>(regionsCapacity, Allocator.Persistent),
				SiteIdIndexes = new NativeHashMap<int, int>(sites.Length, Allocator.Persistent),
				SiteIndexIds = new NativeHashMap<int, int>(sites.Length, Allocator.Persistent)
			};
		}
	}
}