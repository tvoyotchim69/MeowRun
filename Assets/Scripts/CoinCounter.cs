using UnityEngine;
using UnityEngine.UI;
using TMPro; // ƒŒ¡¿¬»“‹ ›“” —“–Œ ”

public class CoinCounter : MonoBehaviour
{
    public static CoinCounter instance;

    [Header("UI References")]
    public TMP_Text coinText; // »«Ã≈Õ»“‹ Ò Text Ì‡ TMP_Text

    private int totalCoins = 0;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        UpdateCoinUI();
        Debug.Log($"Coins: {totalCoins}");
    }

    public void ResetCoins()
    {
        totalCoins = 0;
        UpdateCoinUI();
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = $"ÃÓÌÂÚ˚: {totalCoins}";
        }
    }

    public int GetTotalCoins()
    {
        return totalCoins;
    }
}