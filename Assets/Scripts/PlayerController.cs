using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Cámara & FOV")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float minFOV = 50f;
    [SerializeField] private float maxFOV = 120f;
    [SerializeField] private float fovChangeSpeed = 30f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Atributos")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float currentStamina = 10f;
    [SerializeField] private float staminaRegenRate = 2f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private int ammoCount = 10;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LineRenderer bulletLine; 

    private Rigidbody rb;
    private bool isGrounded;
    private float nextFireTime;
    private float verticalRotation = 0f;

    public bool IsAlive => currentHealth > 0;
    public int AmmoCount => ammoCount;

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
            bulletLine.startColor = Color.yellow;
            bulletLine.endColor = Color.yellow;
            bulletLine.enabled = false;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (!IsAlive) return;

        HandleCameraAndFOV();
        HandleStamina();
        HandleJump();
        HandleAttack();
    }

    private void FixedUpdate()
    {
        if (!IsAlive) return;

        HandleMovement();
        CheckGround();
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized;
        Vector3 targetVelocity = moveDirection * moveSpeed;

        Vector3 velocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        rb.linearVelocity = velocity;
    }

    private void HandleCameraAndFOV()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        if (playerCamera != null)
        {
            if (Input.GetKey(KeyCode.T))
            {
                playerCamera.fieldOfView -= fovChangeSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Y))
            {
                playerCamera.fieldOfView += fovChangeSpeed * Time.deltaTime;
            }

            playerCamera.fieldOfView = Mathf.Clamp(playerCamera.fieldOfView, minFOV, maxFOV);
        }
    }

    private void HandleStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, (GetComponent<Collider>().bounds.extents.y) + groundCheckDistance, groundMask);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && currentStamina >= 5f)
        {
            currentStamina -= 5f;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime && ammoCount > 0)
        {
            nextFireTime = Time.time + fireRate;
            ammoCount--;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
            {
                targetPoint = hit.point;

              
                if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
                {
                    EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(attackDamage);
                    }
                }
            }
            else
            {
                targetPoint = ray.origin + ray.direction * attackRange;
            }

            StartCoroutine(ShowTracer(playerCamera.transform.position + Vector3.down * 0.2f, targetPoint));
        }
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

    public void AddAmmo(int amount)
    {
        ammoCount += amount;
    }
}