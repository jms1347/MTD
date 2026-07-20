using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 성당 최대 2개 건물 배치 및 통행료/수비 보정 계산.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Header("Build Costs")]
        public int BarracksCost = 100;
        public int TaxOfficeCost = 150;
        public int WatchtowerCost = 120;
        public int LandmarkCost = 300;

        void Awake()
        {
            Instance = this;
        }

        public int GetBuildCost(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Barracks: return BarracksCost;
                case BuildingType.TaxOffice: return TaxOfficeCost;
                case BuildingType.Watchtower: return WatchtowerCost;
                case BuildingType.Landmark: return LandmarkCost;
                default: return 0;
            }
        }

        /// <summary>
        /// 통행료 = BaseToll * (건물 배수 합산). 배수 기본 1.0.
        /// 병영 1.5, 조세청 2.0, 랜드마크 3.0 — 중첩 곱연산.
        /// </summary>
        public int CalculateToll(Tile3D tile)
        {
            if (tile == null) return 0;
            float multiplier = 1f;
            for (int i = 0; i < Tile3D.MaxBuildings; i++)
            {
                switch (tile.Buildings[i])
                {
                    case BuildingType.Barracks: multiplier *= 1.5f; break;
                    case BuildingType.TaxOffice: multiplier *= 2.0f; break;
                    case BuildingType.Landmark: multiplier *= 3.0f; break;
                }
            }
            return Mathf.RoundToInt(tile.BaseToll * multiplier);
        }

        /// <summary>
        /// 기본 수비 +1, 망루 +1, 랜드마크 +1.
        /// </summary>
        public int GetDefenseBonus(Tile3D tile)
        {
            int bonus = 1; // 기본 수비 보정
            if (tile == null) return bonus;
            for (int i = 0; i < Tile3D.MaxBuildings; i++)
            {
                if (tile.Buildings[i] == BuildingType.Watchtower) bonus += 1;
                if (tile.Buildings[i] == BuildingType.Landmark) bonus += 1;
            }
            return bonus;
        }

        public bool CanBuild(Tile3D tile, BuildingType type, Player3D player, out string reason)
        {
            reason = null;
            if (tile == null || player == null)
            {
                reason = "잘못된 대상";
                return false;
            }
            if (tile.Owner != player)
            {
                reason = "소유한 성만 건설 가능";
                return false;
            }
            if (!tile.CanOwn)
            {
                reason = "건설 불가 칸";
                return false;
            }
            if (tile.HasLandmark)
            {
                reason = "랜드마크 성은 추가 건설 불가";
                return false;
            }
            if (tile.IsFull)
            {
                reason = "건물 슬롯이 가득 참 (최대 2)";
                return false;
            }
            if (type == BuildingType.None)
            {
                reason = "건물 종류 없음";
                return false;
            }
            if (tile.HasBuilding(type))
            {
                reason = "동일 건물이 이미 있음";
                return false;
            }
            int cost = GetBuildCost(type);
            if (player.Gold < cost)
            {
                reason = "금화 부족";
                return false;
            }
            return true;
        }

        public bool TryBuild(Tile3D tile, BuildingType type, Player3D player)
        {
            if (!CanBuild(tile, type, player, out _)) return false;
            int slot = tile.FreeSlotIndex();
            if (slot < 0) return false;

            int cost = GetBuildCost(type);
            player.TrySpend(cost);
            tile.Buildings[slot] = type;
            SpawnBuildingVisual(tile, slot, type);
            return true;
        }

        public void TransferOwnership(Tile3D tile, Player3D newOwner, bool keepBuildings)
        {
            if (tile == null) return;
            var old = tile.Owner;
            if (old != null) old.LoseTile(tile);

            if (!keepBuildings)
            {
                tile.ClearBuildingsVisual();
            }

            if (newOwner != null) newOwner.OwnTile(tile);
            else tile.Owner = null;
        }

        public void SpawnBuildingVisual(Tile3D tile, int slot, BuildingType type)
        {
            if (tile == null) return;
            tile.EnsureAnchors();
            var go = CreateBuildingMesh(type);
            tile.SetBuildingVisual(slot, go);
        }

        public GameObject CreateBuildingMesh(BuildingType type)
        {
            var root = new GameObject($"Building_{type}");

            switch (type)
            {
                case BuildingType.Barracks:
                    AddPrim(root, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.35f, 0.4f, 0.35f),
                        new Color(0.55f, 0.35f, 0.2f));
                    AddPrim(root, PrimitiveType.Cylinder, new Vector3(0f, 0.48f, 0f), new Vector3(0.12f, 0.12f, 0.12f),
                        new Color(0.7f, 0.2f, 0.15f));
                    break;

                case BuildingType.TaxOffice:
                    AddPrim(root, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.4f, 0.44f, 0.3f),
                        new Color(0.85f, 0.7f, 0.25f));
                    AddPrim(root, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(0.45f, 0.08f, 0.35f),
                        new Color(0.95f, 0.85f, 0.35f));
                    break;

                case BuildingType.Watchtower:
                    AddPrim(root, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.18f, 0.35f, 0.18f),
                        new Color(0.5f, 0.5f, 0.55f));
                    AddPrim(root, PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.28f, 0.12f, 0.28f),
                        new Color(0.4f, 0.4f, 0.45f));
                    break;

                case BuildingType.Landmark:
                    AddPrim(root, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.45f, 0.5f, 0.45f),
                        new Color(0.95f, 0.8f, 0.2f));
                    AddPrim(root, PrimitiveType.Sphere, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.28f,
                        new Color(1f, 0.9f, 0.4f));
                    AddPrim(root, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.08f, 0.15f, 0.08f),
                        new Color(0.9f, 0.2f, 0.2f));
                    break;
            }

            return root;
        }

        static void AddPrim(GameObject parent, PrimitiveType prim, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(prim);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                       ?? Shader.Find("Standard"));
                mat.color = color;
                r.material = mat;
            }
        }
    }
}
