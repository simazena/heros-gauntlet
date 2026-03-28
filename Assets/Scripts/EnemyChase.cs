using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 0.7f;
    public float separationRadius = 1.5f;
    public float separationStrength = 1.5f;

    private Animator _animator;
    private int _animIDSpeed;
    private int _animIDMotionSpeed;
    private int _animIDGrounded;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        _animator = GetComponentInChildren<Animator>();
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        if (_animator != null)
        {
            _animator.SetBool(_animIDGrounded, true);
            _animator.SetFloat(_animIDMotionSpeed, 1f);
        }
    }

    void Update()
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        bool isMoving = direction.magnitude > stoppingDistance;
        if (isMoving)
        {
            Vector3 chaseDir = direction.normalized;
            Vector3 moveDir = (chaseDir + ComputeSeparation()).normalized;

            Quaternion lookRotation = Quaternion.LookRotation(chaseDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

        if (_animator != null)
        {
            _animator.SetFloat(_animIDSpeed, isMoving ? moveSpeed : 0f);
        }
    }

    private Vector3 ComputeSeparation()
    {
        Vector3 push = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(transform.position, separationRadius);
        for (int i = 0; i < nearby.Length; i++)
        {
            if (nearby[i].gameObject == gameObject) continue;
            if (nearby[i].GetComponent<EnemyChase>() == null) continue;
            Vector3 away = transform.position - nearby[i].transform.position;
            away.y = 0f;
            float dist = away.magnitude;
            if (dist > 0.01f)
            {
                push += away.normalized * (1f - dist / separationRadius);
            }
        }
        return push * separationStrength;
    }

    private void OnFootstep(AnimationEvent animationEvent) { }
    private void OnLand(AnimationEvent animationEvent) { }
}
