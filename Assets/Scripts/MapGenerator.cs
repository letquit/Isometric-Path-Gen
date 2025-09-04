using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图生成器类，用于生成等距视图下的地图瓦片和路径。
/// 该类负责初始化地图网格、生成路径、处理用户输入（如重新生成地图）以及管理路径生成的逻辑流程。
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int mapWidth, mapHeight;

    [SerializeField] private GameObject tileReference;
    [SerializeField] private Sprite emptyTile, downPath, leftRight, leftDown, rightDown, downLeft, downRight;

    private int curX;
    private int curY;
    private Sprite spriteToUse;
    private bool forceDirectionChange = false;

    private bool continueLeft = false;
    private bool continueRight = false;
    private int currentCount = 0;

    private enum CurrentDirection
    {
        LEFT,
        RIGHT,
        DOWN,
        UP
    };
    private CurrentDirection curDirection = CurrentDirection.DOWN;

    /// <summary>
    /// 瓦片数据结构，包含瓦片的变换组件、精灵渲染器和标识ID。
    /// 用于存储地图中每个瓦片的状态和引用，便于后续更新和渲染控制。
    /// </summary>
    public struct TileData{
        public Transform transform;
        public SpriteRenderer spriteRenderer;
        public int tileID;
    }

    TileData[,] tileData;

    /// <summary>
    /// 初始化地图数据并开始生成地图。
    /// 在Awake阶段创建TileData二维数组，并调用GenerateMap方法生成地图网格。
    /// </summary>
    void Awake()
    {
        tileData = new TileData[mapWidth, mapHeight];
        GenerateMap();
    }

    /// <summary>
    /// 生成地图瓦片网格，并启动协程生成路径。
    /// 遍历所有地图坐标，根据等距视图的偏移公式计算每个瓦片的位置，
    /// 实例化瓦片对象并设置其渲染层级以确保正确的视觉顺序。
    /// 最后启动GeneratePath协程来逐步生成路径。
    /// </summary>
    void GenerateMap()
    {
        for (int x = mapWidth - 1; x >= 0; x--)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float xOffset = (x + y) / 2f;
                float yOffset = (x - y) / 4f;
                GameObject newTile = Instantiate(tileReference, new Vector2(xOffset, yOffset), Quaternion.identity);
                tileData[x, y].spriteRenderer = newTile.GetComponent<SpriteRenderer>();
                tileData[x, y].tileID = 0;
                tileData[x, y].spriteRenderer.sprite = emptyTile;
                tileData[x, y].transform = newTile.transform;
                
                // 这段代码为每个瓦片设置了明确的渲染层级
                // sortingOrder = y 确保了Y坐标越大的瓦片渲染顺序越靠前，符合等距视图的渲染逻辑
                // URP对渲染顺序更加严格，需要明确指定sorting layer才能正确渲染
                tileData[x, y].spriteRenderer.sortingLayerName = "Default";
                tileData[x, y].spriteRenderer.sortingOrder = y;
            }
        }
        StartCoroutine(GeneratePath());
    }

    /// <summary>
    /// 检测空格键输入以重新生成地图。
    /// 当检测到用户按下空格键时，调用RegenerateMap方法重置地图状态并重新生成路径。
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RegenerateMap();
        }
    }

    /// <summary>
    /// 重置地图瓦片状态并重新生成路径。
    /// 停止当前所有协程，将所有瓦片恢复为初始状态（空瓦片），然后重新启动GeneratePath协程。
    /// </summary>
    void RegenerateMap()
    {
        StopAllCoroutines();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float xOffset = (x + y) / 2f;
                float yOffset = (x - y) / 4f;
                tileData[x, y].spriteRenderer.sprite = emptyTile;
                tileData[x, y].tileID = 0;
                tileData[x, y].transform.position = new Vector2(xOffset, yOffset);
            }
        }
        StartCoroutine(GeneratePath());
    }

    /// <summary>
    /// 协程方法，逐步生成路径。
    /// 从随机的X坐标开始，沿着Y轴方向向下移动，通过CheckCurrentDirections和ChooseDirection方法
    /// 控制路径的走向，并在每一步更新地图瓦片的显示状态。
    /// </summary>
    /// <returns>IEnumerator用于协程控制</returns>
    IEnumerator GeneratePath()
    {
        curX = Random.Range(0, mapWidth);
        curY = 0;

        spriteToUse = downPath;

        while (curY <= mapHeight - 1)
        {
             CheckCurrentDirections();
             ChooseDirection();

            if (curY <= mapHeight - 1)
            {
                UpdateMap(curX, curY, spriteToUse);
            }

            if (curDirection == CurrentDirection.DOWN)
            {
                curY++;
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// 根据当前方向检查并更新当前位置。
    /// 根据当前的方向（LEFT、RIGHT、UP、DOWN）判断是否可以继续移动，
    /// 如果可以则更新curX或curY的值；如果不能移动，则设置forceDirectionChange标志位，
    /// 并调整当前瓦片的位置以避免渲染冲突。
    /// </summary>
    private void CheckCurrentDirections()
    {
        if (curDirection == CurrentDirection.LEFT && curX - 1 >= 0 && tileData[curX - 1, curY].tileID == 0)
        {
            curX--;
        }
        else if (curDirection == CurrentDirection.RIGHT && curX + 1 <= mapWidth - 1 && tileData[curX + 1, curY].tileID == 0)
        {
            curX++;
        }
        else if (curDirection == CurrentDirection.UP && curY - 1 >= 0 && tileData[curX, curY - 1].tileID == 0)
        {
            if (continueLeft && tileData[curX-1, curY-1].tileID == 0 ||
            continueRight && tileData[curX+1, curY-1].tileID == 0 )
            {
                curY--;
            }
            else
            {
                forceDirectionChange = true;
                tileData[curX, curY].transform.position = new Vector2(tileData[curX, curY].transform.position.x, tileData[curX, curY].transform.position.y - 0.3f);
            }
        }
        else if (curDirection != CurrentDirection.DOWN)
        {
            forceDirectionChange = true;
            tileData[curX,curY].transform.position = new Vector2(tileData[curX, curY].transform.position.x, tileData[curX, curY].transform.position.y - 0.3f);
        }
    }

    /// <summary>
    /// 决定是否改变当前移动方向。
    /// 根据当前连续移动的步数和随机概率决定是否改变方向。
    /// 如果满足改变方向的条件，则调用ChangeDirection方法进行方向切换。
    /// </summary>
    private void ChooseDirection()
    {
        if (currentCount < 3 && !forceDirectionChange)
        {
            currentCount++;
        }
        else
        {
            bool chanceToChange = Mathf.FloorToInt(Random.value * 1.99f) == 0;

            if (chanceToChange || forceDirectionChange || currentCount > 7)
            {
                currentCount = 0;
                forceDirectionChange = false;
                ChangeDirection();
            }

            currentCount++;
        }
    }

    /// <summary>
    /// 改变当前移动方向，并设置相应的精灵。
    /// 根据当前方向和周围瓦片的状态，随机选择一个新的方向进行移动。
    /// 同时根据方向变化设置合适的路径精灵（如转弯、直行等）。
    /// </summary>
    private void ChangeDirection()
    {
        int dirValue = Mathf.FloorToInt(Random.value * 2.99f);

        if (dirValue == 0 && curDirection == CurrentDirection.LEFT && curX - 1 > 0
        || dirValue == 0 && curDirection == CurrentDirection.RIGHT && curX + 1 < mapWidth - 1)
        {
            if (curY - 1 >= 0)
            {
                if (tileData[curX, curY - 1].tileID == 0 &&
                tileData[curX - 1, curY - 1].tileID == 0 &&
                tileData[curX + 1, curY - 1].tileID == 0)
                {
                    GoUp();
                    return;
                }
            }
        }

        if (curDirection == CurrentDirection.LEFT)
        {
            UpdateMap(curX, curY, leftDown);
        }
        else if (curDirection == CurrentDirection.RIGHT)
        {
            UpdateMap(curX, curY, rightDown);
        }

        if (curDirection == CurrentDirection.LEFT || curDirection == CurrentDirection.RIGHT)
        {
            curY++;
            spriteToUse = downPath;
            curDirection = CurrentDirection.DOWN;
            return;
        }

        if (curX - 1 > 0 && curX + 1 < mapWidth - 1 || continueLeft || continueRight)
        {
            if (dirValue == 1 && !continueRight || continueLeft)
            {
                if (tileData[curX - 1, curY].tileID == 0)
                {
                    if (continueLeft)
                    {
                        spriteToUse = rightDown;
                        continueLeft = false;
                    }
                    else
                    {
                        spriteToUse = downLeft;
                    }
                    curDirection = CurrentDirection.LEFT;
                }
            }
            else
            {
                if (tileData[curX + 1, curY].tileID == 0)
                {
                    if (continueRight)
                    {
                        continueRight = false;
                        spriteToUse = leftDown;
                    }
                    else
                    {
                        spriteToUse = downRight;
                    }
                    curDirection = CurrentDirection.RIGHT;
                }
            }
        }
        else if (curX - 1 > 0)
        {
            spriteToUse = downLeft;
            curDirection = CurrentDirection.LEFT;
        }
        else if (curX + 1 < mapWidth - 1)
        {
            spriteToUse = downRight;
            curDirection = CurrentDirection.RIGHT;
        }

        if (curDirection == CurrentDirection.LEFT)
        {
            GoLeft();
        }
        else if (curDirection == CurrentDirection.RIGHT)
        {
            GoRight();
        }
    }


    /// <summary>
    /// 向上移动并更新相关状态。
    /// 设置当前方向为UP，并根据之前的方向设置continueLeft或continueRight标志，
    /// 以便在后续步骤中继续向左或向右移动。
    /// </summary>
    private void GoUp()
    {
        if (curDirection == CurrentDirection.LEFT)
        {
            UpdateMap(curX, curY, downRight);
            continueLeft = true;
        }
        else
        {
            UpdateMap(curX, curY, downLeft);
            continueRight = true;
        }
        curDirection = CurrentDirection.UP;
        curY--;
        spriteToUse = downPath;
    }

    /// <summary>
    /// 向左移动并更新相关状态。
    /// 更新当前位置的瓦片显示，并将当前方向设置为LEFT。
    /// </summary>
    private void GoLeft()
    {
        UpdateMap(curX, curY, spriteToUse);
        curX--;
        spriteToUse = leftRight;
    }

    /// <summary>
    /// 向右移动并更新相关状态。
    /// 更新当前位置的瓦片显示，并将当前方向设置为RIGHT。
    /// </summary>
    private void GoRight()
    {
        UpdateMap(curX, curY, spriteToUse);
        curX++;
        spriteToUse = leftRight;
    }

    /// <summary>
    /// 更新指定位置的地图瓦片精灵和状态。
    /// 将指定坐标的瓦片标记为已使用（tileID=1），并设置相应的精灵。
    /// 同时调整瓦片的位置以确保正确的渲染顺序。
    /// </summary>
    /// <param name="mapX">地图X坐标</param>
    /// <param name="mapY">地图Y坐标</param>
    /// <param name="spriteToUse">要使用的精灵</param>
    private void UpdateMap(int mapX, int mapY, Sprite spriteToUse)
    {
        // 手动调整坐标的方式在URP中可能导致渲染顺序问题(没有明确sorting layer的sprite在URP中可能会出现渲染顺序混乱)，
        // URP更加依赖正确的sorting layer和order in layer设置
        Vector3 newPosition = tileData[mapX, mapY].transform.position;
        newPosition.z = -0.1f;
        
        tileData[mapX, mapY].transform.position = new Vector2(tileData[mapX, mapY].transform.position.x, tileData[mapX, mapY].transform.position.y + 0.3f);
        tileData[mapX, mapY].tileID = 1;
        tileData[mapX, mapY].spriteRenderer.sprite = spriteToUse;
    }

}
