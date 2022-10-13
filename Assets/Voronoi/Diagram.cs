using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Voronoi.Structures;

namespace Voronoi
{
	public class Diagram
	{
		public float2[] Sites;
		public VEdge[] Edges;
		public VEdge[][] Regions;

		private const int MaxSitesPerJob = 1024;
		private float4 size;

		public Diagram(float2[] points, float4 size)
		{
			Sites = points;
			this.size = size;
		}
		public void Build(int maxSitesPerJob = MaxSitesPerJob, bool debug = false)
		{
			var jobsCount = CalcJobsCount(Sites.Length, maxSitesPerJob);
			var jobHandles = new NativeList<JobHandle>(jobsCount, Allocator.Persistent);
			var jobs = BuildChunks(Sites, jobsCount, ref jobHandles);
			
			// if (debug) DebugRender(jobs);

			if (jobs.Length == 1)
			{
				jobHandles.Dispose();
				CopyFromNativeCollections(jobs);
				return;
			}

			var mergeJobs = new VoronoiMerger[jobs.Length / 2];
			for (int i = 0, j = 0; j < jobs.Length; i++, j += 2)
			{
				var job = VoronoiMerger.CreateJob(ref jobs[j], ref jobs[j + 1]);
				mergeJobs[i] = job;
				jobHandles.Add(job.Schedule());
			}

			JobHandle.ScheduleBatchedJobs();
			JobHandle.CompleteAll(jobHandles);

			foreach (var job in jobs) job.Dispose();

			var iteration = 0;
			while (mergeJobs.Length > 1)
			{
				iteration++;
				jobHandles.Clear();
				var nextJobs = new VoronoiMerger[mergeJobs.Length / 2];
				for (int i = 0, j = 0; j < mergeJobs.Length; i++, j += 2)
				{
					nextJobs[i] = VoronoiMerger.CreateJob(ref mergeJobs[j], ref mergeJobs[j + 1]);
					nextJobs[i].debug = mergeJobs.Length == 2;
					jobHandles.Add(nextJobs[i].Schedule());
				}
				JobHandle.ScheduleBatchedJobs();
				JobHandle.CompleteAll(jobHandles);

				Debug.Log($"{iteration}");
				// DebugRender(nextJobs, iteration);

				if (mergeJobs.Length == 2)
				foreach (var merger in nextJobs)
				{
					DebugRender(merger.leftRemoved, Color.HSVToRGB(0, 1f, 0.2f), Color.HSVToRGB(0, 1f, .4f));
					DebugRender(merger.rightRemoved, Color.HSVToRGB(0.5f, 1f, 0.2f), Color.HSVToRGB(.5f, 1f, .4f));
					DebugRender(merger.newEdges, Color.white);
					DebugRender(merger.LeftEdges, Color.HSVToRGB(0f, 1f, .5f), Color.HSVToRGB(0f, 1f, 1f));
					DebugRender(merger.RightEdges, Color.HSVToRGB(.5f, 1f, .5f), Color.HSVToRGB(.5f, 1f, 1f));


					foreach (var site in merger.LeftSites)
					{
						Debug.DrawRay(site.Point.ToVector3(), Vector3.up, Color.magenta, float.MaxValue);
					}
					foreach (var site in merger.RightSites)
					{
						Debug.DrawRay(site.Point.ToVector3(), Vector3.up, Color.green, float.MaxValue);
					}


					foreach (var vEdge in merger.newEdges)
					{
						var a = merger.Sites[merger.SiteIdIndexes[vEdge.Left]].Point.ToVector3();
						var b = merger.Sites[merger.SiteIdIndexes[vEdge.Right]].Point.ToVector3();
						Debug.DrawLine(a, b, Color.yellow, float.MaxValue);
					}
					
					
					// foreach (var edge in merger.newEdges)
					// {
						// var a = merger.Sites[merger.SiteIdIndexes[edge.Left]].Point.ToVector3();
						// var b = merger.Sites[merger.SiteIdIndexes[edge.Right]].Point.ToVector3();
						// Debug.DrawLine(a, b, Color.yellow, float.MaxValue);
					// }

					
					// Debug.DrawLine(merger.LeftConvexHull[^1].Point.ToVector3(), merger.LeftConvexHull[0].Point.ToVector3(), Color.yellow, float.MaxValue);
					// for (var i = 1; i < merger.LeftConvexHull.Length; i++)
					// {
						// var a = merger.LeftConvexHull[i - 1].Point.ToVector3();
						// var b = merger.LeftConvexHull[i].Point.ToVector3();
						// Debug.DrawLine(a, b, Color.yellow, float.MaxValue);
					// }
				}

				foreach (var mergeJob in mergeJobs) mergeJob.Dispose();
				mergeJobs = nextJobs;
			}
			
			jobHandles.Dispose();
			
			CopyFromNativeCollections(mergeJobs);
		}

