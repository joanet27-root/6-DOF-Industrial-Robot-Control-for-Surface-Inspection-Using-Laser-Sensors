using UnityEngine;

/// Llig 4 LaserRay, fa calibratge i dóna R,t (i roll/pitch/yaw) del pla respecte a l'estat inicial.
/// Separa l'avanç esperat en X de la desviació inesperada.
public class PlanePoseTracker : MonoBehaviour
{
    [Header("Sensors (4)")]
    public LaserRay[] lasers = new LaserRay[4];

    [Header("Opcions")]
    public bool requireAllHits = true;
    public float maxRmsForOk = 0.2f; // mm o unitats de la teua escena
    public Vector3 expectedXAxisWorld = Vector3.right; // eix X "nominal" al món o en el teu bastidor

    [Header("Offset de escena (se resta a t)")]
    public Vector3 sceneOffset = new Vector3(0f, 1.05f, 0f);


    [Header("Lectures (debug)")]
    public Vector3[] P0 = new Vector3[4]; // referència
    public Vector3[] P = new Vector3[4]; // actual
    public bool calibrated;
    public Quaternion R;      // rotació actual rel. a inicial
    public Vector3 t;         // traslació actual rel. a inicial
    public float rmsError;
    public Vector3 eulerRPY;  // roll, pitch, yaw (graus)
    public Vector3 tUnexpected; // traslació inesperada (treta la X “prevista”)

    // Base del pla inicial (eixos)
    private Vector3 ex0, ey0, ez0; // ez0 = normal del pla (inicial)

    // Valor d'"avanç esperat" en X (pots alimentar-ho des d'un encoder)
    public float expectedAdvanceX = 0f;

    void Update()
    {
        // 1) Llegir colps
        int hits = 0;
        for (int i = 0; i < lasers.Length; i++)
        {
            if (lasers[i] != null && lasers[i].HasHit)
            {
                P[i] = lasers[i].HitPoint;
                hits++;
            }
        }
        if ((requireAllHits && hits < lasers.Length) || hits < 3) return;

        // Si no calibrat encara, no podem calcular R,t
        if (!calibrated) return;

        // 2) Resoldre R,t (Horn)
        var (Rq, tVec, rms) = RigidPoseSolver.Solve(P0, P, null, 25);
        R = Rq; t = tVec - sceneOffset; rmsError = rms;

        // 3) Angles roll/pitch/yaw respecte de la base inicial (ex0,ey0,ez0)
        //    Transformem els eixos inicials per R i mesurem l'angle dins d'aquella base.
        Vector3 ex = R * ex0;
        Vector3 ey = R * ey0;
        Vector3 ez = R * ez0; // normal actual del pla

        // Pitch: inclinació cap a ex0 vs normal inicial
        float pitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(ez, ex0), -1f, 1f)) * Mathf.Rad2Deg;
        // Roll: inclinació cap a ey0
        float roll = -Mathf.Asin(Mathf.Clamp(Vector3.Dot(ez, ey0), -1f, 1f)) * Mathf.Rad2Deg;
        // Yaw: gir dins del pla; projectem ex sobre el pla inicial i comparem amb ex0/ey0
        Vector3 exProj = Vector3.ProjectOnPlane(ex, ez0).normalized;
        float yaw = Mathf.Atan2(Vector3.Dot(exProj, ey0), Vector3.Dot(exProj, ex0)) * Mathf.Rad2Deg;

        eulerRPY = new Vector3(roll, pitch, yaw);

        // 4) Separar l'avanç esperat en X de la traslació total
        //    Assumim que el "X nominal" és expectedXAxisWorld (unitari) i que l'avanç previst és expectedAdvanceX.
        Vector3 tNom = expectedXAxisWorld.normalized * expectedAdvanceX;
        Vector3 dT = t - tNom;
        Vector3 dTparallel = Vector3.Dot(dT, expectedXAxisWorld.normalized) * expectedXAxisWorld.normalized;
        tUnexpected = dT - dTparallel; // desviació lateral/vertical

        // (opcional) alarma simple
        if (rmsError > maxRmsForOk)
        {
            // Debug.LogWarning($"RMS alt: {rmsError:F3}");
        }
    }

    [ContextMenu("Calibrate (use current hits)")]
    public void Calibrate()
    {
        int hits = 0;
        for (int i = 0; i < lasers.Length; i++)
        {
            if (lasers[i] != null && lasers[i].HasHit)
            {
                P0[i] = lasers[i].HitPoint;
                hits++;
            }
        }
        if (hits < 3) { Debug.LogError("Calibratge: calen ≥3 punts"); return; }

        // Base del pla inicial: ex0 dins del pla, ez0 normal, ey0 perpendicular
        // Triem ex0 com (P0[1]-P0[0]) i ez0 com normal del pla (mitjana de dos triangles).
        Vector3 v01 = (P0[1] - P0[0]);
        Vector3 v02 = (P0[2] - P0[0]);
        Vector3 v13 = (P0[3] - P0[1]);
        Vector3 nA = Vector3.Cross(v01, v02);
        Vector3 nB = Vector3.Cross(v02, v13);
        ez0 = (nA + nB).normalized;
        if (ez0.sqrMagnitude < 1e-12f) ez0 = Vector3.Cross(v01, v02).normalized;

        ex0 = Vector3.ProjectOnPlane(v01, ez0).normalized;
        ey0 = Vector3.Cross(ez0, ex0).normalized;

        calibrated = true;
        R = Quaternion.identity;
        t = Vector3.zero;
        eulerRPY = Vector3.zero;
        expectedAdvanceX = 0f;
        // Debug.Log("Calibratge fet.");
    }

    ///  cridar 
    public void SetExpectedAdvanceX(float value) => expectedAdvanceX = value;
}
