using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 50;
    public float deathDuration = 5f;

    private bool _dying;

    public void TakeDamage(int amount)
    {
        if (_dying) return;
        health -= amount;
        if (health <= 0) StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        _dying = true;
        health = 0;

        EnemyChase chase = GetComponent<EnemyChase>();
        if (chase != null) chase.enabled = false;
        EnemyAttack attack = GetComponent<EnemyAttack>();
        if (attack != null) attack.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.SetTrigger("EnemyDeath");

        yield return new WaitForSeconds(deathDuration);
        Destroy(gameObject);
    }
}
