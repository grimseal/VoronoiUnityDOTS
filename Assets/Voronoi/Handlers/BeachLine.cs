using Unity.Collections;
using Unity.Mathematics;
using Voronoi.Helpers;
using Voronoi.Structures;
using static Voronoi.Handlers.MinHeap;
using static Voronoi.Handlers.RedBlueTree;

namespace Voronoi.Handlers
{
	public struct BeachLine
	{
		public static void AddBeachArc(
			FortuneEvent fortuneEvent,
			NativeArray<VSite> sites,
			NativeHashMap<int, int> sitesMap,
			NativeList<VEdge> edges,
			NativeList<float2> edgesEnds,
			NativeList<Arc> arcs,
			NativeArray<FortuneEvent> events, 
			NativeHashMap<int, byte> deleted,
			ref int eventsCount,
			ref int eventIdSeq,
			NativeArray<int> treeArc, 
			NativeArray<int> treeLeft, 
			NativeArray<int> treeRight, 
			NativeArray<int> treeParent, 
			NativeArray<int> treePrevious, 
			NativeArray<int> treeNext, 
			NativeArray<bool> treeRed,
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
                var distanceLeft = LeftBreakpoint(node, treeArc, treePrevious, arcs, sites, directrix) - x;
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

                var distanceRight = x - RightBreakpoint(node, treeArc, treeNext, arcs, sites, directrix);
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

            //our goal is to insert the new node between the
            //left and right sections
            arcs.Add(new Arc(arcs.Length, site));

            //left section could be null, in which case this node is the first
            //in the tree
            var newNode = InsertSuccessor(leftNode, arcs.Length - 1,
	            treeArc, treeLeft, treeRight, treeParent, treePrevious, treeNext, treeRed,
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
	                var leftNodeArc = arcs[treeArc[leftNode]];
	                if (leftNodeArc.Event.Exists)
	                {
		                deleted.Add(leftNodeArc.Event.Id, 1);
		                leftNodeArc.Event = new FortuneEvent();
		                arcs[leftNodeArc.Index] = leftNodeArc;
	                }
                }

                //we leave the existing arc as the left section in the tree
                //however we need to insert the right section defined by the arc
                var copy = arcs.Length;
                arcs.Add(new Arc(copy, arcs[treeArc[leftNode]].Site));
                rightNode = InsertSuccessor(newNode, copy,
	                treeArc, treeLeft, treeRight, treeParent, treePrevious, treeNext, treeRed,
	                ref treeCount, ref root);

                //grab the projection of this site onto the parabola
                float y;
                int leftNodeSite;
                {
	                leftNodeSite = arcs[treeArc[leftNode]].Site;
	                var nodeArcSite = sites[leftNodeSite];
	                y = VMath.EvalParabola(nodeArcSite.X, nodeArcSite.Y, directrix, x);
                }
                var intersection = new float2(x, y);

                //create the two half edges corresponding to this intersection
                var leftEdge = edges.Length;
                var rightEdge = leftEdge + 1;
                AddEdge(new VEdge(leftEdge, intersection, site, leftNodeSite, rightEdge, sitesMap), edges, edgesEnds);
                AddEdge(new VEdge(rightEdge, intersection, leftNodeSite, site, sitesMap), edges, edgesEnds);

                //store the left edge on each arc section
                {
	                var arc = arcs[treeArc[newNode]];
	                arc.Edge = leftEdge;
	                arcs[arc.Index] = arc;
                }
                {
	                var arc = arcs[treeArc[rightNode]];
	                arc.Edge = rightEdge;
	                arcs[arc.Index] = arc;
                }

                //create circle events
                CheckCircle(leftNode, treeArc, treePrevious, treeNext, arcs, sites, events, ref eventsCount, ref eventIdSeq);
                CheckCircle(rightNode, treeArc, treePrevious, treeNext, arcs, sites, events, ref eventsCount, ref eventIdSeq);
            }

            //site is the last beach section on the beach line
            //this can only happen if all previous sites
            //had the same y value
            else if (leftNode > -1 && rightNode < 0)
            {
	            
	            
	            var newEdge = edges.Length;
	            var infEdge = newEdge + 1;

	            var leftNodeSite = arcs[treeArc[leftNode]].Site;
	            var start = new float2((sites[leftNodeSite].X + sites[site].X) / 2, float.MinValue);

	            // new edge
	            AddEdge(new VEdge(newEdge, start, site, leftNodeSite, infEdge, sitesMap), edges, edgesEnds);
	            // inf edge	            
	            AddEdge(new VEdge(infEdge, start, leftNodeSite, site, sitesMap), edges, edgesEnds);

	            {
		            var newNodeArc = arcs[treeArc[newNode]];
		            newNodeArc.Edge = newEdge;
		            arcs[newNodeArc.Index] = newNodeArc;
	            }

	            //cant check circles since they are colinear
            }

