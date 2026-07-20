using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 100개 칸의 데이터 및 최대 2개 건물 3D Mesh 관리.
    /// </summary>
    public class Tile3D : MonoBehaviour
    {
        public const int MaxBuildings = 2;

        [Header("Data")]
        public int TileId;
        public string TileName;
        public TileType Type;
        public ColorGroup ColorGroup;
        public int BasePrice;
        public int BaseToll;

        [Header("Ownership")]
        public Player3D Owner;
        public BuildingType[] Buildings = new BuildingType[MaxBuildings];

        [Header("Visual")]
        public Transform PieceAnchor;
        public Transform BuildingSlot0;
        public Transform BuildingSlot1;
        public Renderer TileRenderer;

        GameObject[] _buildingVisuals = new GameObject[MaxBuildings];

        public bool CanOwn => Type == TileType.Castle || Type == TileType.Gate || Type == TileType.Special || Type == TileType.Throne;
        public bool IsPurchasable => CanOwn && BasePrice > 0;
        public bool HasLandmark => HasBuilding(BuildingType.Landmark);
        public bool IsFull => GetBuildingCount() >= MaxBuildings;

        public int GetBuildingCount()
        {
            int count = 0;
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (Buildings[i] != BuildingType.None) count++;
            }
            return count;
        }

        public bool HasBuilding(BuildingType type)
        {
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (Buildings[i] == type) return true;
            }
            return false;
        }

        public int FreeSlotIndex()
        {
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (Buildings[i] == BuildingType.None) return i;
            }
            return -1;
        }

        public void Setup(TileDatabase.TileDef def)
        {
            TileId = def.Id;
            TileName = def.Name;
            Type = def.Type;
            ColorGroup = def.Group;
            BasePrice = def.BasePrice;
            BaseToll = def.BaseToll;
            name = $"Tile_{def.Id:D3}_{def.Name}";
            ApplyColor();
            EnsureAnchors();
        }

        public void EnsureAnchors()
        {
            if (PieceAnchor == null)
            {
                var go = new GameObject("PieceAnchor");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                PieceAnchor = go.transform;
            }

            if (BuildingSlot0 == null)
            {
                var s0 = new GameObject("BuildingSlot0");
                s0.transform.SetParent(transform, false);
                s0.transform.localPosition = new Vector3(-0.28f, 0.55f, 0.28f);
                BuildingSlot0 = s0.transform;
            }

            if (BuildingSlot1 == null)
            {
                var s1 = new GameObject("BuildingSlot1");
                s1.transform.SetParent(transform, false);
                s1.transform.localPosition = new Vector3(0.28f, 0.55f, 0.28f);
                BuildingSlot1 = s1.transform;
            }
        }

        public Transform GetBuildingSlot(int index)
        {
            return index == 0 ? BuildingSlot0 : BuildingSlot1;
        }

        public void ApplyColor()
        {
            if (TileRenderer == null) TileRenderer = GetComponentInChildren<Renderer>();
            if (TileRenderer == null) return;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                   ?? Shader.Find("Standard"));
            mat.color = SamgukColors.Get(ColorGroup);

            // 특수 칸 강조
            if (Type == TileType.Start || Type == TileType.Throne)
                mat.color = Color.Lerp(mat.color, Color.white, 0.35f);
            else if (Type == TileType.Treasury)
                mat.color = Color.Lerp(mat.color, new Color(1f, 0.85f, 0.2f), 0.45f);
            else if (Type == TileType.Crossroad)
                mat.color = Color.Lerp(mat.color, Color.white, 0.25f);

            TileRenderer.material = mat;
        }

        public void SetBuildingVisual(int slot, GameObject visual)
        {
            if (slot < 0 || slot >= MaxBuildings) return;
            if (_buildingVisuals[slot] != null) Destroy(_buildingVisuals[slot]);
            _buildingVisuals[slot] = visual;
            if (visual != null)
            {
                visual.transform.SetParent(GetBuildingSlot(slot), false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
        }

        public void ClearBuildingsVisual()
        {
            for (int i = 0; i < MaxBuildings; i++)
            {
                if (_buildingVisuals[i] != null)
                {
                    Destroy(_buildingVisuals[i]);
                    _buildingVisuals[i] = null;
                }
                Buildings[i] = BuildingType.None;
            }
        }

        public Vector3 GetStandPosition(int playerIndex, int playerCount)
        {
            EnsureAnchors();
            float angle = playerCount <= 1 ? 0f : (360f / playerCount) * playerIndex;
            float radius = playerCount <= 1 ? 0f : 0.22f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(radius, 0f, 0f);
            return PieceAnchor.position + offset;
        }
    }
}
