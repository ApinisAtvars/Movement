using UnityEngine;
using UnityEngine.InputSystem;

public class MovingSphere : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset inputActions;
    private InputAction moveAction;

    [SerializeField, Range(0f, 100f)]
    private float maxSpeed = 10f;
    private Vector3 velocity;

    [SerializeField, Range(0f, 100f)]
    private float maxAcceleration = 10f;

    [SerializeField]
    // Instead of binding the player to the plane directly, we define their roam area through this property
    private Rect allowedArea = new Rect(-4.5f, -4.5f, 9f, 9f);

    [SerializeField, Range(0f, 1f)]
    [Tooltip("What part of the velocity should be reversed when colliding with wall")]
	float bounciness = 0.5f;

    void Awake()
    {
        moveAction = inputActions["Move"];
        moveAction.Enable();
    }

    void OnEnable()
    {
        moveAction.Enable();        
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    void Update()
    {
        Vector2 playerInput = moveAction.ReadValue<Vector2>();

        /* The maximum speed when moving with a keyboard is sqrt(2),
        so we normalize it so that it doesn't exceed 1.
        */
        // playerInput.Normalize(); // This makes the input either be 1 or 0
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        // transform.localPosition = new Vector3(playerInput.x, 0.5f, playerInput.y); // This just teleports the player

        /* This works but is more like moving on ice because we're not adjusting the velocity directly
        Vector3 acceleration = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        velocity += acceleration * Time.deltaTime;
        */

        // Determine which direction the player wants to move in
        Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        // Define by how much we're able to change the velocity this update
        float maxSpeedChange = maxAcceleration * Time.deltaTime;

        // Adjust the velocity by the maxSpeedChange amount and ensure the change does not exceed the maxSpeedChange
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        Vector3 displacement = velocity * Time.deltaTime;
        
        // transform.localPosition += displacement; // This works but player can go anywhere

        Vector3 newPosition = transform.localPosition + displacement;
        /* Whenever a ball "collides" with an edge, the velocity doesn't change, it's just bound
        No bueno
        if (!allowedArea.Contains(new Vector2(newPosition.x, newPosition.z))) // Check whether the newPosition is not within the constraints
        {
            // newPosition = transform.localPosition; // This ignores all movement even if one of the directions is within bounds
            newPosition.x = Mathf.Clamp(newPosition.x, allowedArea.xMin, allowedArea.xMax);
            newPosition.z = Mathf.Clamp(newPosition.z, allowedArea.yMin, allowedArea.yMax);
        }
        */

        // Actually stop the ball from moving in the according direciton when encountering a "wall"
        if (newPosition.x < allowedArea.xMin) {
			newPosition.x = allowedArea.xMin;
			// velocity.x = 0f; // This works, but let's make it bounce
            velocity.x = -velocity.x * bounciness;
		}
		else if (newPosition.x > allowedArea.xMax) {
			newPosition.x = allowedArea.xMax;
			// velocity.x = 0f; // This works, but let's make it bounce
            velocity.x = -velocity.x * bounciness;
		}
		if (newPosition.z < allowedArea.yMin) {
			newPosition.z = allowedArea.yMin;
			// velocity.z = 0f; // This works, but let's make it bounce
            velocity.z = -velocity.z * bounciness;
		}
		else if (newPosition.z > allowedArea.yMax) {
			newPosition.z = allowedArea.yMax;
			// velocity.z = 0f; // This works, but let's make it bounce
            velocity.z = -velocity.z * bounciness;
		}

        transform.localPosition = newPosition;
        
    }
}
