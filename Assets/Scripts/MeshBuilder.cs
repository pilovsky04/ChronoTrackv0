using System.Collections.Generic;
using UnityEngine;

// Builds flat-shaded (low-poly) meshes. Winding is auto-corrected using a "hint" direction.
public class MeshBuilder
{
    readonly List<Vector3> v = new List<Vector3>();
    readonly List<Vector3> n = new List<Vector3>();
    readonly List<int> t = new List<int>();

    public bool Empty { get { return v.Count == 0; } }

    public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 hint)
    {
        Vector3 nn = Vector3.Cross(b - a, c - a);
        if (nn.sqrMagnitude < 1e-10f) return;
        if (Vector3.Dot(nn, hint) < 0f) { Vector3 tmp = b; b = c; c = tmp; nn = -nn; }
        nn.Normalize();
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        n.Add(nn); n.Add(nn); n.Add(nn);
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
    }

    // a,b,c,d must go around the quad's perimeter.
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 hint)
    {
        Tri(a, b, c, hint);
        Tri(a, c, d, hint);
    }

    void Face(Vector3 fc, Vector3 u, Vector3 w, Vector3 hint)
    {
        Quad(fc - u - w, fc + u - w, fc + u + w, fc - u + w, hint);
    }

    // ax/ay/az are half-extent vectors.
    public void Box(Vector3 c, Vector3 ax, Vector3 ay, Vector3 az)
    {
        Face(c + ax, ay, az, ax); Face(c - ax, ay, az, -ax);
        Face(c + ay, ax, az, ay); Face(c - ay, ax, az, -ay);
        Face(c + az, ax, ay, az); Face(c - az, ax, ay, -az);
    }

    public void Cone(Vector3 baseCenter, float r, float h, int sides, float rot)
    {
        Vector3 apex = baseCenter + Vector3.up * h;
        for (int i = 0; i < sides; i++)
        {
            float a0 = rot + i * Mathf.PI * 2f / sides;
            float a1 = rot + (i + 1) * Mathf.PI * 2f / sides;
            Vector3 p0 = baseCenter + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * r;
            Vector3 p1 = baseCenter + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * r;
            Vector3 mid = (p0 + p1) * 0.5f - baseCenter;
            Tri(p0, p1, apex, new Vector3(mid.x, r * 0.5f, mid.z));
        }
    }

    public Mesh Build(string name)
    {
        Mesh m = new Mesh();
        m.name = name;
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v);
        m.SetNormals(n);
        m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }
}
