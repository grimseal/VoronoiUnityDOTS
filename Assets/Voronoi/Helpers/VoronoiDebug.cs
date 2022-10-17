using Unity.Collections;
using UnityEngine;
using Voronoi.Jobs;
using Voronoi.Structures;

namespace Voronoi
{
    public class VoronoiDebug
    {
        internal static void DebugRender(FortunesWithConvexHull[] hulls)
		{
			var heightStep =  Vector3.up * 10;
			for (var i = 0; i < hulls.Length; i++)
			{
				var color = Color.HSVToRGB(1f / hulls.Length * i, 1f, 1f);
				DebugRender(hulls[i].Edges, color, heightStep * i);
			}
		}


        internal static void DebugRender(VoronoiMerger[] hulls, int iteration)
		{
			var heightStep =  Vector3.up * 10;
			var offset = (Vector3.forward + Vector3.right) * 120 * iteration;
			for (var i = 0; i < hulls.Length; i++)
				DebugRender(hulls[i].Edges, Color.HSVToRGB(1f / hulls.Length * i, 1f, 1f), heightStep * i + offset);

		}

		public static void DebugRender(NativeList<VEdge> edges, Color color, Vector3 offset = new Vector3())
		{
			for (var j = 0; j < edges.Length; j++)
			{
				var start = edges[j].Start.ToVector3() + offset;
				var end = edges[j].End.ToVector3() + offset;
				Debug.DrawLine(start, end, color, float.MaxValue);
			}
		}

		public static void DebugRender(NativeList<VEdge> edges, Color from, Color to, int steps = 3,  Vector3 offset = new Vector3())
		{
			var p = 1f / steps;

			for (int i = 1; i <= steps; i++)
			{
				var t0 = p * (i - 1);
				var t1 = p * i;
				for (var j = 0; j < edges.Length; j++)
				{
					var a = edges[j].Start.ToVector3() + offset;
					var b = edges[j].End.ToVector3() + offset;
					var start = Vector3.Lerp(a, b, t0);
					var end = Vector3.Lerp(a, b, t1);
					var color = Color.Lerp(from, to, t0);
					Debug.DrawLine(start, end, color, float.MaxValue);
				}
			}
			
			
		}

		public static void DebugRender(VEdge[] edges, Color from, Color to, int steps = 3, Vector3 offset = new Vector3())
		{
			var p = 1f / steps;

			for (int i = 1; i <= steps; i++)
			{
				var t0 = p * (i - 1);
				var t1 = p * i;
				for (var j = 0; j < edges.Length; j++)
				{
					var a = edges[j].Start.ToVector3() + offset;
					var b = edges[j].End.ToVector3() + offset;
					var start = Vector3.Lerp(a, b, t0);
					var end = Vector3.Lerp(a, b, t1);
					var color = Color.Lerp(from, to, t0);
					Debug.DrawLine(start, end, color, float.MaxValue);
				}
			}
			
			
		}   
    }
}
