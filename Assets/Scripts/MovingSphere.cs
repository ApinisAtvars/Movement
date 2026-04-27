using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovingSphere : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset inputActions;
    private InputAction moveAction;

    [SerializeField, Range(0f, 100f)]
    private float maxSpeed = 10f;
    private Vector3 velocity, desiredVelocity;

    [SerializeField, Range(0f, 100f)]
    private float maxAcceleration = 10f;
    private Rigidbody body;

    // Jumping
    [SerializeField, Range(0f, 10f)]
    private float jumpHeight = 2f;
    private bool desiredJump;
    private InputAction jumpAction;
    private int groundContactCount;
    private bool OnGround => groundContactCount > 0;
    [SerializeField, Range(0, 5)]
    private int maxAirJumps = 0;
    private int jumpPhase;
    [SerializeField, Range(0f, 100f)]
    [Tooltip("0 - no control, 100 - total control")]
    private float maxAirAcceleration = 1f;
    
    
    // Slopes
    [SerializeField, Range(0f, 90f)]
    private float maxGroundAngle = 25f;
    private float minGroundDotProduct;
    private Vector3 contactNormal;

    void OnValidate()
    {
        // Mathf expects it in radians
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }


    void Awake()
    {
        moveAction = inputActions["Move"];
        jumpAction = inputActions["Jump"];

        moveAction.Enable();
        jumpAction.Enable();


        body = GetComponent<Rigidbody>();

        OnValidate(); // Invoke it here so it gets calculated in builds
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();        
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision)
    {
        // contactCount property tells how many contact points there are
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal; // Get the direction that the sphere should be pushed

            // onGround |= normal.y >= minGroundDotProduct;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal; // keep track of the normal of the object that the ball is on
            }
        }
    }

    void Update()
    {
        Vector2 playerInput = moveAction.ReadValue<Vector2>();
        // ORed with itself in case the FixedUpdate doesn't occur this frame. That way it's not forgotten
        desiredJump |= jumpAction.triggered; // ReadValue reads only a float?

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
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        
    }

    void FixedUpdate()
    {
        UpdateState();

        AdjustVelocity();

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        body.linearVelocity = velocity;

        ClearState();
    }

    void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

            float alignedSpeed = Vector3.Dot(velocity, contactNormal);

            // Limit the amount of upward speed if we already have some
            if (alignedSpeed > 0f)
            {
                // So that we don't slow down if we're already going faster than the jump speed
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            velocity += contactNormal * jumpSpeed;
        }
    }

    void UpdateState()
    {
        // Physics collisions also affect velocity, so retrieve it before adjusting it to match the desired velocity
        velocity = body.linearVelocity;
        if (OnGround)
        {
            jumpPhase = 0;
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    Vector3 ProjectOnContactPlane (Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        // Project the current velocity on both vectors
        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        // Calculate the new X and Z speeds relative to the ground
        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        // Adjust the velocity by adding the differences between the new and old speeds along the relative axes
        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }
}
