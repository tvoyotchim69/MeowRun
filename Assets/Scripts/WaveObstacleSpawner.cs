using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;
    public float[] laneX = { -3f, 0f, 3f }; 
    private List<GameObject> obstacles = new List<GameObject>();
    public int maxObstacleCount = 5;

    private RoadGenerator roadGenerator;

    void Start()
    {
        roadGenerator = FindFirstObjectByType<RoadGenerator>();
        ResetLevel();
    }

    void Update()
    {
        if (roadGenerator == null || roadGenerator.CurrentSpeed == 0) return;

        foreach (GameObject obstacle in obstacles)
        {
            obstacle.transform.position -= new Vector3(0, 0, roadGenerator.CurrentSpeed * Time.deltaTime);
        }

        if (obstacles.Count > 0 && obstacles[0].transform.position.z < -15)
        {
            Destroy(obstacles[0]);
            obstacles.RemoveAt(0);
            CreateNextObstacle();
        }
    }

    private void CreateNextObstacle()
    {
        if (obstaclePrefabs.Length == 0)
        {
            Debug.LogError("No obstacle prefabs assigned!");
            return;
        }

        Vector3 pos = Vector3.zero;
        if (obstacles.Count > 0)
        {
            pos = obstacles[obstacles.Count - 1].transform.position + new Vector3(0, 0, 15);
        }


        GameObject randomPrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];


        int randomLane = Random.Range(0, laneX.Length);
        pos.x = laneX[randomLane];

        GameObject go = Instantiate(randomPrefab, pos, randomPrefab.transform.rotation);
        go.transform.SetParent(transform);
        obstacles.Add(go);
    }

    public void ResetLevel()
    {
        while (obstacles.Count > 0)
        {
            Destroy(obstacles[0]);
            obstacles.RemoveAt(0);
        }

        for (int i = 0; i < maxObstacleCount; i++)
        {
            CreateNextObstacle();
        }
    }
}