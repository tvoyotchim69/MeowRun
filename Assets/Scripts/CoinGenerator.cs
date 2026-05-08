using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] coinPrefabs;

    [Header("Settings")]
    public int maxCoinCount = 10;
    public float spawnDistance = 5f;
    public float destroyZPosition = -15f;

    private List<GameObject> coins = new List<GameObject>();
    private RoadGenerator roadGenerator;

    void Start()
    {
        roadGenerator = FindFirstObjectByType<RoadGenerator>();

        // ПРОВЕРКА: если забыли назначить префабы, прерываем выполнение
        if (coinPrefabs == null || coinPrefabs.Length == 0)
        {
            Debug.LogError("ВНИМАНИЕ: На объекте CoinGenerator не назначены монеты (массив Coin Prefabs пуст!)");
            return;
        }

        ResetLevel();
    }

    void Update()
    {
        // Если дороги нет или она стоит — ничего не двигаем
        if (roadGenerator == null || roadGenerator.CurrentSpeed == 0) return;

        // Двигаем монеты
        for (int i = 0; i < coins.Count; i++)
        {
            if (coins[i] != null)
            {
                coins[i].transform.position -= new Vector3(0, 0, roadGenerator.CurrentSpeed * Time.deltaTime);
            }
        }

        // Удаляем монеты, которые уехали за спину
        if (coins.Count > 0 && coins[0] != null && coins[0].transform.position.z < destroyZPosition)
        {
            GameObject coinToDestroy = coins[0];
            coins.RemoveAt(0);
            Destroy(coinToDestroy);
            CreateNextCoin();
        }
    }

    private void CreateNextCoin()
    {
        // Еще раз проверяем массив перед созданием
        if (coinPrefabs == null || coinPrefabs.Length == 0) return;

        Vector3 pos = Vector3.zero;

        // Определяем позицию по Z относительно последней монеты
        if (coins.Count > 0 && coins[coins.Count - 1] != null)
        {
            pos = coins[coins.Count - 1].transform.position + new Vector3(0, 0, spawnDistance);
        }
        else
        {
            // Если монет еще нет, спавним чуть впереди игрока
            pos = new Vector3(0, 0.5f, 20f);
        }

        // Выбираем случайный префаб
        GameObject randomCoinPrefab = coinPrefabs[Random.Range(0, coinPrefabs.Length)];

        // Выбираем случайную полосу
        float[] laneX = { -3f, 0f, 3f };
        pos.x = laneX[Random.Range(0, laneX.Length)];
        pos.y = 0.5f;

        GameObject go = Instantiate(randomCoinPrefab, pos, Quaternion.identity);
        go.transform.SetParent(transform);
        coins.Add(go);
    }

    public void ResetLevel()
    {
        // Очищаем старые монеты
        foreach (GameObject coin in coins)
        {
            if (coin != null) Destroy(coin);
        }
        coins.Clear();

        // Создаем начальную партию монет
        for (int i = 0; i < maxCoinCount; i++)
        {
            CreateNextCoin();
        }
    }
}