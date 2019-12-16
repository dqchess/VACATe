﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterColour
{
    None,
    Red,
    Yellow,
    Green,
    Purple
}

public class EntityStatsController : MonoBehaviour
{
    // Common entity regenerable stats
    public RegenerableStat health;

    public bool isDead { get; protected set; }

    // Common base stats
    public Stat damage;
    public Stat defense;

    public CharacterColour characterColour = CharacterColour.None;

    protected Animator Anim;
    protected Rigidbody rb;
    protected Collider col;

    // Entity layer mask constant for entity raycasting checks
    public const int EntityLayer = 1 << 9;

    [Header("Spawn Config")]
    [Tooltip("VFX to use when player spawns")]
    [SerializeField] protected GameObject spawnVFX;
    [Tooltip("Controls the speed of the spawn animation")]
    [SerializeField] protected float spawnSpeed = 0.08f;
    [Tooltip("Number of seconds to wait before the spawn sequence starts")]
    [SerializeField] protected float spawnDelay = 0.9f;
    [Tooltip("Number of seconds to wait after the spawn sequence ends")]
    [SerializeField] protected float spawnCooldown = 0;

    protected AudioSource[] Audio;
    // Audio source for playing vocal audio clips
    protected AudioSource VocalAudio;
    // Audio source for playing weapon audio clips
    protected AudioSource WeaponAudio;
    [Header("Audio Clips")]
    [Tooltip("Audio clip that should play when the entity takes damage")]
    [SerializeField] protected AudioHelper.EntityAudioClip takeDamageVocalSFX;
    [Tooltip("Audio clip that should play when the entity dies")]
    [SerializeField] protected AudioHelper.EntityAudioClip entityDeathVocalSFX;

    protected virtual void Awake()
    {
        health.Init();

        Anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        Audio = GetComponents<AudioSource>();
        // Assign audio sources for weapon and vocal SFX
        // There should be 2 audio sources to handle both SFX types playing concurrently
        if (Audio.Length == 2)
        {
            WeaponAudio = Audio[0];
            VocalAudio = Audio[1];
        }
        else if (Audio.Length < 2)
        {
            WeaponAudio = Audio[0];
            VocalAudio = Audio[0];
        }
    }

    protected virtual void Update()
    {
        if (!isDead)
            health.PauseRegen();
    }

    public virtual void TakeDamage(EntityStatsController attacker, float damageValue, float timeDelta = 1f)
    {
        // Ignore attacks if already dead
        if (isDead)
            return;
        Anim.ResetTrigger("TakeDamage");
        Anim.SetTrigger("TakeDamage");
        if (takeDamageVocalSFX != null)
        {
            StartCoroutine(AudioHelper.PlayAudioOverlap(VocalAudio, takeDamageVocalSFX));
        }
        // Calculate any changes based on stats and modifiers here first
        float hitValue = (damageValue - ComputeDefenseModifier()) * timeDelta;
        health.Subtract(hitValue < 0 ? 0 : hitValue);

        if (Mathf.Approximately(health.CurrentValue, 0f))
        {
            Die();
        }
    }

    public virtual void TakeExplosionDamage(EntityStatsController attacker, float maxDamage, float stunTime,
        float explosionForce, Vector3 explosionPoint, float explosionRadius)
    {
        // Ignore explosions if already dead
        if (isDead)
            return;

        // Calculate damage based on distance from the explosion point
        float proximity = (col.ClosestPoint(explosionPoint) - explosionPoint).magnitude;
        float effect = 1 - (proximity / explosionRadius);

        // TODO slightly strange bug where enemies just beyond the explosion take negative damage? This is a temp fix.
        if (effect < 0f)
            return;

        TakeDamage(attacker, maxDamage * effect);

        StartCoroutine(ApplyExplosiveForce(explosionForce, explosionPoint, explosionRadius, stunTime));
    }

    protected virtual IEnumerator ApplyExplosiveForce(float explosionForce, Vector3 explosionPoint, float explosionRadius, float stunTime)
    {
        // Set to stunned before applying explosive force
        // TODO

        // TODO change this to AddForce(<force vector>, ForceMode.Impulse);
        rb.AddExplosionForce(explosionForce, explosionPoint, explosionRadius);

        // Wait for a moment before un-stunning the victim
        yield return new WaitForSeconds(stunTime);
    }

    public virtual float ComputeDamageModifer()
    {
        float baseHit = Random.Range(0, damage.baseValue - 1); // never want to do 0 damage
        return damage.GetValue() - baseHit;
    }

    public virtual float ComputeDefenseModifier()
    {
        float baseDefense = Random.Range(0, defense.baseValue);
        return defense.GetValue() - baseDefense;
    }

    protected virtual void Die()
    {
        // Meant to be implemented with any death tasks
        isDead = true;
    }

    protected virtual IEnumerator Spawn(GameObject obj, float speed = 0.05f, float delay = 0f, float cooldown = 0f)
    {
        Collider col = obj.GetComponent<Collider>();
        float from = -1 * col.bounds.size.y * 2.5f;
        float to = obj.transform.position.y;
        col.enabled = false;
        obj.transform.position = new Vector3(obj.transform.position.x, from, obj.transform.position.z);
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        Anim.SetTrigger("Spawn");
        float offset = 0;
        while (obj.transform.position.y < to)
        {
            obj.transform.position = new Vector3(obj.transform.position.x, from + offset, obj.transform.position.z);
            offset += speed;
            yield return new WaitForSeconds(0.01f);
        }
        col.enabled = true;
        if (cooldown > 0)
        {
            yield return new WaitForSeconds(cooldown);
        }
    }
}