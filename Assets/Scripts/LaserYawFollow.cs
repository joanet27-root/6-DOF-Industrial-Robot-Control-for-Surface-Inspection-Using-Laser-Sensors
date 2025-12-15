using UnityEngine;

public class LaserYawFollow : MonoBehaviour
{
    public Transform marker;          // punto a seguir
    public float maxYawSpeed = 200f;  // °/s
    public Vector2 yawLimits = new Vector2(-170f, 170f);

    void Update()
    {
        if (!marker) return;

        // Vector horizontal desde el Yaw al marker
        Vector3 to = marker.position - transform.position;
        Vector3 flat = new Vector3(to.x, 0f, to.z);

        if (flat.sqrMagnitude < 1e-6f) return; // marker justo encima

        // Ángulo deseado en torno a Y (grados, -180..180)
        float desired = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

        // Ángulo actual local del Yaw (normalizado)
        float current = transform.localEulerAngles.y;
        if (current > 180f) current -= 360f;

        // Clamp (respecto a 0 -> mueve límites si tu base está rotada)
        desired = Mathf.Clamp(desired, yawLimits.x, yawLimits.y);

        // Avanza con velocidad limitada
        float next = Mathf.MoveTowardsAngle(current, desired, maxYawSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0f, next, 0f);
    }
}
