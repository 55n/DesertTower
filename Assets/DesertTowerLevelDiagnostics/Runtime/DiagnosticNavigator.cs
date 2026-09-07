using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DesertTower.Levels.Diagnostics
{
    /// <summary>Preview-only path following. It is not an EnemyAI or a runtime movement contract.</summary>
    public sealed class DiagnosticNavigator : MonoBehaviour
    {
        public IEnumerator Follow(NavMeshAgent agent,IReadOnlyList<Vector3> destinations,DiagnosticSettings settings,Action<bool> completed)
        {
            bool ok=true;
            foreach(var destination in destinations)
            {
                if(!agent || !agent.isOnNavMesh || !NavMesh.SamplePosition(destination,out var target,1.5f,NavMesh.AllAreas) || !agent.SetDestination(target.position))
                { ok=false; break; }
                yield return null;
                float timeout=Time.time+settings.segmentTimeout;
                while(agent && (agent.pathPending || agent.remainingDistance>.6f))
                {
                    if(Time.time>timeout || (!agent.pathPending && agent.pathStatus!=NavMeshPathStatus.PathComplete)) { ok=false; break; }
                    yield return null;
                }
                if(!agent || !ok) { ok=false; break; }
            }
            completed(ok);
            if(agent) Destroy(agent.gameObject);
        }
    }
}
