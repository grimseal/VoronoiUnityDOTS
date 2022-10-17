using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Voronoi.Structures;
using static Voronoi.BeachLine;
using static Voronoi.MinHeap;

namespace Voronoi.Jobs
{
	[BurstCompile(CompileSynchronously = true)]
	internal struct FortunesWithConvexHull : IJob
	{
		public NativeArray<VSite> Sites;
		public NativeList<VEdge> Edges;
		public NativeMultiHashMap<int, int> Regions;
		public NativeHashMap<int, int> SiteIdIndexes;
		public NativeHashMap<int, int> SiteIndexIds;
		public NativeList<VSite> ConvexHull;
		public float4 bounds;

		// determined empirically for a random set of points
		private const float EventsLengthModifier = 1.3f;
		private const float DeletedEventsLengthModifier = 0.1f;

		public void Execute()
		{
			var eventsCount = 0;
			var eventIdSeq = 0;

			var edgesEnds = new NativeList<float2>(Edges.Capacity, Allocator.Temp);
			
			var capacity = Sites.Length * 2;

			var eventsLength = (int) (Sites.Length * EventsLengthModifier);
			var events = new NativeArray<FortuneEvent>(eventsLength, Allocator.Temp);
			
			var deletedCapacity = math.ceilpow2((int) (Sites.Length * DeletedEventsLengthModifier));
			var deleted = new NativeHashMap<int, byte>(deletedCapacity, Allocator.Temp);

			var arcSites = new NativeList<int>(capacity, Allocator.Temp);
			var arcEdges = new NativeList<int>(capacity, Allocator.Temp);
			var arcEvents = new NativeList<FortuneEventArc>(capacity, Allocator.Temp);

			var tree = new RedBlackTree(capacity);
			tree.Reset();

			for (var i = 0; i < Sites.Length; i++)
			{
				var site = Sites[i];
				SiteIdIndexes[site.Id] = i;
				SiteIndexIds[i] = site.Id;
				EventInsert(new FortuneEvent(ref eventIdSeq, i, site.X, site.Y), ref events, ref eventsCount);
			}

			// init edge list
			while (eventsCount != 0)
			{
				var fEvent = EventPop(ref events, ref eventsCount);
				if (fEvent.IsSiteEvent)
					AddBeachArc(fEvent, ref Sites, ref SiteIndexIds, ref Edges, ref edgesEnds,
						ref arcSites, ref arcEdges, ref arcEvents,
						ref events, ref deleted, ref eventsCount, ref eventIdSeq, 
						ref tree);
				else
				{
					if (deleted.ContainsKey(fEvent.Id)) deleted.Remove(fEvent.Id);
					else RemoveBeachArc(fEvent, ref Sites, ref SiteIndexIds, ref Edges, ref edgesEnds, 
						ref arcSites, ref arcEdges, ref arcEvents,
						ref events, ref deleted, ref eventsCount, ref eventIdSeq, 
						ref tree);
				}
			}

			for (int i = 0; i < Edges.Length; i++)
			{
				var e = Edges[i];
				Edges[i] = new VEdge(e.Start, edgesEnds[i], e.Left, e.Right, e.Neighbor);
			}

			var index = 0;
			for (var i = 0; i < Edges.Length; i++)
			{
				var edge = Edges[i];
				var skipNext = edge.Neighbor >= 0;
				if (!ClipEdge(ref edge)) continue;
				if (skipNext) i++;
				Edges[index] = edge;
				Regions.Add(edge.Left, index);
				Regions.Add(edge.Right, index);
				index++;
			}
			Edges.RemoveRange(index, Edges.Length - index);

			ConvexHull.AddRange(Voronoi.ConvexHull.BuildConvexHull(Sites));
		}

		private static bool IsSet(float2 v) => !float.IsNaN(v.x);

