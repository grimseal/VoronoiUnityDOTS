using System.Collections.Generic;
using Voronoi.Structures;
using Unity.Collections;
using Unity.Mathematics;

namespace Voronoi.Handlers
{
    public struct ConvexHull
    {
	    
        public static NativeArray<VSite> BuildConvexHull(NativeArray<VSite> sites)
        {
            // TODO Replace with Chan's algorithm
            // return Solve(sites);
            return AndrewsConvexHull(sites);
            
        }


        #region Andrew's monotone chain convex hull algorithm

        private static NativeList<VSite> AndrewsConvexHull(NativeArray<VSite> sites)
        {
            var points = new NativeArray<VSite>(sites.Length, Allocator.Temp);
            var slice = new NativeSlice<VSite>(sites);
            slice.CopyTo(points);
            points.Sort(new FortuneSiteComparer());

            var lower = new NativeList<VSite>(32, Allocator.Temp);
            for (var i = 0; i < points.Length; i++)
            {
                while (lower.Length >= 2 && Cross(lower[lower.Length - 2], lower[lower.Length - 1], points[i]) <= 0)
                    lower.RemoveAtSwapBack(lower.Length - 1);
                lower.Add(points[i]);
            }

            var upper = new NativeList<VSite>(32, Allocator.Temp);
            for (var i = points.Length - 1; i >= 0; i--)
            {
                while (upper.Length >= 2 && Cross(upper[upper.Length - 2], upper[upper.Length - 1], points[i]) <= 0)
                    upper.RemoveAtSwapBack(upper.Length - 1);
                upper.Add(points[i]);
            }
            
            points.Dispose();
            
            lower.RemoveAtSwapBack(lower.Length - 1);
            upper.RemoveAtSwapBack(upper.Length - 1);
            
            var result = new NativeList<VSite>(lower.Length + upper.Length, Allocator.Temp);
            result.AddRange(lower);
            result.AddRange(upper);
            
            lower.Dispose();
            upper.Dispose();
            
            return result;
        }

        private static float Cross(VSite a, VSite b, VSite o)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }

        private struct FortuneSiteComparer : IComparer<VSite>
        {
            public int Compare(VSite a, VSite b)
            {
                return a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
            }
        }

        #endregion


        #region Merge convex hull
        
        public static NativeList<VSite> Merge(
            NativeArray<VSite> left, NativeArray<VSite> right,
            out VSite leftUpper, out VSite rightUpper,
            out VSite leftLower, out VSite rightLower)
        {
            var convexHull = MergeHandler(left, right,
                out var aUpper,
                out var bUpper,
                out var aLower,
                out var bLower);

            leftUpper = left[aUpper];
            rightUpper = right[bUpper];
            leftLower = left[aLower];
            rightLower = right[bLower];

            return convexHull;
        }

        private static NativeList<VSite> MergeHandler(
            NativeArray<VSite> a, NativeArray<VSite> b, 
            out int aUpper, out int bUpper,
            out int aLower, out int bLower) 
        {
            int n1 = a.Length, n2 = b.Length;
            var aStart = GetRightMostIndex(a);
            var bStart = GetLeftMostIndex(b);
            
            var ia  = aStart;
            var ib = bStart;
            var done = false;
            
            while (!done)
            {
                done = true;
                while (Crossing(b[ib].Point, a[ia].Point, a[(ia  + 1) % n1].Point) >= 0)
                    ia  = (ia  + 1) % n1;
                while (Crossing(a[ia].Point, b[ib].Point, b[(n2 + ib - 1) % n2].Point) <= 0)
                {
                    ib = (n2 + ib - 1) % n2;
                    done = false;
                }
            }

            aUpper = ia;
            bUpper = ib;

            ia = aStart;
            ib = bStart;
            done = false;

            while (!done)
            {
                done = true;
                while (Crossing(a[ia].Point, b[ib].Point, b[(ib + 1) % n2].Point) >= 0)
                    ib = (ib + 1) % n2;
                while (Crossing(b[ib].Point, a[ia].Point, a[(n1 + ia - 1) % n1].Point) <= 0)
                {
                    ia = (n1 + ia - 1) % n1;
                    done = false;
                }
            }

            aLower = ia;
            bLower = ib;

            var capacity = (int) ((a.Length + b.Length) * 0.75f);
            var ret = new NativeList<VSite>(capacity, Allocator.Temp);
  
            // ret contains the convex hull after merging the two convex hulls 
            // with the points sorted in anti-clockwise order 
            var ind = aUpper; 
            ret.Add(a[aUpper]); 
            while (ind != aLower) 
            { 
                ind = (ind + 1) % n1; 
                ret.Add(a[ind]); 
            } 
  
            ind = bLower; 
            ret.Add(b[bLower]); 
            while (ind != bUpper) 
            { 
                ind = (ind + 1) % n2; 
                ret.Add(b[ind]); 
            } 
            return ret;
        }
        
        // Checks whether the line is crossing the polygon 
        private static int Crossing(float2 a, float2 b, float2 c) 
        { 
            var res = (b.y - a.y) * (c.x - b.x) - (c.y - b.y) * (b.x - a.x);
            if (res > 0) return 1;
            if (res < 0) return -1;
            return 0;
        }

        private static int GetRightMostIndex(NativeArray<VSite> hull)
        {
            var x = hull[0].X;
            var index = 0;
            for (var i = 1; i < hull.Length; i++)
            {
                if (x > hull[i].X) continue;
                x = hull[i].X;
                index = i;
            }
            return index;
        }

        private static int GetLeftMostIndex(NativeArray<VSite> hull)
        {
            var x = hull[0].X;
            var index = 0;
            for (var i = 1; i < hull.Length; i++)
            {
                if (x < hull[i].X) continue;
                x = hull[i].X;
                index = i;
            }
            return index;
        }
        
        #endregion

        
    }
}