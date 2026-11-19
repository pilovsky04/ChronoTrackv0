using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Camera cam;
    public Transform target;
    public Rigidbody targetRb;
    public float maxSpeed = 50f;

    public bool orbit;
    public Vector3 orbitCenter;
    public float orbitRadius = 200f, orbitHeight = 100f;

    Vector3 heading = Vector3.forward;
    float angle;

    public void Snap()
    {
        if (target == null) return;
        Vector3 f = target.forward; f.y = 0f;
        if (f.sqrMagnitude > 0.001f) heading = f.normalized;
        transform.position = target.position - heading * 7f + Vector3.up * 3.2f;
    }

    void LateUpdate()
    {
        if (orbit || target == null)
        {
            angle += 7f * Time.unscaledDeltaTime;
            float r = angle * Mathf.Deg2Rad;
            Vector3 pos = orbitCenter + new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r)) * orbitRadius + Vector3.up * orbitHeight;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(orbitCenter - pos, Vector3.up);
            cam.fieldOfView = 60f;
            return;
        }

        float dt = Time.deltaTime;
        Vector3 f = target.forward; f.y = 0f;
        if (f.sqrMagnitude > 0.01f)
            heading = Vector3.Slerp(heading, f.normalized, 1f - Mathf.Exp(-5f * dt)).normalized;

        float spd = targetRb != null ? targetRb.linearVelocity.magnitude : 0f;
        float k = Mathf.Clamp01(spd / maxSpeed);
        Vector3 desired = target.position - heading * Mathf.Lerp(7f, 9.5f, k) + Vector3.up * 3.2f;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-12f * dt));
        Vector3 look = target.position + Vector3.up * 1.2f + heading * 4f;
        transform.rotation = Quaternion.LookRotation(look - transform.position, Vector3.up);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(62f, 80f, k), 1f - Mathf.Exp(-4f * dt));
    }
}
