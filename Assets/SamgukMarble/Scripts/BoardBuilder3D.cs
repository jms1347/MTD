using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 100칸 보드를 Primitive로 동적 생성. 외곽 1~80 + 중원 81~100.
    /// </summary>
    public class BoardBuilder3D : MonoBehaviour
    {
        public const int TileCount = 100;

        [Header("Layout")]
        public float OuterHalfSize = 12f;
        public float CenterHalfSize = 4.5f;
        public float TileSpacing = 1.15f;
        public float TileHeight = 0.25f;
        public Vector3 TileScale = new Vector3(1.0f, 0.25f, 1.0f);

        public Tile3D[] Tiles { get; private set; }

        public Transform BoardRoot { get; private set; }
        public Transform Ground { get; private set; }

        public void Build()
        {
            ClearExisting();
            BoardRoot = new GameObject("BoardRoot").transform;
            BoardRoot.SetParent(transform, false);

            BuildGround();
            Tiles = new Tile3D[TileCount + 1]; // 1-indexed

            for (int id = 1; id <= TileCount; id++)
            {
                var def = TileDatabase.Get(id);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(BoardRoot, false);
                go.transform.position = GetTileWorldPosition(id);
                go.transform.localScale = TileScale;

                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                var tile = go.AddComponent<Tile3D>();
                tile.TileRenderer = go.GetComponent<Renderer>();
                tile.Setup(def);
                Tiles[id] = tile;

                // 이름 라벨 (작은 큐브 위 TextMesh 대신 색상만 — 런타임 경량)
                AddNamePlate(tile, def.Name);
            }
        }

        void ClearExisting()
        {
            if (BoardRoot != null)
            {
                if (Application.isPlaying) Destroy(BoardRoot.gameObject);
                else DestroyImmediate(BoardRoot.gameObject);
                BoardRoot = null;
            }

            var existing = transform.Find("BoardRoot");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
        }

        void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(BoardRoot, false);
            ground.transform.localScale = new Vector3(4.5f, 1f, 4.5f);
            ground.transform.position = new Vector3(0f, -0.05f, 0f);
            var r = ground.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = SamgukColors.Ground;
            r.material = mat;
            Ground = ground.transform;
        }

        void AddNamePlate(Tile3D tile, string label)
        {
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "NamePlate";
            plate.transform.SetParent(tile.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0.55f, -0.35f);
            plate.transform.localScale = new Vector3(0.85f, 0.08f, 0.2f);
            var col = plate.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            var r = plate.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = Color.Lerp(SamgukColors.Get(tile.ColorGroup), Color.black, 0.35f);
            r.material = mat;

            // TextMesh는 빌트인 폰트 의존이 커서 생략. HUD에서 칸 이름 표시.
            tile.gameObject.name = $"Tile_{tile.TileId:D3}_{label}";
        }

        /// <summary>
        /// 외곽: 북(1-20) → 동(21-40) → 남(41-60) → 서(61-80)
        /// 중원: 안쪽 사각 경로 81-100
        /// </summary>
        public Vector3 GetTileWorldPosition(int id)
        {
            if (id < 1) id = 1;
            if (id > 100) id = 100;

            if (id <= 80)
                return OuterPosition(id);
            return CenterPosition(id);
        }

        Vector3 OuterPosition(int id)
        {
            // 각 변 20칸. 코너 포함 균등 배치.
            int side = (id - 1) / 20;       // 0N 1E 2S 3W
            int index = (id - 1) % 20;      // 0..19
            float t = index / 19f;          // 0..1
            float s = OuterHalfSize;

            switch (side)
            {
                case 0: // North: x -s → +s, z = +s
                    return new Vector3(Mathf.Lerp(-s, s, t), TileHeight, s);
                case 1: // East: x = +s, z +s → -s
                    return new Vector3(s, TileHeight, Mathf.Lerp(s, -s, t));
                case 2: // South: x +s → -s, z = -s
                    return new Vector3(Mathf.Lerp(s, -s, t), TileHeight, -s);
                default: // West: x = -s, z -s → +s
                    return new Vector3(-s, TileHeight, Mathf.Lerp(-s, s, t));
            }
        }

        Vector3 CenterPosition(int id)
        {
            // 81-100: 안쪽 사각 5칸×4변
            int local = id - 81; // 0..19
            int side = local / 5;
            int index = local % 5;
            float t = index / 4f;
            float s = CenterHalfSize;

            switch (side)
            {
                case 0: // N
                    return new Vector3(Mathf.Lerp(-s, s, t), TileHeight, s);
                case 1: // E
                    return new Vector3(s, TileHeight, Mathf.Lerp(s, -s, t));
                case 2: // S
                    return new Vector3(Mathf.Lerp(s, -s, t), TileHeight, -s);
                default: // W
                    return new Vector3(-s, TileHeight, Mathf.Lerp(-s, s, t));
            }
        }

        public Tile3D GetTile(int id)
        {
            if (Tiles == null || id < 1 || id > TileCount) return null;
            return Tiles[id];
        }
    }
}
