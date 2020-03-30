using Unity.Collections;
using Unity.Mathematics;
using Voronoi.Helpers;
using Voronoi.Structures;
using static Voronoi.Handlers.MinHeap;
using static Voronoi.Handlers.RedBlackTree;

namespace Voronoi.Handlers
{
	public struct BeachLine
	{
		public static void AddBeachArc(
			FortuneEvent fortuneEvent,
			ref NativeArray<VSite> sites,
			ref NativeHashMap<int, int> sitesMap,
			ref NativeList<VEdge> edges,
			ref NativeList<float2> edgesEnds,
			ref NativeList<Arc> arcs,
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
                var distanceLeft = LeftBreakpoint(node, ref treeValue, ref treePrevious, ref arcs, ref sites, directrix) - x;
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

                var distanceRight = x - RightBreakpoint(node, ref treeValue, ref treeNext, ref arcs, ref sites, directrix);
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
            arcs.Add(new Arc(site));

            //left section could be null, in which case this node is the first
            //in the tree
            var newNode = InsertTreeNode(leftNode, arcs.Length - 1,
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
	                var leftNodeArc = arcs[leftNodeArcIndex];
	                if (leftNodeArc.Event.Exists)
	                {
		                deleted.Add(leftNodeArc.Event.Id, 1);
		                leftNodeArc.Event = new FortuneEvent();
		                arcs[leftNodeArcIndex] = leftNodeArc;
	                }
                }

                //we leave the existing arc as the left section in the tree
                //however we need to insert the right section defined by the arc
                var copy = arcs.Length;
                arcs.AddNoResize(new Arc(arcs[treeValue[leftNode]].Site));
                rightNode = InsertTreeNode(newNode, copy,
	                ref treeValue, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeColor,
	                ref treeCount, ref root);

                //grab the projection of this site onto the parabola
                float y;
                int leftNodeSite;
                {
	                leftNodeSite = arcs[treeValue[leftNode]].Site;
	                var nodeArcSite = sites[leftNodeSite];
	                y = VMath.EvalParabola(nodeArcSite.X, nodeArcSite.Y, directrix, x);
                }
                var intersection = new float2(x, y);

                //create the two half edges corresponding to this intersection
                var leftEdge = edges.Length;
                var rightEdge = leftEdge + 1;
                AddEdge(new VEdge(intersection, site, leftNodeSite, rightEdge, ref sitesMap), ref edges, ref edgesEnds);
                AddEdge(new VEdge(intersection, leftNodeSite, site, ref sitesMap), ref edges, ref edgesEnds);

                //store the left edge on each arc section
                {
	                var arcIndex = treeValue[newNode];
	                var arc = arcs[arcIndex];
	                arc.Edge = leftEdge;
	                arcs[arcIndex] = arc;
                }
                {
	                var arcIndex = treeValue[rightNode];
	                var arc = arcs[arcIndex];
	                arc.Edge = rightEdge;
	                arcs[arcIndex] = arc;
                }

                //create circle events
                CheckCircle(leftNode, ref treeValue, ref treePrevious, ref treeNext, ref arcs, ref sites, ref events, ref eventsCount, ref eventIdSeq);
                CheckCircle(rightNode, ref treeValue, ref treePrevious, ref treeNext, ref arcs, ref sites, ref events, ref eventsCount, ref eventIdSeq);
            }

            //site is the last beach section on the beach line
            //this can only happen if all previous sites
            //had the same y value
            else if (leftNode > -1 && rightNode < 0)
            {
	            
	            
	            var newEdge = edges.Length;
	            var infEdge = newEdge + 1;

	            var leftNodeSite = arcs[treeValue[leftNode]].Site;
	            var start = new float2((sites[leftNodeSite].X + sites[site].X) / 2, float.MinValue);

	            // new edge
	            AddEdge(new VEdge(start, site, leftNodeSite, infEdge, ref sitesMap), ref edges, ref edgesEnds);
	            // inf edge	            
	            AddEdge(new VEdge(start, leftNodeSite, site, ref sitesMap), ref edges, ref edgesEnds);

	            {
		            var arcIndex = treeValue[newNode];
		            var newNodeArc = arcs[arcIndex];
		            newNodeArc.Edge = newEdge;
		            arcs[arcIndex] = newNodeArc;
	            }

	            //cant check circles since they are colinear
            }

            //site is directly above a break point
            else if (leftNode > -1 && leftNode != rightNode)
            {
	            //remove false alarms
	            var leftNodeArcIndex = treeValue[leftNode];
                var leftNodeArc = arcs[leftNodeArcIndex];
                if (leftNodeArc.Event.Exists)
                {
                    deleted.Add(leftNodeArc.Event.Id, 1);
                    leftNodeArc.Event = new FortuneEvent();
                    arcs[leftNodeArcIndex] = leftNodeArc;
                }

                var rightNodeArcIndex = treeValue[rightNode];
                var rightNodeArc = arcs[rightNodeArcIndex];
                if (rightNodeArc.Event.Exists)
                {
                    deleted.Add(rightNodeArc.Event.Id, 1);
                    rightNodeArc.Event = new FortuneEvent();
                    arcs[rightNodeArcIndex] = rightNodeArc;
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
	                AddEdge(new VEdge(vertex, site, leftNodeArc.Site, ref sitesMap), ref edges, ref edgesEnds);
	                var arcIndex = treeValue[newNode];
	                var newNodeArc = arcs[arcIndex];
	                newNodeArc.Edge = edge;
	                arcs[arcIndex] = newNodeArc;
                }
                {
	                var edge = edges.Length;
	                AddEdge(new VEdge(vertex, rightNodeArc.Site, site, ref sitesMap), ref edges, ref edgesEnds);
	                rightNodeArc.Edge = edge;
	                arcs[rightNodeArcIndex] = rightNodeArc;
                }

                CheckCircle(leftNode, ref treeValue, ref treePrevious, ref treeNext, ref arcs, ref sites, ref events, ref eventsCount, ref eventIdSeq);
                CheckCircle(rightNode, ref treeValue, ref treePrevious, ref treeNext, ref arcs, ref sites, ref events, ref eventsCount, ref eventIdSeq);
            }
		}