            //site is directly above a break point
            else if (leftNode > -1 && leftNode != rightNode)
            {
	            //remove false alarms
                var leftNodeArc = arcs[treeArc[leftNode]];
                if (leftNodeArc.Event.Exists)
                {
                    deleted.Add(leftNodeArc.Event.Id, 1);
                    leftNodeArc.Event = new FortuneEvent();
                    arcs[leftNodeArc.Index] = leftNodeArc;
                }

                var rightNodeArc = arcs[treeArc[rightNode]];
                if (rightNodeArc.Event.Exists)
                {
                    deleted.Add(rightNodeArc.Event.Id, 1);
                    rightNodeArc.Event = new FortuneEvent();
                    arcs[rightNodeArc.Index] = rightNodeArc;
                }

                //the breakpoint will dissapear if we add this site
                //which means we will create an edge
                //we treat this very similar to a circle event since
                //an edge is finishing at the center of the circle
                //created by circumscribing the left center and right
                //sites

                //bring a to the origin
                var siteData = sites[site];
                var leftSite = sites[leftNodeArc.Site];
                var ax = leftSite.X;
                var ay = leftSite.Y;
                var bx = siteData.X - ax;
                var by = siteData.Y - ay;

                var rightSite = sites[rightNodeArc.Site];
                var cx = rightSite.X - ax;
                var cy = rightSite.Y - ay;
                var d = bx*cy - by*cx;
                var magnitudeB = bx*bx + by*by;
                var magnitudeC = cx*cx + cy*cy;
                var vertex = new float2((cy * magnitudeB - by * magnitudeC) / (2 * d) + ax,
	                (bx * magnitudeC - cx * magnitudeB) / (2 * d) + ay);

                edgesEnds[rightNodeArc.Edge] = vertex;

                //next we create a two new edges
                {
	                var edge = edges.Length;
	                AddEdge(new VEdge(edge, vertex, site, leftNodeArc.Site, sitesMap), edges, edgesEnds);
	                var newNodeArc = arcs[treeArc[newNode]];
	                newNodeArc.Edge = edge;
	                arcs[newNodeArc.Index] = newNodeArc;
                }
                {
	                var edge = edges.Length;
	                AddEdge(new VEdge(edge, vertex, rightNodeArc.Site, site, sitesMap), edges, edgesEnds);
	                rightNodeArc.Edge = edge;
	                arcs[rightNodeArc.Index] = rightNodeArc;
                }

                CheckCircle(leftNode, treeArc, treePrevious, treeNext, arcs, sites, events, ref eventsCount, ref eventIdSeq);
                CheckCircle(rightNode, treeArc, treePrevious, treeNext, arcs, sites, events, ref eventsCount, ref eventIdSeq);
            }
		}

