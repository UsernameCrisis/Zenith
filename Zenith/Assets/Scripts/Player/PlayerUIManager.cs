using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private Volume deathVolume;
    private Vignette vignette;
    void Start()
    {
        deathVolume.profile.TryGet(out vignette);
    }

    

    public IEnumerator DeathVignette(float duration = 1f)
    {
        float elapsed = 0f;
        float startIntensity = 0.3f;
        float targetIntensity = 0.75f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        vignette.intensity.value = targetIntensity;
    }
}
