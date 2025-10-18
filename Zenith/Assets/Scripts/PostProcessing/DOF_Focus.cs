using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DOFFocus : MonoBehaviour
{
    public Transform target;
    public Volume volume;
    private DepthOfField dof;

    void Start()
    {
        volume.profile.TryGet(out dof);
    }

    void Update()
    {
        if (dof != null && target != null)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, target.position);
            dof.focusDistance.value = distance;
        }
    }
}
