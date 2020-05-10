using System;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class Demo : MonoBehaviour
{
    public int siteCount = 128;
    public float width = 15;
    public float height = 10;
    public int seed;

    void Start()
    {
        UnityEditor.EditorWindow.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        Build(width, height, siteCount, true);
    }

    void Build(float w, float h, int c, bool render = false)
    {
        // generate points
        var points = GenerateRandomPoints(w, h, c);
        
        var sw = Stopwatch.StartNew();
        
        // build voronoi diagram
        var diagram = new Voronoi.Diagram(points);
        diagram.Build();
        
        sw.Stop();
        Debug.Log($"elapsed time {sw.ElapsedMilliseconds}ms");

        // debug output
        foreach (var edge in diagram.Edges)
            Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.white, float.MaxValue);

        // highlight ~10 random regions
        for (var i = 0; i < math.min(10, points.Length); i++)
            foreach (var edge in diagram.Regions[Random.Range(0, points.Length)])
                Debug.DrawLine(edge.Start.ToVector3(), edge.End.ToVector3(), Color.red, float.MaxValue);
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