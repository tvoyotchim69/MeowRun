using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    private static bool shouldSkipMainMenu = false;
    private static int selectedMusicIndex = 0;

    [Header("Movement Settings")]
    public float laneWidth = 3f;
    public float laneChangeSpeed = 18f;

    [Header("Jump Settings")]
    public float jumpForce = 22f;
    public float extraGravity = 95f;
    public float fastFallForce = 75f;

    [Header("Progression (Difficulty)")]
    public float accelerationInterval = 15f;
    public float speedMultiplier = 1.15f;
    public float maxTotalAcceleration = 2.5f;
    private float currentTotalMultiplier = 1.0f;
    private float nextAccelerationTime;

    [Header("Shrink/Slide Settings")]
    public float shrinkScaleY = 0.4f;
    public float shrinkDuration = 1.0f;

    [Header("Lane Limits")]
    public int minLane = -1;
    public int maxLane = 1;

    [Header("UI & Menus")]
    public GameObject mainMenuPanel;
    public GameObject tapToStartText;
    public GameObject gameOverPanel;
    public GameObject settingsPanel;
    public GameObject blurVolume;
    public GameObject speedUpText;
    private Animator speedUpAnimator;

    [Header("Audio & Music")]
    public AudioSource backgroundMusic;
    public AudioClip[] musicTracks;
    public Slider volumeSlider;
    public TMP_Dropdown musicDropdown;
    public AudioClip speedUpSound;
    private AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip deathSound; // Звук взрыва (BOOM)

    [Header("Animation")]
    public Animator heroAnimator;

    [Header("VFX (Optional)")]
    public GameObject explosionPrefab; // Префаб частиц взрыва, если есть

    private int currentLane = 0;
    private Vector3 targetPosition;
    private bool isGrounded = true;
    private bool isGameActive = false;
    private bool isPaused = false;
    private bool isShrinked = false;
    private bool isWaitingToStart = false;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private RoadGenerator roadGenerator;
    private Coroutine slideCoroutine;
    private Coroutine speedMessageCoroutine;

    private Vector3 originalScale;
    private float originalHeight;
    private Vector3 originalCenter;

    void Start()
    {
        // Настройки по умолчанию
        SetVolume(0.25f);
        if (volumeSlider != null) volumeSlider.value = 0.25f;

        Time.timeScale = 0;
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.freezeRotation = true; // Ровный бег при старте
            rb.rotation = Quaternion.identity; // Выпрямляем кота
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (capsuleCollider != null)
        {
            originalHeight = capsuleCollider.height;
            originalCenter = capsuleCollider.center;
        }

        roadGenerator = FindFirstObjectByType<RoadGenerator>();
        targetPosition = transform.position;

        if (SwipeManager.instance != null)
            SwipeManager.instance.MoveEvent += OnSwipe;

        InitializeAudio();
        SetupUI();

        if (speedUpText != null) speedUpText.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;

        if (speedUpText != null) speedUpAnimator = speedUpText.GetComponent<Animator>();
    }

    void InitializeAudio()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.ignoreListenerPause = true;
            if (musicTracks != null && musicTracks.Length > selectedMusicIndex)
            {
                backgroundMusic.clip = musicTracks[selectedMusicIndex];
            }

            if (volumeSlider != null)
            {
                if (volumeSlider.value <= 0.01f) volumeSlider.value = 0.25f;
                backgroundMusic.volume = volumeSlider.value;
            }

            if (musicDropdown != null)
            {
                musicDropdown.value = selectedMusicIndex;
                musicDropdown.RefreshShownValue();
            }
        }
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame) ToggleSettings();
        if (isPaused) return;

        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame) RestartGame();
            return;
        }

        if (isWaitingToStart)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame || Pointer.current.press.wasPressedThisFrame) BeginRun();
            return;
        }

        if (!isGameActive) return;

        HandleKeyboardInput();
        HandleMovement();
        CheckGrounded();
        HandleProgression();
    }

    void HandleProgression()
    {
        if (currentTotalMultiplier < maxTotalAcceleration)
        {
            if (Time.time >= nextAccelerationTime)
            {
                if (roadGenerator != null)
                {
                    currentTotalMultiplier *= speedMultiplier;
                    if (currentTotalMultiplier > maxTotalAcceleration) currentTotalMultiplier = maxTotalAcceleration;

                    roadGenerator.UpdateSpeedOnly(speedMultiplier);
                    if (heroAnimator) heroAnimator.speed *= 1.05f;

                    if (speedMessageCoroutine != null) StopCoroutine(speedMessageCoroutine);
                    speedMessageCoroutine = StartCoroutine(ShowSpeedUpMessage());
                }
                nextAccelerationTime = Time.time + accelerationInterval;
            }
        }
    }

    IEnumerator ShowSpeedUpMessage()
    {
        if (speedUpText != null && speedUpAnimator != null)
        {
            speedUpText.SetActive(true);
            if (speedUpSound != null && audioSource != null) audioSource.PlayOneShot(speedUpSound);

            speedUpAnimator.SetTrigger("ShowTrigger");
            yield return new WaitForSecondsRealtime(2f);
            speedUpText.SetActive(false);
        }
    }

    public void ToggleSettings()
    {
        isPaused = !isPaused;
        if (settingsPanel != null) settingsPanel.SetActive(isPaused);

        if (isPaused) Time.timeScale = 0f;
        else if (isGameActive && !isWaitingToStart) Time.timeScale = 1f;
    }

    public void SetVolume(float volume)
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = volume;
            if (volume > 0.05f && !backgroundMusic.isPlaying && (isGameActive || isPaused || isWaitingToStart))
                backgroundMusic.Play();
        }
    }

    public void ChangeMusic(int trackIndex)
    {
        if (musicTracks != null && trackIndex >= 0 && trackIndex < musicTracks.Length)
        {
            selectedMusicIndex = trackIndex;
            backgroundMusic.Stop();
            backgroundMusic.clip = musicTracks[selectedMusicIndex];
            backgroundMusic.Play();
        }
    }

    public void ExitToMainMenu()
    {
        shouldSkipMainMenu = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void BeginRun()
    {
        isWaitingToStart = false;
        isGameActive = true;
        Time.timeScale = 1;
        nextAccelerationTime = Time.time + accelerationInterval;

        if (CoinCounter.instance != null) CoinCounter.instance.ResetRunCoins();

        if (roadGenerator != null) roadGenerator.StartLevel();
        if (tapToStartText != null) tapToStartText.SetActive(false);

        if (backgroundMusic != null && backgroundMusic.clip != null && !backgroundMusic.isPlaying)
            backgroundMusic.Play();

        if (heroAnimator != null) heroAnimator.SetBool("isRunning", true);
    }

    void FixedUpdate()
    {
        if (!isGameActive || isPaused) return;
        if (rb.linearVelocity.y < 1f) rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    void SetupUI()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (shouldSkipMainMenu)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (blurVolume != null) blurVolume.SetActive(false);
            StartGame();
        }
        else
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (blurVolume != null) blurVolume.SetActive(true);
            if (tapToStartText != null) tapToStartText.SetActive(false);
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle")) GameOver();
    }

    public void GameOver()
    {
        if (!isGameActive) return;
        isGameActive = false;

        // 1. Звук взрыва
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // 2. Визуальный эффект (если назначен префаб)
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 3. Физика полета
        if (rb != null)
        {
            rb.freezeRotation = false; // Разрешаем кувыркаться
            Vector3 explosionDir = new Vector3(Random.Range(-7f, 7f), 25f, -15f); // Подбрасываем высоко
            rb.AddForce(explosionDir, ForceMode.Impulse);
            rb.AddTorque(new Vector3(Random.Range(-10f, 10f), 10f, Random.Range(-10f, 10f)), ForceMode.Impulse);
        }

        // 4. Показ меню с задержкой
        StartCoroutine(SlowDownAndShowMenu());

        if (CoinCounter.instance != null) CoinCounter.instance.SaveCoinsToWallet();
        if (backgroundMusic != null) backgroundMusic.Stop();
    }

    IEnumerator SlowDownAndShowMenu()
    {
        yield return new WaitForSecondsRealtime(1.5f); // Время на полет

        Time.timeScale = 0;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (blurVolume != null) blurVolume.SetActive(true);
    }

    void Jump()
    {
        if (isShrinked) { if (slideCoroutine != null) StopCoroutine(slideCoroutine); StopSlideAndResetScale(); }
        if (isGrounded)
        {
            if (jumpSound != null && audioSource != null) audioSource.PlayOneShot(jumpSound);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            if (heroAnimator) heroAnimator.SetTrigger("Jump");
        }
    }

    void Slide()
    {
        if (!isGrounded) rb.linearVelocity = new Vector3(rb.linearVelocity.x, -fastFallForce, rb.linearVelocity.z);

        if (!isShrinked)
        {
            if (slideSound != null && audioSource != null) audioSource.PlayOneShot(slideSound);
            slideCoroutine = StartCoroutine(PerformShrink());
        }
    }

    IEnumerator PerformShrink()
    {
        isShrinked = true;
        if (capsuleCollider != null)
        {
            float newHeight = originalHeight * shrinkScaleY;
            capsuleCollider.height = newHeight;
            float offset = (originalHeight - newHeight) / 2f;
            capsuleCollider.center = new Vector3(originalCenter.x, originalCenter.y - offset, originalCenter.z);
        }
        transform.localScale = new Vector3(originalScale.x, originalScale.y * shrinkScaleY, originalScale.z);
        if (heroAnimator) heroAnimator.SetTrigger("Slide");
        yield return new WaitForSeconds(shrinkDuration);
        StopSlideAndResetScale();
    }

    void StopSlideAndResetScale()
    {
        if (capsuleCollider != null) { capsuleCollider.height = originalHeight; capsuleCollider.center = originalCenter; }
        transform.localScale = originalScale;
        isShrinked = false;
    }

    public void StartGame()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (blurVolume != null) blurVolume.SetActive(false);
        if (tapToStartText != null) tapToStartText.SetActive(true);
        isWaitingToStart = true;
    }

    public void RestartGame()
    {
        shouldSkipMainMenu = true;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnSwipe(bool[] swipes)
    {
        if (!isGameActive || isPaused) return;
        if (swipes[(int)SwipeManager.Direction.Left]) MoveLane(-1);
        else if (swipes[(int)SwipeManager.Direction.Right]) MoveLane(1);
        else if (swipes[(int)SwipeManager.Direction.Up]) Jump();
        else if (swipes[(int)SwipeManager.Direction.Down]) Slide();
    }

    void HandleMovement()
    {
        Vector3 currentPos = rb.position;
        float xPos = Mathf.Lerp(currentPos.x, targetPosition.x, laneChangeSpeed * Time.deltaTime);
        rb.MovePosition(new Vector3(xPos, rb.position.y, rb.position.z));
    }

    void CheckGrounded()
    {
        float currentScale = isShrinked ? shrinkScaleY : 1f;
        float rayDistance = (originalHeight * currentScale) / 2f + 0.2f;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayDistance);
    }

    void MoveLane(int direction)
    {
        int newLane = currentLane + direction;
        if (newLane >= minLane && newLane <= maxLane)
        {
            currentLane = newLane;
            targetPosition = new Vector3(currentLane * laneWidth, targetPosition.y, targetPosition.z);
        }
    }

    void HandleKeyboardInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        bool[] swipes = new bool[4];
        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) swipes[0] = true;
        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) swipes[1] = true;
        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) swipes[2] = true;
        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) swipes[3] = true;
        if (swipes[0] || swipes[1] || swipes[2] || swipes[3]) OnSwipe(swipes);
    }

    void OnDestroy()
    {
        if (SwipeManager.instance != null) SwipeManager.instance.MoveEvent -= OnSwipe;
    }
}