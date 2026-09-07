using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DesertTower.Levels.Diagnostics
{
    /// <summary>Schedules preview capsules only. Production wave management belongs to the game.</summary>
    public sealed class DiagnosticWaveRunner : MonoBehaviour
    {
        LevelRoot level;
        DiagnosticSettings settings;
        DiagnosticVisuals visuals;
        DiagnosticNavigator navigator;
        int pending;
        public int Spawned { get; private set; }
        public int Arrived { get; private set; }
        public int Failed { get; private set; }
        public int Active { get; private set; }
        public bool Started { get; private set; }
        public bool IsComplete => Started && pending==0 && Active==0;

        public void Begin(LevelRoot root,int waveIndex,DiagnosticSettings options,DiagnosticVisuals factory)
        {
            level=root; settings=options; visuals=factory; navigator=gameObject.AddComponent<DiagnosticNavigator>();
            if(!level.waves || waveIndex<0 || waveIndex>=level.waves.waves.Count) return;
            var groups=level.waves.waves[waveIndex].groups;
            pending=groups.Count; Started=true;
            foreach(var group in groups) StartCoroutine(Spawn(group));
        }
        IEnumerator Spawn(SpawnGroup group)
        {
            yield return new WaitForSeconds(Mathf.Max(0,group.delay));
            if(!level.TryResolveSpawnGroup(group,out var binding,out _)) { Failed+=Mathf.Max(1,group.count); pending--; yield break; }
            var destinations=new List<Vector3>();
            if(settings.followSuggestedRoute && binding.SuggestedRoute)
                foreach(var p in binding.SuggestedRoute.waypoints) destinations.Add(binding.SuggestedRoute.transform.TransformPoint(p));
            destinations.Add(binding.Target.transform.position);
            for(int i=0;i<Mathf.Clamp(group.count,1,500);i++)
            {
                Vector2 offset=Random.insideUnitCircle*binding.Spawn.spawnRadius;
                var point=binding.Spawn.transform.position+new Vector3(offset.x,0,offset.y);
                if(!NavMesh.SamplePosition(point,out var hit,1.5f,NavMesh.AllAreas)) Failed++;
                else
                {
                    var actor=new GameObject("Navigation probe"); actor.transform.SetParent(transform,false); actor.transform.position=hit.position;
                    var mesh=visuals.Create(PrimitiveType.Capsule,"Probe visual",group.element ? group.element.previewColor : new Color(.9f,.32f,.14f),actor.transform);
                    mesh.transform.localPosition=Vector3.up*.9f; mesh.transform.localScale=new Vector3(.65f,.9f,.65f);
                    var agent=actor.AddComponent<NavMeshAgent>(); agent.radius=settings.agentRadius; agent.height=settings.agentHeight;
                    agent.speed=settings.probeSpeed; agent.acceleration=20; agent.angularSpeed=360; agent.stoppingDistance=.25f;
                    Spawned++; Active++;
                    StartCoroutine(navigator.Follow(agent,destinations,settings,ok=>{ if(ok) Arrived++; else Failed++; Active--; }));
                }
                yield return new WaitForSeconds(Mathf.Max(.05f,group.interval));
            }
            pending--;
        }
    }
}
