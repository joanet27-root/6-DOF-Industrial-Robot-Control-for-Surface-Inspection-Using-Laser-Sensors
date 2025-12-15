using UnityEngine;

public class TrajectoryPlayer : MonoBehaviour
{


    [Header("Offset desde PlanePoseTracker")]
    public PlanePoseTracker planeTracker;   // arrastra aquí el objeto del láser
    public bool usePlaneOffset = true;
    public float offsetDelay = 2f;          // segundos de espera antes de usar el offset
    public float offsetScale = 1f;          // normalmente 1.0

    float elapsedSincePlay = 0f;

    [Header("Disparador")]
    [Tooltip("Se pone a true durante triggerDuration cuando se alcanza un punto disparador.")]
    public bool triggerActive = false;

    [Header("Pausa en punto DISPARADOR")]
    public float triggerPauseDuration = 0.5f;   // segundos parado
    bool triggerPauseActive = false;
    float triggerPauseTimer = 0f;


    public float triggerDuration = 0.5f;   // segundos
    float triggerTimer = 0f;

    [Header("Offset de pieza (en metros, eje Y)")]
    public float externalYOffset = 0f;

    [Header("Referencias")]
    public DhRobot robot;
    public TrajectoryRecorder recorder;

    [Header("Parámetros de seguimiento")]
    [Tooltip("Ganancia de posición")]
    public float posGain = 2f;

    [Tooltip("Ganancia de orientación")]
    public float rotGain = 0.2f;

    [Tooltip("Tolerancia de posición para dar el punto como alcanzado (m)")]
    public float positionTolerance = 0.002f;

    [Tooltip("Escala de tiempo (1 = tiempo real)")]
    public float timeScale = 1f;

    [Tooltip("Si es true, cuando llegue al último punto vuelve al primero")]
    public bool loop = true;

    [Tooltip("Empezar automáticamente al darle a Play")]
    public bool playOnStart = true;

    [Tooltip("Ir a HOME antes de empezar la trayectoria")]
    public bool startFromHome = true;

    [Tooltip("Parámetro de amortiguación DLS")]
    public float damping = 1e-4f;

    int currentIndex = 0;
    bool playing = false;

    // Velocidades máximas del efector
    public float maxLinearSpeed = 1f;   // [m/s]
    public float maxAngularSpeed = 2f;  // [rad/s]  (2 rad/s ≈ 114º/s)



    void Start()
    {
        if (playOnStart)
            Play();
    }

    public void Play()
    {
        if (robot == null || recorder == null || recorder.points == null || recorder.points.Count == 0)
        {
            Debug.LogWarning("[TrajectoryPlayer] Falta robot, recorder o puntos.");
            return;
        }

        robot.Init();

        if (startFromHome)
            robot.GoToHome();

        currentIndex = 0;
        elapsedSincePlay = 0f;   
        playing = true;
    }


    public void Stop()
    {
        playing = false;
    }

    void Update()
    {
        if (!playing) return;
        if (robot == null || recorder == null) return;
        if (recorder.points == null || recorder.points.Count == 0) return;

        // === PAUSA EN PUNTO DISPARADOR ===
        if (triggerPauseActive)
        {
            triggerPauseTimer -= Time.deltaTime;

            if (triggerPauseTimer <= 0f)
            {
                triggerPauseActive = false;
            }

            return;   // NO movemos el robot mientras está parado
        }

        // tiempo desde que se dio a Play
        elapsedSincePlay += Time.deltaTime;

        float dt = Time.deltaTime * Mathf.Max(timeScale, 0.0001f);

        // Pose objetivo actual (LOCAL a la base)
        TrajectoryRecorder.PoseSample sample = recorder.points[currentIndex];
        Transform baseTf = robot.transform;

        // ===== OFFSET DESDE PlanePoseTracker =====
        float dynamicOffset = 0f;

        if (usePlaneOffset && planeTracker != null && elapsedSincePlay >= offsetDelay)
        {
            float yWorld = planeTracker.t.y;
            dynamicOffset = yWorld * offsetScale;
        }

        // Posición local con offset aplicado en Y del robot
        Vector3 local = sample.localPosition;
        local.y += dynamicOffset;

        // Pasamos objetivo a MUNDO
        Vector3 targetPosWorld = baseTf.TransformPoint(local);
        Quaternion targetRotWorld = baseTf.rotation * sample.localRotation;

        // ===== LIMITAR VELOCIDAD LINEAL Y ANGULAR =====

        // Pose actual del efector
        Vector3 eePosWorld = robot.GetEndEffectorPosition();
        Quaternion eeRotWorld = robot.GetEndEffectorRotation();

        // --- lineal ---
        Vector3 posError = targetPosWorld - eePosWorld;
        float maxPosStep = maxLinearSpeed * dt;

        if (maxPosStep > 0f && posError.magnitude > maxPosStep)
        {
            // recortamos el objetivo para que no esté demasiado lejos este frame
            targetPosWorld = eePosWorld + posError.normalized * maxPosStep;
        }

        // --- angular ---
        Quaternion delta = targetRotWorld * Quaternion.Inverse(eeRotWorld);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (angleDeg > 180f) angleDeg -= 360f;
        float absAngleDeg = Mathf.Abs(angleDeg);

        // maxAngularSpeed está en rad/s → grados por frame
        float maxAngStepDeg = maxAngularSpeed * Mathf.Rad2Deg * dt;

        if (maxAngStepDeg > 0f && absAngleDeg > maxAngStepDeg && absAngleDeg > 1e-3f)
        {
            float t = maxAngStepDeg / absAngleDeg; // 0..1
                                                   // avanzamos solo un trocito hacia la orientación objetivo
            targetRotWorld = Quaternion.Slerp(eeRotWorld, targetRotWorld, t);
        }

        // Distancia actual efector–punto (con objetivo ya limitado)
        float distEE_Target = Vector3.Distance(eePosWorld, targetPosWorld);

        // Paso de IK completo (posición + orientación)
        robot.StepResolvedRateToPoseWorld(
            targetPosWorld,
            targetRotWorld,
            posGain,
            rotGain,
            dt,
            damping
        );

        // === gestionar duración del disparador ===
        if (triggerActive)
        {
            triggerTimer -= Time.deltaTime;
            if (triggerTimer <= 0f)
                triggerActive = false;
        }

        // ¿Hemos llegado a este punto?
        if (distEE_Target < positionTolerance)
        {
            // Si este punto es disparador, activamos la señal
            if (sample.isTrigger)
            {
                triggerActive = true;
                triggerTimer = triggerDuration;

                // ACTIVAR PAUSA EN FOTO
                triggerPauseActive = true;
                triggerPauseTimer = triggerPauseDuration;
            }


            currentIndex++;

            if (currentIndex >= recorder.points.Count)
            {
                if (loop)
                    currentIndex = 0;
                else
                {
                    playing = false;
                    Debug.Log("[TrajectoryPlayer] Trayectoria finalizada.");
                }
            }
        }
    }
}
