using UnityEngine;
using TMPro;

public class CoinCounter : MonoBehaviour
{
    public static CoinCounter instance;

    [Header("UI Text References")]
    [Tooltip("Текст счетчика внутри игры (во время бега)")]
    public TMP_Text gameCoinText;

    [Header("Lobby Settings")]
    [Tooltip("Текст общего кошелька в главном меню")]
    public TMP_Text lobbyCoinText;

    [Header("UI Panels")]
    [Tooltip("Объект-родитель игрового счетчика (чтобы скрывать его в меню)")]
    public GameObject gameCoinPanel;

    private int currentRunCoins = 0;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateLobbyUI();
        UpdateGameUI();

        if (gameCoinPanel != null) gameCoinPanel.SetActive(false);
        //PlayerPrefs.SetInt("TotalWallet", 0); // очистка монет
        //UpdateLobbyUI();
    }

    public void AddCoins(int amount)
    {
        currentRunCoins += amount;
        UpdateGameUI();
    }

    public void SaveCoinsToWallet()
    {
        int totalWallet = PlayerPrefs.GetInt("TotalWallet", 0);
        totalWallet += currentRunCoins;
        PlayerPrefs.SetInt("TotalWallet", totalWallet);
        PlayerPrefs.Save();

        UpdateLobbyUI();

        // После сохранения (при смерти) снова скрываем игровой счетчик
        if (gameCoinPanel != null) gameCoinPanel.SetActive(false);
    }

    public void ResetRunCoins()
    {
        currentRunCoins = 0;
        UpdateGameUI();

        // Когда сбрасываем монеты для старта — включаем панель
        if (gameCoinPanel != null) gameCoinPanel.SetActive(true);
    }

    private void UpdateGameUI()
    {
        if (gameCoinText != null)
        {
            gameCoinText.text = $"Монеты: {currentRunCoins}";
        }
    }

    public void UpdateLobbyUI()
    {
        if (lobbyCoinText != null)
        {
            int totalWallet = PlayerPrefs.GetInt("TotalWallet", 0);
            lobbyCoinText.text = $"Кошелек: {totalWallet}";
        }
    }
}