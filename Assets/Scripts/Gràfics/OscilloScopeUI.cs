using UnityEngine;
using UnityEngine.UI;

/// Mostra una gràfica scrollable en una finestra UI.
/// Assigna'l a un RawImage dins d'un Canvas.
public class OscilloscopeUI : MonoBehaviour
{
    public int width = 400;
    public int height = 200;
    public Color backgroundColor = Color.black;
    public Color lineColor = Color.green;
    public float yRange = 1f; // valor màxim esperat (±)

    private Texture2D tex;
    private Color[] clearColors;
    private RawImage rawImage;
    private int xPos;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Point;
        clearColors = new Color[width * height];
        for (int i = 0; i < clearColors.Length; i++) clearColors[i] = backgroundColor;
        tex.SetPixels(clearColors);
        tex.Apply();
        rawImage.texture = tex;
        xPos = 0;
    }

    /// Afig una mostra (valor en -yRange..+yRange)
    public void AddSample(float value)
    {
        // Desplaça a píxel Y
        int y = Mathf.RoundToInt((value / (2f * yRange) + 0.5f) * (height - 1));
        y = Mathf.Clamp(y, 0, height - 1);

        // Dibuixa una columna
        for (int j = 0; j < height; j++)
            tex.SetPixel(xPos, j, backgroundColor);

        tex.SetPixel(xPos, y, lineColor);

        // Avança i reinicia si cal
        xPos++;
        if (xPos >= width) xPos = 0;

        tex.Apply(false);
    }
}
