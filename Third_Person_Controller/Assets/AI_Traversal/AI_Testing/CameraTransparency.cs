using System.Collections.Generic;
using UnityEngine;

public class CameraTransparency : MonoBehaviour {
    [SerializeField] private Transform player;
    [SerializeField] private float overlapSphereRadius;
    [SerializeField] private LayerMask layerMask;
    
    private readonly Stack<Collider> _previousColliders = new Stack<Collider>();

    private int _previousHits;
    
    private void FixedUpdate() {
        DetectColliders();
    }

    private void DetectColliders() {
        var hits = Physics.RaycastAll(transform.position,(player.position + transform.up) - transform.position, 
            Vector3.Distance(transform.position, player.position + transform.up), layerMask);

        if (_previousHits == hits.Length) return;
        
        while (_previousColliders.Count > 0) {
            var col = _previousColliders.Pop();
            foreach (var material in col.GetComponent<MeshRenderer>().materials) {
                Debug.Log("MAKING MATS OPAQUE");
                var newColor = material.color;
                newColor.a = 1;
                material.color = newColor;
            }
        }

        foreach (var hit in hits) {
            _previousColliders.Push(hit.collider);
            foreach (var material in hit.collider.GetComponent<MeshRenderer>().materials) {
                Debug.Log("MAKING MATS TRANSPARENT");
                var newColor = material.color;
                newColor.a = 0.2f;
                material.color = newColor;
            }
        }
            
        _previousHits = hits.Length;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, (player.position + transform.up) - transform.position);
        Gizmos.DrawWireSphere(player.position + transform.up, overlapSphereRadius);
    }
}
