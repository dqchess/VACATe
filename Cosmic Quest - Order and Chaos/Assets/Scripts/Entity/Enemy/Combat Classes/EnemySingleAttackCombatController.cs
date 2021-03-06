﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySingleAttackCombatController : EnemyCombatController
{
    /// <summary>
    /// Single attack enemy's primary attack
    /// </summary>
    public override void PrimaryAttack()
    {
        if (AttackCooldown > 0f)
            return;

        // animation
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
}