using UnityEngine;

[System.Serializable]
public class HighPassFilter
{
    // y[n] = a*(y[n-1] + x[n] - x[n-1])  on a = tau/(tau+dt), tau = 1/(2*pi*fc)
    public float cutoffHz = 2f; // ajusta-ho a la teva màquina
    private float a, prevX, prevY;
    private bool inited;

    public void Reset() { inited = false; prevX = prevY = 0f; }

    public float Step(float x, float dt)
    {
        float tau = 1f / (2f * Mathf.PI * Mathf.Max(0.001f, cutoffHz));
        a = tau / (tau + Mathf.Max(1e-4f, dt));
        if (!inited) { inited = true; prevX = x; prevY = 0f; return 0f; }
        float y = a * (prevY + x - prevX);
        prevX = x; prevY = y;
        return y;
    }
}
