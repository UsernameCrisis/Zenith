using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerUIManager : MonoBehaviour
{
    private Volume deathVolume;
    private Vignette vignette;


    public IEnumerator DeathVignette(float duration = 1f)
    {
        deathVolume = FindFirstObjectByType<Volume>();
        deathVolume.profile.TryGet(out vignette);
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

        yield return new WaitForSeconds(duration * 1.5f);
    }
}
