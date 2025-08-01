using UnityEngine;
using System.Collections;

public class AutomaticDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openHeight = 3f; // How high the door opens from its initial position
    [SerializeField] private float openSpeed = 2f; // Speed when opening
    [SerializeField] private float closeSpeed = 1f; // Speed when closing
    [SerializeField] private bool useGravityOnClose = false; // Use gravity-like acceleration when closing
    [SerializeField] private float gravityAcceleration = 9.8f; // Gravity acceleration value

    [Header("Animation Curves (Optional)")]
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.Linear(0, 0, 1, 1); // Curve for opening animation
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.Linear(0, 0, 1, 1); // Curve for closing animation
    [SerializeField] private bool useCurves = false; // Enable curve-based animation

    private Vector3 _closedPosition; // Initial position of the door (when closed)
    private Vector3 _openPosition; // Target position when the door is open
    private Coroutine _currentMoveCoroutine; // To manage ongoing door movement

    void Start()
    {
        _closedPosition = transform.position;
        _openPosition = _closedPosition + Vector3.up * openHeight;
    }

    // Call this when the button is pressed
    public void StartOpening()
    {
        if (_currentMoveCoroutine != null)
            StopCoroutine(_currentMoveCoroutine);
        _currentMoveCoroutine = StartCoroutine(MoveDoorTowards(_openPosition, openSpeed, openCurve, true));
    }

    // Call this when the button is released
    public void StartClosing()
    {
        if (_currentMoveCoroutine != null)
            StopCoroutine(_currentMoveCoroutine);

        if (useGravityOnClose)
            _currentMoveCoroutine = StartCoroutine(MoveDoorWithGravity());
        else
            _currentMoveCoroutine = StartCoroutine(MoveDoorTowards(_closedPosition, closeSpeed, closeCurve, false));
    }

    private IEnumerator MoveDoorTowards(Vector3 targetPosition, float speed, AnimationCurve curve, bool opening)
    {
        Vector3 startPosition = transform.position;
        float totalDistance = Vector3.Distance(startPosition, targetPosition);
        float currentDistance = Vector3.Distance(startPosition, transform.position);
        float initialProgress = (totalDistance > 0) ? currentDistance / totalDistance : 0f;

        float startTime = Time.time;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f) // Move until very close to target
        {
            float timeSinceStart = Time.time - startTime;
            float distanceCovered = timeSinceStart * speed;
            float fractionOfJourney = (totalDistance > 0) ? distanceCovered / totalDistance : 1f;

            if (useCurves)
            {
                float curveValue = curve.Evaluate(fractionOfJourney);
                transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            }
            yield return null;
        }
        transform.position = targetPosition; // Ensure it snaps to the exact target
    }

    private IEnumerator MoveDoorWithGravity()
    {
        float currentVelocity = 0f;
        while (transform.position.y > _closedPosition.y)
        {
            currentVelocity += gravityAcceleration * Time.deltaTime;
            Vector3 newPosition = transform.position;
            newPosition.y -= currentVelocity * Time.deltaTime;
            
            if (newPosition.y <= _closedPosition.y)
            {
                newPosition.y = _closedPosition.y;
                transform.position = newPosition;
                break;
            }
            
            transform.position = newPosition;
            yield return null;
        }
        transform.position = _closedPosition;
    }

    void OnDrawGizmos()
    {
        // Gizmos for Automatic Door
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_openPosition, Vector3.one * 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_closedPosition, Vector3.one * 0.2f);
        }
        else
        {
            // In editor, show potential open position relative to current door position
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * openHeight, Vector3.one * 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
    }
}

