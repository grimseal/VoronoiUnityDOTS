// ReSharper disable CheckNamespace
using Unity.Collections;
using Unity.Mathematics;
using Voronoi.Structures;
using static Voronoi.MinHeap;
using static Voronoi.RedBlackTree;

namespace Voronoi
{
	internal static class BeachLine
	{
		public static void AddBeachArc(
			FortuneEvent fortuneEvent,
			ref NativeArray<VSite> sites,
			ref NativeHashMap<int, int> sitesMap,
			ref NativeList<VEdge> edges,
			ref NativeList<float2> edgesEnds,
			ref NativeList<int> arcSites,
			ref NativeList<int> arcEdges,
			ref NativeList<FortuneEventArc> arcEvents,
			ref NativeArray<FortuneEvent> events, 
			ref NativeHashMap<int, byte> deleted,
			ref int eventsCount,
			ref int eventIdSeq,
			ref NativeArray<int> treeValue, 
			ref NativeArray<int> treeLeft, 
			ref NativeArray<int> treeRight, 
			ref NativeArray<int> treeParent, 
			ref NativeArray<int> treePrevious, 
			ref NativeArray<int> treeNext, 
			ref NativeArray<bool> treeColor,
			ref int treeCount,
			ref int root) 
		{
			
			var site = fortuneEvent.Site;
            var x = sites[site].X;
            var directrix = sites[site].Y;

            var leftNode = -1;
            var rightNode = -1;
            var node = root;

            //find the parabola(s) above this site
            while (node > -1 && leftNode < 0 && rightNode < 0)
            {
                var distanceLeft = LeftBreakpoint(node, ref treeValue, ref treePrevious, ref arcSites, ref sites, directrix) - x;
                if (distanceLeft > 0)
                {
                    //the new site is before the left breakpoint
                    if (treeLeft[node] < 0)
                    {
                        rightNode = node;
                    }
                    else
                    {
                        node = treeLeft[node];
                    }
                    continue;
                }

                var distanceRight = x - RightBreakpoint(node, ref treeValue, ref treeNext, ref arcSites, ref sites, directrix);
                if (distanceRight > 0)
                {
                    //the new site is after the right breakpoint
                    if (treeRight[node] < 0)
                    {
                        leftNode = node;
                    }
                    else
                    {
                        node = treeRight[node];
                    }
                    continue;
                }

                //the point lies below the left breakpoint
                if (VMath.ApproxEqual(distanceLeft, 0))
                {
                    leftNode = treePrevious[node];
                    rightNode = node;
                    continue;
                }

                //the point lies below the right breakpoint
                if (VMath.ApproxEqual(distanceRight, 0))
                {
                    leftNode = node;
                    rightNode = treeNext[node];
                    continue;
                }

                // distance Right < 0 and distance Left < 0
                // this section is above the new site
                leftNode = rightNode = node;
            }

            // our goal is to insert the new node between the
            // left and right sections
            AddArc(site, ref arcSites, ref arcEdges, ref arcEvents);

            //left section could be null, in which case this node is the first
            //in the tree
            var newNode = InsertTreeNode(leftNode, arcSites.Length - 1,
	            ref treeValue, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeColor,
	            ref treeCount, ref root);

            //new beach section is the first beach section to be added
            if (leftNode < 0 && rightNode < 0)
            {
                return;
            }

            //main case:
            //if both left section and right section point to the same valid arc
            //we need to split the arc into a left arc and a right arc with our 
            //new arc sitting in the middle
            if (leftNode > -1 && leftNode == rightNode)
            {
                //if the arc has a circle event, it was a false alarm.
                //remove it
                {
	                var leftNodeArcIndex = treeValue[leftNode];
	                var leftNodeArcEvent = arcEvents[leftNodeArcIndex];
	                if (leftNodeArcEvent.Exists)
	                {
		                deleted.Add(leftNodeArcEvent.Id, 1);
		                arcEvents[leftNodeArcIndex] = FortuneEventArc.Null;
	                }
                }

                //we leave the existing arc as the left section in the tree
                //however we need to insert the right section defined by the arc
                var copy = arcSites.Length;
                AddArc(arcSites[treeValue[leftNode]], ref arcSites, ref arcEdges, ref arcEvents);
                rightNode = InsertTreeNode(newNode, copy,
	                ref treeValue, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeColor,
	                ref treeCount, ref root);

                //grab the projection of this site onto the parabola
                var leftNodeSite = arcSites[treeValue[leftNode]];
                var nodeArcSite = sites[leftNodeSite];
                var y = VMath.EvalParabola(nodeArcSite.X, nodeArcSite.Y, directrix, x);
                var intersection = new float2(x, y);

                //create the two half edges corresponding to this intersection
                var leftEdge = edges.Length;
                var rightEdge = leftEdge + 1;
                AddEdge(new VEdge(intersection, site, leftNodeSite, rightEdge, ref sitesMap), ref edges, ref edgesEnds);
                AddEdge(new VEdge(intersection, leftNodeSite, site, ref sitesMap), ref edges, ref edgesEnds);

                //store the left edge on each arc section
                arcEdges[treeValue[newNode]] = leftEdge;
                arcEdges[treeValue[rightNode]] = rightEdge;

                //create circle events
                CheckCircle(leftNode, ref treeValue, ref treePrevious, ref treeNext, ref arcSites, ref arcEvents, ref sites, ref events, ref eventsCount, ref eventIdSeq);
                CheckCircle(rightNode, ref treeValue, ref treePrevious, ref treeNext, ref arcSites, ref arcEvents, ref sites, ref events, ref eventsCount, ref eventIdSeq);
            }

            //site is the last beach section on the beach line
            //this can only happen if all previous sites
            //had the same y value
            else if (leftNode > -1 && rightNode < 0)
            {
	            
	            
	            var newEdge = edges.Length;
	            var infEdge = newEdge + 1;

	            var leftNodeSite = arcSites[treeValue[leftNode]];
	            var start = new float2((sites[leftNodeSite].X + sites[site].X) / 2, float.MinValue);

	            // new edge
	            AddEdge(new VEdge(start, site, leftNodeSite, infEdge, ref sitesMap), ref edges, ref edgesEnds);
	            // inf edge	            
	            AddEdge(new VEdge(start, leftNodeSite, site, ref sitesMap), ref edges, ref edgesEnds);

	            arcEdges[treeValue[newNode]] = newEdge;

	            //cant check circles since they are colinear
            }

            //site is directly above a break point
            else if (leftNode > -1 && leftNode != rightNode)
            {
	            //remove false alarms
	            var leftNodeArcIndex = treeValue[leftNode];
	            var leftNodeArcEvent = arcEvents[leftNodeArcIndex];
                if (leftNodeArcEvent.Exists)
                {
                    deleted.Add(leftNodeArcEvent.Id, 1);
                    arcEvents[leftNodeArcIndex] = FortuneEventArc.Null;
                }

                var rightNodeArcIndex = treeValue[rightNode];
                var rightNodeArcEvent = arcEvents[rightNodeArcIndex];
                if (rightNodeArcEvent.Exists)
                {
                    deleted.Add(rightNodeArcEvent.Id, 1);
                    arcEvents[rightNodeArcIndex] = FortuneEventArc.Null;
                }

                //the breakpoint will dissapear if we add this site
                //which means we will create an edge
                //we treat this very similar to a circle event since
                //an edge is finishing at the center of the circle
                //created by circumscribing the left center and right
                //sites

                //bring a to the origin
                var siteData = sites[site];
                var leftSite = sites[arcSites[leftNodeArcIndex]];
                var ax = leftSite.X;
                var ay = leftSite.Y;
                var bx = siteData.X - ax;
                var by = siteData.Y - ay;

                var rightSite = sites[arcSites[rightNodeArcIndex]];
                var cx = rightSite.X - ax;
                var cy = rightSite.Y - ay;
                var d = bx*cy - by*cx;
                var magnitudeB = bx*bx + by*by;
                var magnitudeC = cx*cx + cy*cy;
                var vertex = new float2((cy * magnitudeB - by * magnitudeC) / (2 * d) + ax,
	                (bx * magnitudeC - cx * magnitudeB) / (2 * d) + ay);

                edgesEnds[arcEdges[rightNodeArcIndex]] = vertex;

                // next we create a two new edges
                arcEdges[treeValue[newNode]] = edges.Length;
                AddEdge(new VEdge(vertex, site, arcSites[leftNodeArcIndex], ref sitesMap), ref edges, ref edgesEnds);
                arcEdges[rightNodeArcIndex] = edges.Length;
                AddEdge(new VEdge(vertex, arcSites[rightNodeArcIndex], site, ref sitesMap), ref edges, ref edgesEnds);

                CheckCircle(leftNode, ref treeValue, ref treePrevious, ref treeNext, ref arcSites, ref arcEvents, ref sites, ref events, ref eventsCount, ref eventIdSeq);
                CheckCircle(rightNode, ref treeValue, ref treePrevious, ref treeNext, ref arcSites, ref arcEvents, ref sites, ref events, ref eventsCount, ref eventIdSeq);
            }
		}

