using System;
using Pathfinding;
using UnityEngine;

public class TraversingAgent : MonoBehaviour {
    private enum ObstacleClass {
        Obstacle, Wall
    }
    
    private static readonly int TraversingCliff = Animator.StringToHash("IsInCliff");
    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    [Header("Climb Down Cliff Configs")]
    [SerializeField] private Transform groundChecker;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float cliffCheckForwardMultiplier;
    [SerializeField] private float cliffAllowableDistance;

    [Header("Climb Up Cliff Configs")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float obstacleHeight;
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
    private bool _traversingObstacle;
    private Vector3 _obstacleHitPosition;
    private Vector3 _overObstaclePosition;
    
    private ObstacleClass _obstacleClass;
    
    private bool _reachedCliffStartingPosition;
    
    private Animator _animator;
    private Vector3 _approachDirection; // Track the direction towards the cliff edge


    private void Awake() {
        _followerEntity = GetComponent<FollowerEntity>();
        _animator = GetComponent<Animator>();
        //_animator.SetBool(WalkingHash, true);
    }

    private void FixedUpdate() {
        CheckForJumpObstacles();
        HandleClimbingDownwards();
    }

    private void HandleClimbingDownwards() {
        CheckDownCliffs();

         if (_followerEntity.enabled) {
             _animator.SetBool(WalkingHash, _followerEntity.remainingDistance > 1f);
         }
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
        
        var hit = _obstacleCheckTuple.Item1;
        
        _detectedWallOrObstacle = true;
        
        var obstacleCollider = hit.collider;
        
        _obstacleClass = obstacleCollider.bounds.size.y > obstacleHeight ? ObstacleClass.Wall : ObstacleClass.Obstacle;
        _obstacleHitPosition = hit.point;
        _overObstaclePosition = obstacleCollider.bounds.center + obstacleCollider.transform.forward;
        _traversingObstacle = true;
    }
    
    private void TraverseObstacle() {
        _followerEntity.enabled = false;

        if (!_reachedObstacle) {
            transform.position = Vector3.MoveTowards(transform.position, _obstacleHitPosition, 4 * Time.deltaTime);

            if (Vector3.Distance(transform.position, _obstacleHitPosition) < 0.001f) {
                _reachedObstacle = true;
                _jumpStartPosition = transform.position;
                _jumpTargetPosition = _overObstaclePosition + transform.forward * obstacleClearance;
                _jumpProgress = 0f;
            
                transform.LookAt(_jumpTargetPosition);
            }
        }
        else {
            _jumpProgress += Time.deltaTime / jumpDuration;
        
            Vector3 horizontalPos = Vector3.Lerp(_jumpStartPosition, _jumpTargetPosition, _jumpProgress);
        
            float verticalOffset = climbingCurve.Evaluate(_jumpProgress) * jumpHeight;
        
            Vector3 newPosition = new Vector3(horizontalPos.x, _jumpStartPosition.y + verticalOffset, horizontalPos.z);
            transform.position = newPosition;

            if (_jumpProgress >= 1f) {
                _traversingObstacle = false;
                _detectedWallOrObstacle = false;
                _reachedObstacle = false;
                _followerEntity.enabled = true;
            }
        }
    }

    private void ClimbUpWall() {
        Debug.Log("SHOULD CLIMB WALL");
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
            _currentTargetPosition = ForwardDirectionVector + new Vector3(0, 0, 0.5f);
            _approachDirection = (_currentTargetPosition - transform.position).normalized;
            _reachedCliffStartingPosition = false;
            _animator.SetBool(WalkingHash, false);
            _downACliff = true;
        }
    }

    private void HandleCliffTraversal() {
        _followerEntity.enabled = false;
        
        if (!_reachedCliffStartingPosition) {
            transform.position = Vector3.MoveTowards(transform.position, _currentTargetPosition, 4 * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, _currentTargetPosition) < 0.001f) {
                Quaternion lookRotation = Quaternion.LookRotation(-_approachDirection, Vector3.up);
                transform.rotation = lookRotation;
                _animator.SetBool(TraversingCliff, true);
                _reachedCliffStartingPosition = true;
            }    
        }
        else {
            Vector3 targetPosition = new Vector3(transform.position.x, _cliffDestinationPosition.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 1 * Time.deltaTime);
            
            if (Mathf.Abs(transform.position.y - _cliffDestinationPosition.y) < 0.1f) {
                _downACliff = false;
                _reachedCliffStartingPosition = false;
                _animator.SetBool(TraversingCliff, false);
                _followerEntity.enabled = true;
            }
        }
    }

    private bool IsGrounded() {
        return Physics.CheckSphere(groundChecker.position, groundCheckRadius, groundLayer);
    }

    private Tuple<RaycastHit, bool> DetectingCliff() {
        bool hits = Physics.Raycast(ForwardDirectionVector, Vector3.down, out var hit, Mathf.Infinity, groundLayer);
        return new Tuple<RaycastHit, bool>(hit, hits);
    }

    private Tuple<RaycastHit, bool> DetectingWall() {
        bool hits = Physics.Raycast(origin: OriginPosition,transform.forward, out var hit, wallCheckDistance, wallLayer);
        return new Tuple<RaycastHit, bool>(hit, hits);
    }
    
    private void OnDrawGizmos() {
        var cliffCheckTuple = DetectingCliff();
        var wallCheckTuple = DetectingWall();
        
        Gizmos.color = wallCheckTuple.Item2 ? Color.green : Color.red;
        Gizmos.DrawLine(OriginPosition, OriginPosition + transform.forward * wallCheckDistance);
        
        Gizmos.color = cliffCheckTuple.Item2 ? Color.green : Color.red;
        Gizmos.DrawLine(ForwardDirectionVector, ForwardDirectionVector + Vector3.down * 100.0f);
        
        if (groundChecker == null) return;
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Gizmos.DrawSphere(groundChecker.position, groundCheckRadius);
    }
}
