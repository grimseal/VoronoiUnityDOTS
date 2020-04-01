using Unity.Collections;
using Unity.Mathematics;

namespace Voronoi.Helpers
{
    public struct VGeometry
    {
        
        public static float2 Perpendicular(float2 vector2, bool counterClockwise = false)
        {
            return !counterClockwise ? new float2(vector2.y, -vector2.x) : new float2(-vector2.y, vector2.x);
        }
        
        public static double2 Perpendicular(double2 vector2, bool counterClockwise = false)
        {
            return !counterClockwise ? new double2(vector2.y, -vector2.x) : new double2(-vector2.y, vector2.x);
        }

        public static bool ApproxEquals(float2 a, float2 b, float tolerance = float.Epsilon)
        {
            return math.abs(a.x - b.x) <= tolerance && math.abs(a.y - b.y) <= tolerance;
        }

        public static readonly float max = math.sqrt(math.sqrt(float.MaxValue)) / 2; 

        public static float2 BuildRayEnd(double2 start, double2 left, double2 right, NativeList<double2> candidates)
		{
			double minX = -max;
	        double minY = -max;
	        double maxX = max;
	        double maxY = max;

	        var slopeRise = left.x - right.x;
	        var slopeRun = -(left.y - right.y);
	        var slope = slopeRise / slopeRun;
	        var intercept = start.x - slope*start.x;

	        //horizontal ray
	        if (VMath.ApproxEqual(slopeRise, 0))
		        return (float2)(slopeRun > 0 ? new double2(maxX, start.y) : new double2(minX, start.y));

	        //vertical ray
	        if (VMath.ApproxEqual(slopeRun, 0))
		        return (float2)(slopeRise > 0 ? new double2(start.x, maxY) : new double2(start.x, minY));

	        var topX = new double2(CalcX(slope, maxY, intercept), maxY);
            var bottomX = new double2(CalcX(slope, minY, intercept), minY);
            var leftY = new double2(minX, CalcY(slope, minX, intercept));
            var rightY = new double2(maxX, CalcY(slope, maxX, intercept));

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
		            return (float2)(ax*ax + ay*ay > bx*bx + @by*@by ? candidates[0] : candidates[1]);
	            }
	            //if there is one candidate we are inside
	            case 1:
		            return (float2)candidates[0];
	            default:
		            //there were no candidates
		            return new float2(float.MinValue, float.MinValue);
            }
		}

		private static double CalcY(double m, double x, double b)
		{
			return m * x + b;
		}

		private static double CalcX(double m, double y, double b)
		{
			return (y - b) / m;
		}
		
		private static bool Within(double x, double a, double b)
		{
			return VMath.ApproxGreaterThanOrEqualTo(x, a) && VMath.ApproxLessThanOrEqualTo(x, b);
		}

		public static bool PointOnLineSegment(double2 pt1, double2 pt2, double2 pt, float epsilon = 0.001f)
		{
			if (pt.x - math.max(pt1.x, pt2.x) > epsilon || 
			    math.min(pt1.x, pt2.x) - pt.x > epsilon || 
			    pt.y - math.max(pt1.y, pt2.y) > epsilon || 
			    math.min(pt1.y, pt2.y) - pt.y > epsilon)
				return false;

			if (math.abs(pt2.x - pt1.x) < epsilon)
				return math.abs(pt1.x - pt.x) < epsilon || math.abs(pt2.x - pt.x) < epsilon;
			if (math.abs(pt2.y - pt1.y) < epsilon)
				return math.abs(pt1.y - pt.y) < epsilon || math.abs(pt2.y - pt.y) < epsilon;

			double x = pt1.x + (pt.y - pt1.y) * (pt2.x - pt1.x) / (pt2.y - pt1.y);
			double y = pt1.y + (pt.x - pt1.x) * (pt2.y - pt1.y) / (pt2.x - pt1.x);

			return math.abs(pt.x - x) < epsilon || math.abs(pt.y - y) < epsilon;
		}

		public static bool Intersection(double2 a, double2 b, double2 c, double2 d, out double2 point) {

			var a1 = b.y - a.y;
			var b1 = a.x - b.x;
			var c1 = a1 * a.x + b1 * a.y;
 
			var a2 = d.y - c.y;
			var b2 = c.x - d.x;
			var c2 = a2 * c.x + b2 * c.y;
 
			var delta = a1 * b2 - a2 * b1;
	        
			// lines is parallel
			if (delta == 0)
			{
				point = double2.zero;
				return false;
			}

			point = new double2((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
			return true;
		}

		private static readonly float3 Up = new float3(0, 0, 1);

		public static int RaySide(double2 a, double2 b, double2 c)
		{
			var fwd = new double3(b - a, 0);
			var target = new double3(c - a, 0);
			var cross = math.cross(fwd, target);
			// var dot = math.dot(cross, Up);
			if (cross.z > 0) return 1;
			if (cross.z < 0) return -1;
			return 0;
		}
    }
}