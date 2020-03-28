using Unity.Mathematics;

namespace Voronoi.Helpers
{
	public static class VMath
	{
		private const float Epsilon = float.Epsilon * 1E+15f;

		public static float EvalParabola(float focusX, float focusY, float directrix, float x)
		{
			return 0.5f*( (x - focusX) * (x - focusX) /(focusY - directrix) + focusY + directrix);
		}

		public static float IntersectParabolaX(float focus1X, float focus1Y, float focus2X, float focus2Y,
			float directrix)
		{
			//admittedly this is pure voodoo.
			//there is attached documentation for this function
			return ApproxEqual(focus1Y, focus2Y)
				? (focus1X + focus2X)/2
				: (focus1X*(directrix - focus2Y) + focus2X*(focus1Y - directrix) +
				   math.sqrt((directrix - focus1Y)*(directrix - focus2Y)*
				             ((focus1X - focus2X)*(focus1X - focus2X) +
				              (focus1Y - focus2Y)*(focus1Y - focus2Y))
				   )
				  )/(focus1Y - focus2Y);
		}

		public static bool ApproxEqual(float value1, float value2, float tolarance = Epsilon)
		{
			return math.abs(value1 - value2) <= tolarance;
		}
		
		public static bool ApproxEqual(double value1, double value2, float tolarance = Epsilon)
		{
			return math.abs(value1 - value2) <= tolarance;
		}

		public static bool ApproxGreaterThanOrEqualTo(float value1, float value2)
		{
			return value1 > value2 || ApproxEqual(value1, value2);
		}
		
		public static bool ApproxGreaterThanOrEqualTo(double value1, double value2)
		{
			return value1 > value2 || ApproxEqual(value1, value2);
		}

		public static bool ApproxLessThanOrEqualTo(float value1, float value2)
		{
			return value1 < value2 || ApproxEqual(value1, value2);
		}
		
		public static bool ApproxLessThanOrEqualTo(double value1, double value2)
		{
			return value1 < value2 || ApproxEqual(value1, value2);
		}
	}
}