		public static void RemoveBeachArc(
			FortuneEvent fortuneEvent, 
			ref NativeArray<VSite> sites,
			ref NativeHashMap<int, int> sitesMap,
			ref NativeList<VEdge> edges,
			ref NativeList<float2> edgesEnds,
			ref NativeList<int> arcSites,
			ref NativeList<int> arcEdges,
			ref NativeList<FortuneEventArc> arcEvents,
			ref NativeArray<FortuneEvent> events,
			ref NativeHashMap<int, byte> deleted,
			ref int eventsCount,
			ref int eventIdSeq,
			ref NativeArray<int> treeValue, 
			ref NativeArray<int> treeLeft, 
			ref NativeArray<int> treeRight, 
			ref NativeArray<int> treeParent, 
			ref NativeArray<int> treePrevious, 
			ref NativeArray<int> treeNext, 
			ref NativeArray<bool> treeColor,
			ref int treeCount,
			ref int root)
		{
			var node = fortuneEvent.Node;
            var x = fortuneEvent.X;
            var y = fortuneEvent.YCenter;

            var vertex = new float2(x, y);

            //multiple edges could end here
            var toBeRemoved = new NativeList<int>(treeCount, Allocator.Temp);

            //look left
            var prev = treePrevious[node];
            var prevEvent = arcEvents[treeValue[prev]];
            while (prevEvent.Exists &&
                   VMath.ApproxEqual(x - prevEvent.X, 0) &&
                   VMath.ApproxEqual(y - prevEvent.Y, 0))
            {
	            toBeRemoved.AddNoResize(prev);
	            prev = treePrevious[prev];
	            prevEvent = arcEvents[treeValue[prev]];
            }

            var next = treeNext[node];
	        var nextEvent = arcEvents[treeValue[next]];
            while (nextEvent.Exists &&
                   VMath.ApproxEqual(x - nextEvent.X, 0) &&
                   VMath.ApproxEqual(y - nextEvent.Y, 0))
            {
	            toBeRemoved.AddNoResize(next);
	            next = treeNext[next];
	            nextEvent = arcEvents[treeValue[next]];
            }

            
            {
	            var arcIndex = treeValue[node];
	            edgesEnds[arcEdges[arcIndex]] = vertex;
	            edgesEnds[arcEdges[treeValue[next]]] = vertex;
	            arcEvents[arcIndex] = FortuneEventArc.Null;
            }

            // odds are this double writes a few edges but this is clean...
            for (var i = 0; i < toBeRemoved.Length; i++)
            {
	            var nodeIndex = toBeRemoved[i];
	            var arcIndex = treeValue[nodeIndex];
	            edgesEnds[arcEdges[arcIndex]] = vertex;
	            edgesEnds[arcEdges[treeValue[treeNext[nodeIndex]]]] = vertex;
	            deleted.Add(arcEvents[arcIndex].Id, 1);
	            arcEvents[arcIndex] = FortuneEventArc.Null;
            }

            // need to delete all upcoming circle events with this node
            var prevArcIndex = treeValue[prev];
            var prevArcEvent = arcEvents[prevArcIndex];
            if (prevArcEvent.Exists)
            {
                deleted.Add(prevArcEvent.Id, 1);
                arcEvents[prevArcIndex] = FortuneEventArc.Null;
            }

            var nextArcIndex = treeValue[next];
            var nextArcEvent = arcEvents[nextArcIndex];
            if (nextArcEvent.Exists)
            {
                deleted.Add(nextArcEvent.Id, 1);
                arcEvents[nextArcIndex] = FortuneEventArc.Null;
            }


            // create a new edge with start point at the vertex and assign it to next
            arcEdges[nextArcIndex] = edges.Length;
            AddEdge(new VEdge(vertex, arcSites[nextArcIndex], arcSites[prevArcIndex], ref sitesMap), ref edges, ref edgesEnds);

            // remove the section from the tree
            RemoveTreeNode(node,
	            ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeColor, ref root);
            for (var i = 0; i < toBeRemoved.Length; i++)
	            RemoveTreeNode(toBeRemoved[i],
		            ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeColor, ref root);

            CheckCircle(prev, ref treeValue, ref treePrevious, ref treeNext, 
	            ref arcSites, ref arcEvents,
	            ref sites, ref events, ref eventsCount, ref eventIdSeq);
            CheckCircle(next, ref treeValue, ref treePrevious, ref treeNext, 
	            ref arcSites, ref arcEvents,
	            ref sites, ref events, ref eventsCount, ref eventIdSeq);
		}

