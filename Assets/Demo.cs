using System;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using NaughtyAttributes;

public class Demo : MonoBehaviour
{
    public int siteCount = 128;
    public float width = 15;
    public float height = 10;
    public int seed;
    public int sitesPerJob = 1024;
    public bool render = false;
    public bool debug = false;

    void Start()
    {
        UnityEditor.EditorWindow.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        Build(width, height, siteCount, render);
    }

    [Button]
    private void BuildRandom()
    {
        Build(width, height, siteCount, render);
    }
    
    void Build(float w, float h, int c, bool debugRender = false)
    {
        // generate points
        var points = GenerateRandomPoints(w, h, c);
        
        var sw = Stopwatch.StartNew();
        
        // build voronoi diagram
        var diagram = new Voronoi.Diagram(points, new float4(0,0,width, height));
        diagram.Build(sitesPerJob, debug);
        
        sw.Stop();
        Debug.Log($"elapsed time {sw.ElapsedMilliseconds}ms");

        if (!debugRender) return;
        foreach (var edge in diagram.Edges)
            Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.white, float.MaxValue);
    }

    private float2[] GenerateRandomPoints(float w, float h, int c)
    {
        if (seed == 0) seed = CurrentEpoch(); 
        Random.InitState(seed);
        Debug.Log($"seed {seed}");
        var points = new float2[c];
        for (var i = 0; i < c; i++) points[i] = new float2(Random.Range(0, w), Random.Range(0, h));
        return points;
    }
    
    public static int CurrentEpoch()
    {
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentEpochTime = (int)(DateTime.UtcNow - epochStart).TotalSeconds;
        return currentEpochTime;
    }
}