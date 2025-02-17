using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int MovingHash = Animator.StringToHash("IsWalking");
    private static readonly int RollingHash = Animator.StringToHash("Rolling");

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float runningMovementSpeed = 5f;
    [SerializeField] private float speedTransitionTime = 1f;
    [SerializeField] private float rotationSpeed = 2.0f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private float rollSpeed = 10.0f;
    [SerializeField] private float runningRollSpeed = 8.0f;

    private float _moveInput;
    private float _turnInput;
    private float _verticalVelocity;
    private float _currentSpeed;
    private float _currentRollSpeed;
    
    private float _targetSpeed;
    private bool _isRunning;
    private float _currentTransitionTime;

    private CharacterController _characterController;
    private Animator _animator;

    private bool IsMoving => _moveInput != 0 || _turnInput != 0;
    private bool RequestedRoll => Input.GetKeyDown(KeyCode.Q);
    private bool IsRunning => Input.GetKey(KeyCode.LeftShift);

    private bool _rolling;

    private void Awake() {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Update() {
        if (!_rolling) {
            InputManagement();
            Movement();
        } else {
            HandleRolling();
        }
        
        UpdateAnimationParameters();
    }

    private void HandleRolling() {
        _currentSpeed = 0.0f;
        _targetSpeed = 0.0f;
        float moveStep = _currentRollSpeed * Time.deltaTime;
        _characterController.Move(transform.forward * moveStep);
    }

    private void Movement() {
        GroundMovement();
        Turn();
    }

    private void GroundMovement() {
        if (!IsMoving) {
            SmoothTransitionCurrentSpeed(0.0f);
        }

        if (RequestedRoll && IsGrounded()) {
            StartRolling();
            _animator.SetTrigger(RollingHash);
            return;
        }

        var move = new Vector3(_turnInput, 0f, _moveInput).normalized;
        move = cameraTransform.transform.TransformDirection(move);
        move.y = VerticalForceCalculation();

        if (IsMoving) {
            SmoothTransitionCurrentSpeed(IsRunning ? runningMovementSpeed : movementSpeed);
        }

        move.x *= _currentSpeed;
        move.z *= _currentSpeed;

        _characterController.Move(move * Time.deltaTime);
    }

    private void Turn() {
        if (!IsMoving) return;

        Vector3 currentRotation = _characterController.velocity.normalized;
        currentRotation.y = 0.0f;
        currentRotation.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(currentRotation);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private float VerticalForceCalculation() {
        if (IsGrounded()) {
            _verticalVelocity = -1f;

            if (Input.GetButtonDown("Jump")) {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * gravity * 2);
                _animator.SetTrigger(JumpHash);
            }
        } else {
            _verticalVelocity -= gravity * Time.deltaTime;
        }

        return _verticalVelocity;
    }

    private void SmoothTransitionCurrentSpeed(float newSpeed) {
        if (Mathf.Abs(_targetSpeed - newSpeed) > 0.001f) {
            _currentTransitionTime = 0.0f;
            _targetSpeed = newSpeed;
        }

        if (Mathf.Abs(_currentSpeed - _targetSpeed) > 0.001f) {
            _currentTransitionTime += Time.deltaTime;
            _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, _currentTransitionTime / speedTransitionTime);
        }
    }

    private bool IsGrounded() {
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void InputManagement() {
        _moveInput = Input.GetAxisRaw("Vertical");
        _turnInput = Input.GetAxisRaw("Horizontal");
    }

    private void UpdateAnimationParameters() {
        _animator.SetFloat(MoveSpeedHash, _currentSpeed);
        _animator.SetBool(GroundedHash, IsGrounded());
        _animator.SetBool(MovingHash, IsMoving);
    }

    public void StartRolling() {
        Debug.Log("StartRolling");
        _currentRollSpeed = IsRunning ? runningRollSpeed : rollSpeed;
        _rolling = true;
    }

    public void StopRolling() {
        Debug.Log("StopRolling");
        _rolling = false;
    }

    private void OnDrawGizmos() {
        if (groundCheck == null) return;

        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Gizmos.DrawSphere(groundCheck.position, groundCheckRadius);
    }
}