		private static float LeftBreakpoint(
			int node,
			ref NativeArray<int> treeValue,
			ref NativeArray<int> treePrevious,
			ref NativeList<int> arcSites,
			ref NativeArray<VSite> sites,
			float directrix)
		{
			var leftNode = treePrevious[node];
			//degenerate parabola
			var a = sites[arcSites[treeValue[node]]];
			if (VMath.ApproxEqual(a.Y - directrix, 0))
				return a.X;
			//node is the first piece of the beach line
			if (leftNode < 0)
				return float.NegativeInfinity;
			//left node is degenerate
			var b = sites[arcSites[treeValue[leftNode]]];
			if (VMath.ApproxEqual(b.Y - directrix, 0))
				return b.X;
			return VMath.IntersectParabolaX(b.X, b.Y, a.X, a.Y, directrix);
		}

		private static float RightBreakpoint(
			int node, 
			ref NativeArray<int> treeValue,
			ref NativeArray<int> treeNext,
			ref NativeList<int> arcSites,
			ref NativeArray<VSite> sites, 
			float directrix)
		{
			var rightNode = treeNext[node];
			//degenerate parabola
			var a = sites[arcSites[treeValue[node]]];
			if (VMath.ApproxEqual(a.Y - directrix, 0))
				return a.X;
			//node is the last piece of the beach line
			if (rightNode < 0)
				return float.PositiveInfinity;
			//left node is degenerate
			var b = sites[arcSites[treeValue[rightNode]]];
			if (VMath.ApproxEqual(b.Y - directrix, 0))
				return b.X;
			return VMath.IntersectParabolaX(a.X, a.Y, b.X, b.Y, directrix);
		}

