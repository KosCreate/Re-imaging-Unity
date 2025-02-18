using System;
using Pathfinding;
using UnityEngine;

public class TraversingAgent : MonoBehaviour {
    private static readonly int TraversingCliff = Animator.StringToHash("IsInCliff");
    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    [SerializeField] private Transform groundChecker;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float cliffCheckForwardMultiplier;
    [SerializeField] private float cliffAllowableDistance;
    private Vector3 ForwardOffset => transform.position + transform.forward * cliffCheckForwardMultiplier;

    private Tuple<RaycastHit, bool> _cliffCheckTuple;
    private Vector3 _cliffDestinationPosition;
    private Vector3 _currentTargetPosition;
    private FollowerEntity _followerEntity;
    
    private bool _inCliff;
    private bool _reachedCliffStartingPosition;
    
    private Animator _animator;
    private Vector3 _approachDirection; // Track the direction towards the cliff edge

    private void Awake() {
        _followerEntity = GetComponent<FollowerEntity>();
        _animator = GetComponent<Animator>();
        _animator.SetBool(WalkingHash, true);
    }

    private void FixedUpdate() {
        CheckForCliffs();

        if (_followerEntity.enabled) {
            _animator.SetBool(WalkingHash, _followerEntity.remainingDistance > 1f);
        }
    }

    private void CheckForCliffs() {
        _cliffCheckTuple = DetectingCliff();
        
        if (_inCliff) {
            HandleCliffTraversal();
            return;
        }
        
        if (!_cliffCheckTuple.Item2) return;
        
        var hit = _cliffCheckTuple.Item1;
        
        if (Mathf.Abs(transform.position.y - hit.point.y) > cliffAllowableDistance) {
            _cliffDestinationPosition = hit.point;
            _currentTargetPosition = ForwardOffset + new Vector3(0, 0, 0.5f);
            _approachDirection = (_currentTargetPosition - transform.position).normalized;
            _reachedCliffStartingPosition = false;
            _animator.SetBool(WalkingHash, false);
            _inCliff = true;
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
                _inCliff = false;
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
        bool hits = Physics.Raycast(ForwardOffset, Vector3.down, out var hit, Mathf.Infinity, groundLayer);
        return new Tuple<RaycastHit, bool>(hit, hits);
    }
    
    private void OnDrawGizmos() {
        var cliffCheckTuple = DetectingCliff();
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, ForwardOffset);
        Gizmos.color = cliffCheckTuple.Item2 ? Color.green : Color.red;
        Gizmos.DrawLine(ForwardOffset, ForwardOffset + Vector3.down * 100.0f);
        
        if (groundChecker == null) return;
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Gizmos.DrawSphere(groundChecker.position, groundCheckRadius);
    }
}