		private FortunesWithConvexHull[] BuildChunks(float2[] sites, int jobsCount, ref NativeList<JobHandle> jobHandles)
		{
			var vSites = new NativeArray<VSite>(sites.Length, Allocator.Persistent);
			for (var i = 0; i < sites.Length; i++) vSites[i] = new VSite(i, sites[i]);
			vSites.Sort(new FortuneSiteComparer());

			var sitesPerJob = (int) math.ceil((float) vSites.Length / jobsCount);
			var jobs = new FortunesWithConvexHull[jobsCount];
			
			Debug.Log($"sites {vSites.Length}; jobs {jobsCount}; sites per job {sitesPerJob}");
			
			for (var i = 0; i < jobsCount; i++)
			{
				var start = i * sitesPerJob;
				var length = math.min(sitesPerJob, vSites.Length - start);
				jobs[i] = FortunesWithConvexHull.CreateJob(new NativeSlice<VSite>(vSites, start, length), size);
				jobHandles.Add(jobs[i].Schedule());
			}

			JobHandle.ScheduleBatchedJobs();
			JobHandle.CompleteAll(jobHandles);
			jobHandles.Clear();
			vSites.Dispose();

			return jobs;
		}

		private static int CalcJobsCount(int sitesCount, int maxSitesPerJob)
		{
			return math.ceilpow2((int) math.ceil((float) sitesCount / maxSitesPerJob));
		}

		private void CopyFromNativeCollections(FortunesWithConvexHull[] data)
		{
			CopyFromNativeCollections(data[0].Edges.Length, ref data[0].Edges, ref data[0].Regions);
			data[0].Dispose();
		}

		private void CopyFromNativeCollections(VoronoiMerger[] data)
		{
			CopyFromNativeCollections(data[0].Edges.Length, ref data[0].Edges, ref data[0].Regions);
			data[0].Dispose();
		}

		private void CopyFromNativeCollections(int edgesCount, ref NativeList<VEdge> edges, ref NativeMultiHashMap<int, int> regions)
		{
			Edges = new VEdge[edgesCount];
			NativeArray<VEdge>.Copy(edges, Edges, Edges.Length);
			
			Regions = new VEdge[Sites.Length][];
			for (var i = 0; i < Sites.Length; i++)
			{
				var region = regions.GetValuesForKey(i);
				var list = new List<VEdge>();
				foreach (var edge in region) list.Add(Edges[edge]);
				Regions[i] = list.ToArray();
			}
		}

		private void DebugRender(FortunesWithConvexHull[] hulls)
		{
			var heightStep =  Vector3.up * 10;
			for (var i = 0; i < hulls.Length; i++)
			{
				var color = Color.HSVToRGB(1f / hulls.Length * i, 1f, 1f);
				DebugRender(hulls[i].Edges, color, heightStep * i);
			}
		}


		private void DebugRender(VoronoiMerger[] hulls, int iteration)
		{
			var heightStep =  Vector3.up * 10;
			var offset = (Vector3.forward + Vector3.right) * 120 * iteration;
			for (var i = 0; i < hulls.Length; i++)
				DebugRender(hulls[i].Edges, Color.HSVToRGB(1f / hulls.Length * i, 1f, 1f), heightStep * i + offset);

		}

		private void DebugRender(NativeList<VEdge> edges, Color color, Vector3 offset = new Vector3())
		{
			for (var j = 0; j < edges.Length; j++)
			{
				var start = edges[j].Start.ToVector3() + offset;
				var end = edges[j].End.ToVector3() + offset;
				Debug.DrawLine(start, end, color, float.MaxValue);
			}
		}

		private void DebugRender(NativeList<VEdge> edges, Color from, Color to, Vector3 offset = new Vector3())
		{
			var steps = 3;
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

		private class FortuneSiteComparer : IComparer<VSite>
		{
			public int Compare(VSite a, VSite b)
			{
				return a.X.CompareTo(b.X);
			}
		}
	}
}