		private bool ClipEdge(ref VEdge edge)
		{
			var minX = bounds.x;
			var minY = bounds.y;
			var maxX = bounds.z;
			var maxY = bounds.w;
			
			var accept = false;

			// edge = new VEdge(Edges[i].Start, edgesEnds[i], Edges[i].Left, Edges[i].Right, Edges[i].Neighbor);
			// edge = Edges[i];
			var edgeStart = edge.Start;
			var edgeEnd = edge.End;

			if (!IsSet(edgeEnd))
			{
				// accept = ClipRay(ref edge);
				var direction = GetDirection(edge);
				edge = new VEdge(edge.Start, edge.Start + direction * 100, edge.Left, edge.Right, edge.Neighbor);
				accept = ClipEdge(ref edge);
			}
			else
			{
				// Cohen–Sutherland
                var start = ComputeOutCode(edgeStart.x, edgeStart.y, minX, minY, maxX, maxY);
                var end = ComputeOutCode(edgeEnd.x, edgeEnd.y, minX, minY, maxX, maxY);

                while (true)
                {
                    if ((start | end) == 0)
                    {
                        accept = true;
                        break;
                    }
                    if ((start & end) != 0)
                    {
                        break;
                    }

                    float x = -1, y = -1;
                    var outcode = start != 0 ? start : end;

                    if ((outcode & 0x8) != 0) // top
                    {
                        x = edgeStart.x + (edgeEnd.x - edgeStart.x) * (maxY - edgeStart.y) / (edgeEnd.y - edgeStart.y);
                        y = maxY;
                    }
                    else if ((outcode & 0x4) != 0) // bottom
                    {
                        x = edgeStart.x + (edgeEnd.x - edgeStart.x) * (minY - edgeStart.y) / (edgeEnd.y - edgeStart.y);
                        y = minY;
                    }
                    else if ((outcode & 0x2) != 0) //right
                    {
                        y = edgeStart.y + (edgeEnd.y - edgeStart.y) * (maxX - edgeStart.x) / (edgeEnd.x - edgeStart.x);
                        x = maxX;
                    }
                    else if ((outcode & 0x1) != 0) //left
                    {
                        y = edgeStart.y + (edgeEnd.y - edgeStart.y) * (minX - edgeStart.x) / (edgeEnd.x - edgeStart.x);
                        x = minX;
                    }

                    if (outcode == start)
                    {
	                    edge = new VEdge(new float2(x, y), edgeEnd, edge.Left, edge.Right, edge.Neighbor);
                        start = ComputeOutCode(x, y, minX, minY, maxX, maxY);
                    }
                    else
                    {
	                    edge = new VEdge(edgeStart, new float2(x, y), edge.Left, edge.Right, edge.Neighbor);
                        end = ComputeOutCode(x, y, minX, minY, maxX, maxY);
                    }
                }
			}
			
			//if we have a neighbor
			if (edge.Neighbor >= 0)
			{
				//check it

				var neighbor = Edges[edge.Neighbor];
				var valid = ClipEdge(ref neighbor);

				//both are valid
				if (accept && valid)
				{
					edge = new VEdge(neighbor.End, edge.End, edge.Left, edge.Right, edge.Neighbor);
				}
				// this edge isn't valid, but the neighbor is
				// flip and set
				if (!accept && valid)
				{
					edge = new VEdge(neighbor.End, neighbor.Start, edge.Left, edge.Right, edge.Neighbor);
					accept = true;
				}
			}
			
			return accept;
		}
        
		private static int ComputeOutCode(float x, float y, float minX, float minY, float maxX, float maxY)
		{
			int code = 0;
			if (x.ApproxEqual(minX) || x.ApproxEqual(maxX))
			{ }
			else if (x < minX)
				code |= 0x1;
			else if (x > maxX)
				code |= 0x2;

			if (y.ApproxEqual(minY) || x.ApproxEqual(maxY))
			{ }
			else if (y < minY)
				code |= 0x4;
			else if (y > maxY)
				code |= 0x8;
			return code;
		}

		private float2 GetDirection(in VEdge edge)
		{
			var direction = Sites[SiteIdIndexes[edge.Right]].Point - Sites[SiteIdIndexes[edge.Left]].Point;
			var v = new float3(direction.xy, 0);
			var up = new float3(0, 0, 1);
			return math.normalize(math.cross(v, up).xy);
		}
		
