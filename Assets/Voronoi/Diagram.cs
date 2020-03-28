using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Voronoi.Jobs;
using Voronoi.Structures;

namespace Voronoi
{
	public class Diagram
	{
		public float2[] Sites;
		public VEdge[] Edges;
		public VEdge[][] Regions;

		public Diagram(float2[] points)
		{
			Sites = points;
		}

		public void Build()
		{
			var vSites = new NativeArray<VSite>(Sites.Length, Allocator.Persistent);
			for (var i = 0; i < Sites.Length; i++) vSites[i] = new VSite(i, Sites[i]);
			vSites.Sort(new FortuneSiteComparer());
			
			var job = FortunesAlgorithm.CreateJob(vSites);
			job.Run();

			Edges = job.Edges.ToArray();
			Regions = new VEdge[Sites.Length][];

			for (var i = 0; i < Sites.Length; i++)
			{
				var region = job.Regions.GetValuesForKey(i);
				var edges = new List<VEdge>();
				foreach (var edge in region) edges.Add(edge);
				Regions[i] = edges.ToArray();
			}

			job.Dispose();
		}

		private class FortuneSiteComparer : IComparer<VSite>
		{
			public int Compare(VSite a, VSite b)
			{
				return a.X.CompareTo(b.X);
			}
		}
	}
}