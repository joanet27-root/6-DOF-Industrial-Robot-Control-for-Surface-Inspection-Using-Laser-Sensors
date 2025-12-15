using UnityEngine;

public class ScopeTester : MonoBehaviour
{
    public OscilloscopeUI_Fixed scope;  // ARROSSEGA el OscilloDisplay ací
    public float freqHz = 2f;
    public float amp = 0.5f;

    void Update()
    {
        if (!scope) return;
        float s = amp * Mathf.Sin(2f * Mathf.PI * freqHz * Time.time);
        scope.AddSample(s);
    }
}
