using UnityEngine;
using TMPro;
using UnityEngine.UI;
using StarterAssets;

public class PlayerControl : MonoBehaviour
{
    public Slider healthBar;
    public TMP_Text healthText;
    public int health = 100;
    public int maxHealth = 0;

    public int lightDamage = 10;
    public int heavyDamage = 25;
    public float attackRange = 1.5f;
    public float attackRadius = 1f;

    private Animator _animator;
    private StarterAssetsInputs _input;
    private int _animIDPunch;
    private int _animIDHookPunch;

    void Start()
    {
        maxHealth = health;
        _animator = GetComponent<Animator>();
        _input = GetComponent<StarterAssetsInputs>();
        _animIDPunch = Animator.StringToHash("Punch");
        _animIDHookPunch = Animator.StringToHash("HookPunch");
    }

    void Update()
    {
        healthText.text = health + " / " + maxHealth;
        healthBar.value = (float)health / (float)maxHealth;

        if (_input.attack)
        {
            _animator.SetTrigger(_animIDPunch);
            DealDamage(lightDamage);
            _input.attack = false;
        }

        if (_input.heavyAttack)
        {
            _animator.SetTrigger(_animIDHookPunch);
            DealDamage(heavyDamage);
            _input.heavyAttack = false;
        }
    }

    private void DealDamage(int amount)
    {
        Vector3 center = transform.position + transform.forward * attackRange + Vector3.up;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius);
        foreach (var hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(amount);
            }
        }
    }
}
