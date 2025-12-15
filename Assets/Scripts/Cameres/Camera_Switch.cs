using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class CameraBinding
    {
        public string name;          // Solo para identificar en el inspector (Main, Front, Back, TCP)
        public Camera camera;
        public KeyCode key;

        [Header("Preview (Image_Play)")]
        [Tooltip("Si es false, el Image_Play se oculta con esta cámara (por ejemplo en TCP).")]
        public bool showPreview = true;

        [Tooltip("Posición del Image_Play (anclado) cuando esta cámara está activa.")]
        public Vector2 previewAnchoredPosition;
    }

    [Header("Cámaras y teclas")]
    public CameraBinding[] cameraBindings; // Asigna aquí cámaras, teclas y posiciones

    [Header("UI")]
    [Tooltip("RectTransform del Image_Play (el RawImage o Image de la foto).")]
    public RectTransform imagePlayRect;   // arrastra aquí el objeto UI de la foto

    private CameraBinding activeBinding;

    void Start()
    {
        if (cameraBindings.Length > 0)
        {
            activeBinding = cameraBindings[0];
            SetActiveCamera(activeBinding);
        }
    }

    void Update()
    {
        foreach (var binding in cameraBindings)
        {
            if (Input.GetKeyDown(binding.key))
            {
                SetActiveCamera(binding);
            }
        }
    }

    void SetActiveCamera(CameraBinding binding)
    {
        // Activar solo la cámara elegida
        foreach (var b in cameraBindings)
        {
            if (b.camera != null)
                b.camera.enabled = (b == binding);
        }

        activeBinding = binding;

        // Actualizar posición y visibilidad del Image_Play
        if (imagePlayRect != null)
        {
            if (binding.showPreview)
            {
                imagePlayRect.gameObject.SetActive(true);
                imagePlayRect.anchoredPosition = binding.previewAnchoredPosition;
            }
            else
            {
                imagePlayRect.gameObject.SetActive(false);
            }
        }
    }
}
