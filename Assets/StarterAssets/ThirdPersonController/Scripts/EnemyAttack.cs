using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public Transform target;
    public int damage = 5;
    public float attackRange = 1.8f;
    public float attackCooldown = 1.5f;

    private PlayerControl _playerControl;
    private float _nextAttackTime;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        if (target != null) _playerControl = target.GetComponent<PlayerControl>();
    }

    void Update()
    {
        if (_playerControl == null || _playerControl.health <= 0) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange && Time.time >= _nextAttackTime)
        {
            _playerControl.health -= damage;
            if (_playerControl.health < 0) _playerControl.health = 0;
            _nextAttackTime = Time.time + attackCooldown;
        }
    }
}
