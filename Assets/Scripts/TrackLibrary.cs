using System.Collections.Generic;
using UnityEngine;

public class TrackDef
{
    public string id, name, blurb;
    public float width;
    public int trees;
    public Color grass, roadA, roadB, accent, sky, foliage, sunColor, ambient;
    public float sunIntensity;
    public Vector3[] points; // closed loop, first segment defines the start direction
}

public static class TrackLibrary
{
    static Color C(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f); }
    static Vector3 P(float x, float y, float z) { return new Vector3(x, y, z); }

    public static readonly TrackDef[] All = new TrackDef[]
    {
        new TrackDef {
            id = "sunrise", name = "Sunrise Circuit",
            blurb = "Wide, flowing and friendly.\nA perfect warm-up.",
            width = 14f, trees = 150,
            grass = C(118, 168, 88), roadA = C(70, 72, 80), roadB = C(80, 82, 90), accent = C(255, 140, 40),
            sky = C(250, 196, 150), foliage = C(52, 120, 62), sunColor = C(255, 226, 190), ambient = C(150, 140, 150), sunIntensity = 1.2f,
            points = new Vector3[] {
                P(0,0,0), P(50,0,0), P(100,0,10), P(135,0,50), P(120,0,100), P(70,0,120),
                P(20,0,100), P(-20,0,110), P(-60,0,80), P(-60,0,30), P(-35,0,-8) }
        },
        new TrackDef {
            id = "canyon", name = "Canyon Run",
            blurb = "Tight corners and rolling hills.\nKeep it on the road.",
            width = 12f, trees = 70,
            grass = C(184, 126, 80), roadA = C(60, 58, 64), roadB = C(70, 68, 74), accent = C(40, 200, 220),
            sky = C(182, 212, 236), foliage = C(120, 140, 70), sunColor = C(255, 240, 215), ambient = C(160, 150, 150), sunIntensity = 1.25f,
            points = new Vector3[] {
                P(0,0,0), P(60,0,0), P(110,2,-20), P(150,6,-10), P(170,10,30), P(140,12,70), P(90,10,60), P(60,8,90),
                P(70,5,140), P(30,2,170), P(-20,0,150), P(-30,0,100), P(-70,0,70), P(-70,0,20), P(-40,0,-10) }
        },
        new TrackDef {
            id = "midnight", name = "Midnight Highway",
            blurb = "Long, fast and lit by the moon.\nFor the brave.",
            width = 11f, trees = 110,
            grass = C(40, 74, 64), roadA = C(46, 48, 60), roadB = C(54, 56, 68), accent = C(255, 60, 160),
            sky = C(34, 42, 86), foliage = C(24, 70, 60), sunColor = C(150, 170, 255), ambient = C(70, 80, 125), sunIntensity = 0.7f,
            points = new Vector3[] {
                P(0,0,0), P(80,0,0), P(170,0,-10), P(240,0,20), P(260,0,80), P(220,0,130), P(150,0,120), P(130,0,70),
                P(90,0,60), P(60,0,100), P(80,0,160), P(30,0,200), P(-40,0,180), P(-60,0,120), P(-50,0,50), P(-30,0,10) }
        }
    };
}

// Closed Catmull-Rom spline sampled into dense points.
public class TrackPath
{
    public Vector3[] pos, tan, right;
    public int Count { get { return pos.Length; } }

    public TrackPath(Vector3[] c, int sub)
    {
        int n = c.Length;
        List<Vector3> list = new List<Vector3>();
        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = c[(i - 1 + n) % n], p1 = c[i], p2 = c[(i + 1) % n], p3 = c[(i + 2) % n];
            for (int s = 0; s < sub; s++) list.Add(CR(p0, p1, p2, p3, s / (float)sub));
        }
        pos = list.ToArray();
        int m = pos.Length;
        tan = new Vector3[m];
        right = new Vector3[m];
        for (int i = 0; i < m; i++)
        {
            tan[i] = (pos[(i + 1) % m] - pos[(i - 1 + m) % m]).normalized;
            Vector3 flat = new Vector3(tan[i].x, 0f, tan[i].z).normalized;
            right[i] = Vector3.Cross(Vector3.up, flat);
        }
    }

    static Vector3 CR(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * b) + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (-a + 3f * b - 3f * c + d) * t3);
    }

    public float MinDist2D(Vector3 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i < pos.Length; i++)
        {
            float dx = pos[i].x - p.x, dz = pos[i].z - p.z;
            float d = dx * dx + dz * dz;
            if (d < best) best = d;
        }
        return Mathf.Sqrt(best);
    }
}