		private bool ClipRay(ref VEdge edge)
		{
			var start = edge.Start;
			var left = edge.Left;
			var right = edge.Right;
			var l = Sites[SiteIdIndexes[left]].Point;
			var r = Sites[SiteIdIndexes[right]].Point;
			var slopeRise = l.x - r.x;
			var slopeRun = -(l.y - r.y);
			var slopes = new float2(slopeRise, slopeRun);
			if (slopeRise.ApproxEqual(0) || slopeRun.ApproxEqual(0)) throw new Exception();
			var slope = slopeRise / slopeRun;
			var intercept = start.x - slope * start.x;

			
			var minX = bounds.x;
			var minY = bounds.y;
			var maxX = bounds.z;
			var maxY = bounds.w;

			// horizontal ray
			if (slopeRise.ApproxEqual(0))
			{
				if (!Within(start.y, minY, maxY))
					return false;
				if (slopeRun > 0 && start.x > maxX)
					return false;
				if (slopeRun < 0 && start.x < minX)
					return false;
				if (Within(start.x, minX, maxX))
				{
					if (slopeRun > 0)
						edge = new VEdge(edge.Start, new float2(maxX, start.y), left, right, edge.Neighbor);
					else
						edge = new VEdge(edge.Start, new float2(minX, start.y), left, right, edge.Neighbor);
				}
				else
				{
					if (slopeRun > 0)
						edge = new VEdge(new float2(minX, start.y), new float2(maxX, start.y),
							left, right, edge.Neighbor);
					else
						edge = new VEdge(new float2(maxX, start.y), new float2(minX, start.y),
							left, right, edge.Neighbor);
				}
				return true;
			}

			// vertical ray
			if (slopeRun.ApproxEqual(0))
			{
				if (start.x < minX || start.x > maxX)
					return false;
				if (slopeRise > 0 && start.y > maxY)
					return false;
				if (slopeRise < 0 && start.y < minY)
					return false;
				if (Within(start.y, minY, maxY))
				{
					if (slopeRise > 0)
						edge = new VEdge(edge.Start, new float2(start.x, maxY), left, right, edge.Neighbor);
					else
						edge = new VEdge(edge.Start, new float2(start.x, minY), left, right, edge.Neighbor);
				}
				else
				{
					if (slopeRise > 0)
						edge = new VEdge(new float2(start.x, minY), new float2(start.x, maxY),
							left, right, edge.Neighbor);
					else
						edge = new VEdge(new float2(start.x, maxY), new float2(start.x, minY),
							left, right, edge.Neighbor);
				}
				return true;
			}

	        var topX = new float2(CalcX(slope, maxY, intercept), maxY);
            var bottomX = new float2(CalcX(slope, minY, intercept), minY);
            var leftY = new float2(minX, CalcY(slope, minX, intercept));
            var rightY = new float2(maxX, CalcY(slope, maxX, intercept));

            var candidates = new StructList4<float2>();
            if (Within(topX.x, minX, maxX) && IsAlign(topX, start, slopes)) candidates.Add(topX);
            if (Within(bottomX.x, minX, maxX) && IsAlign(bottomX, start, slopes)) candidates.Add(bottomX);
            if (Within(leftY.y, minY, maxY) && IsAlign(leftY, start, slopes)) candidates.Add(leftY);
            if (Within(rightY.y, minY, maxY) && IsAlign(rightY, start, slopes)) candidates.Add(rightY);


            switch (candidates.Length)
            {
	            case 2:
	            {
		            var ax = candidates[0].x - start.x;
		            var ay = candidates[0].y - start.y;
		            var bx = candidates[1].x - start.x;
		            var by = candidates[1].y - start.y;

		            if (ax * ax + ay * ay > bx * bx + by * by)
			            edge = new VEdge(candidates[1], candidates[0], left, right, edge.Neighbor);
		            else
			            edge = new VEdge(candidates[0], candidates[1], left, right, edge.Neighbor);

		            break;
	            }
	            case 1:
		            edge = new VEdge(edge.Start, candidates[0], left, right, edge.Neighbor);
		            break;
            }

            return IsSet(edge.End);
		}

		
		// reject candidates which don't align with the slope
		private static bool IsAlign(float2 candidate, float2 start, float2 slopes)
		{
			var ax = candidate.x - start.x;
			var ay = candidate.y - start.y;
			return !(slopes.x * ax + slopes.y * ay < 0);
		}
		
		private static bool IsAlign(float2 candidate, float2 start, double2 slopes)
		{
			var ax = candidate.x - start.x;
			var ay = candidate.y - start.y;
			return !(slopes.x * ax + slopes.y * ay < 0);
		}
		
		private static bool Within(float x, float a, float b)
		{
			return x.ApproxGreaterThanOrEqualTo(a) && x.ApproxLessThanOrEqualTo(b);
		}

		private static float CalcY(float m, float x, float b)
		{
			return m * x + b;
		}

		private static float CalcY(double m, double x, double b)
		{
			return (float)(m * x + b);
		}

		private static float CalcX(float m, float y, float b)
		{
			return (y - b) / m;
		}
		
		private static float CalcX(double m, double y, double b)
		{
			return (float)((y - b) / m);
		}

		public void Dispose()
		{
			Sites.Dispose();
			Edges.Dispose();
			Regions.Dispose();
			SiteIndexIds.Dispose();
			SiteIdIndexes.Dispose();
			ConvexHull.Dispose();
		}

		public static FortunesWithConvexHull CreateJob(NativeSlice<VSite> sites, float4 bounds)
		{
			const int regionsCapacity = 1 << 4;
			var arr = new NativeArray<VSite>(sites.Length, Allocator.Persistent);
			sites.CopyTo(arr);
			
			return new FortunesWithConvexHull
			{
				bounds = bounds,
				Sites = arr,
				Edges = new NativeList<VEdge>(arr.Length * 4, Allocator.Persistent),
				Regions = new NativeMultiHashMap<int, int>(regionsCapacity, Allocator.Persistent),
				SiteIdIndexes = new NativeHashMap<int, int>(arr.Length, Allocator.Persistent),
				SiteIndexIds = new NativeHashMap<int, int>(arr.Length, Allocator.Persistent),
				ConvexHull = new NativeList<VSite>((int)math.sqrt(arr.Length), Allocator.Persistent)
			};
		}

		private void DrawLine(VEdge edge, Color color)
		{
			DrawLine(edge.Start, edge.End, color);
		}
		
		private void DrawLine(float2 from, float2 to, Color color)
		{
			Debug.DrawLine(from.ToVector3(), to.ToVector3(), color, float.MaxValue);
		}
	}
}