		private static void CheckCircle(
			int node,
			ref NativeArray<int> treeValue,
			ref NativeArray<int> treePrevious,
			ref NativeArray<int> treeNext,
			ref NativeList<int> arcSites,
			ref NativeList<FortuneEventArc> arcEvents,
			ref NativeArray<VSite> sites,
			ref NativeArray<FortuneEvent> events,
			ref int eventsCount,
			ref int eventIdSeq)
		{
			//if (node < 0)
			//    return;
			// var treeNode = nodes[node];
			var left = treePrevious[node];
			var right = treeNext[node];
			if (left < 0 || right < 0)
				return;

			var leftSiteIndex = arcSites[treeValue[left]];
			var centerSiteIndex = arcSites[treeValue[node]];
			var rightSiteIndex = arcSites[treeValue[right]];

			//if the left arc and right arc are defined by the same
			//focus, the two arcs cannot converge
			if (leftSiteIndex == rightSiteIndex)
				return;
			
			var leftSite = sites[leftSiteIndex];
			var centerSite = sites[centerSiteIndex];
			var rightSite = sites[rightSiteIndex];

			// http://mathforum.org/library/drmath/view/55002.html
			// because every piece of this program needs to be demoed in maple >.<

			//MATH HACKS: place center at origin and
			//draw vectors a and c to
			//left and right respectively
			float bx = centerSite.X,
				by = centerSite.Y,
				ax = leftSite.X - bx,
				ay = leftSite.Y - by,
				cx = rightSite.X - bx,
				cy = rightSite.Y - by;

			//The center beach section can only dissapear when
			//the angle between a and c is negative
			var d = ax*cy - ay*cx;
			if (VMath.ApproxGreaterThanOrEqualTo(d, 0))
				return;

			var magnitudeA = ax*ax + ay*ay;
			var magnitudeC = cx*cx + cy*cy;
			var x = (cy*magnitudeA - ay*magnitudeC)/(2*d);
			var y = (ax*magnitudeC - cx*magnitudeA)/(2*d);

			//add back offset
			var yCenter = y + by;
			//y center is off
			var vPoint = new float2(x + bx, yCenter + math.sqrt(x * x + y * y));
			var circleEvent = new FortuneEvent(ref eventIdSeq, ref vPoint, yCenter, node);
			arcEvents[treeValue[node]] = new FortuneEventArc(circleEvent);
			EventInsert(circleEvent, ref events, ref eventsCount);
		}


		private static void AddArc(int site, ref NativeList<int> arcSites, ref NativeList<int> arcEdges,
			ref NativeList<FortuneEventArc> arcEvents)
		{
			arcSites.AddNoResize(site);
			arcEdges.AddNoResize(-1);
			arcEvents.AddNoResize(FortuneEventArc.Null);
		}

		private static void AddEdge(VEdge edge, ref NativeList<VEdge> edges, ref NativeList<float2> edgeEnds)
		{
			edges.AddNoResize(edge);
			edgeEnds.AddNoResize(Float2Min);
		}
		
		private static readonly float2 Float2Min = new float2(float.MinValue, float.MinValue);
	}
}