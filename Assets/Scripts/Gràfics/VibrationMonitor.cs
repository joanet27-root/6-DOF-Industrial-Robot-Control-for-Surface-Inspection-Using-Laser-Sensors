using UnityEngine;

public class VibrationMonitor : MonoBehaviour
{
    public PlanePoseTracker tracker;
    public LaserRay[] lasers = new LaserRay[4];
    public OscilloscopeUI scope;   // RawImage amb OscilloscopeUI

    [Header("Filtres/escales")]
    public bool useNormalComponent = true;
    public float mmScale = 1000f;     // Unity -> mm
    public float cutoffHz = 2f;       // passa-alt

    private HighPassFilter[] hp;
    private Vector3 n0;               // normal inicial del pla
    private bool baseReady;

    void Start()
    {
        // filtres
        hp = new HighPassFilter[lasers.Length];
        for (int i = 0; i < hp.Length; i++)
        {
            hp[i] = new HighPassFilter { cutoffHz = cutoffHz };
            hp[i].Reset();
        }
    }

    void Update()
    {
        if (!scope || !tracker) return;

        // Auto-calibra quan hi haja 3 hits
        if (!tracker.calibrated)
        {
            int hits = 0;
            for (int i = 0; i < lasers.Length; i++)
                if (lasers[i] && lasers[i].HasHit) hits++;
            if (hits >= 3) tracker.Calibrate();
            else return;
        }

        // Normal inicial (una vegada)
        if (!baseReady)
        {
            var v01 = tracker.P0[1] - tracker.P0[0];
            var v02 = tracker.P0[2] - tracker.P0[0];
            n0 = Vector3.Cross(v01, v02).normalized;
            if (n0.sqrMagnitude < 1e-8f) return;
            baseReady = true;
        }

        // Residuals
        int valid = 0;
        float sum = 0f;
        for (int i = 0; i < lasers.Length; i++)
        {
            if (!(lasers[i] && lasers[i].HasHit)) continue;

            Vector3 Pm = lasers[i].HitPoint;                    // mesurat
            Vector3 Pref = tracker.R * tracker.P0[i] + tracker.t; // previst
            Vector3 r = Pm - Pref;

            float sample = useNormalComponent ? Vector3.Dot(r, n0) : r.magnitude;
            float filtered = hp[i].Step(sample, Time.deltaTime);

            sum += filtered;
            valid++;
        }
        if (valid == 0) return;

        // Mostra la mitjana (vibració global del pla) en mm
        scope.AddSample((sum / valid) * mmScale);
    }
}
