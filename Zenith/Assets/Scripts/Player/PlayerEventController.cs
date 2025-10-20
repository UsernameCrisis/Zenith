using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public class PlayerEventController : MonoBehaviour
{
    private float invincibilityDuration;
    public void Initialize(float _invincibilityDuration)
    {
        invincibilityDuration = _invincibilityDuration;
    }
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Projectile")) ProjectileHitEvent(other.GetComponent<Projectile>());
        if (other.CompareTag("Enemy")) EnemyHitEvent();
    }
    //Enter Combat Scene With Enemy
    private void EnemyHitEvent()
    {
        //TODO
    }
    //Take Damage From Projectile
    private void ProjectileHitEvent(Projectile projectile)
    {
        Player.Instance._playerData.TakeDamage(projectile.getDamage());
        Player.Instance._playerUIController._playerHealthUI.UpdateUI(Player.Instance._playerData.getCurrentHP(), Player.Instance._playerData.getMaxHP());
        Destroy(projectile.gameObject);
        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        var _playerMovement = gameObject.GetComponentInParent<PlayerMovement>();
        _playerMovement.canMove(false);
        _playerMovement.getAnimator().SetBool("isHit", true);
        

        yield return new WaitForSeconds(invincibilityDuration);

        _playerMovement.getAnimator().SetBool("isHit", false);
        if (!_playerMovement.dead())
        {
            _playerMovement.canMove(true);
        }
    }
    public void die()
    {
        StartCoroutine(DeathVignette());
        //TODO back to town?
    }

    //Death Animation
    IEnumerator DeathVignette(float duration = 1f)
    {
        Vignette vignette = gameObject.GetComponentInParent<PlayerData>().getVignette();
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
