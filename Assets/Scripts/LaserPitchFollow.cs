using UnityEngine;

public class LaserPitchFollow : MonoBehaviour
{
    public Transform marker;            // punto a seguir
    public Transform yawPivot;          // objeto Yaw (define el "right" horizontal)
    public float maxPitchSpeed = 200f;  // °/s
    public Vector2 pitchLimits = new Vector2(-45f, 45f);

    [Header("Calibración")]
    public bool invertPitch = false;    // invierte el signo si apunta al revés
    public float pitchOffsetDeg = 0f;   // pequeño offset mecánico (+/-)

    void Update()
    {
        if (!marker || !yawPivot) return;

        // Vector desde PITCH al marker
        Vector3 to = marker.position - transform.position;

        // Descomponer respecto al eje horizontal del yaw (right)
        float distXZ = Vector3.ProjectOnPlane(to, yawPivot.right).magnitude;
        float desired = Mathf.Atan2(to.y, distXZ) * Mathf.Rad2Deg;

        // Ajustes de signo y offset
        if (invertPitch) desired = -desired;
        desired += pitchOffsetDeg;

        // Actual (normalizado -180..180)
        float current = transform.localEulerAngles.x;
        if (current > 180f) current -= 360f;

        desired = Mathf.Clamp(desired, pitchLimits.x, pitchLimits.y);

        float next = Mathf.MoveTowards(current, desired, maxPitchSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(next, 0f, 0f);
    }
}
