﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRangedCombatController : PlayerCombatController
{
    [Header("Primary Attack")]
    [Tooltip("The minimum base damage that this attack can deal before scaling")]
    public float primaryAttackMinDamage = 4f;
    [Tooltip("The maximum base damage that this attack can deal before scaling")]
    public float primaryAttackMaxDamage = 5f;
    [Tooltip("The distance the primary attack arrow can travel")]
    public float primaryAttackRange = 20f;
    [Tooltip("Time in seconds to charge primary attack to full power")]
    public float primaryAttackChargeTime = 1.5f;
    [Tooltip("The minimum force to launch the primary attack projectile at")]
    public float primaryAttackMinLaunchForce = 150f;
    [Tooltip("The maximum force to launch the primary attack projectile at")]
    public float primaryAttackMaxLaunchForce = 800f;
    [Tooltip("The delay before the arrow projectile is launched")]
    public float primaryAttackLaunchDelay = 0.3f;
    [Tooltip("The curve of charge time versus damage and range")]
    public AnimationCurve primaryAttackEffectCurve;
    [Tooltip("The amount of the player's mana depleted (and necessary) per attack")]
    public float primaryAttackManaDepletion = 25f;
    [Tooltip("The percent modifier of movement speed during this attack")]
    [Range(0f, 1f)]
    public float primaryAttackMovementModifier = 0.5f;
    [Tooltip("The arrow prefab for the primary attack")]
    public GameObject primaryProjectilePrefab;
    [Tooltip("Weapon audio effect for primary attack charge")]
    [SerializeField] protected AudioHelper.EntityAudioClip primaryAttackChargeWeaponSFX;
    [Tooltip("Weapon audio effect for primary attack release")]
    [SerializeField] protected AudioHelper.EntityAudioClip primaryAttackReleaseWeaponSFX;

    [Header("Secondary Attack")]
    [Tooltip("The trap prefab for the secondary attack")]
    public GameObject secondaryAttackTrapPrefab;
    [Tooltip("The delay before spawning the trap prefab")]
    public float secondaryAttackSpawnDelay = 0.5f;
    [Tooltip("The amount of the player's mana depleted (and necessary) per attack")]
    public float secondaryAttackManaDepletion = 50f;
    [Tooltip("The percent modifier of movement speed during this attack")]
    [Range(0f, 1f)]
    public float secondaryAttackMovementModifier = 0.1f;
    [Tooltip("Weapon audio effect for secondary attack")]
    [SerializeField] protected AudioHelper.EntityAudioClip secondaryAttackWeaponSFX;
    [Tooltip("Weapon audio effect for secondary attack explosion")]
    [SerializeField] protected AudioHelper.EntityAudioClip secondaryAttackWeaponExplosionSFX;

    private bool _isPrimaryCharging;
    private float _primaryChargeTime;

    protected override void Update()
    {
        base.Update();

        // Handle attack charge ups
        if (_isPrimaryCharging)
        {
            _primaryChargeTime += Time.deltaTime;
            if (_primaryChargeTime > primaryAttackChargeTime)
            {
                // Clamp to max charge time
                _primaryChargeTime = primaryAttackChargeTime;
            }
        }
    }

    protected override void PrimaryAttack()
    {
        float chargePercent = Mathf.InverseLerp(0f, primaryAttackChargeTime, _primaryChargeTime);

        float damageValue = primaryAttackEffectCurve.Evaluate(chargePercent) * Random.Range(primaryAttackMinDamage, primaryAttackMaxDamage + Stats.damage.GetValue());
        float primaryAttackLaunchForce = Mathf.Lerp(primaryAttackMinLaunchForce, primaryAttackMaxLaunchForce, primaryAttackEffectCurve.Evaluate(chargePercent));

        // Launch projectile in the direction the player is facing
        StartCoroutine(LaunchProjectile(primaryProjectilePrefab, transform.forward, primaryAttackLaunchForce, primaryAttackRange, damageValue, primaryAttackLaunchDelay));

        // Reset attack timeout and deplete mana
        AttackCooldown = primaryAttackTimeout;
        (Stats as PlayerStatsController).mana.Subtract(primaryAttackManaDepletion);
    }

    protected override void SecondaryAttack()
    {
        if (AttackCooldown > 0 || (Stats as PlayerStatsController).mana.CurrentValue < secondaryAttackManaDepletion)
            return;

        // Place explosive trap
        StartCoroutine(PlaceTrap(secondaryAttackTrapPrefab, secondaryAttackSpawnDelay));

        // Trigger secondary attack animation
        Anim.SetTrigger("SecondaryAttack");

        // Play the attack audio
        StartCoroutine(AudioHelper.PlayAudioOverlap(WeaponAudio, secondaryAttackWeaponSFX));

        // Reset attack timeout and deplete mana
        AttackCooldown = secondaryAttackTimeout;
        (Stats as PlayerStatsController).mana.Subtract(secondaryAttackManaDepletion);

        // Apply movement speed modifier
        StartCoroutine(Motor.ApplyTimedMovementModifier(secondaryAttackMovementModifier, secondaryAttackTimeout));
    }

    protected override void UltimateAbility()
    {
        // TODO implement melee class ultimate ability
        Anim.SetTrigger("UltimateAbility");
    }

    private IEnumerator LaunchProjectile(GameObject projectilePrefab, Vector3 direction, float launchForce, float range, float damage, float launchDelay = 0f)
    {
        ResetTakeDamageAnim();
        if (launchDelay > 0f)
            yield return new WaitForSeconds(launchDelay);

        // Launch projectile from projectile pool
        GameObject projectile = ObjectPooler.Instance.GetPooledObject(projectilePrefab);
        projectile.GetComponent<DamageProjectile>().Launch(Stats, direction, launchForce, range, damage);
    }

    private IEnumerator PlaceTrap(GameObject trapPrefab, float spawnDelay = 0f)
    {
        ResetTakeDamageAnim();
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        // Place trap from object pool in front of the player
        GameObject trap = ObjectPooler.Instance.GetPooledObject(trapPrefab);
        ExplosiveTrap explosiveTrap = trap.GetComponent<ExplosiveTrap>();
        explosiveTrap.SetExplosionAudio(WeaponAudio, secondaryAttackWeaponExplosionSFX);
        explosiveTrap.PlaceTrap(Stats, transform.position + transform.forward);
    }

    protected override void OnPrimaryAttack(InputValue value)
    {
        if (AttackCooldown > 0 || (Stats as PlayerStatsController).mana.CurrentValue < primaryAttackManaDepletion)
            return;

        if (value.isPressed)
        {
            _isPrimaryCharging = true;
            _primaryChargeTime = 0f;
            Anim.SetBool("PrimaryAttack", true);
            StartCoroutine(AudioHelper.PlayAudioOverlap(WeaponAudio, primaryAttackChargeWeaponSFX));
            (Stats as PlayerStatsController).mana.PauseRegen();
            Motor.ApplyMovementModifier(primaryAttackMovementModifier);
        }
        else if (_isPrimaryCharging)
        {
            _isPrimaryCharging = false;
            Anim.SetBool("PrimaryAttack", false);
            StartCoroutine(AudioHelper.PlayAudioOverlap(WeaponAudio, primaryAttackReleaseWeaponSFX));
            (Stats as PlayerStatsController).mana.StartRegen();
            Motor.ResetMovementModifier();
            PrimaryAttack();
        }
    }
}