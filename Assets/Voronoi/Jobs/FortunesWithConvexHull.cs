using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Voronoi.Helpers;
using Voronoi.Structures;
using static Voronoi.Handlers.BeachLine;
using static Voronoi.Handlers.MinHeap;

namespace Voronoi.Jobs
{
	[BurstCompile(CompileSynchronously = true)]
	public struct FortunesWithConvexHull : IJob
	{
		public NativeArray<VSite> Sites;
		public NativeList<VEdge> Edges;
		public NativeArray<int> EdgesCount;
		public NativeMultiHashMap<int, int> Regions;
		public NativeHashMap<int, int> SiteIdIndexes;
		public NativeHashMap<int, int> SiteIndexIds;
		public NativeList<VSite> ConvexHull;

		// determined empirically for a random set of points
		private const float EventsLengthModifier = 1.05f;
		private const float DeletedEventsLengthModifier = 0.1f;

		public void Execute()
		{
			var eventsCount = 0;
			int eventIdSeq = 0;

			var edgesEnds = new NativeList<float2>(Edges.Capacity, Allocator.Temp);
			
			var capacity = Sites.Length * 2;
			
			var treeCount = 0;
			var rbTreeRoot = -1;
			var treeValue = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeLeft = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeRight = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeParent = new NativeArray<int>(capacity, Allocator.Temp); 
			var treePrevious = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeNext = new NativeArray<int>(capacity, Allocator.Temp); 
			var treeRed = new NativeArray<bool>(capacity, Allocator.Temp);

			var eventsLength = (int) (Sites.Length * EventsLengthModifier);
			var events = new NativeArray<FortuneEvent>(eventsLength, Allocator.Temp);
			
			var deletedCapacity = math.ceilpow2((int) (Sites.Length * DeletedEventsLengthModifier));
			var deleted = new NativeHashMap<int, byte>(deletedCapacity, Allocator.Temp);

			var arcSites = new NativeList<int>(capacity, Allocator.Temp);
			var arcEdges = new NativeList<int>(capacity, Allocator.Temp);
			var arcEvents = new NativeList<FortuneEventArc>(capacity, Allocator.Temp);
			

			for (var i = 0; i < Sites.Length; i++)
			{
				var site = Sites[i];
				SiteIdIndexes[site.Id] = i;
				SiteIndexIds[i] = site.Id;
				EventInsert(new FortuneEvent(ref eventIdSeq, i, site.X, site.Y), ref events, ref eventsCount);
			}

			for (var i = 0; i < capacity; i++)
			{
				treeValue[i] = -1;
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
					AddBeachArc(fEvent, ref Sites, ref SiteIndexIds, ref Edges, ref edgesEnds,
						ref arcSites, ref arcEdges, ref arcEvents,
						ref events, ref deleted, ref eventsCount, ref eventIdSeq, 
						ref treeValue, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeRed, 
						ref treeCount, ref rbTreeRoot);
				else
				{
					if (deleted.ContainsKey(fEvent.Id)) deleted.Remove(fEvent.Id);
					else RemoveBeachArc(fEvent, ref Sites, ref SiteIndexIds, ref Edges, ref edgesEnds, 
						ref arcSites, ref arcEdges, ref arcEvents,
						ref events, ref deleted, ref eventsCount, ref eventIdSeq, 
						ref treeValue, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeRed, 
						ref treeCount, ref rbTreeRoot);
				}
			}
			
			var newIndex = 0;
			var temp = new NativeList<float2>(4, Allocator.Temp);
			for (var i = 0; i < Edges.Length; i++)
			{
				VEdge edge;
				var n = Edges[i].Neighbor;
				if (n < 0)
				{
					edge = IsNotSet(edgesEnds[i]) ?
						new VEdge(Edges[i].Start, BuildRayEnd(i, ref temp), Edges[i].Left, Edges[i].Right) :
						new VEdge(Edges[i].Start, edgesEnds[i], Edges[i].Left, Edges[i].Right);
				}
				else
				{
					if (IsNotSet(edgesEnds[i]))
						edge = new VEdge( edgesEnds[n], BuildRayEnd(i, ref temp), Edges[i].Left, Edges[i].Right);
					else if (IsNotSet(edgesEnds[n]))
						edge = new VEdge(edgesEnds[i], BuildRayEnd(n, ref temp), Edges[i].Left, Edges[i].Right);
					else
						edge = new VEdge(edgesEnds[i], edgesEnds[n], Edges[i].Left, Edges[i].Right);
					i++;
				}

				Edges[newIndex] = edge;
				Regions.Add(edge.Left, newIndex);
				Regions.Add(edge.Right, newIndex);
				newIndex++;
			}
			EdgesCount[0] = newIndex;
			for (var i = Edges.Length - 1; i >= newIndex; i--) Edges.RemoveAtSwapBack(i);

			ConvexHull.AddRange(Handlers.ConvexHull.BuildConvexHull(Sites));
		}

		

