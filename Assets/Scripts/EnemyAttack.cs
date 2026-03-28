using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public Transform target;
    public int damage = 5;
    public float attackRange = 0.9f;
    public float attackCooldown = 3f;

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
        if (distance <= attackRange && Time.time >= _nextAttackTime)
        {
            if (_animator != null) _animator.SetTrigger(_animIDEnemyAttack);
            if (!_playerControl.IsInvulnerable)
            {
                _playerControl.health -= damage;
                if (_playerControl.health < 0) _playerControl.health = 0;
            }
            _nextAttackTime = Time.time + attackCooldown;
        }
    }
}
