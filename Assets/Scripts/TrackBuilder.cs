using UnityEngine;

public class Track
{
    public TrackDef def;
    public TrackPath path;
    public GameObject root;
    public int[] gateIdx;
    public int spawnIdx;
    public Bounds bounds;
}

public static class TrackBuilder
{
    public static Material Mat(Color c, float smooth = 0.05f)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material m = new Material(sh);
        if (m.HasProperty("_Color")) m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    static void Add(Transform parent, string name, MeshBuilder mb, Color c)
    {
        if (mb.Empty) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mb.Build(name);
        go.AddComponent<MeshRenderer>().sharedMaterial = Mat(c);
    }

    public static Track Build(TrackDef d, int gates)
    {
        Track t = new Track();
        t.def = d;
        t.root = new GameObject("Track_" + d.name);
        Transform root = t.root.transform;
        TrackPath p = new TrackPath(d.points, 24);
        t.path = p;
        int n = p.Count;
        float hw = d.width * 0.5f;

        t.bounds = new Bounds(p.pos[0], Vector3.zero);
        for (int i = 1; i < n; i++) t.bounds.Encapsulate(p.pos[i]);
        t.gateIdx = new int[gates];
        for (int k = 0; k < gates; k++) t.gateIdx[k] = k * n / gates;
        t.spawnIdx = (n - 4) % n;

        MeshBuilder roadA = new MeshBuilder(), roadB = new MeshBuilder(), col = new MeshBuilder();
        MeshBuilder curbR = new MeshBuilder(), curbW = new MeshBuilder();
        MeshBuilder white = new MeshBuilder(), black = new MeshBuilder(), accent = new MeshBuilder();
        Vector3 lift = Vector3.up * 0.03f;
        float cw = 1.3f;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector3 lA = p.pos[i] - p.right[i] * hw, rA = p.pos[i] + p.right[i] * hw;
            Vector3 lB = p.pos[j] - p.right[j] * hw, rB = p.pos[j] + p.right[j] * hw;
            MeshBuilder road = ((i / 6) % 2 == 0) ? roadA : roadB;
            road.Quad(lA, lB, rB, rA, Vector3.up);
            col.Quad(lA, lB, rB, rA, Vector3.up);

            Vector3 oLA = lA - p.right[i] * cw, oLB = lB - p.right[j] * cw;
            Vector3 oRA = rA + p.right[i] * cw, oRB = rB + p.right[j] * cw;
            MeshBuilder cb = ((i / 2) % 2 == 0) ? curbR : curbW;
            cb.Quad(oLA + lift, oLB + lift, lB + lift, lA + lift, Vector3.up);
            cb.Quad(rA + lift, rB + lift, oRB + lift, oRA + lift, Vector3.up);
            col.Quad(oLA, oLB, lB, lA, Vector3.up);
            col.Quad(rA, rB, oRB, oRA, Vector3.up);
        }

        // Start / finish checker line
        Vector3 P0 = p.pos[0], F0 = p.tan[0], R0 = p.right[0];
        int cells = 8;
        float cellW = d.width / cells, cellL = 1.4f;
        for (int j = 0; j < 2; j++)
            for (int k = 0; k < cells; k++)
            {
                Vector3 b = P0 + R0 * (-hw + k * cellW) + F0 * ((j - 1) * cellL) + lift * 1.5f;
                Vector3 q = b + R0 * cellW, q2 = q + F0 * cellL, b2 = b + F0 * cellL;
                (((j + k) % 2 == 0) ? white : black).Quad(b, q, q2, b2, Vector3.up);
            }

        // Start arch
        Vector3 f = F0.normalized;
        accent.Box(P0 - R0 * (hw + 1.6f) + Vector3.up * 3.5f, R0 * 0.4f, Vector3.up * 3.5f, f * 0.4f);
        accent.Box(P0 + R0 * (hw + 1.6f) + Vector3.up * 3.5f, R0 * 0.4f, Vector3.up * 3.5f, f * 0.4f);
        accent.Box(P0 + Vector3.up * 7.2f, R0 * (hw + 2f), Vector3.up * 0.6f, f * 0.4f);

