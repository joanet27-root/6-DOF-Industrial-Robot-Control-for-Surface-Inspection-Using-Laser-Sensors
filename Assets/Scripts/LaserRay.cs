using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserRay : MonoBehaviour
{
    [Header("Medición")]
    public float maxDistance = 20f;
    public LayerMask hitMask = ~0; // todo por defecto
    public bool drawWhileNoHit = true;

    [Header("Visual")]
    public Transform hitDot;       // opcional: esfera/punto pequeño
    public Light sourceGlow;       // opcional: Spot/Point para halo
    public float beamWidth = 0.006f;

    // --- Lecturas (Inspector opcional) ---
    [SerializeField, Tooltip("Solo lectura (debug)")]
    private bool hasHitDebug;
    [SerializeField, Tooltip("Solo lectura (m)")]
    private float measuredDistanceDebug;
    [SerializeField, Tooltip("Solo lectura")]
    private Vector3 hitPointDebug;
    [SerializeField, Tooltip("Solo lectura")]
    private Vector3 hitNormalDebug;

    // Getters públicos
    public bool HasHit => hasHitDebug;
    public float MeasuredDistance => measuredDistanceDebug;
    public Vector3 HitPoint => hitPointDebug;
    public Vector3 HitNormal => hitNormalDebug;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr.positionCount != 2) lr.positionCount = 2;
        lr.startWidth = lr.endWidth = beamWidth;
    }

    void Update()
    {
        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            hasHitDebug = true;
            measuredDistanceDebug = hit.distance;
            hitPointDebug = hit.point;
            hitNormalDebug = hit.normal;

            // Línea hasta el impacto
            lr.SetPosition(0, origin);
            lr.SetPosition(1, hitPointDebug);

            // Punto de impacto
            if (hitDot)
            {
                hitDot.position = hitPointDebug + hitNormalDebug * 0.001f;
                hitDot.forward = hitNormalDebug;
                hitDot.gameObject.SetActive(true);
            }
        }
        else
        {
            hasHitDebug = false;
            measuredDistanceDebug = maxDistance;
            Vector3 end = origin + dir * maxDistance;

            if (drawWhileNoHit)
            {
                lr.SetPosition(0, origin);
                lr.SetPosition(1, end);
            }

            if (hitDot) hitDot.gameObject.SetActive(false);
        }

        // Halo opcional
        if (sourceGlow) sourceGlow.enabled = true;
    }

    void OnValidate()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr) { lr.startWidth = lr.endWidth = beamWidth; }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * Mathf.Min(maxDistance, 1f));
    }
}