		private float2 BuildRayEnd(int index, ref NativeList<float2> candidates)
		{
			var l = SiteIdIndexes[Edges[index].Left];
			var r = SiteIdIndexes[Edges[index].Right];
			var left = new float2(Sites[l].X, Sites[l].Y);
			var right = new float2(Sites[r].X, Sites[r].Y);
			var start = Edges[index].Start;

			float minX = -VGeometry.max;
			float minY = -VGeometry.max;
			float maxX = VGeometry.max;
			float maxY = VGeometry.max;

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
	            candidates.AddNoResize(topX);
            if (Within(bottomX.x, minX, maxX))
	            candidates.AddNoResize(bottomX);
            if (Within(leftY.y, minY, maxY))
	            candidates.AddNoResize(leftY);
            if (Within(rightY.y, minY, maxY))
	            candidates.AddNoResize(rightY);

            // reject candidates which don't align with the slope
            for (var i = candidates.Length - 1; i > -1; i--)
            {
                var candidate = candidates[i];
                // grab vector representing the edge
                var ax = candidate.x - start.x;
                var ay = candidate.y - start.y;
                if (slopeRun*ax + slopeRise*ay < 0) candidates.RemoveAtSwapBack(i);
            }

            switch (candidates.Length)
            {
	            // if there are two candidates we are outside the closer one is start
	            // the further one is the end
	            case 2:
	            {
		            var ax = candidates[0].x - start.x;
		            var ay = candidates[0].y - start.y;
		            var bx = candidates[1].x - start.x;
		            var by = candidates[1].y - start.y;
		            return ax*ax + ay*ay > bx*bx + @by*@by ? candidates[0] : candidates[1];
	            }
	            // if there is one candidate we are inside
	            case 1:
		            return candidates[0];
	            default:
		            // there were no candidates
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

		public void Dispose()
		{
			Sites.Dispose();
			Edges.Dispose();
			EdgesCount.Dispose();
			Regions.Dispose();
			SiteIndexIds.Dispose();
			SiteIdIndexes.Dispose();
			ConvexHull.Dispose();
		}

		public static FortunesWithConvexHull CreateJob(NativeSlice<VSite> sites)
		{
			const int regionsCapacity = 1 << 4;
			var arr = new NativeArray<VSite>(sites.Length, Allocator.Persistent);
			sites.CopyTo(arr);
			
			return new FortunesWithConvexHull
			{
				Sites = arr,
				Edges = new NativeList<VEdge>(arr.Length * 4, Allocator.Persistent),
				EdgesCount = new NativeArray<int>(1,  Allocator.Persistent),
				Regions = new NativeMultiHashMap<int, int>(regionsCapacity, Allocator.Persistent),
				SiteIdIndexes = new NativeHashMap<int, int>(arr.Length, Allocator.Persistent),
				SiteIndexIds = new NativeHashMap<int, int>(arr.Length, Allocator.Persistent),
				ConvexHull = new NativeList<VSite>((int)math.sqrt(arr.Length), Allocator.Persistent)
			};
		}
	}
}