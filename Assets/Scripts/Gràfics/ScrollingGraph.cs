using UnityEngine;

/// Dibuixa N sèries com a gràfic desplaçant-se en el temps (scroll).
/// Posa aquest component en un GameObject buit i crea tants fills amb LineRenderer
/// com sèries tingues (colors i amplada els configures al LineRenderer).
public class ScrollingGraph : MonoBehaviour
{
    [Range(64, 4096)] public int maxSamples = 600;
    public float secondsVisible = 10f;   // ample temporal visible
    public float yRange = 0.5f;          // ± rang vertical (unitats escena)
    public Vector2 origin = new Vector2(0f, 0f); // offset del gràfic en món
    public float xScale = 1f;            // escalar horitzontal (auto si = 0)

    private LineRenderer[] lines;
    private float[,] buffer; // [serie, index]
    private int writeIdx;
    private float tAccum;

    void Awake()
    {
        lines = GetComponentsInChildren<LineRenderer>();
        int S = lines.Length;
        buffer = new float[S, maxSamples];
        for (int s = 0; s < S; s++)
        {
            lines[s].positionCount = maxSamples;
        }
    }

    public void AddSample(int series, float value)
    {
        if (series < 0 || series >= lines.Length) return;
        buffer[series, writeIdx] = Mathf.Clamp(value, -yRange, yRange);
    }

    void LateUpdate()
    {
        tAccum += Time.deltaTime;
        // avança l’escriptura una posició per frame (scroll uniforme)
        writeIdx = (writeIdx + 1) % maxSamples;

        // redibuixa
        float width = (secondsVisible > 0f) ? secondsVisible : maxSamples * Time.deltaTime;
        float dx = (xScale > 0f) ? xScale : (width / maxSamples);

        for (int s = 0; s < lines.Length; s++)
        {
            int n = maxSamples;
            for (int i = 0; i < n; i++)
            {
                int idx = (writeIdx + i) % n; // “finestra” circular
                float x = origin.x + i * dx;
                float y = origin.y + buffer[s, idx];
                lines[s].SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }
}
