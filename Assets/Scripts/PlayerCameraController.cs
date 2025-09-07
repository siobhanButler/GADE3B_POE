using UnityEngine;
using UnityEngine.InputSystem;  //new input system

public class PlayerCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    [Header("Click")]
    public LayerMask clickableLayers = 1 << 3; // Only Layer 3 (Cliackable layer)
    public float clickDistance = 1000f;
    
    public Camera cam;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    void Update()
    {
        // Try to reacquire camera if missing (e.g., instantiated later)
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }
        }

        // --- WASD movement ---
        float h = 0f;
        float v = 0f;
        if (Keyboard.current != null)   //if a key is being pressed
        {
            if (Keyboard.current.aKey.isPressed) h -= 1f;   //A (horizontal left)
            if (Keyboard.current.dKey.isPressed) h += 1f;   //D (horizontal right)
            if (Keyboard.current.sKey.isPressed) v -= 1f;   //S (vertical backward)
            if (Keyboard.current.wKey.isPressed) v += 1f;   //W (vertical forward)
        }
        Vector3 move = new Vector3(h, 0, v);
        if (move.sqrMagnitude > 1e-3f)
        {
            move = move.normalized;
        }
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);

        // --- Zoom with scroll wheel ---
        float scroll = 0f;
        if (Mouse.current != null)
        {
            // Mouse scroll in new Input System tends to be larger; scale it down
            scroll = Mouse.current.scroll.ReadValue().y * 0.1f;
        }
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            if (cam.orthographic) // orthographic zoom (like Sims/RTS)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
            else // perspective zoom (FOV zoom)
            {
                cam.fieldOfView -= scroll * zoomSpeed;
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
            }
        }

        // --- Click handling ---
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));
            
            // Get all hits along the ray
            RaycastHit[] hits = Physics.RaycastAll(ray, clickDistance, clickableLayers);
            
            // Sort by distance to prioritize closest clickable objects
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            
            // Find the first clickable object
            foreach (RaycastHit hit in hits)
            {
                IClickable clickable = hit.collider.GetComponentInParent<IClickable>();
                if (clickable == null)
                {
                    clickable = hit.collider.GetComponent<IClickable>();
                }
                if (clickable != null)
                {
                    clickable.OnClick();
                    break; // Only click the first (closest) clickable object
                }
            }
        }
    }
}
