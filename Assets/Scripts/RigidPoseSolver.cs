using UnityEngine;
using System;

/// Resol la posició i orientació rígida (mínims quadrats) que mapeja P0 -> P.
/// Implementació basada en Horn (quaternion) amb power-iteration sobre la matriu N (4x4).
public static class RigidPoseSolver
{
    /// <summary>
    /// Resol R (Quaternion) i t (Vector3) tal que P[i] ≈ R * P0[i] + t
    /// </summary>
    /// <param name="P0">Punts de referència</param>
    /// <param name="P">Punts actuals</param>
    /// <param name="weights">opc: pesos per punt (llargada 3..N). Si null, tots 1.</param>
    public static (Quaternion R, Vector3 t, float rms) Solve(Vector3[] P0, Vector3[] P, float[] weights = null, int powerIters = 20)
    {
        if (P0 == null || P == null || P0.Length != P.Length || P0.Length < 3)
            throw new ArgumentException("Calen ≥3 correspons i mateixes longituds");

        int n = P0.Length;
        // Pesos
        float[] w = weights ?? new float[n];
        if (weights == null) for (int i = 0; i < n; i++) w[i] = 1f;

        // Centroides ponderats
        float W = 0f;
        Vector3 c0 = Vector3.zero, c = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            float wi = Mathf.Max(0f, w[i]);
            W += wi;
            c0 += wi * P0[i];
            c += wi * P[i];
        }
        if (W <= 0f) throw new ArgumentException("Suma de pesos zero");
        c0 /= W; c /= W;

        // Punts centrats
        // Matriu de correlació S = sum w * (Q0 * Q^T)
        float Sxx = 0, Sxy = 0, Sxz = 0, Syx = 0, Syy = 0, Syz = 0, Szx = 0, Szy = 0, Szz = 0;
        for (int i = 0; i < n; i++)
        {
            float wi = w[i];
            Vector3 a = P0[i] - c0;
            Vector3 b = P[i] - c;

            Sxx += wi * a.x * b.x; Sxy += wi * a.x * b.y; Sxz += wi * a.x * b.z;
            Syx += wi * a.y * b.x; Syy += wi * a.y * b.y; Syz += wi * a.y * b.z;
            Szx += wi * a.z * b.x; Szy += wi * a.z * b.y; Szz += wi * a.z * b.z;
        }

        // Matriu de Davenport N (4x4) per trobar el quaternion màxim propi
        // N = [ trace(S)      Syz - Szy   Szx - Sxz   Sxy - Syx
        //       Syz - Szy     Sxx - Syy - Szz   Sxy + Syx   Szx + Sxz
        //       Szx - Sxz     Sxy + Syx   -Sxx + Syy - Szz  Szy + Syz
        //       Sxy - Syx     Szx + Sxz   Szy + Syz   -Sxx - Syy + Szz ]
        float tr = Sxx + Syy + Szz;
        float[,] N = new float[4, 4];
        N[0, 0] = tr;
        N[0, 1] = Syz - Szy;
        N[0, 2] = Szx - Sxz;
        N[0, 3] = Sxy - Syx;

        N[1, 0] = N[0, 1];
        N[1, 1] = Sxx - Syy - Szz;
        N[1, 2] = Sxy + Syx;
        N[1, 3] = Szx + Sxz;

        N[2, 0] = N[0, 2];
        N[2, 1] = N[1, 2];
        N[2, 2] = -Sxx + Syy - Szz;
        N[2, 3] = Szy + Syz;

        N[3, 0] = N[0, 3];
        N[3, 1] = N[1, 3];
        N[3, 2] = N[2, 3];
        N[3, 3] = -Sxx - Syy + Szz;

        // Power iteration per al màxim autovector (quaternion)
        // Vector inicial (1,0,0,0)
        double q0 = 1, q1 = 0, q2 = 0, q3 = 0;
        for (int it = 0; it < powerIters; it++)
        {
            double r0 = N[0, 0] * q0 + N[0, 1] * q1 + N[0, 2] * q2 + N[0, 3] * q3;
            double r1 = N[1, 0] * q0 + N[1, 1] * q1 + N[1, 2] * q2 + N[1, 3] * q3;
            double r2 = N[2, 0] * q0 + N[2, 1] * q1 + N[2, 2] * q2 + N[2, 3] * q3;
            double r3 = N[3, 0] * q0 + N[3, 1] * q1 + N[3, 2] * q2 + N[3, 3] * q3;
            double norm = System.Math.Sqrt(r0 * r0 + r1 * r1 + r2 * r2 + r3 * r3) + 1e-12;
            q0 = r0 / norm; q1 = r1 / norm; q2 = r2 / norm; q3 = r3 / norm;
        }
        var Rq = new Quaternion((float)q1, (float)q2, (float)q3, (float)q0).normalized;

        // Traslació
        Vector3 t = c - (Rq * c0);

        // RMS d'error (opcional, útil per a alarms de planaritat/lectures dolentes)
        float err2 = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector3 pr = Rq * P0[i] + t;
            err2 += w[i] * (pr - P[i]).sqrMagnitude;
        }
        float rms = Mathf.Sqrt(err2 / Mathf.Max(W, 1e-6f));

        return (Rq, t, rms);
    }
}
