using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _orientationTransform;

    [Header("Movement Settings")]
    [SerializeField] private KeyCode _movementKey;
    [SerializeField] private float _movementSpeed;


    [Header("Jump Settings")]
    [SerializeField] private KeyCode _jumpKey;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpCooldown;
    [SerializeField] private float _airMultipler;
    [SerializeField] private float _airDrag;
    [SerializeField] private bool _canJump;

    [Header("Sliding Settings")]
    [SerializeField] private KeyCode _slideKey;
    [SerializeField] private float _slideMultiplier;
    [SerializeField] private float _slideDrag;


    [Header("Ground Check Settings")]
    [SerializeField] private float _playerHeight;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundDrag;

    private StateController _stateController;

    private Rigidbody _playerRigidBody;
    private float _horizontalInput, _verticalInput;
    private Vector3 _movementDirection;
    private bool _isSliding = false;




    void Awake()
    {
        _stateController = GetComponent<StateController>();
        _playerRigidBody = GetComponent<Rigidbody>();
        _playerRigidBody.freezeRotation = true;
    }

    private void Update()
    {
        SetInputs();
        SetStates();
        SetPlayerDrag();
        LimitPlayerSpeed();
    }

    private void FixedUpdate()
    {
        SetPlayerMovement();
    }

    private void SetInputs()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(_slideKey))
        {
            _isSliding = true;
        }
        else if (Input.GetKeyDown(_movementKey))
        {
            _isSliding = false;
        }
        else if (Input.GetKey(_jumpKey) && _canJump && IsGrounded())
        {
            _canJump = false;
            SetPlayerJumping();
            Invoke(nameof(ResetJumping), _jumpCooldown);
        }

    }

    private void SetStates()
    {
        var movementDirection = GetMovementDirection();
        var isGrounded = IsGrounded();
        var isSliding = IsSliding();
        var currentState = _stateController.GetCurrentState();

        var newState = currentState switch
        {
            _ when movementDirection == Vector3.zero && isGrounded && !isSliding => PlayerState.Idle,
            _ when movementDirection != Vector3.zero && isGrounded && !isSliding => PlayerState.Move,
            _ when movementDirection != Vector3.zero && isGrounded && isSliding => PlayerState.Slide,
            _ when movementDirection == Vector3.zero && isGrounded && isSliding => PlayerState.SlideIdle,
            _ when !_canJump && !isGrounded => PlayerState.Jump,
            _ => currentState
        };

        if (newState != currentState)
        {
            _stateController.ChangeState(newState);
        }
    }

    private void SetPlayerMovement()
    {
        _movementDirection = _orientationTransform.forward * _verticalInput + _orientationTransform.right * _horizontalInput;

        float forceMultiplier = _stateController.GetCurrentState() switch
        {
            PlayerState.Move => 1f,
            PlayerState.Slide => _slideMultiplier,
            PlayerState.Jump => _airMultipler,
            _ => 1f
        };

        _playerRigidBody.AddForce(_movementDirection.normalized * _movementSpeed * forceMultiplier, ForceMode.Force);

    }

    private void SetPlayerDrag()
    {
        _playerRigidBody.linearDamping = _stateController.GetCurrentState() switch
        {
            PlayerState.Move => _groundDrag,
            PlayerState.Slide => _slideDrag,
            PlayerState.Jump => _airDrag,
            _ => _playerRigidBody.linearDamping
        };
    }

    private void LimitPlayerSpeed()
    {
        Vector3 flatVelocity = new Vector3(_playerRigidBody.linearVelocity.x, 0f, _playerRigidBody.linearVelocity.z);
        if (flatVelocity.magnitude > _movementSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * _movementSpeed;
            _playerRigidBody.linearVelocity = new Vector3(limitedVelocity.x, _playerRigidBody.linearVelocity.y, limitedVelocity.z);
        }
    }

    private void SetPlayerJumping()
    {
        _playerRigidBody.linearVelocity = new Vector3(_playerRigidBody.linearVelocity.x, 0f, _playerRigidBody.linearVelocity.z);
        _playerRigidBody.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }
    private void ResetJumping()
    {
        _canJump = true;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, _playerHeight * 05f + 0.2f, _groundLayer);
    }

    private Vector3 GetMovementDirection()
    {
        return _movementDirection.normalized;
    }

    private bool IsSliding()
    {
        return _isSliding;
    }

}
