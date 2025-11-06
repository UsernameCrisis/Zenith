using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using Unity.VisualScripting;

[CustomEditor(typeof(EnemyVision))]
public class FieldOfViewEditor : Editor
{
    void OnSceneGUI()
    {
        EnemyVision fov = (EnemyVision)target;

        Handles.color = Color.white;
        Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.radius);

        Vector3 viewAngle1 = DirectionFromAngle(fov.transform.eulerAngles.y, -fov.angle / 2, ref fov);
        Vector3 viewAngle2 = DirectionFromAngle(fov.transform.eulerAngles.y, fov.angle / 2, ref fov);

        Handles.color = Color.yellow;
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle1 * fov.radius);
        Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle2 * fov.radius);

        if (fov.visibleTargets != null && fov.visibleTargets.Count > 0)
        {
            Handles.color = Color.green;

            foreach (GameObject target in fov.visibleTargets)
            {
                if (target != null)
                    Handles.DrawLine(fov.transform.position, target.transform.position);
            }
        }
    }

    private Vector3 DirectionFromAngle(float eulerY, float angleInDegrees, ref EnemyVision fov)
    {
        angleInDegrees += eulerY + fov.rotation;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    // void OnSceneGUI()
    // {
    //     EnemyVision fov = (EnemyVision)target;

    //     Handles.color = Color.white;

    //     Handles.DrawWireArc(fov.transform.position, Vector3.up, Vector3.forward, 360, fov.radius);

    //     Vector3 viewAngle1 = DirectionFromAngle(fov.transform.eulerAngles.y, -fov.angle/2);
    //     Vector3 viewAngle2 = DirectionFromAngle(fov.transform.eulerAngles.y, fov.angle / 2);

    //     Handles.color = Color.yellow;
    //     Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle1 * fov.radius);
    //     Handles.DrawLine(fov.transform.position, fov.transform.position + viewAngle2 * fov.radius);

    //     if (fov.canSeeTarget)
    //     {
    //         Handles.color = Color.green;
    //         Handles.DrawLine(fov.transform.position, fov.targetObject.transform.position);
    //     }
    // }

    // private Vector3 DirectionFromAngle(float eulerY, float angleInDegrees)
    // {
    //     angleInDegrees += eulerY;

    //     return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    // }
}
