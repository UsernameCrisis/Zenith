using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerEventController : MonoBehaviour
{
   IEnumerator DeathVignette(float duration = 1f, Vignette vignette = null)
    {
        if (vignette == null) yield return null;

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
