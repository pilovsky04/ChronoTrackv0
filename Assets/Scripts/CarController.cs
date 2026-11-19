using UnityEngine;

// Arcade car: a Rigidbody with custom grip, steering and speed handling.
public class CarController : MonoBehaviour
{
    public bool controllable;
    public float maxSpeed = 50f;
    public float accel = 24f;
    public float brakeDecel = 38f;
    public float reverseMax = 14f;

    public Rigidbody rb { get; private set; }
    public bool OnRoad { get; private set; }
    public bool Grounded { get; private set; }
    public float SpeedKmh { get { return rb.linearVelocity.magnitude * 3.6f; } }

    Transform[] spinNodes;
    Transform[] frontPivots;
    float steer, spin;
    AudioSource engine;
    static AudioClip engineClip;
    Vector3 groundNormal = Vector3.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public static CarController Create(Vector3 pos, Quaternion rot, Color body)
    {
        GameObject go = new GameObject("Car");
        go.layer = 2; // Ignore Raycast, so the ground probe never hits the car itself
        go.transform.SetPositionAndRotation(pos, rot);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.linearDamping = 0.02f;
        rb.angularDamping = 1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass = new Vector3(0f, 0.4f, 0f);

        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.65f, 0f);
        bc.size = new Vector3(1.8f, 1.2f, 4f);
        PhysicsMaterial pm = new PhysicsMaterial("Car");
        pm.dynamicFriction = 0f;
        pm.staticFriction = 0f;
        pm.bounciness = 0f;
        pm.frictionCombine = PhysicsMaterialCombine.Minimum;
        pm.bounceCombine = PhysicsMaterialCombine.Minimum;
        bc.sharedMaterial = pm;

        CarController cc = go.AddComponent<CarController>();
        cc.BuildVisuals(go.transform, body);

