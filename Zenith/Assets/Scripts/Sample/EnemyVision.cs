using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEditor.Search;
using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public float radius;
    [UnityEngine.Range(0, 360)] public float angle;
    public List<GameObject> visibleTargets = new List<GameObject>();

    public LayerMask targetMask;
    public LayerMask obstructionMask;
    [HideInInspector] public bool canSeeTarget;
    [HideInInspector] public float rotation;
    [HideInInspector] public GameObject Target;

    void Start()
    {
        StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    // private void FieldOfViewCheck()
    // {
    //     Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

    //     if (rangeChecks.Length != 0)
    //     {
    //         Transform target = rangeChecks[0].transform;
    //         Vector3 directionToTarget = (target.position - transform.position).normalized;

    //         if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
    //         {
    //             float distanceToTarget = Vector3.Distance(transform.position, target.position);

    //             if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
    //             {
    //                 canSeeTarget = true;
    //             }
    //             else
    //             {
    //                 canSeeTarget = false;
    //             }
    //         }
    //         else
    //         {
    //             canSeeTarget = false;
    //         }
    //     }
    //     else if (canSeeTarget == true)
    //     {
    //         canSeeTarget = false;
    //     }
    // }

    public void FieldOfViewCheck()
    {
        // Debug.Log(canSeeTarget + " " + gameObject.GetInstanceID());
        //sphere
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        HashSet<GameObject> objectsInRange = new HashSet<GameObject>();

        //check cone
        foreach (Collider targetCollider in rangeChecks)
        {
            Transform target = targetCollider.transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            Vector3 forwardWithOffset = Quaternion.Euler(0, rotation, 0) * transform.forward;
            if (Vector3.Angle(forwardWithOffset, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                {
                    objectsInRange.Add(target.gameObject);
                    // if (!visibleTargets.Contains(target.gameObject) && (target.gameObject != this.gameObject))
                    // {
                    //     visibleTargets.Add(target.gameObject);
                    // }
                }
            }
        }

        visibleTargets.Clear();
        foreach (GameObject target in objectsInRange)
        {
            visibleTargets.Add(target);
        }

        // Debug.Log("GameObject: " + gameObject.GetInstanceID() + " " + "Target :" + visibleTargets.Count);

        if (Target == null && visibleTargets.Count > 0)
        {
            Target = visibleTargets[0].gameObject;
            canSeeTarget = true;
        }
        else if (visibleTargets.Count == 0)  {
            canSeeTarget = true;
        }
    }
}
