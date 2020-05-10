using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Voronoi.Structures;

// todo allocate memory from system before run build
namespace Voronoi
{
	public class Diagram
	{
		public float2[] Sites;
		public VEdge[] Edges;
		public VEdge[][] Regions;

		private const int MaxSitesPerJob = 1024;

		public Diagram(float2[] points)
		{
			Sites = points;
		}

		public void Build()
		{
			var vSites = new NativeArray<VSite>(Sites.Length, Allocator.Persistent);
			for (var i = 0; i < Sites.Length; i++) vSites[i] = new VSite(i, Sites[i]);
			vSites.Sort(new FortuneSiteComparer());

			var jobsCount = math.ceilpow2((int) math.ceil((float) vSites.Length / MaxSitesPerJob));
			var sitesPerJob = (int) math.ceil((float) vSites.Length / jobsCount);
			var jobs = new FortunesWithConvexHull[jobsCount];
			var jobHandles = new NativeList<JobHandle>(jobsCount, Allocator.Persistent);
			
			Debug.Log($"sites {vSites.Length}; jobs {jobsCount}; sites per job {sitesPerJob}");
			
			for (var i = 0; i < jobsCount; i++)
			{
				var start = i * sitesPerJob;
				var length = math.min(sitesPerJob, vSites.Length - start);
				jobs[i] = FortunesWithConvexHull.CreateJob(new NativeSlice<VSite>(vSites, start, length));
				jobHandles.Add(jobs[i].Schedule());
			}

			JobHandle.ScheduleBatchedJobs();
			JobHandle.CompleteAll(jobHandles);
			jobHandles.Clear();
			vSites.Dispose();

			if (jobs.Length == 1)
			{
				jobHandles.Dispose();
				Prepare(jobs[0]);
				return;
			}
			
			var mergeJobs = new List<VoronoiMerger>();
			for (var i = 0; i < jobs.Length; i += 2)
			{
				var job = VoronoiMerger.CreateJob(jobs[i], jobs[i + 1]);
				mergeJobs.Add(job);
				jobHandles.Add(job.Schedule());
			}

			JobHandle.ScheduleBatchedJobs();
			JobHandle.CompleteAll(jobHandles);

			foreach (var job in jobs) job.Dispose();

			while (mergeJobs.Count > 1)
			{
				jobHandles.Clear();
				var nextJobs = new List<VoronoiMerger>();
				for (var i = 0; i < mergeJobs.Count; i += 2)
				{
					var job = VoronoiMerger.CreateJob(mergeJobs[i], mergeJobs[i + 1]);
					nextJobs.Add(job);
					jobHandles.Add(job.Schedule());
				}
				JobHandle.ScheduleBatchedJobs();
				JobHandle.CompleteAll(jobHandles);
				
				foreach (var mergeJob in mergeJobs) mergeJob.Dispose();
				mergeJobs.Clear();

				mergeJobs.AddRange(nextJobs);
			}
			
			jobHandles.Dispose();
			
			Prepare(mergeJobs[0]);
		}

		private void Prepare(FortunesWithConvexHull job)
		{
			Prepare(job.EdgesCount[0], job.Edges, job.Regions);
			job.Dispose();
		}

		private void Prepare(VoronoiMerger job)
		{
			Prepare(job.Edges.Length, job.Edges, job.Regions);
			job.Dispose();
		}

		private void Prepare(int edgesCount, NativeList<VEdge> edges, NativeMultiHashMap<int, int> regions)
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

		private class FortuneSiteComparer : IComparer<VSite>
		{
			public int Compare(VSite a, VSite b)
			{
				return a.X.CompareTo(b.X);
			}
		}
	}
}