		public static void RemoveBeachArc(
			FortuneEvent fortuneEvent, 
			NativeArray<VSite> sites,
			NativeHashMap<int, int> sitesMap,
			NativeList<VEdge> edges,
			NativeList<float2> edgesEnds,
			NativeList<Arc> arcs,
			NativeArray<FortuneEvent> events,
			NativeHashMap<int, byte> deleted,
			ref int eventsCount,
			ref int eventIdSeq,
			NativeArray<int> treeArc, 
			NativeArray<int> treeLeft, 
			NativeArray<int> treeRight, 
			NativeArray<int> treeParent, 
			NativeArray<int> treePrevious, 
			NativeArray<int> treeNext, 
			NativeArray<bool> treeRed,
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
            var prevEvent = arcs[treeArc[prev]].Event;
            while (prevEvent.Exists && 
                   VMath.ApproxEqual(x - prevEvent.X, 0) && 
                   VMath.ApproxEqual(y - prevEvent.Y, 0))
            {
                toBeRemoved.Add(prev);
                prev = treePrevious[prev];
                prevEvent = arcs[treeArc[prev]].Event;
            }

            var next = treeNext[node];
            var nextEvent = arcs[treeArc[next]].Event;
            while (nextEvent.Exists &&
                   VMath.ApproxEqual(x - nextEvent.X, 0) &&
                   VMath.ApproxEqual(y - nextEvent.Y, 0))
            {
                toBeRemoved.Add(next);
                next = treeNext[next];
                nextEvent = arcs[treeArc[next]].Event;
            }

            {
	            var nodeArc =  arcs[treeArc[node]];
	            edgesEnds[nodeArc.Edge] = vertex;
	            edgesEnds[arcs[treeArc[next]].Edge] = vertex;
	            nodeArc.Event = new FortuneEvent();
	            arcs[nodeArc.Index] = nodeArc;
            }

            //odds are this double writes a few edges but this is clean...
            for (var i = 0; i < toBeRemoved.Length; i++)
            {
	            var remove = toBeRemoved[i];
	            var removeArc = arcs[treeArc[remove]];
	            edgesEnds[removeArc.Edge] = vertex;
	            edgesEnds[arcs[treeArc[treeNext[remove]]].Edge] = vertex;
	            deleted.Add(removeArc.Event.Id, 1);
	            removeArc.Event = new FortuneEvent();
	            arcs[removeArc.Index] = removeArc;
            }


            //need to delete all upcoming circle events with this node
            var prevArc = arcs[treeArc[prev]];
            if (prevArc.Event.Exists)
            {
                deleted.Add(prevArc.Event.Id, 1);
                prevArc.Event = new FortuneEvent();
                arcs[prevArc.Index] = prevArc; }

            var nextArc =  arcs[treeArc[next]];
            if (nextArc.Event.Exists)
            {
                deleted.Add(nextArc.Event.Id, 1);
                nextArc.Event = new FortuneEvent();
                arcs[nextArc.Index] = nextArc;
            }


            //create a new edge with start point at the vertex and assign it to next
            var newEdge = edges.Length;
            AddEdge(new VEdge(newEdge, vertex, nextArc.Site, prevArc.Site, sitesMap), edges, edgesEnds);
            nextArc.Edge = newEdge;
            arcs[nextArc.Index] = nextArc;
            
            //remove the sectionfrom the tree
            RemoveNode(node, treeLeft, treeRight, treeParent, treePrevious, treeNext, treeRed, ref root);
            for (var i = 0; i < toBeRemoved.Length; i++)
	            RemoveNode(toBeRemoved[i], treeLeft, treeRight, treeParent, treePrevious, treeNext, treeRed, ref root);

            CheckCircle(prev, treeArc, treePrevious, treeNext, arcs, sites, events, ref eventsCount, ref eventIdSeq);
            CheckCircle(next, treeArc, treePrevious, treeNext, arcs, sites, events, ref eventsCount, ref eventIdSeq);
		}

		private static float LeftBreakpoint(
			int node,
			NativeArray<int> treeArc,
			NativeArray<int> treePrevious,
			NativeList<Arc> arcs,
			NativeArray<VSite> sites,
			float directrix)
		{
			var leftNode = treePrevious[node];
			//degenerate parabola
			var a = sites[arcs[treeArc[node]].Site];
			if (VMath.ApproxEqual(a.Y - directrix, 0))
				return a.X;
			//node is the first piece of the beach line
			if (leftNode < 0)
				return float.NegativeInfinity;
			//left node is degenerate
			var b = sites[arcs[treeArc[leftNode]].Site];
			if (VMath.ApproxEqual(b.Y - directrix, 0))
				return b.X;
			return VMath.IntersectParabolaX(b.X, b.Y, a.X, a.Y, directrix);
		}

		private static float RightBreakpoint(
			int node, 
			NativeArray<int> treeArc,
			NativeArray<int> treeNext,
			NativeList<Arc> arcs,
			NativeArray<VSite> sites, 
			float directrix)
		{
			var rightNode = treeNext[node];
			//degenerate parabola
			var a = sites[arcs[treeArc[node]].Site];
			if (VMath.ApproxEqual(a.Y - directrix, 0))
				return a.X;
			//node is the last piece of the beach line
			if (rightNode < 0)
				return float.PositiveInfinity;
			//left node is degenerate
			var b = sites[arcs[treeArc[rightNode]].Site];
			if (VMath.ApproxEqual(b.Y - directrix, 0))
				return b.X;
			return VMath.IntersectParabolaX(a.X, a.Y, b.X, b.Y, directrix);
		}

		private static void CheckCircle(
			int node,
			NativeArray<int> treeArc,
			NativeArray<int> treePrevious,
			NativeArray<int> treeNext,
			NativeList<Arc> arcs,
			NativeArray<VSite> sites,
			NativeArray<FortuneEvent> events,
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

			var leftSiteIndex = arcs[treeArc[left]].Site;
			var centerSiteIndex = arcs[treeArc[node]].Site;
			var rightSiteIndex = arcs[treeArc[right]].Site;

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
			var arc = arcs[treeArc[node]];
			arc.Event = circleEvent;
			arcs[treeArc[node]] = arc;
			EventInsert(circleEvent, events, ref eventsCount);
		}


		private static readonly float2 Float2Min = new float2(float.MinValue, float.MinValue);

		private static void AddEdge(VEdge edge, NativeList<VEdge> edges, NativeList<float2> edgeEnds)
		{
			edges.Add(edge);
			edgeEnds.Add(Float2Min);
		}
	}
}