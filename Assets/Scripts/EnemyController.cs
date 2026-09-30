using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("Atributos")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float detectionRange = 5f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LineRenderer bulletLine;

    private Rigidbody rb;
    private Transform playerTransform;
    private PlayerController playerController;
    private float nextFireTime;

    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (bulletLine == null)
        {
            bulletLine = gameObject.AddComponent<LineRenderer>();
            bulletLine.startWidth = 0.05f;
            bulletLine.endWidth = 0.05f;
            bulletLine.material = new Material(Shader.Find("Sprites/Default"));
            bulletLine.startColor = Color.red;
            bulletLine.endColor = Color.red;
            bulletLine.enabled = false;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        if (!IsAlive || playerTransform == null || !playerController.IsAlive) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            LookAtPlayer();

            if (distanceToPlayer <= attackRange && Time.time >= nextFireTime)
            {
                AttackPlayer();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsAlive || playerTransform == null || !playerController.IsAlive)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange && distanceToPlayer > 1.2f)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0;
            Vector3 targetVelocity = direction * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void LookAtPlayer()
    {
        Vector3 lookTarget = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.LookAt(lookTarget);
    }

    private void AttackPlayer()
    {
        nextFireTime = Time.time + fireRate;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = (playerTransform.position - origin).normalized;
        Vector3 targetPoint;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange))
        {
            targetPoint = hit.point;

            if (((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
            {
                PlayerController player = hit.collider.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(attackDamage);
                }
            }
        }
        else
        {
            targetPoint = origin + direction * attackRange;
        }

        StartCoroutine(ShowTracer(origin, targetPoint));
    }

    private IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        bulletLine.SetPosition(0, start);
        bulletLine.SetPosition(1, end);
        bulletLine.enabled = true;
        yield return new WaitForSeconds(0.05f);
        bulletLine.enabled = false;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}