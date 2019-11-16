﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyCombatController))]
public class EnemyMotorController : MonoBehaviour
{
    public float aggroRadius = 10f;
    public float deAggroRadius = 15f;

    private EnemyCombatController _combat;
    private EnemyStatsController _stats;
    private Animator _anim;

    private List<GameObject> _targets;
    private Transform _currentTarget;
    private NavMeshAgent _agent;

    private void Awake()
    {
        _anim = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _combat = GetComponent<EnemyCombatController>();
        _stats = GetComponent<EnemyStatsController>();
    }

    private void Start()
    {
        // Store references to the transforms of players
        _targets = PlayerManager.Instance.players;
    }

    private void FixedUpdate()
    {
        // Prevent enemy activity during death animation
        if (_stats.isDead)
        {
            return;
        }

        // TODO Should enemies wander when no target is selected?

        // Enemy target selection follows this precedence:
        //   1. Player who attacked them last (TODO)
        //   2. Player they are currently following
        //   3. Closest player in their sight

        // No target selected, try to find one
        if (!_currentTarget)
        {
            float minDistance = float.MaxValue;

            // Find the nearest player within enemy's sight
            foreach (GameObject target in _targets)
            {
                float distance = Vector3.Distance(target.transform.position, transform.position);
                if (distance <= aggroRadius && distance < minDistance)
                {
                    // Ensure enemy can see them directly
                    if (Physics.Linecast(transform.position, target.transform.position, out RaycastHit hit) && hit.transform.CompareTag("Player"))
                    {
                        _currentTarget = target.transform;
                        minDistance = distance;
                    }
                }
            }
        }
        
        if (_currentTarget)
        {
            float distance = Vector3.Distance(_currentTarget.position, transform.position);
            if (distance <= deAggroRadius)
            {
                _agent.SetDestination(_currentTarget.position);

                if (distance <= _agent.stoppingDistance)
                {
                    FaceTarget();

                    // TODO probably not the most efficient way to handle enemy combat decisions?
                    _combat.PrimaryAttack();

                    // Check to see if current target died -- TODO move this elsewhere?
                    if (_currentTarget.GetComponent<PlayerStatsController>().isDead)
                    {
                        _currentTarget = null;
                    }
                }
            }
            else
            {
                // Target out of range, cancel enemy aggro
                // Enemy will still move to the last known location of the player
                _currentTarget = null;
            }
        }

        // Set walk animation
        _anim.SetBool("Walk Forward", _agent.velocity != Vector3.zero);
    }

    private void FaceTarget()
    {
        Vector3 direction = (_currentTarget.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the aggro and de-aggro radii of the enemy in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, deAggroRadius);
    }
}