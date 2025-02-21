using System;
using System.Collections;
using AI_Testing;
using Pathfinding;
using Pathfinding.Examples;
using UnityEngine;

public class TraversingAgent : MonoBehaviour {
    private enum ObstacleClass {
        Obstacle, Wall
    }
    
    private static readonly int StartTraversingCliffDownwardsHash = Animator.StringToHash("StartTraversingDown");
    private static readonly int TraversingCliffDownwardsHash = Animator.StringToHash("ShouldClimbDownCliff");
    private static readonly int TraversingCliffUpWardsHash = Animator.StringToHash("ShouldClimbUpWall");
    private static readonly int JumpOverObstacleHash = Animator.StringToHash("OverObstacle");
    private static readonly int JumpChasmHash = Animator.StringToHash("JumpChasm");
    private static readonly int WalkingHash = Animator.StringToHash("Walking");
    private static readonly int WonHash = Animator.StringToHash("Victory");
    private static readonly int LandHash = Animator.StringToHash("Land");

    [Header("Climb Down Cliff Configs")]
    [SerializeField] private Transform groundChecker;
    [SerializeField] private LayerMask cliffDetectionLayers;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float cliffCheckForwardMultiplier;
    [SerializeField] private float cliffAllowableDistance;

    [Header("Obstacle Detection Configs")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleCheckDistance = 3.0f;
    [SerializeField] private float climbeableObstacleHeight = 1.5f;
    [SerializeField] private AnimationCurve climbingCurve;
    
    [Header("Obstacle Configs")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.75f;
    [SerializeField] private float obstacleClearance = 0.5f; // Distance past the obstacle to land

    private Vector3 _jumpStartPosition;
    private Vector3 _jumpTargetPosition;
    private float _jumpProgress;
    
    private Vector3 OriginPosition => transform.position + transform.up * 0.25f;
    private Vector3 ForwardDirectionVector => transform.position + transform.forward * cliffCheckForwardMultiplier;

    private Tuple<RaycastHit, bool> _downCliffCheckTuple;
    private Tuple<RaycastHit, bool> _obstacleCheckTuple;
    
    private Vector3 _cliffDestinationPosition;
    private Vector3 _currentTargetPosition;
    private FollowerEntity _followerEntity;
    
    private bool _downACliff;
    private bool _detectedWallOrObstacle;
    private bool _reachedObstacle;
    
    private Vector3 _obstacleHitPosition;
    private Vector3 _overObstaclePosition;
    
    private Vector3 _wallHitPosition;
    private Vector3 _wallDestinationPosition;
    
    private ObstacleClass _obstacleClass;
    
    private bool _reachedCliffStartingPosition;
    
    private Animator _animator;
    private Vector3 _approachDirection;

    private Obstacle _currentObstacle;

    private bool _endLevel;

    private bool _landed;
    private bool _jumping;

    private void Awake() {
        _followerEntity = GetComponent<FollowerEntity>();
        _animator = GetComponent<Animator>();
        _landed = true;
    }

    private void OnEnable() {
        FollowerJumpLink.OnStartOffMeshLink += OnStartOffMeshLink;
        FollowerJumpLink.OnEndOffMeshLink += OnEndOffMeshLink;
    }

    private void OnDisable() {
        FollowerJumpLink.OnStartOffMeshLink -= OnStartOffMeshLink;
        FollowerJumpLink.OnEndOffMeshLink -= OnEndOffMeshLink;
    }

    private void OnStartOffMeshLink() {
        _jumping = true;
        _animator.SetBool(WalkingHash, false);
        _animator.SetTrigger(JumpChasmHash);
        StartCoroutine(SetLanding());
    }

    private IEnumerator SetLanding() {
        yield return new WaitForSeconds(0.05f);
        _landed = false;
    }

    private void OnEndOffMeshLink() {
        // Disable agent
        // Play animation
        // enable agent again
        StartCoroutine(LandAndContinue());
    }

    private IEnumerator LandAndContinue() {
        _followerEntity.isStopped = true;
        yield return new WaitForSeconds(0.6f);
        _followerEntity.isStopped = false;
        _jumping = false;
    }

    private void FixedUpdate() {
        if (_jumping) {
            CheckLanding();
            return;
        }
        
        CheckForJumpObstacles();
        HandleClimbingDownwards();
    }

    private void CheckLanding() {
        if (_landed) return;
        
        if (!Physics.Raycast(transform.position + transform.up * 0.5f, Vector3.down, 2.0f, groundLayer)) return;
        
        _animator.SetTrigger(LandHash);
        _landed = true;
    }

    private void HandleClimbingDownwards() {
        if (_endLevel) return;
        CheckDownCliffs();

         if (_followerEntity.enabled) {
             _animator.SetBool(WalkingHash, _followerEntity.remainingDistance > 1f && !_followerEntity.isStopped);
         }

         if (_downACliff || _detectedWallOrObstacle) return;
         
         if (_followerEntity.remainingDistance <= 1) {
             StartCoroutine(WaitAndEndLevel());
             _endLevel = true;
         }
    }

    private IEnumerator WaitAndEndLevel() {
        yield return new WaitForSeconds(2f);
        _animator.SetTrigger(WonHash);
    }

    private void CheckForJumpObstacles() {
        _obstacleCheckTuple = DetectingWall();
        
        if (_detectedWallOrObstacle) {
            switch (_obstacleClass) {
                case ObstacleClass.Obstacle:
                    TraverseObstacle();
                    break;
                case ObstacleClass.Wall:
                    ClimbUpWall();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return;
        }
        
        if (!_obstacleCheckTuple.Item2) {
            _detectedWallOrObstacle = false;
            return;
        }

        if (_obstacleCheckTuple.Item1.collider.GetComponent<Obstacle>().Traversed) {
            _detectedWallOrObstacle = false;
            return;
        }
        
        var hit = _obstacleCheckTuple.Item1;
        
        var obstacleCollider = hit.collider;
        Debug.Log($"-- OBSTACLE HEIGHT : {obstacleCollider.bounds.size.y} --");
        _obstacleClass = obstacleCollider.bounds.size.y >= climbeableObstacleHeight ? ObstacleClass.Wall : ObstacleClass.Obstacle;

        if (_obstacleClass == ObstacleClass.Obstacle) {
            _obstacleHitPosition = hit.point;
            _overObstaclePosition = obstacleCollider.bounds.center + obstacleCollider.transform.forward * 1.5f;
        }
        else {
            _wallHitPosition = hit.point;
            _wallHitPosition.z -= 0.5f;
            _wallDestinationPosition = new Vector3(_wallHitPosition.x, _wallHitPosition.y + obstacleCollider.bounds.size.y, _wallHitPosition.z);
        }
        
        _currentObstacle = obstacleCollider.GetComponent<Obstacle>();
        _detectedWallOrObstacle = true;
    }
    
    private void TraverseObstacle() {
        _followerEntity.enabled = false;

        if (!_reachedObstacle) {
            var obstacleTarget = _obstacleHitPosition;
            obstacleTarget.z -= 0.5f;
            MoveToPosition(obstacleTarget, () => {
                _reachedObstacle = true;
                _jumpStartPosition = transform.position;
                // Adjust jump to go towards the agent's facing direction instead of the obstacle's forward direction
                _jumpTargetPosition = transform.position + transform.forward * (Vector3.Distance(transform.position, _overObstaclePosition) + obstacleClearance);
                _jumpProgress = 0f;
                _animator.SetTrigger(JumpOverObstacleHash);
            });
        }
        else {
            _jumpProgress += Time.deltaTime / jumpDuration;

            Vector3 horizontalPos = Vector3.Lerp(_jumpStartPosition, _jumpTargetPosition, _jumpProgress);

            float verticalOffset = climbingCurve.Evaluate(_jumpProgress) * jumpHeight;

            Vector3 newPosition = new Vector3(horizontalPos.x, _jumpStartPosition.y + verticalOffset, horizontalPos.z);
            transform.position = newPosition;

            if (_jumpProgress >= 1f) {
                _currentObstacle.Traversed = true;
                _detectedWallOrObstacle = false;
                _followerEntity.enabled = true;
                _reachedObstacle = false;
            }
        }
    }

    private void ClimbUpWall() {
        _followerEntity.enabled = false;

        if (!_reachedObstacle) {
            MoveToPosition(_wallHitPosition, () => {
                // Rotate the agent to face the wall before climbing
                Vector3 directionToWall = (_wallHitPosition - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(directionToWall, Vector3.up);
                transform.rotation = lookRotation;

                _animator.SetBool(TraversingCliffUpWardsHash, true);
                _reachedObstacle = true;
            });
        }
        else {
            MoveToVerticalPosition(_wallDestinationPosition, () => {
                _animator.SetBool(TraversingCliffUpWardsHash, false);
                _reachedObstacle = false;
                _detectedWallOrObstacle = false;
                _currentObstacle.Traversed = true;
                _followerEntity.enabled = true; 
            });
        }
    }

    private void CheckDownCliffs() {
        _downCliffCheckTuple = DetectingCliff();
        
        if (_downACliff) {
            HandleCliffTraversal();
            return;
        }
        
        if (!_downCliffCheckTuple.Item2) return;
        
        var hit = _downCliffCheckTuple.Item1;
        
        if (Mathf.Abs(transform.position.y - hit.point.y) > cliffAllowableDistance) {
            _cliffDestinationPosition = hit.point;
            _currentTargetPosition = ForwardDirectionVector + (Vector3.forward * 0.5f);
            _approachDirection = (_currentTargetPosition - transform.position).normalized;
            _reachedCliffStartingPosition = false;
            _animator.SetBool(WalkingHash, false);
            _downACliff = true;
        }
    }

    private void HandleCliffTraversal() {
        _followerEntity.enabled = false;
        
        if (!_reachedCliffStartingPosition) {
            MoveToPosition(_currentTargetPosition, () => {
                Quaternion lookRotation = Quaternion.LookRotation(-_approachDirection, Vector3.up);
                transform.rotation = lookRotation;
                _animator.SetTrigger(StartTraversingCliffDownwardsHash);
                _animator.SetBool(TraversingCliffDownwardsHash, true);
                _reachedCliffStartingPosition = true;
            });
        }
        else
        {
            MoveToVerticalPosition(_cliffDestinationPosition, () => {
                _downACliff = false;
                _reachedCliffStartingPosition = false;
                _animator.SetBool(TraversingCliffDownwardsHash, false);
                _animator.ResetTrigger(StartTraversingCliffDownwardsHash);
                _followerEntity.enabled = true;
            });
        }
    }
    
    private bool IsGrounded() {
        return Physics.CheckSphere(groundChecker.position, groundCheckRadius, groundLayer);
    }

    private Tuple<RaycastHit, bool> DetectingCliff() {
        bool hits = Physics.Raycast(transform.up + ForwardDirectionVector, Vector3.down, out var hit, Mathf.Infinity, cliffDetectionLayers);
        
        if (hits && hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground") || _detectedWallOrObstacle) {
            return new Tuple<RaycastHit, bool>(hit, false);
        }
        
        return new Tuple<RaycastHit, bool>(hit, hits);
    }

    private Tuple<RaycastHit, bool> DetectingWall() {
        bool hits = Physics.Raycast(origin: OriginPosition,transform.forward, out var hit, obstacleCheckDistance, obstacleLayer);
        return new Tuple<RaycastHit, bool>(hit, hits);
    }
    
    private void MoveToPosition(Vector3 target, Action onReached = null, float speed = 4f) {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.001f) onReached?.Invoke();
    }
    
    private void MoveToVerticalPosition(Vector3 target, Action onReached = null, float speed = 1f) {
        var targetPosition = new Vector3(transform.position.x, target.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        if (Mathf.Abs(transform.position.y - target.y) < 0.1) onReached?.Invoke();
    }
    
    private void OnDrawGizmos() {
        var cliffCheckTuple = DetectingCliff();
        var wallCheckTuple = DetectingWall();
        
        Gizmos.color = wallCheckTuple.Item2 ? Color.green : Color.red;
        Gizmos.DrawLine(OriginPosition, OriginPosition + transform.forward * obstacleCheckDistance);

        
        Gizmos.color = cliffCheckTuple.Item2 ? Color.green : Color.red;
        Gizmos.DrawLine(transform.up + ForwardDirectionVector, ForwardDirectionVector + Vector3.down * 100.0f);
        Gizmos.DrawSphere(cliffCheckTuple.Item1.point, 0.1f);
        
        if (_detectedWallOrObstacle) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_wallDestinationPosition, _obstacleClass == ObstacleClass.Wall ? 0.1f : 0.0f);
        }
        
        Gizmos.color = _landed ? Color.blue : Color.red;
        Gizmos.DrawRay(transform.position + transform.up * 0.5f, Vector3.down);
        
        if (groundChecker == null) return;
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Gizmos.DrawSphere(groundChecker.position, groundCheckRadius);
    }
}
