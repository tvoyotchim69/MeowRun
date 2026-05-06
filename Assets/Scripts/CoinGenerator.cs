using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinGenerator : MonoBehaviour
{
    public GameObject[] coinPrefabs;
    private List<GameObject> coins = new List<GameObject>();
    public int maxCoinCount = 10;
    public float spawnDistance = 5f;

    private RoadGenerator roadGenerator;

    void Start()
    {
        roadGenerator = FindFirstObjectByType<RoadGenerator>();
        ResetLevel();
    }

    void Update()
    {
        if (roadGenerator == null || roadGenerator.CurrentSpeed == 0) return;

        foreach (GameObject coin in coins)
        {
            if (coin != null)
                coin.transform.position -= new Vector3(0, 0, roadGenerator.CurrentSpeed * Time.deltaTime);
        }

        if (coins.Count > 0 && coins[0] != null && coins[0].transform.position.z < -15)
        {
            Destroy(coins[0]);
            coins.RemoveAt(0);
            CreateNextCoin();
        }
    }

    private void CreateNextCoin()
    {
        if (coinPrefabs.Length == 0)
        {
            Debug.LogError("No coin prefabs assigned!");
            return;
        }

        Vector3 pos = Vector3.zero;
        if (coins.Count > 0 && coins[coins.Count - 1] != null)
        {
            pos = coins[coins.Count - 1].transform.position + new Vector3(0, 0, spawnDistance);
        }

        GameObject randomCoin = coinPrefabs[Random.Range(0, coinPrefabs.Length)];

        float[] laneX = { -3f, 0f, 3f };
        int randomLane = Random.Range(0, laneX.Length);
        pos.x = laneX[randomLane];
        pos.y = 0.5f;

        GameObject go = Instantiate(randomCoin, pos, Quaternion.identity);
        go.transform.SetParent(transform);
        coins.Add(go);
    }

    public void ResetLevel()
    {
        while (coins.Count > 0)
        {
            if (coins[0] != null)
                Destroy(coins[0]);
            coins.RemoveAt(0);
        }

        for (int i = 0; i < maxCoinCount; i++)
        {
            CreateNextCoin();
        }
    }
}