        // Checkpoint markers
        for (int k = 1; k < gates; k++)
        {
            int gi = t.gateIdx[k];
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 c = p.pos[gi] + p.right[gi] * (s * (hw + 2.4f)) + Vector3.up * 1.6f;
                accent.Box(c, p.right[gi] * 0.15f, Vector3.up * 1.6f, p.tan[gi] * 0.15f);
            }
        }

        // Trees
        System.Random rng = new System.Random(d.points.Length * 7919 + (int)d.width);
        MeshBuilder trunks = new MeshBuilder(), leavesA = new MeshBuilder(), leavesB = new MeshBuilder();
        float minX = t.bounds.min.x - 70f, maxX = t.bounds.max.x + 70f, minZ = t.bounds.min.z - 70f, maxZ = t.bounds.max.z + 70f;
        int placed = 0;
        for (int attempt = 0; attempt < d.trees * 30 && placed < d.trees; attempt++)
        {
            float x = minX + (float)rng.NextDouble() * (maxX - minX);
            float z = minZ + (float)rng.NextDouble() * (maxZ - minZ);
            Vector3 tp = new Vector3(x, 0f, z);
            if (p.MinDist2D(tp) < hw + 9f) continue;
            float s = 0.8f + (float)rng.NextDouble() * 1.0f;
            trunks.Box(tp + Vector3.up * s, Vector3.right * 0.25f * s, Vector3.up * s, Vector3.forward * 0.25f * s);
            MeshBuilder lv = (placed % 2 == 0) ? leavesA : leavesB;
            float rot = (float)rng.NextDouble() * 6f;
            lv.Cone(tp + Vector3.up * 1.8f * s, 2.4f * s, 3.6f * s, 6, rot);
            lv.Cone(tp + Vector3.up * 3.6f * s, 1.8f * s, 3.2f * s, 6, rot + 0.5f);
            placed++;
        }

        // Distant mountains
        MeshBuilder mountains = new MeshBuilder();
        for (int k = 0; k < 16; k++)
        {
            float ang = k * Mathf.PI * 2f / 16f + (float)rng.NextDouble() * 0.3f;
            float dist = 650f + (float)rng.NextDouble() * 250f;
            Vector3 bp = t.bounds.center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist - Vector3.up * 3f;
            mountains.Cone(bp, 90f + (float)rng.NextDouble() * 90f, 50f + (float)rng.NextDouble() * 100f, 5, (float)rng.NextDouble() * 6f);
        }

        Add(root, "RoadA", roadA, d.roadA);
        Add(root, "RoadB", roadB, d.roadB);
        Add(root, "CurbRed", curbR, C(215, 50, 50));
        Add(root, "CurbWhite", curbW, C(235, 235, 235));
        Add(root, "StartWhite", white, C(240, 240, 240));
        Add(root, "StartBlack", black, C(20, 20, 20));
        Add(root, "Accent", accent, d.accent);
        Add(root, "Trunks", trunks, C(92, 64, 44));
        Add(root, "LeavesA", leavesA, d.foliage);
        Add(root, "LeavesB", leavesB, Color.Lerp(d.foliage, Color.white, 0.12f));
        Add(root, "Mountains", mountains, Color.Lerp(d.grass * 0.6f, d.sky, 0.5f));

        GameObject colGo = new GameObject("RoadCollider");
        colGo.transform.SetParent(root, false);
        colGo.AddComponent<MeshCollider>().sharedMesh = col.Build("RoadCollider");

        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "Ground";
        g.transform.SetParent(root, false);
        g.transform.position = new Vector3(t.bounds.center.x, -0.05f, t.bounds.center.z);
        g.transform.localScale = new Vector3(300f, 1f, 300f);
        g.GetComponent<MeshRenderer>().sharedMaterial = Mat(d.grass, 0f);

        return t;
    }

    static Color C(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f); }
}
