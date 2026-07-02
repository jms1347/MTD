using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DefenseMapLayout", menuName = "UkDefense/Map Layout")]
public class DefenseMapLayout : ScriptableObject
{
    [Header("그리드")]
    public int width = 48;
    public int height = 48;
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [Header("주요 셀")]
    public Vector2Int nexusCell = new(24, 24);
    public Vector2Int playerSpawnCell = new(26, 22);
    public Vector2Int farmGateCell = new(27, 23);

    [Header("연동")]
    public DefenseTowerLayout towerLayout;

    [Header("레인")]
    [Tooltip("켜면 플레이 시 자동 레인을 타일에 덮어씁니다. 끄면 에디터에서 칠한 타일을 그대로 사용합니다.")]
    public bool autoGenerateLanes;

    [SerializeField] private DefenseMapTileType[] tiles = Array.Empty<DefenseMapTileType>();

    public DefenseMapTileType GetTile(Vector2Int cell)
    {
        if (!IsInside(cell))
            return DefenseMapTileType.Grass;

        return tiles[ToIndex(cell)];
    }

    public void SetTile(Vector2Int cell, DefenseMapTileType type)
    {
        if (!IsInside(cell))
            return;

        EnsureTiles();
        tiles[ToIndex(cell)] = type;
    }

    public void EnsureTiles()
    {
        int size = width * height;
        if (tiles == null || tiles.Length != size)
        {
            var next = new DefenseMapTileType[size];
            if (tiles != null)
            {
                int copy = Mathf.Min(tiles.Length, size);
                Array.Copy(tiles, next, copy);
            }

            for (int i = 0; i < next.Length; i++)
            {
                if (next[i] == 0 && tiles == null)
                    next[i] = DefenseMapTileType.Grass;
            }

            tiles = next;
        }
    }

    public void Fill(DefenseMapTileType type)
    {
        EnsureTiles();
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = type;
    }

    public Vector3 GetNexusWorld() => DefenseMapGrid.CellToWorld(this, nexusCell);

    public Vector3 GetPlayerSpawnWorld() => DefenseMapGrid.CellToWorld(this, playerSpawnCell);

    public Vector3 GetFarmGateWorld() => DefenseMapGrid.CellToWorld(this, farmGateCell);

    public float MapHalfExtent => DefenseMapGrid.GetMapHalfExtent(this);

    public void SyncTowerLayout()
    {
        if (towerLayout == null)
            return;

        towerLayout.arenaCenter = origin;
    }

    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    public int ToIndex(Vector2Int cell) => cell.x + cell.y * width;
}
