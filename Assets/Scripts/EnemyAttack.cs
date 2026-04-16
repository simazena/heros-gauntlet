using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public Transform target;
    public int damage = 5;
    public float attackRange = 0.9f;
    public float attackCooldown = 3f;
    public float hitNormalizedTime = 0.45f;
    public float fallbackHitDelay = 0.5f;
    public AudioClip attackSfx;

    private PlayerControl _playerControl;
    private Animator _animator;
    private int _animIDEnemyAttack;
    private float _nextAttackTime;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        if (target != null) _playerControl = target.GetComponent<PlayerControl>();

        _animator = GetComponentInChildren<Animator>();
        _animIDEnemyAttack = Animator.StringToHash("EnemyAttack");
    }

    void Update()
    {
        if (_playerControl == null || _playerControl.health <= 0) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange && IsTargetInFront() && Time.time >= _nextAttackTime)
        {
            if (_animator != null) _animator.SetTrigger(_animIDEnemyAttack);
            if (attackSfx != null) AudioSource.PlayClipAtPoint(attackSfx, transform.position);
            StartCoroutine(ApplyDamageDelayed());
            _nextAttackTime = Time.time + attackCooldown;
        }
    }

    private IEnumerator ApplyDamageDelayed()
    {
        if (_animator != null)
        {
            yield return null;
            float waitStart = Time.time;
            while (_animator.IsInTransition(0))
            {
                if (Time.time - waitStart > 0.5f) break;
                yield return null;
            }
            float pollStart = Time.time;
            while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < hitNormalizedTime)
            {
                if (Time.time - pollStart > 2f) break;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(fallbackHitDelay);
        }

        if (_playerControl == null || _playerControl.health <= 0) yield break;
        if (_playerControl.IsInvulnerable) yield break;
        if (Vector3.Distance(transform.position, target.position) > attackRange) yield break;
        if (!IsTargetInFront()) yield break;

        _playerControl.health -= damage;
        if (_playerControl.health < 0) _playerControl.health = 0;
    }

    private bool IsTargetInFront()
    {
        if (target == null) return false;
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return true;
        return Vector3.Dot(transform.forward, toTarget.normalized) > 0f;
    }
}
