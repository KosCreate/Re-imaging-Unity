using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int MovingHash = Animator.StringToHash("IsWalking");
    private static readonly int RollingHash = Animator.StringToHash("Rolling");
    private static readonly int LandingHash = Animator.StringToHash("Landing");

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float runningMovementSpeed = 5f;
    [SerializeField] private float speedTransitionTime = 1f;
    [SerializeField] private float rotationSpeed = 2.0f;
    [SerializeField] private Transform cameraTransform;
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float landingDistance = 1.5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [Header("Rolling Settings")]
    [SerializeField] private float rollSpeed = 10.0f;
    [SerializeField] private float runningRollSpeed = 8.0f;
    [SerializeField] private float allowableRollingBufferTime = 1.0f;

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
    private static bool RequestedRoll => Input.GetKeyDown(KeyCode.Q);
    private static bool IsRunning => Input.GetKey(KeyCode.LeftShift);
    private static bool RequestedJump => Input.GetButtonDown("Jump");
    
    private bool _rolling;
    private Vector3 _rollDirection;

    private float _rollTime;
    private bool _bufferedRoll;

    private bool _reachedJumpTarget;
    private float _targetY;
    private bool _jump;
    
    private void Awake() {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Update() {
        if (!_rolling) {
            InputManagement();
            Movement();
        } 
        else {
            HandleRolling();
        }

        UpdateAnimationParameters();
    }

    private void HandleRolling() {
        _currentSpeed = 0.0f;
        _targetSpeed = 0.0f;
        float moveStep = _currentRollSpeed * Time.deltaTime;

        Vector3 targetPosition = _rollDirection * moveStep;
        _characterController.Move(targetPosition);

        if (_rollDirection != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(_rollDirection);
            targetRotation.x = 0.0f;
            targetRotation.z = 0.0f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        _rollTime += Time.deltaTime;

        if (RequestedRoll && !_bufferedRoll && _rollTime >= allowableRollingBufferTime) {
            Debug.Log("-- BUFFERED ROLL --");
            _bufferedRoll = true;
        }
    }

    private void Movement() {
        GroundMovement();
        UpdateTurn();
    }

    private void GroundMovement() {
        if (!IsMoving) {
            SmoothTransitionCurrentSpeed(0.0f);
        }

        if (RequestedRoll && IsGrounded()) {
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

    private void UpdateTurn() {
        if (!IsMoving) return;
        Vector3 currentRotation = _characterController.velocity;
        currentRotation.y = 0.0f;
        currentRotation.Normalize();

        if (currentRotation != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(currentRotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private float VerticalForceCalculation() {
        if (IsGrounded()) {
            _verticalVelocity = -1f;
            _animator.ResetTrigger(LandingHash);

            if (_jump) {
                _jump = false;   
                _reachedJumpTarget = false;
            }
            
            if (RequestedJump) {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * gravity * 2);
                _targetY = transform.position.y + jumpHeight;
                _animator.SetTrigger(JumpHash);
                _jump = true;
            }
        } 
        else {
            if (_jump) {
                if (Mathf.Abs(transform.position.y - _targetY) < 1.5f) {
                    _reachedJumpTarget = true;
                    Debug.Log("REACHED JUMP TARGET");
                }

                if (_reachedJumpTarget) {
                    if (ShouldLand()) {
                        Debug.Log("SHOULD LAND");
                        _animator.SetTrigger(LandingHash);
                    }
                }
            }
            else {
                if (ShouldLand()) {
                    Debug.Log("SHOULD LAND");
                    _animator.SetTrigger(LandingHash);
                }
            }
            
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
        bool sphereCast = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        return sphereCast;
    }

    // private bool IsGrounded() {
    //     return Physics.Raycast(groundCheck.position + transform.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
    // }

    private bool ShouldLand() {
        return Physics.Raycast(groundCheck.position + transform.up * 0.5f, Vector3.down, landingDistance, groundLayer);
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
        _currentRollSpeed = IsRunning ? runningRollSpeed : rollSpeed;
        _rollDirection = cameraTransform.forward.normalized;
        _rollTime = 0.0f;
        _rolling = true;
    }

    public void StopRolling() {
        _rolling = false;

        if (!_bufferedRoll) return;
        _animator.SetTrigger(RollingHash);
        _bufferedRoll = false;
    }

    private void OnDrawGizmos() {
        if (groundCheck == null) return;

        Gizmos.color = IsGrounded() ? Color.cyan : Color.red;
        Gizmos.DrawSphere(groundCheck.position, groundCheckRadius);
        //Gizmos.DrawRay(groundCheck.position + transform.up * 0.1f, Vector3.down * groundCheckDistance);
        
        Gizmos.color = ShouldLand() ? Color.green : Color.red;
        Gizmos.DrawRay(groundCheck.position + transform.up * 0.5f, Vector3.down * landingDistance);
    }
}