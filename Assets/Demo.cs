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
        
        // generate points
        var points = GenerateRandomPoints();
        
        var sw = Stopwatch.StartNew();
        
        // build voronoi diagram
        var diagram = new Voronoi.Diagram(points);
        diagram.Build();
        
        sw.Stop();
        Debug.Log($"elapsed time {sw.ElapsedMilliseconds}ms");

        // debug output
        foreach (var edge in diagram.Edges)
            Debug.DrawLine(ToVector3(edge.Start), ToVector3(edge.End), Color.white, float.MaxValue);

        // highlight ~10 random regions
        for (var i = 0; i < math.min(10, points.Length); i++)
            foreach (var edge in diagram.Regions[Random.Range(0, points.Length)])
                Debug.DrawLine(ToVector3(edge.Start), ToVector3(edge.End), Color.red, float.MaxValue);

    }

    private float2[] GenerateRandomPoints()
    {
        if (seed != 0) Random.InitState(seed);
        var points = new float2[siteCount];
        for (var i = 0; i < siteCount; i++) points[i] = new float2(Random.Range(0, width), Random.Range(0, height));
        return points;
    }

    private static Vector3 ToVector3(float2 v)
    {
        return new Vector3(v.x, 0, v.y);
    }
}