using UnityEngine;
using UnityEngine.Events;

public class PressureButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask detectableLayers; // Layers that can activate the button
    [SerializeField] private float detectionRadius = 0.1f; // Radius for overlap circle detection
    [SerializeField] private bool multipleObjectsCanPress = false; // Can multiple objects press the button?

    [Header("Events")]
    public UnityEvent OnButtonPressed; // Event fired when button is pressed
    public UnityEvent OnButtonReleased; // Event fired when button is released

    private int _pressCount = 0; // Number of detectable objects currently pressing the button
    private bool _isCurrentlyPressed = false; // Internal state to track if the button is considered pressed

    void Update()
    {
        CheckForPress();
    }

    private void CheckForPress()
    {
        // Get all colliders within the detection radius on the specified layers
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectableLayers);

        int currentDetectingObjects = 0;
        foreach (Collider2D hit in hitColliders)
        {
            // Ensure the detected object is not the button itself and has a non-trigger collider
            if (hit.gameObject != gameObject && !hit.isTrigger)
            {
                currentDetectingObjects++;
            }
        }

        // Logic for button press/release based on currentDetectingObjects
        if (currentDetectingObjects > 0 && !_isCurrentlyPressed)
        {
            // Button was just pressed (first object entered)
            _isCurrentlyPressed = true;
            _pressCount = currentDetectingObjects;
            OnButtonPressed.Invoke();
        }
        else if (currentDetectingObjects == 0 && _isCurrentlyPressed)
        {
            // Button was just released (last object left)
            _isCurrentlyPressed = false;
            _pressCount = 0;
            OnButtonReleased.Invoke();
        }
        else if (multipleObjectsCanPress && currentDetectingObjects != _pressCount)
        {
            // If multiple objects are allowed, update count if it changes while pressed
            _pressCount = currentDetectingObjects;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}


