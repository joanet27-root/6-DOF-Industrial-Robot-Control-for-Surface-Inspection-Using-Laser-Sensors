using UnityEngine;

public class SkidMoverConPasosYPersistentes : MonoBehaviour
{
    [Header("Movimiento principal")]
    [Tooltip("Velocidad de avance en +X (mm/s).")]
    public float speedMmPerSec = 200f;

    [Tooltip("Distancia total a recorrer en +X (mm). Si es 0, no hay límite y no se programan pasos.")]
    public float totalDistanceMm = 0f;

    [Tooltip("Si está activo, el avance en X y los pasos en Y se aplican en espacio LOCAL (ejes del objeto). Si está desactivado, se aplican en mundo.")]
    public bool useLocalSpace = true;

    [Header("Pasos en Y (persistentes)")]
    [Tooltip("Número de pasos (eventos) a lo largo de la distancia total. Cada paso añade (o resta) Y.")]
    [Min(0)] public int movimientos = 0;

    [Tooltip("Magnitud MÁXIMA del paso en Y (mm). Si 'Magnitud aleatoria' está desactivado, será exactamente este valor.")]
    public float stepYmmMax = 300f;

    [Tooltip("Si está activado, cada paso usa una magnitud aleatoria en [0 .. stepYmmMax]. Si no, usa stepYmmMax fijo.")]
    public bool magnitudAleatoria = true;

    [Tooltip("Si está activado, el signo del paso (+/-) es aleatorio. Si no, alterna +, -, +, -.")]
    public bool signoAleatorio = true;

    [Tooltip("Semilla para aleatoriedad (magnitud y/o signo).")]
    public int seed = 1337;

    [Header("Gizmos")]
    [Tooltip("Dibuja los puntos donde ocurren los pasos.")]
    public bool dibujarPuntos = true;

    [Header("Suavizado en Y")]
    [Tooltip("Si está activo, los pasos en Y se alcanzan de forma suave (interpolando).")]
    public bool suavizarY = true;

    [Tooltip("Velocidad máxima de cambio en Y (mm/s) cuando se suaviza.")]
    public float speedYmmPerSec = 200f;


    // --- Estado interno ---
    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    float xTravelMeters;          // recorrido acumulado en X (m)
    float totalDistMeters;        // distancia objetivo (m) o Infinity
    float[] triggersMeters;       // puntos de disparo (m)
    int nextTriggerIdx = 0;

    int altSign = +1;             // para alternar signo cuando no es aleatorio
    float offsetY_m = 0f;          // desplazamiento ACTUAL en Y (m)
    float offsetY_target_m = 0f;   // desplazamiento OBJETIVO en Y (m)


    void Start()
    {
        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;

        totalDistMeters = (totalDistanceMm > 0f) ? totalDistanceMm / 1000f : Mathf.Infinity;

        // Precalcular triggers: Distancia / Movimientos
        if (movimientos > 0 && totalDistMeters < Mathf.Infinity)
        {
            triggersMeters = new float[movimientos];
            float paso = totalDistMeters / movimientos;
            for (int i = 0; i < movimientos; i++)
                triggersMeters[i] = paso * (i + 1);
        }
        else
        {
            triggersMeters = new float[0];
        }

        Random.InitState(seed);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float speedMetersPerSec = speedMmPerSec / 1000f;

        // 1) Avance en X (hasta distancia objetivo, si existe)
        if (xTravelMeters < totalDistMeters)
        {
            xTravelMeters += speedMetersPerSec * dt;
            if (xTravelMeters > totalDistMeters)
                xTravelMeters = totalDistMeters;
        }

        // 2) Disparar paso si toca
        if (nextTriggerIdx < triggersMeters.Length && xTravelMeters >= triggersMeters[nextTriggerIdx])
        {
            AplicarPasoYPersistente();
            nextTriggerIdx++;
        }

        // 2.5) Suavizar el movimiento en Y hacia el objetivo
        if (suavizarY)
        {
            float maxDelta = (speedYmmPerSec / 1000f) * Time.deltaTime; // mm/s -> m/frame
            offsetY_m = Mathf.MoveTowards(offsetY_m, offsetY_target_m, maxDelta);
        }
        else
        {
            // Sin suavizado: saltos instantáneos como antes
            offsetY_m = offsetY_target_m;
        }


        // 3) Aplicar transform
        Vector3 travelX = new Vector3(xTravelMeters, 0f, 0f);
        Vector3 offsetYVec = new Vector3(0f, offsetY_m, 0f);

        if (useLocalSpace)
        {
            transform.localPosition = baseLocalPos + travelX + offsetYVec;
            transform.localRotation = baseLocalRot; // no rotamos
        }
        else
        {
            // Base en mundo
            Vector3 baseWorldPos = transform.parent ? transform.parent.TransformPoint(baseLocalPos) : baseLocalPos;
            Quaternion baseWorldRot = transform.parent ? transform.parent.rotation * baseLocalRot : baseLocalRot;

            transform.position = baseWorldPos + travelX + offsetYVec;
            transform.rotation = baseWorldRot;
        }
    }

    void AplicarPasoYPersistente()
    {
        // Magnitud (mm -> m)
        float magMm = magnitudAleatoria ? Random.Range(0f, Mathf.Abs(stepYmmMax)) : Mathf.Abs(stepYmmMax);
        float mag_m = magMm / 1000f;

        // Signo
        int sign;
        if (signoAleatorio)
        {
            sign = (Random.value < 0.5f) ? -1 : +1;
        }
        else
        {
            sign = altSign;
            altSign = -altSign; // alternar para la próxima
        }

        // Sumar al offset ACUMULADO y persistente en Y
        offsetY_target_m += sign * mag_m;
    }

    void OnValidate()
    {
        speedMmPerSec = Mathf.Max(0f, speedMmPerSec);
        totalDistanceMm = Mathf.Max(0f, totalDistanceMm);
        movimientos = Mathf.Max(0, movimientos);
        stepYmmMax = Mathf.Max(0f, stepYmmMax);
        speedYmmPerSec = Mathf.Max(0f, speedYmmPerSec);

    }

    void OnDrawGizmosSelected()
    {
        if (!dibujarPuntos) return;

        Gizmos.color = Color.cyan;

        // Origen para dibujar
        Vector3 origin;
        if (Application.isPlaying)
            origin = transform.parent ? transform.parent.TransformPoint(baseLocalPos) : baseLocalPos;
        else
            origin = transform.parent ? transform.parent.TransformPoint(transform.localPosition) : transform.localPosition;

        // Línea de avance X
        float distM = (totalDistanceMm > 0f) ? totalDistanceMm / 1000f : 1f;
        Gizmos.DrawLine(origin, origin + Vector3.right * distM);

        // Puntos de triggers
        if (movimientos > 0 && totalDistanceMm > 0f)
        {
            float paso = distM / movimientos;
            for (int i = 1; i <= movimientos; i++)
            {
                Vector3 p = origin + Vector3.right * (paso * i);
                Gizmos.DrawSphere(p, 0.01f);
            }
        }

        // Línea indicando el offset Y acumulado actual (solo en play)
        if (Application.isPlaying && Mathf.Abs(offsetY_m) > 0f)
        {
            Vector3 p0 = origin + Vector3.right * (xTravelMeters);
            Vector3 p1 = p0 + Vector3.up * (offsetY_m);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawSphere(p1, 0.01f);
        }
    }
}
