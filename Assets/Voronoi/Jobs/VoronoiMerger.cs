// #define V_DEBUG
// using UnityEngine;
using System;
using Unity.Burst;
using Voronoi.Helpers;
using Voronoi.Structures;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voronoi.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct VoronoiMerger : IJob
    {
        #region Left voronoi data

        /// <summary>
        /// Left voronoi sites collection
        /// </summary>
        public NativeArray<VSite> LeftSites;
        
        /// <summary>
        /// Left voronoi edges collection
        /// </summary>
        public NativeList<VEdge> LeftEdges;
        
        /// <summary>
        /// Left diagram convex hull collection where elements is site indexes
        /// </summary>
        public NativeList<VSite> LeftConvexHull;
        
        /// <summary>
        /// Dictionary of left diagram where key is site id and value is site array index 
        /// </summary>
        public NativeHashMap<int, int> LeftSiteIdIndexes;
        
        /// <summary>
        /// Associative array where key is site id and value is edge indexes of left diagram
        /// </summary>
        public NativeMultiHashMap<int, int> LeftRegions;
        
        #endregion

        #region Right voronoi data

        /// <summary>
        /// Right voronoi sites collection
        /// </summary>
        public NativeArray<VSite> RightSites;
        
        /// <summary>
        /// Right voronoi edges collection
        /// </summary>
        public NativeList<VEdge> RightEdges;
        
        /// <summary>
        /// Right diagram convex hull collection where elements is site indexes  
        /// </summary>
        public NativeList<VSite> RightConvexHull;
        
        /// <summary>
        /// Dictionary of right diagram where key is site id and value is site array index  
        /// </summary>
        public NativeHashMap<int, int> RightSiteIdIndexes;
        
        /// <summary>
        /// Associative array where key is site id and value is edge indexes of right diagram
        /// </summary>
        public NativeMultiHashMap<int, int> RightRegions;

        #endregion

        #region Output data

        public NativeArray<VSite> Sites;
        public NativeList<VEdge> Edges;
        public NativeList<VSite> ConvexHull;
        public NativeHashMap<int, int> SiteIdIndexes;
        public NativeMultiHashMap<int, int> Regions;

        #endregion
        

        public void Execute()
        {
            const int capacity = 1024;
            var newEdges = new NativeList<VEdge>(capacity, Allocator.Temp);
            var regionEnterPoints = new NativeHashMap<int, float2>(capacity, Allocator.Temp);
            var regionEnterEdges = new NativeHashMap<int, VEdge>(capacity, Allocator.Temp);
            var regionEnterEdgesIndexes = new NativeHashMap<int, int>(capacity, Allocator.Temp);;
            
            var temp = new NativeList<double2>(4, Allocator.Temp);

            var leftEdgeIndexesToRemove = new NativeHashMap<int, byte>(capacity, Allocator.Temp);
            var rightEdgeIndexesToRemove = new NativeHashMap<int, byte>(capacity, Allocator.Temp);

            #if V_DEBUG
                var leftCopy = new NativeList<VEdge>(LeftEdges.Capacity, Allocator.Temp);
                leftCopy.AddRange(LeftEdges);
                var rightCopy = new NativeList<VEdge>(RightEdges.Capacity, Allocator.Temp);
                rightCopy.AddRange(RightEdges);
            #endif

            // merge sites
            NativeArray<VSite>.Copy(LeftSites, 0, Sites, 0, LeftSites.Length);
            NativeArray<VSite>.Copy(RightSites, 0, Sites, LeftSites.Length, RightSites.Length);
            for (var i = 0; i < LeftSites.Length; i++) SiteIdIndexes[LeftSites[i].Id] = i;
            for (var i = 0; i < RightSites.Length; i++) SiteIdIndexes[RightSites[i].Id] = i + LeftSites.Length;

            // merge convex hulls and find upper and lower tangents
            ConvexHull.AddRange(Handlers.ConvexHull.Merge(LeftConvexHull, RightConvexHull, 
                out var lLeft, out var lRight,
                out var qLeft, out var qRight));

            var left = lLeft;
            var right = lRight;
            double2 currentPoint;
            VEdge currentEdge;
            int currentEdgeIndex;

            
            // incoming ray
            var middle = (left.Point + right.Point) * 0.5f;
            var rayDir = VGeometry.Perpendicular(right.Point - left.Point);
            var leftDistance = RayRegionCrossing(middle, rayDir, left, ref LeftEdges, ref LeftRegions,
                out var leftPoint, out var leftEdgeIndex, out var leftEdge);
            var rightDistance = RayRegionCrossing(middle, rayDir, right, ref RightEdges, ref RightRegions,
                out var rightPoint, out var rightEdgeIndex, out var rightEdge);
            VEdge startEdge;
            if (leftDistance < rightDistance)
            {
                currentPoint = leftPoint;
                currentEdge = leftEdge;
                currentEdgeIndex = leftEdgeIndex;
                var end = VGeometry.BuildRayEnd(currentPoint, right.Point, left.Point, temp);
                startEdge = new VEdge(currentPoint, end, left.Id, right.Id);
                currentEdge = CutLeftEdge(end, VEdge.Null, currentPoint, currentEdge);
                LeftEdges[currentEdgeIndex] = currentEdge;
                var enumerator = LeftRegions.GetValuesForKey(left.Id);
                while (enumerator.MoveNext())
                {
                    var edgeIndex = enumerator.Current;
                    if (edgeIndex == currentEdgeIndex) continue;
                    var dir = GetEdgeSideLeft(end, currentPoint, edgeIndex);
                    if (dir > 0) leftEdgeIndexesToRemove.TryAdd(edgeIndex, 0);
                }
                enumerator.Dispose();
                left = LeftSites[LeftSiteIdIndexes[leftEdge.Left == left.Id ? leftEdge.Right : leftEdge.Left]];
                regionEnterPoints[left.Id] = (float2) currentPoint;
                regionEnterEdges[left.Id] = currentEdge;
                regionEnterEdgesIndexes[left.Id] = currentEdgeIndex;
            }
            else
            {
                currentPoint = rightPoint;
                currentEdge = rightEdge;
                currentEdgeIndex = rightEdgeIndex;
                var end = VGeometry.BuildRayEnd(currentPoint, right.Point, left.Point, temp);
                startEdge = new VEdge(currentPoint, end, left.Id, right.Id);
                currentEdge = CutRightEdge(end, VEdge.Null, currentPoint, currentEdge);
                RightEdges[currentEdgeIndex] = currentEdge;
                var enumerator = RightRegions.GetValuesForKey(right.Id);
                while (enumerator.MoveNext())
                {
                    var edgeIndex = enumerator.Current;
                    if (edgeIndex == currentEdgeIndex) continue;
                    var dir = GetEdgeSideRight(end, currentPoint, edgeIndex);
                    if (dir < 1) rightEdgeIndexesToRemove.TryAdd(edgeIndex, 0);
                }
                enumerator.Dispose();
                right = RightSites[RightSiteIdIndexes[rightEdge.Left == right.Id ? rightEdge.Right : rightEdge.Left]];
                regionEnterPoints[right.Id] = (float2) currentPoint;
                regionEnterEdges[right.Id] = currentEdge;
                regionEnterEdgesIndexes[right.Id] = currentEdgeIndex;
            }
            newEdges.Add(startEdge);
            
            regionEnterPoints[startEdge.Left] = startEdge.End;
            regionEnterPoints[startEdge.Right] = startEdge.End;
            regionEnterEdges[startEdge.Left] = VEdge.Null;
            regionEnterEdges[startEdge.Right] = VEdge.Null;
            regionEnterEdgesIndexes[startEdge.Left] = -1;
            regionEnterEdgesIndexes[startEdge.Right] = -1;
            

            // edges
            while (!(left == qLeft && right == qRight))
            {
                var perp = VGeometry.Perpendicular(right.Point - left.Point);
                var lCrossed = RegionCrossing(currentPoint, perp, left,
                    ref LeftEdges, ref LeftRegions, ref currentEdge,
                    out var lDistance, out var lVertex, out leftEdgeIndex, out var lEdge);
                var rCrossed = RegionCrossing(currentPoint, perp, right, 
                    ref RightEdges, ref RightRegions, ref currentEdge,
                    out var rDistance, out var rVertex, out rightEdgeIndex, out var rEdge);

                if (!lCrossed && !rCrossed)
                {
                    #if V_DEBUG
                    DebugOutput(Sites, lLeft, lRight, qLeft, qRight,leftCopy, rightCopy, newEdges);
                    #endif
                    throw new Exception("Voronoi merge error: no crossing");
                }

                VEdge newEdge;
                var leftId = left.Id;
                var rightId = right.Id;
                if (lDistance < rDistance)
                {
                    #if V_DEBUG
                        if (!VGeometry.PointOnLineSegment(lEdge.Start, lEdge.End, lVertex))
                        {
                            DebugOutput(Sites, lLeft, lRight, qLeft, qRight, leftCopy, rightCopy, newEdges);
                            Debug.DrawLine(lEdge.Start.ToVector3(), lEdge.End.ToVector3(), Color.red, 
                                float.MaxValue);
                            Debug.Log(lEdge.Start);
                            Debug.Log(lEdge.End);
                            throw new Exception("Voronoi merge error: wrong crossing");
                        }
                    #endif
                    newEdge = new VEdge(currentPoint, lVertex, leftId, rightId);
                    left = LeftSites[LeftSiteIdIndexes[lEdge.Left == leftId ? lEdge.Right : lEdge.Left]];
                    currentPoint = lVertex;
                    currentEdge = lEdge;
                    currentEdgeIndex = leftEdgeIndex;

                    newEdges.Add(newEdge);
                    
                    // region exit
                    var enterPoint = regionEnterPoints[leftId];
                    var exitPoint = newEdge.End;
                    var enterEdge = regionEnterEdges[leftId];
                    var enterEdgeIndex = regionEnterEdgesIndexes[leftId];

                    LeftEdges[currentEdgeIndex] = CutLeftEdge(enterPoint, enterEdge, exitPoint, currentEdge);

                    var enumerator = LeftRegions.GetValuesForKey(leftId);
                    while (enumerator.MoveNext())
                    {
                        var edgeIndex = enumerator.Current;
                        if (edgeIndex == currentEdgeIndex) continue;
                        if (edgeIndex == enterEdgeIndex) continue;
                        var dir = GetEdgeSideLeft(enterPoint, exitPoint, edgeIndex);
                        if (dir > 0) leftEdgeIndexesToRemove.TryAdd(edgeIndex, 0);
                    }
                    enumerator.Dispose();
                    
                    // region enter
                    regionEnterPoints[left.Id] = (float2) currentPoint;
                    regionEnterEdges[left.Id] = currentEdge;
                    regionEnterEdgesIndexes[left.Id] = currentEdgeIndex;
                }
                else
                {
                    #if V_DEBUG
                    if (!VGeometry.PointOnLineSegment(rEdge.Start, rEdge.End, rVertex))
                    {
                        DebugOutput(Sites, lLeft, lRight, qLeft, qRight, leftCopy, rightCopy, newEdges);
                        Debug.DrawLine(rEdge.Start.ToVector3(), rEdge.End.ToVector3(), Color.red, 
                            float.MaxValue);
                        Debug.Log(rEdge.Start);
                        Debug.Log(rEdge.End);
                        throw new Exception("Voronoi merge error: wrong crossing");
                    }
                    #endif
                    newEdge = new VEdge(currentPoint, rVertex, leftId, rightId);
                    right = RightSites[RightSiteIdIndexes[rEdge.Left == rightId ? rEdge.Right : rEdge.Left]];
                    currentPoint = rVertex;
                    currentEdge = rEdge;
                    currentEdgeIndex = rightEdgeIndex;
                    
                    newEdges.Add(newEdge);
                    
                    // region exit
                    var enterPoint = regionEnterPoints[rightId];
                    var exitPoint = newEdge.End;
                    var enterEdge = regionEnterEdges[rightId];
                    var enterEdgeIndex = regionEnterEdgesIndexes[rightId];

                    RightEdges[currentEdgeIndex] = CutRightEdge(enterPoint, enterEdge, exitPoint, currentEdge);

                    var enumerator = RightRegions.GetValuesForKey(rightId);
                    while (enumerator.MoveNext())
                    {
                        var edgeIndex = enumerator.Current;
                        if (edgeIndex == currentEdgeIndex) continue;
                        if (edgeIndex == enterEdgeIndex) continue;
                        var dir = GetEdgeSideRight(enterPoint, exitPoint, edgeIndex);
                        if (dir < 0) rightEdgeIndexesToRemove.TryAdd(edgeIndex, 0);
                    }
                    enumerator.Dispose();
                    
                    // region enter
                    regionEnterPoints[right.Id] = (float2) currentPoint;
                    regionEnterEdges[right.Id] = currentEdge;
                    regionEnterEdgesIndexes[right.Id] = currentEdgeIndex;
                }
            }

            
            // outgoing ray
            var endPoint = VGeometry.BuildRayEnd((left.Point + right.Point) * 0.5f, 
                left.Point, right.Point, temp);
            newEdges.Add(new VEdge(currentPoint, endPoint, left.Id, right.Id));


            #region Merge data

            // remove old edges
            var leftEdgeIndexes = leftEdgeIndexesToRemove.GetKeyArray(Allocator.Temp);
            leftEdgeIndexes.Sort();
            for (var i = leftEdgeIndexes.Length - 1; i >= 0; i--) LeftEdges.RemoveAtSwapBack(leftEdgeIndexes[i]);
            var rightEdgeIndexes = rightEdgeIndexesToRemove.GetKeyArray(Allocator.Temp);
            rightEdgeIndexes.Sort();
            for (var i = rightEdgeIndexes.Length - 1; i >= 0; i--) RightEdges.RemoveAtSwapBack(rightEdgeIndexes[i]);

            // merge edges and regions
            for (var i = 0; i < LeftEdges.Length; i++)
            {  
                Regions.Add(LeftEdges[i].Left, i);
                Regions.Add(LeftEdges[i].Right, i);
            }
            Edges.AddRange(LeftEdges);

            for (var i = 0; i < newEdges.Length; i++)
            {
                var index = Edges.Length + i;
                Regions.Add(newEdges[i].Left, index);
                Regions.Add(newEdges[i].Right, index);
            }
            Edges.AddRange(newEdges);

            for (var i = 0; i < RightEdges.Length; i++)
            {
                var index = Edges.Length + i;
                Regions.Add(RightEdges[i].Left, index);
                Regions.Add(RightEdges[i].Right, index);
            }
            Edges.AddRange(RightEdges);
            
            #endregion
            
            #if V_DEBUG
                Debug.Log($"sites length {Sites.Length}\n" +
                          $"newEdges {newEdges.Capacity} {newEdges.Length}\n" +
                          $"regionEnterPoints {regionEnterPoints.Capacity}\n" +
                          $"regionEnterEdges {regionEnterEdges.Capacity}\n" +
                          $"regionEnterEdgesIndexes {regionEnterEdgesIndexes.Capacity}\n" +
                          $"leftEdgeIndexesToRemove {leftEdgeIndexesToRemove.Capacity}\n" +
                          $"rightEdgeIndexesToRemove {rightEdgeIndexesToRemove.Capacity}");
            #endif

            // Если точки имеют одинаковую координату X, то стоит их сортировать по координате Y,
            // таким образом, чтобы равномерно и последовательно их разделить.
        }

        private static double RayRegionCrossing(
            float2 middle, double2 normal, VSite site,
            ref NativeList<VEdge> edges,
            ref NativeMultiHashMap<int, int> regions,
            out double2 crossingVertex, out int crossedEdgeIndex, out VEdge crossedEdge)
        {
            var distance = double.MaxValue;
            var minPoint = double2.zero;
            var minEdge = new VEdge();
            var minEdgeIndex = -1;
            var atan = math.atan2(normal.x, normal.y);
            var cos = math.cos(atan);
            var sin = math.sin(atan);
            var a = middle;
            var b = middle + normal;

            var region = regions.GetValuesForKey(site.Id);
            while (region.MoveNext())
            {
                var edgeIndex = region.Current;
                var edge = edges[edgeIndex];
                var c = edge.Start;
                var d = edge.End;
                if (!VGeometry.Intersection(a, b, c, d, out var point)) continue;
                var offset = point - middle;
                var aligned = new double2( offset.x * cos - offset.y * sin,  offset.x * sin + offset.y * cos);
                if (aligned.y > distance) continue;
                if (!VGeometry.PointOnLineSegment(c, d, point)) continue;
                distance = aligned.y;
                minPoint = point;
                minEdge = edge;
                minEdgeIndex = edgeIndex;
            }
            region.Dispose();

            crossingVertex = minPoint;
            crossedEdge = minEdge;
            crossedEdgeIndex = minEdgeIndex;
            return distance;
        }

        private static bool RegionCrossing(
            double2 start,
            double2 dir,
            VSite site,
            ref NativeList<VEdge> edges,
            ref NativeMultiHashMap<int, int> regions,
            ref VEdge currentEdge,
            out double approach,
            out double2 crossingVertex,
            out int crossedEdgeIndex,
            out VEdge crossedEdge)
        {
            var minApproach = double.MaxValue;
            var minPoint = double2.zero;
            var minEdge = new VEdge();
            var minEdgeIndex = -1;
            var a = start;
            var b = start + dir;
            var crossed = false;
            
            var region = regions.GetValuesForKey(site.Id);
            while (region.MoveNext())
            {
                var edgeIndex = region.Current;
                var edge = edges[edgeIndex];
                if (VEdge.IsEqual(edge, currentEdge)) continue;
                var c = edge.Start;
                var d = edge.End;
                if (!VGeometry.Intersection(a, b, c, d, out var point)) continue;
                var delta = point - start;
                var apr = math.dot(delta, delta);
                if (minApproach < apr || math.dot(dir, delta) <= 0) continue;
                if (!VGeometry.PointOnLineSegment(c, d, point)) continue;
                minApproach = apr;
                minPoint = point;
                minEdge = edge;
                minEdgeIndex = edgeIndex;
                crossed = true;
            }
            region.Dispose();

            crossingVertex = minPoint;
            crossedEdge = minEdge;
            approach = minApproach;
            crossedEdgeIndex = minEdgeIndex;
            return crossed;
        }

        private int GetEdgeSideLeft(double2 enterPoint, double2 exitPoint, int edgeIndex)
        {
            var edge = LeftEdges[edgeIndex];
            return math.max(
                VGeometry.RaySide(enterPoint, exitPoint, edge.End),
                VGeometry.RaySide(enterPoint, exitPoint, edge.Start));
        }
        
        private int GetEdgeSideRight(double2 enterPoint, double2 exitPoint, int edgeIndex)
        {
            var edge = RightEdges[edgeIndex];
            return math.min(
                VGeometry.RaySide(enterPoint, exitPoint, edge.End),
                VGeometry.RaySide(enterPoint, exitPoint, edge.Start));
        }

        private static VEdge CutLeftEdge(double2 enterPoint, VEdge enterEdge, double2 exitPoint, VEdge exitEdge)
        {
            if (VEdge.IsEqual(enterEdge, exitEdge))
                return new VEdge(enterPoint, exitPoint, exitEdge.Left, exitEdge.Right);
            
            return VGeometry.RaySide(enterPoint, exitPoint, exitEdge.Start) <
                   VGeometry.RaySide(enterPoint, exitPoint, exitEdge.End) 
                ? new VEdge(exitEdge.Start, exitPoint, exitEdge.Left, exitEdge.Right)
                : new VEdge(exitEdge.End, exitPoint, exitEdge.Left, exitEdge.Right);
        }
        
        private static VEdge CutRightEdge(double2 enterPoint, VEdge enterEdge, double2 exitPoint, VEdge exitEdge)
        {
            if (VEdge.IsEqual(enterEdge, exitEdge))
                return new VEdge(enterPoint, exitPoint, exitEdge.Left, exitEdge.Right);

            return VGeometry.RaySide(enterPoint, exitPoint, exitEdge.Start) >
                   VGeometry.RaySide(enterPoint, exitPoint, exitEdge.End)
                ? new VEdge(exitEdge.Start, exitPoint, exitEdge.Left, exitEdge.Right)
                : new VEdge(exitEdge.End, exitPoint, exitEdge.Left, exitEdge.Right);
        }

        public void Dispose()
        {
            Sites.Dispose();
            Edges.Dispose();
            Regions.Dispose();
            SiteIdIndexes.Dispose();
            ConvexHull.Dispose();
        }

        public static VoronoiMerger CreateJob(FortunesWithConvexHull left, FortunesWithConvexHull right)
        {
            var sites = new NativeArray<VSite>(left.Sites.Length + right.Sites.Length, Allocator.Persistent);
            var edges = new NativeList<VEdge>(left.Edges.Capacity + right.Edges.Capacity, Allocator.Persistent);
            var regions = new NativeMultiHashMap<int, int>(left.Regions.Capacity + right.Regions.Capacity, Allocator.Persistent);
            var siteIdIndexes = new NativeHashMap<int, int>(left.SiteIdIndexes.Capacity + right.SiteIdIndexes.Capacity, Allocator.Persistent);
            var convexHull = new NativeList<VSite>(left.ConvexHull.Capacity + right.ConvexHull.Capacity, Allocator.Persistent);

            return new VoronoiMerger
            {
                LeftSites = left.Sites,
                LeftEdges = left.Edges,
                LeftRegions = left.Regions,
                LeftSiteIdIndexes = left.SiteIdIndexes,
                LeftConvexHull = left.ConvexHull,

                RightSites = right.Sites,
                RightEdges = right.Edges,
                RightRegions = right.Regions,
                RightSiteIdIndexes = right.SiteIdIndexes,
                RightConvexHull = right.ConvexHull,

                Sites = sites,
                Edges = edges,
                Regions = regions,
                SiteIdIndexes = siteIdIndexes,
                ConvexHull = convexHull
            };
        }

        public static VoronoiMerger CreateJob(VoronoiMerger left, VoronoiMerger right)
        {
            var sites = new NativeArray<VSite>(left.Sites.Length + right.Sites.Length, Allocator.Persistent);
            var edges = new NativeList<VEdge>(left.Edges.Capacity + right.Edges.Capacity, Allocator.Persistent);
            var regions = new NativeMultiHashMap<int, int>(left.Regions.Capacity + right.Regions.Capacity, Allocator.Persistent);
            var siteIdIndexes = new NativeHashMap<int, int>(left.SiteIdIndexes.Capacity + right.SiteIdIndexes.Capacity, Allocator.Persistent);
            var convexHull = new NativeList<VSite>(left.ConvexHull.Capacity + right.ConvexHull.Capacity, Allocator.Persistent);

            return new VoronoiMerger
            {
                LeftSites = left.Sites,
                LeftEdges = left.Edges,
                LeftRegions = left.Regions,
                LeftSiteIdIndexes = left.SiteIdIndexes,
                LeftConvexHull = left.ConvexHull,

                RightSites = right.Sites,
                RightEdges = right.Edges,
                RightRegions = right.Regions,
                RightSiteIdIndexes = right.SiteIdIndexes,
                RightConvexHull = right.ConvexHull,

                Sites = sites,
                Edges = edges,
                Regions = regions,
                SiteIdIndexes = siteIdIndexes,
                ConvexHull = convexHull
            };
        }


        #if V_DEBUG
        private void DebugOutput(NativeArray<VSite> sites, VSite lLeft, VSite lRight, VSite qLeft, VSite qRight,
            NativeList<VEdge> leftCopy, NativeList<VEdge> rightCopy, NativeList<VEdge> newEdges)
        {
            var fortune = FortunesWithConvexHull.CreateJob(sites);
            fortune.Execute();

            for (int i = 0; i < fortune.EdgesCount[0]; i++)
            {
                var edge = fortune.Edges[i];
                Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.black.SetAlpha(0.5f),
                    float.MaxValue);
            }


            for (var i = 1; i < ConvexHull.Length; i++)
                Debug.DrawLine(ConvexHull[i - 1].Point.ToVector3(),
                    ConvexHull[i].Point.ToVector3(), Color.white.SetAlpha(0.5f),
                    float.MaxValue);
            
            Debug.DrawLine(ConvexHull[0].Point.ToVector3(),
                ConvexHull[ConvexHull.Length - 1].Point.ToVector3(), Color.white.SetAlpha(0.5f),
                float.MaxValue);
            
            // {
            //     var offset = new Vector3(-1, 0, 0);
            //     var convexHull = LeftConvexHull;
            //     for (var i = 1; i < convexHull.Length; i++)
            //     {
            //         var color = Color.HSVToRGB(1f / convexHull.Length * i, 1, 1);
            //         Debug.DrawLine(convexHull[i - 1].Point.ToVector3() + offset,
            //             convexHull[i].Point.ToVector3() + offset, color,float.MaxValue);
            //     }
            //     Debug.DrawLine(convexHull[0].Point.ToVector3() + offset,
            //         convexHull[convexHull.Length - 1].Point.ToVector3() + offset, Color.red,
            //         float.MaxValue);
            // }
            //
            // {
            //     var offset = new Vector3(1, 0, 0);
            //     var convexHull = RightConvexHull;
            //     for (var i = 1; i < convexHull.Length; i++)
            //     {
            //         var color = Color.HSVToRGB(1f / convexHull.Length * i, 1, 1);
            //         Debug.DrawLine(convexHull[i - 1].Point.ToVector3() + offset,
            //             convexHull[i].Point.ToVector3() + offset, color,float.MaxValue);
            //     }
            //     Debug.DrawLine(convexHull[0].Point.ToVector3() + offset,
            //         convexHull[convexHull.Length - 1].Point.ToVector3() + offset, Color.red,
            //         float.MaxValue);
            // }
            
            Debug.DrawLine(lLeft.Point.ToVector3(), lRight.Point.ToVector3(), Color.red,
                float.MaxValue);
            
            Debug.DrawLine(qLeft.Point.ToVector3(), qRight.Point.ToVector3(), Color.blue,
                float.MaxValue);
            
            // for (var i = 0; i < LeftEdges.Length; i++)
            // {
            //     var edge = LeftEdges[i];
            //     Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.red.SetAlpha(0.8f),
            //         float.MaxValue);
            // }
            //
            // for (var i = 0; i < RightEdges.Length; i++)
            // {
            //     var edge = RightEdges[i];
            //     Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.blue.SetAlpha(0.8f),
            //         float.MaxValue);
            // }
            
            for (var i = 0; i < leftCopy.Length; i++)
            {
                var edge = leftCopy[i];
                Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.white.SetAlpha(0.5f),
                    float.MaxValue);
            }
            
            for (var i = 0; i < rightCopy.Length; i++)
            {
                var edge = rightCopy[i];
                Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.white.SetAlpha(0.5f),
                    float.MaxValue);
            }
            
            for (var i = 0; i < newEdges.Length; i++)
                Debug.DrawLine(newEdges[i].Start.ToVector3(), newEdges[i].End.ToVector3(), Color.black,
                    float.MaxValue);
                    
        }
        #endif
    }
}