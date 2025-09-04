using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int mapWidth, mapHeight;
    [SerializeField] private GameObject isometricTitle;

    private void Awake()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        for (int x = mapWidth; x >= 0; x--)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float xOffset = (x + y) / 2.0f;
                float yOffset = (x - y) / 4.0f;
                GameObject tile = Instantiate(isometricTitle, new Vector3(xOffset, yOffset, 0), Quaternion.identity);
            }
        }
    }
}