		public static void RemoveBeachArc(
			FortuneEvent fortuneEvent, 
			ref NativeArray<VSite> sites,
			ref NativeHashMap<int, int> sitesMap,
			ref NativeList<VEdge> edges,
			ref NativeList<float2> edgesEnds,
			ref NativeList<Arc> arcs,
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
			ref NativeArray<bool> treeRed,
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
            var prevEvent = arcs[treeValue[prev]].Event;
            while (prevEvent.Exists && 
                   VMath.ApproxEqual(x - prevEvent.X, 0) && 
                   VMath.ApproxEqual(y - prevEvent.Y, 0))
            {
                toBeRemoved.AddNoResize(prev);
                prev = treePrevious[prev];
                prevEvent = arcs[treeValue[prev]].Event;
            }

            var next = treeNext[node];
            var nextEvent = arcs[treeValue[next]].Event;
            while (nextEvent.Exists &&
                   VMath.ApproxEqual(x - nextEvent.X, 0) &&
                   VMath.ApproxEqual(y - nextEvent.Y, 0))
            {
                toBeRemoved.AddNoResize(next);
                next = treeNext[next];
                nextEvent = arcs[treeValue[next]].Event;
            }

            {
	            var arcIndex = treeValue[node];
	            var nodeArc =  arcs[arcIndex];
	            edgesEnds[nodeArc.Edge] = vertex;
	            edgesEnds[arcs[treeValue[next]].Edge] = vertex;
	            nodeArc.Event = new FortuneEvent();
	            arcs[arcIndex] = nodeArc;
            }

            //odds are this double writes a few edges but this is clean...
            for (var i = 0; i < toBeRemoved.Length; i++)
            {
	            var remove = toBeRemoved[i];
	            var arcIndex = treeValue[remove];
	            var removeArc = arcs[arcIndex];
	            edgesEnds[removeArc.Edge] = vertex;
	            edgesEnds[arcs[treeValue[treeNext[remove]]].Edge] = vertex;
	            deleted.Add(removeArc.Event.Id, 1);
	            removeArc.Event = new FortuneEvent();
	            arcs[arcIndex] = removeArc;
            }


            //need to delete all upcoming circle events with this node
            var prevArcIndex = treeValue[prev];
            var prevArc = arcs[prevArcIndex];
            if (prevArc.Event.Exists)
            {
                deleted.Add(prevArc.Event.Id, 1);
                prevArc.Event = new FortuneEvent();
                arcs[prevArcIndex] = prevArc; }

            var nextArcIndex = treeValue[next];
            var nextArc =  arcs[nextArcIndex];
            if (nextArc.Event.Exists)
            {
                deleted.Add(nextArc.Event.Id, 1);
                nextArc.Event = new FortuneEvent();
                arcs[nextArcIndex] = nextArc;
            }


            //create a new edge with start point at the vertex and assign it to next
            var newEdge = edges.Length;
            AddEdge(new VEdge(vertex, nextArc.Site, prevArc.Site, ref sitesMap), ref edges, ref edgesEnds);
            nextArc.Edge = newEdge;
            arcs[nextArcIndex] = nextArc;
            
            //remove the sectionfrom the tree
            RemoveTreeNode(node, ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeRed, ref root);
            for (var i = 0; i < toBeRemoved.Length; i++)
	            RemoveTreeNode(toBeRemoved[i], ref treeLeft, ref treeRight, ref treeParent, ref treePrevious, ref treeNext, ref treeRed, ref root);

            CheckCircle(prev, ref treeValue, ref treePrevious, ref treeNext, ref arcs, ref sites, ref events, ref eventsCount, ref eventIdSeq);
            CheckCircle(next, ref treeValue, ref treePrevious, ref treeNext, ref arcs, ref sites, ref events, ref eventsCount, ref eventIdSeq);
		}

		private static float LeftBreakpoint(
			int node,
			ref NativeArray<int> treeArc,
			ref NativeArray<int> treePrevious,
			ref NativeList<Arc> arcs,
			ref NativeArray<VSite> sites,
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
			ref NativeArray<int> treeArc,
			ref NativeArray<int> treeNext,
			ref NativeList<Arc> arcs,
			ref NativeArray<VSite> sites, 
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
			ref NativeArray<int> treeArc,
			ref NativeArray<int> treePrevious,
			ref NativeArray<int> treeNext,
			ref NativeList<Arc> arcs,
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
			EventInsert(circleEvent, ref events, ref eventsCount);
		}


		private static readonly float2 Float2Min = new float2(float.MinValue, float.MinValue);

		private static void AddEdge(VEdge edge, ref NativeList<VEdge> edges, ref NativeList<float2> edgeEnds)
		{
			edges.AddNoResize(edge);
			edgeEnds.AddNoResize(Float2Min);
		}
	}
}