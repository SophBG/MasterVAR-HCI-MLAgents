using UnityEngine;
using Unity.MLAgents;
using UnityEngine.InputSystem;
using Unity.MLAgents.Actuators;

public class MLPlayer : Agent
{
    [Header("Movement")]
    public float jumpForce;
    public float jumpCooldown;
    private bool readyToJump;
    private bool jumpRequested;
    public Transform origin;
    private Rigidbody rb = null;

    [Header("Input Actions")]
    public InputActionAsset inputActions;
    private InputAction jumpAction;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask groundLayer;
    private bool grounded;

    [Header("Obstacle")]
    public string obstacleTag;

    private void OnDestroy()
    {
        // Clean up jump action subscription
        jumpAction.performed -= OnJump;
    }
    
    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, groundLayer);
    }

    public override void Initialize()
    {
        rb = this.GetComponent<Rigidbody>();

        jumpAction = InputSystem.actions.FindAction("Jump");
        readyToJump = true;
        jumpRequested = false;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (actions.DiscreteActions[0] == 1 && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    public override void OnEpisodeBegin()
    {
        ResetPlayer();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;

        // Set up jump action callback
        jumpAction.performed += OnJump;
        if (jumpRequested)
        {
            discreteActions[0] = 1;
            jumpRequested = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(obstacleTag))
        {
            AddReward(-1.0f);
            Destroy(collision.gameObject);
            EndEpisode();
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpRequested = true;
    }

    private void Jump()
    {
        // reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        AddReward(-0.01f);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void ResetPlayer()
    {
        this.transform.position = new Vector3(origin.position.x, origin.position.y, origin.position.z);
    }

    public void AddReward()
    {
        AddReward(0.1f);
    }
}
