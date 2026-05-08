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
    public float accelerationInterval = 15f; // Изменено на 15 секунд
    public float speedMultiplier = 1.15f;    // Изменено на 15% (1.15)
    public float maxTotalAcceleration = 2.5f; // Можно чуть увеличить лимит
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
    private Animator speedUpAnimator; // Ссылка на Animator на тексте

    [Header("Audio & Music")]
    public AudioSource backgroundMusic;
    public AudioClip[] musicTracks;
    public Slider volumeSlider;
    public TMP_Dropdown musicDropdown;
    public AudioClip speedUpSound; // Ссылка на звуковой файл (WAV/MP3)
    private AudioSource audioSource; // Компонент для воспроизведения звуков
    public AudioClip jumpSound;   // Звук прыжка
    public AudioClip slideSound;  // Звук скольжения

    [Header("Animation")]
    public Animator heroAnimator;

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
        // Громкость по умолчанию при первом запуске
        SetVolume(0.25f);
        if (volumeSlider != null) volumeSlider.value = 0.25f;

        Time.timeScale = 0;
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.freezeRotation = true;
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

        // Скрываем уведомление об ускорении при старте
        if (speedUpText != null) speedUpText.SetActive(false);

        // Получаем AudioSource. Если его нет на объекте, добавляем.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.ignoreListenerPause = true; // Чтобы звук был слышен даже на паузе (по желанию)

        // Получаем Animator с объекта текста
        if (speedUpText != null)
        {
            speedUpAnimator = speedUpText.GetComponent<Animator>();
        }
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
                // Если слайдер не трогали (он в 0), ставим наш дефолт
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

                    // Показываем сообщение
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
            // 1. Включаем объект (если он был выключен)
            speedUpText.SetActive(true);

            // 2. Воспроизводим звук (один раз, не прерывая музыку)
            if (speedUpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(speedUpSound);
            }

            // 3. Запускаем анимацию всплывания и исчезновения
            speedUpAnimator.SetTrigger("ShowTrigger");

            // 4. Ждем время, равное длительности анимации (например, 2.5 секунды)
            yield return new WaitForSecondsRealtime(2f);

            // 5. Выключаем объект обратно
            speedUpText.SetActive(false);
        }
    }

    public void ToggleSettings()
    {
        isPaused = !isPaused;
        if (settingsPanel != null) settingsPanel.SetActive(isPaused);

        if (isPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            if (isGameActive && !isWaitingToStart) Time.timeScale = 1f;
        }
    }

    public void SetVolume(float volume)
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = volume;
            if (volume > 0.05f && !backgroundMusic.isPlaying && (isGameActive || isPaused || isWaitingToStart))
            {
                backgroundMusic.Play();
            }
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

        // Это активирует панель монет и обнулит их
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
        Time.timeScale = 0;

        // Это сохранит монеты в кошелек и СКРОЕТ игровой счетчик
        if (CoinCounter.instance != null) CoinCounter.instance.SaveCoinsToWallet();

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (blurVolume != null) blurVolume.SetActive(true);
        if (backgroundMusic != null) backgroundMusic.Stop();
        if (heroAnimator != null) heroAnimator.speed = 0;
        if (speedUpText != null) speedUpText.SetActive(false);
    }

    void Jump()
    {
        if (isShrinked) { if (slideCoroutine != null) StopCoroutine(slideCoroutine); StopSlideAndResetScale(); }
        if (isGrounded)
        {
            // ВОСПРОИЗВЕДЕНИЕ ЗВУКА ПРЫЖКА
            if (jumpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpSound);
            }

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
            // ВОСПРОИЗВЕДЕНИЕ ЗВУКА СКОЛЬЖЕНИЯ
            if (slideSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(slideSound);
            }

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