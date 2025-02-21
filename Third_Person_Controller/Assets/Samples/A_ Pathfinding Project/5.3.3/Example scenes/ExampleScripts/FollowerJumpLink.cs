#if MODULE_ENTITIES
using System;
using UnityEngine;
using System.Collections;
using Pathfinding.ECS;

namespace Pathfinding.Examples {
	[HelpURL("https://arongranberg.com/astar/documentation/stable/followerjumplink.html")]
	public class FollowerJumpLink : MonoBehaviour, IOffMeshLinkHandler, IOffMeshLinkStateMachine {
		public static event Action OnStartOffMeshLink;
		public static event Action OnEndOffMeshLink;
		[SerializeField] private float jumpHeight = 2.0f;
		private void OnEnable() => GetComponent<NodeLink2>().onTraverseOffMeshLink = this;
		private void OnDisable() => GetComponent<NodeLink2>().onTraverseOffMeshLink = null;
		
		IOffMeshLinkStateMachine IOffMeshLinkHandler.GetOffMeshLinkStateMachine(AgentOffMeshLinkTraversalContext context) => this;
		
		void IOffMeshLinkStateMachine.OnFinishTraversingOffMeshLink (AgentOffMeshLinkTraversalContext context) {
			Debug.Log("An agent finished traversing an off-mesh link");
			OnEndOffMeshLink?.Invoke();
		}

		void IOffMeshLinkStateMachine.OnAbortTraversingOffMeshLink () {
			Debug.Log("An agent aborted traversing an off-mesh link");
		}

		IEnumerable IOffMeshLinkStateMachine.OnTraverseOffMeshLink (AgentOffMeshLinkTraversalContext ctx) {
			var start = ctx.link.relativeStart;
			var end = ctx.link.relativeEnd;
			var dir = end - start;
			
			OnStartOffMeshLink?.Invoke();
			// Disable local avoidance while traversing the off-mesh link.
			// If it was enabled, it will be automatically re-enabled when the agent finishes traversing the link.
			ctx.DisableLocalAvoidance();

			// Move and rotate the agent to face the other side of the link.
			// When reaching the off-mesh link, the agent may be facing the wrong direction.
			while (!ctx.MoveTowards(
				position: start,
				rotation: Quaternion.LookRotation(dir, ctx.movementPlane.up),
				gravity: true,
				slowdown: true).reached) {
				yield return null;
			}

			var bezierP0 = start;
			var bezierP1 = start + Vector3.up * jumpHeight;
			var bezierP2 = end + Vector3.up * jumpHeight;
			var bezierP3 = end;
			var jumpDuration = 1.0f;

			// Animate the AI to jump from the start to the end of the link
			for (float t = 0; t < jumpDuration; t += ctx.deltaTime) {
				ctx.transform.Position = AstarSplines.CubicBezier(bezierP0, bezierP1, bezierP2, bezierP3, Mathf.SmoothStep(0, 1, t / jumpDuration));
				yield return null;
			}
		}
	}
}
/// <summary>[followerEntity.onTraverseOffMeshLink]</summary>
#else
using UnityEngine;
namespace Pathfinding.Examples {
	[HelpURL("https://arongranberg.com/astar/documentation/stable/followerjumplink.html")]
	public class FollowerJumpLink : MonoBehaviour {}
}
#endif
