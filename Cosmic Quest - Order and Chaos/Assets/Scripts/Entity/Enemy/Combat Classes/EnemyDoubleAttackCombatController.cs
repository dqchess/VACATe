﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyDoubleAttackCombatController : EnemyCombatController
{
    public float secondaryAttackCooldown = 1f;
    public float secondaryAttackDelay = 0.6f;
    [SerializeField] protected AudioHelper.EntityAudioClip secondaryAttackSFX;
   
    /// <summary>
    /// Double attack enemy's primary attack
    /// </summary>
    public override void PrimaryAttack()
    {
        if (AttackCooldown > 0f)
            return;

        Anim.SetTrigger("PrimaryAttack");
        // audio
        StartCoroutine(AudioHelper.PlayAudioOverlap(WeaponAudio, primaryAttackSFX));

        // Attack any enemies within the attack sweep and range
        foreach (GameObject player in Players.Where(player => CanDamageTarget(player.transform.position, attackRadius, attackAngle)))
        {
            // Calculate and perform damage
            StartCoroutine(PerformDamage(player.GetComponent<EntityStatsController>(), Stats.ComputeDamageModifer(), primaryAttackDelay));
        }

        AttackCooldown = primaryAttackCooldown;
    }
    /// <summary>
    /// Double attack enemy's secondary attack
    /// </summary>
    public override void SecondaryAttack()
    {
        if (AttackCooldown > 0f)
            return;

        Anim.SetTrigger("SecondaryAttack");
        // audio
        StartCoroutine(AudioHelper.PlayAudioOverlap(WeaponAudio, secondaryAttackSFX));

        // Attack any enemies within the attack sweep and range
        foreach (GameObject player in Players.Where(player => CanDamageTarget(player.transform.position, attackRadius, attackAngle)))
        {
            // Calculate and perform damage
            StartCoroutine(PerformDamage(player.GetComponent<EntityStatsController>(), Stats.ComputeDamageModifer(), secondaryAttackDelay));
        }

        AttackCooldown = secondaryAttackCooldown;
    }
}