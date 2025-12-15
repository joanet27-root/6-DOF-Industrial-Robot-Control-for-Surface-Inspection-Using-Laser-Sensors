using UnityEngine;

/// HUD para mostrar en pantalla la pose calculada por PlanePoseTracker.
/// Muestra X Y Z Rx Ry Rz como offsets respecto a la calibración.
/// Opcionalmente muestra también la desviación sin la X prevista.
public class PlanePoseHUD : MonoBehaviour
{
    [Header("Fuente de datos")]
    public PlanePoseTracker tracker;

    [Header("Formato / unidades")]
    [Tooltip("Escala para pasar de unidades de escena a unidades de visualización. Ej.: si trabajas en metros y quieres mm, usa 1000.")]
    public float unitsScale = 1000f; // m -> mm
    public string unitsLabel = "mm";
    [Tooltip("Decimales para X/Y/Z (en unidades de visualización).")]
    public int decimalsLinear = 2;
    [Tooltip("Decimales para Rx/Ry/Rz (grados).")]
    public int decimalsAngular = 2;

    [Header("Zero snap (para que salgan 0 en reposo)")]
    [Tooltip("Valores |X|,|Y|,|Z| menores que este umbral (en unidades de visualización) se muestran como 0.")]
    public float zeroSnapLinearUnits = 0.05f; // 0.05 mm
    [Tooltip("Valores |Rx|,|Ry|,|Rz| menores que este umbral (grados) se muestran como 0.")]
    public float zeroSnapAngularDeg = 0.01f;

    [Header("Presentación")]
    public bool showUnexpectedDelta = false;   // Muestra también la línea A (Delta) sin X prevista
    public Vector2 anchor = new Vector2(12f, 12f);
    public int fontSize = 16;
    public float lineHeight = 22f;
    public Color textColor = Color.white;
    public Color backColor = new Color(0f, 0f, 0f, 0.55f);
    public float padding = 10f;

    GUIStyle style;
    Texture2D backTex;

    void Awake()
    {
        style = new GUIStyle
        {
            fontSize = fontSize,
            normal = new GUIStyleState { textColor = textColor }
        };

        backTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        backTex.SetPixel(0, 0, backColor);
        backTex.Apply();
    }



    void OnDisable()
    {
        if (backTex != null)
        {
            Destroy(backTex);
            backTex = null;
        }
    }

    void OnGUI()
    {
        // Init segura (evita errores si no pasó por Awake)
        if (style == null)
        {
            style = new GUIStyle
            {
                fontSize = fontSize,
                normal = new GUIStyleState { textColor = textColor }
            };
        }
        if (backTex == null)
        {
            backTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            backTex.SetPixel(0, 0, backColor);
            backTex.Apply();
        }

        // Panel base
        string header = "Posición Calculada";
        string body;

        if (tracker == null)
        {
            body = "Tracker no asignado.";
        }
        else if (!tracker.calibrated)
        {
            body = "Esperando calibración… (ejecuta Calibrate con ≥3 láseres)";
        }
        else
        {
            // Datos del tracker
            Vector3 t = tracker.t;                  // traslación total (lo que miden los láseres)
            Vector3 rpy = tracker.eulerRPY;         // grados
            Vector3 tUnits = t * unitsScale;

            // Snap a cero
            tUnits = SnapLinearUnits(tUnits, zeroSnapLinearUnits);
            rpy = SnapAngularDeg(rpy, zeroSnapAngularDeg);

            body =
                $"X: {tUnits.x.ToString($"F{decimalsLinear}")} {unitsLabel}   " +
                $"Y: {tUnits.y.ToString($"F{decimalsLinear}")} {unitsLabel}   " +
                $"Z: {tUnits.z.ToString($"F{decimalsLinear}")} {unitsLabel}   " +
                $"Rx: {rpy.x.ToString($"F{decimalsAngular}")}°   " +
                $"Ry: {rpy.y.ToString($"F{decimalsAngular}")}°   " +
                $"Rz: {rpy.z.ToString($"F{decimalsAngular}")}°";
        }

        // Layout simple
        int lines = 2; // header + body
        if (showUnexpectedDelta && tracker != null && tracker.calibrated) lines = 3;

        float height = padding * 2 + lineHeight * lines;
        float width = 820f;
        Rect rect = new Rect(anchor.x, anchor.y, width, height);

        GUI.DrawTexture(rect, backTex);

        float y = anchor.y + padding;
        GUI.Label(new Rect(anchor.x + padding, y, width - 2 * padding, lineHeight), header, style); y += lineHeight;
        GUI.Label(new Rect(anchor.x + padding, y, width - 2 * padding, lineHeight), body, style); y += lineHeight;

        // Línea extra (opcional) con Δ sin X prevista
        if (showUnexpectedDelta && tracker != null && tracker.calibrated)
        {
            Vector3 tU = tracker.tUnexpected * unitsScale;
            tU = SnapLinearUnits(tU, zeroSnapLinearUnits);
            Vector3 rpyU = SnapAngularDeg(tracker.eulerRPY, zeroSnapAngularDeg); // rotación igual

            string lineDelta =
                $"Δ (sin X prevista)  " +
                $"X: {tU.x.ToString($"F{decimalsLinear}")} {unitsLabel}   " +
                $"Y: {tU.y.ToString($"F{decimalsLinear}")} {unitsLabel}   " +
                $"Z: {tU.z.ToString($"F{decimalsLinear}")} {unitsLabel}   " +
                $"Rx: {rpyU.x.ToString($"F{decimalsAngular}")}°   " +
                $"Ry: {rpyU.y.ToString($"F{decimalsAngular}")}°   " +
                $"Rz: {rpyU.z.ToString($"F{decimalsAngular}")}°";

            GUI.Label(new Rect(anchor.x + padding, y, width - 2 * padding, lineHeight), lineDelta, style);
        }
    }


    Vector3 SnapLinearUnits(Vector3 v, float epsUnits)
    {
        if (Mathf.Abs(v.x) < epsUnits) v.x = 0f;
        if (Mathf.Abs(v.y) < epsUnits) v.y = 0f;
        if (Mathf.Abs(v.z) < epsUnits) v.z = 0f;
        return v;
    }

    Vector3 SnapAngularDeg(Vector3 v, float epsDeg)
    {
        if (Mathf.Abs(v.x) < epsDeg) v.x = 0f;
        if (Mathf.Abs(v.y) < epsDeg) v.y = 0f;
        if (Mathf.Abs(v.z) < epsDeg) v.z = 0f;
        return v;
    }
}
