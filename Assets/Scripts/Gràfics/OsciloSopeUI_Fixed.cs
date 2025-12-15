using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class OscilloscopeUI_Fixed : MonoBehaviour
{
    public int width = 400;
    public int height = 200;
    public Color backgroundColor = Color.black;
    public Color lineColor = Color.green;
    public float yRange = 1f; // ±rang visible

    Texture2D tex;
    RawImage ri;
    int xPos;

    void Awake()
    {
        ri = GetComponent<RawImage>();
        CreateTexture();
    }

    void OnEnable()
    {
        if (ri == null) ri = GetComponent<RawImage>();
        if (tex == null) CreateTexture();
    }

    void CreateTexture()
    {
        tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Point;

        var clear = new Color[width * height];
        for (int i = 0; i < clear.Length; i++) clear[i] = backgroundColor;
        tex.SetPixels(clear);
        tex.Apply();

        ri.texture = tex;
        xPos = 0;
        Debug.Log("[Scope] Texture creada i assignada.");
    }

    int prevY = -1; // afegeix-ho com a camp de classe

    public void AddSample(float value)
    {
        if (tex == null) return;

        int y = Mathf.RoundToInt((value / (2f * yRange) + 0.5f) * (height - 1));
        y = Mathf.Clamp(y, 0, height - 1);

        // neteja columna
        for (int j = 0; j < height; j++) tex.SetPixel(xPos, j, backgroundColor);

        // dibuixa punt o segment vertical entre prevY i y
        if (prevY >= 0)
        {
            int y0 = Mathf.Min(prevY, y);
            int y1 = Mathf.Max(prevY, y);
            for (int j = y0; j <= y1; j++) tex.SetPixel(xPos, j, lineColor);
        }
        else
        {
            tex.SetPixel(xPos, y, lineColor);
        }

        prevY = y;
        xPos = (xPos + 1) % width;
        if (xPos == 0) prevY = -1; // reinicia quan “torna” al principi
        tex.Apply(false);
    }

}
