using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 3D 말 Primitive 생성, 1칸씩 점프 이동, 재산 및 소유 성 관리.
    /// </summary>
    public class Player3D : MonoBehaviour
    {
        public string PlayerName;
        public int PlayerIndex;
        public Color PieceColor = Color.white;
        public int Gold = 1500;
        public int TileIndex = 1; // 1~100
        public bool IsInExile;
        public int ExileTurnsLeft;
        public bool IsBankrupt;

        public readonly List<Tile3D> OwnedTiles = new List<Tile3D>();

        public Transform PieceRoot { get; private set; }
        public bool IsMoving { get; private set; }

        [Header("Jump")]
        public float JumpHeight = 1.1f;
        public float JumpDuration = 0.28f;

        public void Initialize(string playerName, int index, Color color, Vector3 startPos)
        {
            PlayerName = playerName;
            PlayerIndex = index;
            PieceColor = color;
            Gold = 1500;
            TileIndex = 1;
            BuildPieceVisual();
            transform.position = startPos;
        }

        void BuildPieceVisual()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            PieceRoot = new GameObject("PieceRoot").transform;
            PieceRoot.SetParent(transform, false);

            // 몸통 (큐브)
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(PieceRoot, false);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            ApplyColor(body, PieceColor);
            DestroyCollider(body);

            // 머리 (구체)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(PieceRoot, false);
            head.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            head.transform.localScale = Vector3.one * 0.28f;
            ApplyColor(head, Color.Lerp(PieceColor, Color.white, 0.25f));
            DestroyCollider(head);

            // 깃발 (실린더)
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flag.name = "Banner";
            flag.transform.SetParent(PieceRoot, false);
            flag.transform.localPosition = new Vector3(0.18f, 0.55f, 0f);
            flag.transform.localScale = new Vector3(0.06f, 0.28f, 0.06f);
            ApplyColor(flag, Color.Lerp(PieceColor, Color.black, 0.2f));
            DestroyCollider(flag);

            var flagTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagTop.name = "BannerCloth";
            flagTop.transform.SetParent(PieceRoot, false);
            flagTop.transform.localPosition = new Vector3(0.28f, 0.78f, 0f);
            flagTop.transform.localScale = new Vector3(0.18f, 0.12f, 0.04f);
            ApplyColor(flagTop, PieceColor);
            DestroyCollider(flagTop);
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                   ?? Shader.Find("Standard"));
            mat.color = color;
            r.material = mat;
        }

        static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        public void AddGold(int amount) => Gold += amount;

        public bool TrySpend(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public void OwnTile(Tile3D tile)
        {
            if (tile == null) return;
            if (!OwnedTiles.Contains(tile)) OwnedTiles.Add(tile);
            tile.Owner = this;
        }

        public void LoseTile(Tile3D tile)
        {
            if (tile == null) return;
            OwnedTiles.Remove(tile);
            if (tile.Owner == this) tile.Owner = null;
        }

        public int TotalAssetValue()
        {
            int sum = Gold;
            foreach (var t in OwnedTiles)
            {
                sum += t.BasePrice;
                sum += t.GetBuildingCount() * 50;
            }
            return sum;
        }

        /// <summary>
        /// 모두의 마블 스타일: 경로의 각 타일로 부드럽게 점프 이동.
        /// </summary>
        public IEnumerator JumpAlongPath(IList<Vector3> worldPositions, System.Action<int> onStepLanded = null)
        {
            if (worldPositions == null || worldPositions.Count == 0) yield break;
            IsMoving = true;

            for (int i = 0; i < worldPositions.Count; i++)
            {
                yield return JumpTo(worldPositions[i]);
                onStepLanded?.Invoke(i);
            }

            IsMoving = false;
        }

        public IEnumerator JumpTo(Vector3 target)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;
            float dur = Mathf.Max(0.05f, JumpDuration);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float eased = t * t * (3f - 2f * t);
                Vector3 pos = Vector3.Lerp(start, target, eased);
                float arc = 4f * JumpHeight * t * (1f - t);
                pos.y = Mathf.Lerp(start.y, target.y, eased) + arc;
                transform.position = pos;
                yield return null;
            }

            transform.position = target;
        }
    }
}
