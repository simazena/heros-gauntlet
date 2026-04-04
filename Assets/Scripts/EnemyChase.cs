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
    private CapsuleCollider _capsule;
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
        _capsule = GetComponent<CapsuleCollider>();
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
            transform.position += ResolveMove(moveDir * moveSpeed * Time.deltaTime);
        }

        if (_animator != null)
        {
            _animator.SetFloat(_animIDSpeed, isMoving ? moveSpeed : 0f);
        }
    }

    private Vector3 ResolveMove(Vector3 desired)
    {
        if (_capsule == null) return desired;
        float dist = desired.magnitude;
        if (dist < 0.0001f) return Vector3.zero;

        Vector3 worldCenter = transform.position + _capsule.center;
        float halfHeight = Mathf.Max(0f, _capsule.height * 0.5f - _capsule.radius);
        Vector3 p1 = worldCenter + Vector3.down * halfHeight;
        Vector3 p2 = worldCenter + Vector3.up * halfHeight;
        Vector3 dir = desired / dist;

        RaycastHit[] hits = Physics.CapsuleCastAll(p1, p2, _capsule.radius, dir, dist, ~0, QueryTriggerInteraction.Ignore);
        float minDist = dist;
        Vector3 hitNormal = Vector3.zero;
        bool blocked = false;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c.gameObject.name != "HealthPlatform") continue;
            if (hits[i].distance < minDist)
            {
                minDist = hits[i].distance;
                hitNormal = hits[i].normal;
                blocked = true;
            }
        }

        if (!blocked) return desired;

        float safeDist = Mathf.Max(0f, minDist - 0.02f);
        Vector3 moveAlong = dir * safeDist;
        Vector3 leftover = desired - moveAlong;
        Vector3 slide = Vector3.ProjectOnPlane(leftover, hitNormal);
        slide.y = 0f;
        return moveAlong + slide;
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
