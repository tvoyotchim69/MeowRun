using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float laneWidth = 3f;
    public float slideSpeed = 10f;
    public float jumpForce = 8f;  // Сила прыжка

    [Header("Lane Limits")]
    public int minLane = -1;
    public int maxLane = 1;

    [Header("Animation")]
    public Animator heroAnimator;

    private int currentLane = 0;
    private Vector3 targetPosition;
    private bool isGrounded = true;
    private bool isGameActive = false;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private RoadGenerator roadGenerator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (rb != null)
        {
            rb.useGravity = true;  // Включаем гравитацию Unity
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // Настройки коллайдера по умолчанию
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 1.5f;
            capsuleCollider.radius = 0.4f;
            capsuleCollider.center = new Vector3(0, 0.75f, 0);
        }

        if (heroAnimator == null)
        {
            heroAnimator = GetComponent<Animator>();
        }

        roadGenerator = FindFirstObjectByType<RoadGenerator>();
        targetPosition = transform.position;

        if (SwipeManager.instance != null)
        {
            SwipeManager.instance.MoveEvent += OnSwipe;
            SwipeManager.instance.ClickEvent += OnClick;
        }

        SetRunningAnimation(false);
    }

    void OnDestroy()
    {
        if (SwipeManager.instance != null)
        {
            SwipeManager.instance.MoveEvent -= OnSwipe;
            SwipeManager.instance.ClickEvent -= OnClick;
        }
    }

    void Update()
    {
        bool shouldGameRun = (roadGenerator != null && roadGenerator.enabled && SwipeManager.instance != null && SwipeManager.instance.enabled);

        if (shouldGameRun != isGameActive)
        {
            isGameActive = shouldGameRun;
            SetRunningAnimation(isGameActive);
        }

        if (!isGameActive)
            return;

        HandleMovement();
        CheckGrounded();
    }

    void FixedUpdate()
    {
        if (!isGameActive) return;

        // Применяем прыжок в FixedUpdate для физической корректности
    }

    void SetRunningAnimation(bool isRunning)
    {
        if (heroAnimator != null)
        {
            heroAnimator.SetBool("isRunning", isRunning);
            if (isRunning)
            {
                heroAnimator.Play("Kitty_001_run");
                heroAnimator.speed = 1;
            }
            else
            {
                heroAnimator.speed = 0;
            }
        }
    }

    void OnSwipe(bool[] swipes)
    {
        if (!isGameActive) return;

        if (swipes[(int)SwipeManager.Direction.Left])
        {
            MoveLeft();
        }
        else if (swipes[(int)SwipeManager.Direction.Right])
        {
            MoveRight();
        }
        else if (swipes[(int)SwipeManager.Direction.Up])
        {
            Jump();
        }
        else if (swipes[(int)SwipeManager.Direction.Down])
        {
            Slide();
        }
    }

    void OnClick(Vector2 pos)
    {
        // Не используется
    }

    void MoveLeft()
    {
        if (currentLane > minLane)
        {
            currentLane--;
            targetPosition = new Vector3(currentLane * laneWidth, targetPosition.y, targetPosition.z);
        }
    }

    void MoveRight()
    {
        if (currentLane < maxLane)
        {
            currentLane++;
            targetPosition = new Vector3(currentLane * laneWidth, targetPosition.y, targetPosition.z);
        }
    }

    void Jump()
    {
        if (isGrounded && isGameActive)
        {
            // Добавляем импульс силы вверх
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            Debug.Log($"Hero jumped! Force: {jumpForce}");

            if (heroAnimator != null)
            {
                heroAnimator.SetTrigger("Jump");
            }
        }
    }

    void Slide()
    {
        Debug.Log("Hero slide!");

        if (capsuleCollider != null)
        {
            StartCoroutine(SlideCollider());
        }

        if (heroAnimator != null)
        {
            heroAnimator.SetTrigger("Slide");
        }
    }

    IEnumerator SlideCollider()
    {
        float originalHeight = capsuleCollider.height;
        Vector3 originalCenter = capsuleCollider.center;

        capsuleCollider.height = originalHeight * 0.5f;
        capsuleCollider.center = new Vector3(originalCenter.x, originalCenter.y - originalHeight * 0.25f, originalCenter.z);

        yield return new WaitForSeconds(0.5f);

        capsuleCollider.height = originalHeight;
        capsuleCollider.center = originalCenter;
    }

    void HandleMovement()
    {
        if (rb == null) return;

        // Плавное движение по X
        Vector3 newPosition = rb.position;
        newPosition.x = Mathf.Lerp(newPosition.x, targetPosition.x, slideSpeed * Time.deltaTime);
        rb.MovePosition(newPosition);
    }

    void CheckGrounded()
    {
        if (capsuleCollider == null) return;

        float rayDistance = capsuleCollider.height / 2f + 0.1f;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance))
        {
            if (!isGrounded)
            {
                isGrounded = true;
                Debug.Log("Landed on ground");
            }
        }
        else
        {
            if (isGrounded)
            {
                isGrounded = false;
            }
        }

        // Визуализация луча
        Debug.DrawRay(transform.position, Vector3.down * rayDistance, Color.red);
    }

    public void StartRunning()
    {
        isGameActive = true;
        SetRunningAnimation(true);
    }

    public void StopRunning()
    {
        isGameActive = false;
        SetRunningAnimation(false);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Hit: {collision.gameObject.name}");
    }
}