        cc.engine = go.AddComponent<AudioSource>();
        cc.engine.clip = EngineClip();
        cc.engine.loop = true;
        cc.engine.spatialBlend = 0f;
        cc.engine.volume = 0.16f;
        cc.engine.Play();
        return cc;
    }

    static AudioClip EngineClip()
    {
        if (engineClip != null) return engineClip;
        const int sr = 44100;
        float[] data = new float[sr];
        for (int i = 0; i < sr; i++)
        {
            float t = i / (float)sr;
            float saw = 2f * ((55f * t) % 1f) - 1f;
            data[i] = 0.35f * Mathf.Sin(2f * Mathf.PI * 55f * t)
                    + 0.20f * Mathf.Sin(2f * Mathf.PI * 110f * t)
                    + 0.12f * Mathf.Sin(2f * Mathf.PI * 165f * t)
                    + 0.18f * saw;
        }
        engineClip = AudioClip.Create("engine", sr, 1, sr, false);
        engineClip.SetData(data, 0);
        return engineClip;
    }

    static Transform Prim(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color c, Vector3 euler)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        Collider col = g.GetComponent<Collider>();
        col.enabled = false;
        Destroy(col);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localEulerAngles = euler;
        g.GetComponent<MeshRenderer>().sharedMaterial = TrackBuilder.Mat(c, 0.25f);
        return g.transform;
    }

    void BuildVisuals(Transform parent, Color body)
    {
        Color dark = new Color(0.08f, 0.08f, 0.09f);
        Color glass = new Color(0.12f, 0.16f, 0.22f);
        Vector3 z = Vector3.zero;
        Prim(parent, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.45f, 4f), body, z);
        Prim(parent, PrimitiveType.Cube, new Vector3(0f, 1.02f, -0.25f), new Vector3(1.5f, 0.4f, 1.9f), glass, z);
        Prim(parent, PrimitiveType.Cube, new Vector3(0f, 1.24f, -0.25f), new Vector3(1.55f, 0.06f, 1.7f), body, z);
        Prim(parent, PrimitiveType.Cube, new Vector3(0f, 1.1f, -1.85f), new Vector3(1.7f, 0.06f, 0.5f), dark, z);
        Prim(parent, PrimitiveType.Cube, new Vector3(-0.5f, 0.92f, -1.85f), new Vector3(0.06f, 0.3f, 0.06f), dark, z);
        Prim(parent, PrimitiveType.Cube, new Vector3(0.5f, 0.92f, -1.85f), new Vector3(0.06f, 0.3f, 0.06f), dark, z);
        Prim(parent, PrimitiveType.Cube, new Vector3(-0.6f, 0.68f, 2.0f), new Vector3(0.4f, 0.14f, 0.06f), new Color(1f, 0.95f, 0.7f), z);
        Prim(parent, PrimitiveType.Cube, new Vector3(0.6f, 0.68f, 2.0f), new Vector3(0.4f, 0.14f, 0.06f), new Color(1f, 0.95f, 0.7f), z);
        Prim(parent, PrimitiveType.Cube, new Vector3(-0.6f, 0.68f, -2.0f), new Vector3(0.4f, 0.14f, 0.06f), new Color(0.9f, 0.05f, 0.05f), z);
        Prim(parent, PrimitiveType.Cube, new Vector3(0.6f, 0.68f, -2.0f), new Vector3(0.4f, 0.14f, 0.06f), new Color(0.9f, 0.05f, 0.05f), z);

        Vector3[] wp = {
            new Vector3(-0.95f, 0.4f, 1.3f), new Vector3(0.95f, 0.4f, 1.3f),
            new Vector3(-0.95f, 0.4f, -1.3f), new Vector3(0.95f, 0.4f, -1.3f) };
        spinNodes = new Transform[4];
        frontPivots = new Transform[2];
        for (int i = 0; i < 4; i++)
        {
            GameObject pv = new GameObject("Wheel" + i);
            pv.transform.SetParent(parent, false);
            pv.transform.localPosition = wp[i];
            GameObject sp = new GameObject("Spin");
            sp.transform.SetParent(pv.transform, false);
            Prim(sp.transform, PrimitiveType.Cylinder, z, new Vector3(0.7f, 0.15f, 0.7f), dark, new Vector3(0f, 0f, 90f));
            Prim(sp.transform, PrimitiveType.Cylinder, z, new Vector3(0.38f, 0.17f, 0.38f), new Color(0.7f, 0.7f, 0.75f), new Vector3(0f, 0f, 90f));
            spinNodes[i] = sp.transform;
            if (i < 2) frontPivots[i] = pv.transform;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 up = transform.up;
        RaycastHit hit;
        Grounded = Physics.Raycast(transform.position + up * 0.9f, -up, out hit, 1.4f, ~(1 << 2), QueryTriggerInteraction.Ignore);
        groundNormal = Grounded ? hit.normal : Vector3.up;
        OnRoad = Grounded && hit.collider.name == "RoadCollider";

        Vector3 v = rb.linearVelocity;
        float tgtSteer = controllable ? GameInput.Steer : 0f;
        steer = Mathf.MoveTowards(steer, tgtSteer, dt * (Mathf.Abs(tgtSteer) > 0.01f ? 4.5f : 7f));

        Vector3 av = rb.angularVelocity;
        Vector3 upL = transform.up;
        float yaw = Vector3.Dot(av, upL);

        if (Grounded)
        {
            float th = controllable ? GameInput.Throttle : 0f;
            bool brake = controllable ? GameInput.Brake : true;
            float tMax = maxSpeed * (OnRoad ? 1f : 0.55f);

            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            Vector3 rgt = Vector3.Cross(groundNormal, fwd);
            float vf = Vector3.Dot(v, fwd);
            float vr = Vector3.Dot(v, rgt);

            float a = 0f;
            if (th > 0.01f) a = vf < -0.5f ? brakeDecel : accel * th * (1f - Mathf.Clamp01(vf / tMax));
            else if (th < -0.01f) a = vf > 0.5f ? -brakeDecel : -accel * 0.6f * (-th) * (1f - Mathf.Clamp01(-vf / reverseMax));
            if (vf > tMax) a -= (vf - tMax) * 2f;
            a -= vf * (OnRoad ? 0.04f : 0.4f);
            if (brake) a -= Mathf.Clamp(vf * 10f, -30f, 30f);
            rb.AddForce(fwd * a, ForceMode.Acceleration);

            float grip = brake ? 2.5f : (OnRoad ? 9f : 6f);
            rb.AddForce(-rgt * vr * grip, ForceMode.Acceleration);
            rb.AddForce(-groundNormal * (12f + Mathf.Abs(vf) * 0.2f), ForceMode.Acceleration);

            float speedF = Mathf.Clamp01(Mathf.Abs(vf) / 7f);
            float maxYaw = Mathf.Lerp(2.3f, 0.85f, Mathf.Clamp01(Mathf.Abs(vf) / maxSpeed));
            if (brake && controllable) maxYaw *= 1.35f;
            yaw = steer * maxYaw * speedF * (vf >= -0.5f ? 1f : -1f);

            // Align with the surface (spring + damping on pitch/roll only)
            rb.AddTorque(Vector3.Cross(transform.up, groundNormal) * 25f, ForceMode.Acceleration);
            Vector3 perp = av - upL * Vector3.Dot(av, upL);
            perp *= Mathf.Max(0f, 1f - 9f * dt);
            rb.angularVelocity = perp + upL * yaw;
        }
        else
        {
            rb.AddForce(Vector3.down * 10f, ForceMode.Acceleration);
            rb.AddTorque(Vector3.Cross(transform.up, Vector3.up) * 4f, ForceMode.Acceleration);
        }
    }

    void Update()
    {
        float vf = Vector3.Dot(rb.linearVelocity, transform.forward);
        spin = (spin + vf / 0.35f * Mathf.Rad2Deg * Time.deltaTime) % 360f;
        Quaternion sr = Quaternion.Euler(spin, 0f, 0f);
        for (int i = 0; i < spinNodes.Length; i++) spinNodes[i].localRotation = sr;
        Quaternion st = Quaternion.Euler(0f, steer * 28f, 0f);
        for (int i = 0; i < frontPivots.Length; i++) frontPivots[i].localRotation = st;

        float thr = controllable ? Mathf.Max(0f, GameInput.Throttle) : 0f;
        engine.pitch = 0.6f + Mathf.Clamp01(Mathf.Abs(vf) / maxSpeed) * 2.2f + thr * 0.15f;
    }

    public void PlaceAt(Vector3 p, Quaternion r)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(p, r);
        rb.position = p;
        rb.rotation = r;
        steer = 0f;
    }
}
