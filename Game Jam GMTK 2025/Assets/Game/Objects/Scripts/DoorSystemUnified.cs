using UnityEngine;

public class DoorSystem : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PressureButton pressureButton; // Reference to the PressureButton script
    [SerializeField] private AutomaticDoor automaticDoor; // Reference to the AutomaticDoor script

    void Start()
    {
        // Ensure both components are assigned
        if (pressureButton == null)
        {
            Debug.LogError("DoorSystem: PressureButton reference is not set! Please assign it in the Inspector.");
            enabled = false; // Disable this script if essential components are missing
            return;
        }
        if (automaticDoor == null)
        {
            Debug.LogError("DoorSystem: AutomaticDoor reference is not set! Please assign it in the Inspector.");
            enabled = false;
            return;
        }

        // Subscribe to the button's events
        pressureButton.OnButtonPressed.AddListener(automaticDoor.StartOpening);
        pressureButton.OnButtonReleased.AddListener(automaticDoor.StartClosing);
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when the object is destroyed
        if (pressureButton != null)
        {
            pressureButton.OnButtonPressed.RemoveListener(automaticDoor.StartOpening);
            pressureButton.OnButtonReleased.RemoveListener(automaticDoor.StartClosing);
        }
    }
}