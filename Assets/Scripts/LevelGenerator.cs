using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Manual Level")]
    [SerializeField] private GameObject manualLevel;

    [Header("Level Prefabs")]
    [SerializeField] private GameObject outsideCorner;
    [SerializeField] private GameObject outsideWall;
    [SerializeField] private GameObject insideCorner;
    [SerializeField] private GameObject insideWall;
    [SerializeField] private GameObject standardPellet;
    [SerializeField] private GameObject powerPellet;
    [SerializeField] private GameObject tJunction;
    [SerializeField] private GameObject ghostExitWall;

    [Header("Camera")]
    [SerializeField] private float cameraPadding = 2f;

    private GameObject generatedLevel;
    private int[,] fullMap;

    // Top-left quadrant
    private int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };

    void Start()
    {
        // Remove manual level in Play mode
        if (manualLevel != null)
        {
            manualLevel.SetActive(false);
            Destroy(manualLevel);
        }

        generatedLevel = new GameObject("GeneratedLevel");

        generatedLevel.transform.position = Vector3.zero;
        generatedLevel.transform.rotation = Quaternion.identity;
        generatedLevel.transform.localScale = Vector3.one;

        BuildFullMap();
        GenerateFullLevel();
        FitCameraToLevel();
    }

    void BuildFullMap()
    {
        int sourceRows = levelMap.GetLength(0);
        int sourceColumns = levelMap.GetLength(1);

        int fullRows = sourceRows * 2 - 1;
        int fullColumns = sourceColumns * 2;

        fullMap = new int[fullRows, fullColumns];

        for (int row = 0; row < sourceRows; row++)
        {
            for (int column = 0; column < sourceColumns; column++)
            {
                int tileType = levelMap[row, column];

                int mirrorColumn =
                    fullColumns - 1 - column;

                // Top quadrants
                fullMap[row, column] = tileType;
                fullMap[row, mirrorColumn] = tileType;

                // Avoid duplicating the middle row
                if (row < sourceRows - 1)
                {
                    int mirrorRow =
                        fullRows - 1 - row;

                    fullMap[mirrorRow, column] = tileType;
                    fullMap[mirrorRow, mirrorColumn] = tileType;
                }
            }
        }
    }

    void GenerateFullLevel()
    {
        int rows = fullMap.GetLength(0);
        int columns = fullMap.GetLength(1);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int tileType = fullMap[row, column];

                if (tileType == 0)
                    continue;

                GameObject prefab = GetPrefab(tileType);

                if (prefab == null)
                    continue;

                GameObject tile = Instantiate(
                    prefab,
                    generatedLevel.transform
                );

                tile.name =
                    "Tile_" + row + "_" + column;

                tile.transform.localPosition =
                    new Vector3(
                        column,
                        -row,
                        0f
                    );

                float rotation =
                    GetRotation(
                        tileType,
                        row,
                        column
                    );

                tile.transform.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        rotation
                    );

                tile.transform.localScale =
                    Vector3.one;
            }
        }
    }

    GameObject GetPrefab(int type)
    {
        switch (type)
        {
            case 1:
                return outsideCorner;

            case 2:
                return outsideWall;

            case 3:
                return insideCorner;

            case 4:
                return insideWall;

            case 5:
                return standardPellet;

            case 6:
                return powerPellet;

            case 7:
                return tJunction;

            case 8:
                return ghostExitWall;

            default:
                return null;
        }
    }

    // Check whether a grid position is open
    bool IsOpen(int row, int column)
    {
        if (row < 0 ||
            row >= fullMap.GetLength(0) ||
            column < 0 ||
            column >= fullMap.GetLength(1))
        {
            return true;
        }

        int type = fullMap[row, column];

        return type == 0 ||
               type == 5 ||
               type == 6;
    }

    float GetRotation(
        int tileType,
        int row,
        int column
    )
    {
        if (tileType == 2 ||
            tileType == 4 ||
            tileType == 8)
        {
            return GetStraightRotation(
                tileType,
                row,
                column
            );
        }

        if (tileType == 1 ||
            tileType == 3)
        {
            return GetCornerRotation(
                tileType,
                row,
                column
            );
        }

        if (tileType == 7)
        {
            return GetTJunctionRotation(
                row,
                column
            );
        }

        return 0f;
    }

    float GetStraightRotation(
        int tileType,
        int row,
        int column
    )
    {
        int openUpDown = 0;
        int openLeftRight = 0;

        if (IsOpen(row - 1, column))
            openUpDown++;

        if (IsOpen(row + 1, column))
            openUpDown++;

        if (IsOpen(row, column - 1))
            openLeftRight++;

        if (IsOpen(row, column + 1))
            openLeftRight++;

        if (openUpDown > openLeftRight)
        {
            return 0f;
        }

        if (openLeftRight > openUpDown)
        {
            return 90f;
        }

        int horizontalScore =
            ScanForEndpoint(
                row,
                column,
                0,
                -1,
                tileType
            )
            +
            ScanForEndpoint(
                row,
                column,
                0,
                1,
                tileType
            );

        int verticalScore =
            ScanForEndpoint(
                row,
                column,
                -1,
                0,
                tileType
            )
            +
            ScanForEndpoint(
                row,
                column,
                1,
                0,
                tileType
            );

        if (horizontalScore > verticalScore)
        {
            return 0f;
        }

        if (verticalScore > horizontalScore)
        {
            return 90f;
        }

        int horizontalNeighbours = 0;
        int verticalNeighbours = 0;

        if (IsSameStraightFamily(
            row,
            column - 1,
            tileType))
        {
            horizontalNeighbours++;
        }

        if (IsSameStraightFamily(
            row,
            column + 1,
            tileType))
        {
            horizontalNeighbours++;
        }

        if (IsSameStraightFamily(
            row - 1,
            column,
            tileType))
        {
            verticalNeighbours++;
        }

        if (IsSameStraightFamily(
            row + 1,
            column,
            tileType))
        {
            verticalNeighbours++;
        }

        if (verticalNeighbours >
            horizontalNeighbours)
        {
            return 90f;
        }

        return 0f;
    }

    bool IsSameStraightFamily(
        int row,
        int column,
        int tileType
    )
    {
        if (row < 0 ||
            row >= fullMap.GetLength(0) ||
            column < 0 ||
            column >= fullMap.GetLength(1))
        {
            return false;
        }

        int other = fullMap[row, column];

        if (tileType == 2)
        {
            return other == 2;
        }

        if (tileType == 4 ||
            tileType == 8)
        {
            return other == 4 ||
                   other == 8;
        }

        return false;
    }

    int ScanForEndpoint(
        int row,
        int column,
        int rowDirection,
        int columnDirection,
        int tileType
    )
    {
        int currentRow =
            row + rowDirection;

        int currentColumn =
            column + columnDirection;

        while (
            currentRow >= 0 &&
            currentRow < fullMap.GetLength(0) &&
            currentColumn >= 0 &&
            currentColumn < fullMap.GetLength(1)
        )
        {
            int currentType =
                fullMap[
                    currentRow,
                    currentColumn
                ];

            if (tileType == 2 &&
                currentType == 2)
            {
                currentRow += rowDirection;
                currentColumn += columnDirection;
                continue;
            }

            if ((tileType == 4 ||
                 tileType == 8) &&
                (currentType == 4 ||
                 currentType == 8))
            {
                currentRow += rowDirection;
                currentColumn += columnDirection;
                continue;
            }

            if (tileType == 2)
            {
                if (currentType == 1 ||
                    currentType == 7)
                {
                    return 1;
                }

                return 0;
            }

            if (tileType == 4 ||
                tileType == 8)
            {
                if (currentType == 3 ||
                    currentType == 7 ||
                    currentType == 8)
                {
                    return 1;
                }

                return 0;
            }
        }

        return 0;
    }

    float GetCornerRotation(
        int tileType,
        int row,
        int column
    )
    {
        bool openUp =
            IsOpen(row - 1, column);

        bool openDown =
            IsOpen(row + 1, column);

        bool openLeft =
            IsOpen(row, column - 1);

        bool openRight =
            IsOpen(row, column + 1);

        // Default corner is ┌
        if (openUp && openLeft)
        {
            return 0f;
        }

        if (openDown && openLeft)
        {
            return 90f;
        }

        if (openDown && openRight)
        {
            return 180f;
        }

        if (openUp && openRight)
        {
            return 270f;
        }

        bool connectionUp =
            StraightNeighbourConnects(
                row - 1,
                column,
                true
            );

        bool connectionDown =
            StraightNeighbourConnects(
                row + 1,
                column,
                true
            );

        bool connectionLeft =
            StraightNeighbourConnects(
                row,
                column - 1,
                false
            );

        bool connectionRight =
            StraightNeighbourConnects(
                row,
                column + 1,
                false
            );

        if (connectionRight &&
            connectionDown)
        {
            return 0f;
        }

        if (connectionRight &&
            connectionUp)
        {
            return 90f;
        }

        if (connectionLeft &&
            connectionUp)
        {
            return 180f;
        }

        if (connectionLeft &&
            connectionDown)
        {
            return 270f;
        }

        bool upperLeft =
            IsOpen(row - 1, column - 1);

        bool upperRight =
            IsOpen(row - 1, column + 1);

        bool lowerLeft =
            IsOpen(row + 1, column - 1);

        bool lowerRight =
            IsOpen(row + 1, column + 1);

        if (upperLeft)
            return 0f;

        if (lowerLeft)
            return 90f;

        if (lowerRight)
            return 180f;

        if (upperRight)
            return 270f;

        return 0f;
    }

    bool StraightNeighbourConnects(
        int row,
        int column,
        bool needsVertical
    )
    {
        if (row < 0 ||
            row >= fullMap.GetLength(0) ||
            column < 0 ||
            column >= fullMap.GetLength(1))
        {
            return false;
        }

        int type = fullMap[row, column];

        if (type != 2 &&
            type != 4 &&
            type != 8)
        {
            return false;
        }

        float rotation =
            GetStraightRotation(
                type,
                row,
                column
            );

        bool vertical =
            Mathf.Approximately(
                rotation,
                90f
            );

        if (needsVertical)
        {
            return vertical;
        }

        return !vertical;
    }

    float GetTJunctionRotation(
        int row,
        int column
    )
    {
        bool openUp =
            IsOpen(row - 1, column);

        bool openDown =
            IsOpen(row + 1, column);

        bool openLeft =
            IsOpen(row, column - 1);

        bool openRight =
            IsOpen(row, column + 1);

        // Default T junction is ┬
        if (openUp)
        {
            return 0f;
        }

        if (openLeft)
        {
            return 90f;
        }

        if (openDown)
        {
            return 180f;
        }

        if (openRight)
        {
            return 270f;
        }

        return 0f;
    }

    void FitCameraToLevel()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        int rows =
            fullMap.GetLength(0);

        int columns =
            fullMap.GetLength(1);

        float centreX =
            (columns - 1) / 2f;

        float centreY =
            -(rows - 1) / 2f;

        mainCamera.transform.position =
            new Vector3(
                centreX,
                centreY,
                mainCamera.transform.position.z
            );

        mainCamera.orthographic = true;

        float halfHeight =
            rows / 2f +
            cameraPadding;

        float halfWidth =
            columns / 2f +
            cameraPadding;

        float sizeFromWidth =
            halfWidth /
            mainCamera.aspect;

        mainCamera.orthographicSize =
            Mathf.Max(
                halfHeight,
                sizeFromWidth
            );
    }
}