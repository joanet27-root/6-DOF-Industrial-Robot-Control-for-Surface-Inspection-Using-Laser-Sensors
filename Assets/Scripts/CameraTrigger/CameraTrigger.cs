using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraTrigger : MonoBehaviour
{
    [Header("Referencias")]
    public TrajectoryPlayer player;
    public Camera tcpCamera;
    public RawImage previewImage;
    public GameObject luzCamara;

    [Header("Captura")]
    public int captureWidth = 640;
    public int captureHeight = 480;

    [Header("Flash")]
    public int numeroParpadeos = 3;
    public float tiempoEncendido = 0.1f;
    public float tiempoApagado = 0.1f;

    bool hasCapturedThisTrigger = false;
    Texture2D lastCapture;

    void Update()
    {
        if (player == null || tcpCamera == null) return;

        // Detectar flanco de subida
        if (player.triggerActive && !hasCapturedThisTrigger)
        {
            StartCoroutine(CaptureAndFlash());
            hasCapturedThisTrigger = true;
        }
        else if (!player.triggerActive)
        {
            hasCapturedThisTrigger = false;
        }
    }

    IEnumerator CaptureAndFlash()
    {
        // Lanzamos el flash EN PARALELO (no bloquea la foto)
        if (luzCamara != null)
            StartCoroutine(FlashParpadeo());

        // Esperamos al final del frame para capturar bien
        yield return new WaitForEndOfFrame();

        // Crear RenderTexture
        RenderTexture rt = new RenderTexture(captureWidth, captureHeight, 24);
        tcpCamera.targetTexture = rt;
        RenderTexture.active = rt;

        if (lastCapture == null ||
            lastCapture.width != captureWidth ||
            lastCapture.height != captureHeight)
        {
            lastCapture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        }

        tcpCamera.Render();
        lastCapture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        lastCapture.Apply();

        // Limpiar
        tcpCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // Mostrar en UI
        // DESPUÉS
        if (previewImage != null)
        {
            // Solo actualizamos la textura.
            // La visibilidad/posición la controlará CameraSwitcher.
            previewImage.texture = lastCapture;
        }

    }

    IEnumerator FlashParpadeo()
    {
        for (int i = 0; i < numeroParpadeos; i++)
        {
            luzCamara.SetActive(true);
            yield return new WaitForSeconds(tiempoEncendido);

            luzCamara.SetActive(false);
            yield return new WaitForSeconds(tiempoApagado);
        }
    }

    public Texture2D GetLastCapture()
    {
        return lastCapture;
    }
}
