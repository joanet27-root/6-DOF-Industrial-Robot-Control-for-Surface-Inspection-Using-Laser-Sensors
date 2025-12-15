using System.IO;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Logger para experimento de altura constante:
/// - y_tcp: altura real del TCP (mundo)
/// - y_ref: consigna = y0 + offset (mundo)
/// - offset: contribución del plano (mundo)
/// - err: y_tcp - y_ref
/// </summary>
public class HeightRefLogger : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del efector final (TCP) del robot")]
    public Transform tcp;

    [Tooltip("Objeto que define el plano físico (capó)")]
    public Transform planeObject;

    [Tooltip("PlanePoseTracker usado para obtener el offset Δy")]
    public PlanePoseTracker planeTracker;

    [Header("Configuración de la consigna")]
    [Tooltip("Altura nominal y0 (m) de la trayectoria sin corrección")]
    public float nominalHeight = 1.6f;  // altura en mundo 

    [Tooltip("Factor de escala aplicado al desplazamiento del plano (debe coincidir con TrajectoryPlayer)")]
    public float offsetScale = 1.0f;

    [Header("Log")]
    public string fileName = "height_log.csv";
    public bool logOnStart = true;
    public float samplePeriod = 0.02f;

    private StreamWriter writer;
    private float nextSampleTime;

    private void Start()
    {
        if (logOnStart)
            BeginLogging();
    }

    public void BeginLogging()
    {
        if (writer != null) return;

        string path = Path.Combine(Application.persistentDataPath, fileName);
        writer = new StreamWriter(path, false);

        // Cabecera
        writer.WriteLine("time,y_tcp,y_ref,offset,err");

        nextSampleTime = Time.time;
        Debug.Log($"[HeightRefLogger] Logging en: {path}");
    }

    public void EndLogging()
    {
        if (writer == null) return;

        writer.Flush();
        writer.Close();
        writer = null;
        Debug.Log("[HeightRefLogger] Logging detenido.");
    }

    private void OnDisable()
    {
        EndLogging();
    }

    private void Update()
    {
        if (writer == null) return;
        if (Time.time < nextSampleTime) return;

        nextSampleTime += samplePeriod;

        if (tcp == null || planeTracker == null)
        {
            Debug.LogWarning("[HeightRefLogger] Faltan referencias (tcp o planeTracker).");
            return;
        }

        float t = Time.time;

        // Altura del TCP en mundo
        float yTcp = tcp.position.y;

        // Offset procedente del plano (usa el mismo origen que usas en TrajectoryPlayer: t.y, tUnexpected.y, etc.)
        float offset = planeTracker.t.y * offsetScale;

        // Consigna: altura nominal + offset
        float yRef = nominalHeight + offset;

        // Error
        float err = yTcp - yRef;

        writer.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0:F4},{1:F5},{2:F5},{3:F5},{4:F5}",
            t, yTcp, yRef, offset, err
        ));
    }
}
