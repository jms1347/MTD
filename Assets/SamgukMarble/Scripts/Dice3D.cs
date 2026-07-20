using System.Collections;
using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 3D 주사위 투척 및 회전 연출. 1~6 결과 반환.
    /// </summary>
    public class Dice3D : MonoBehaviour
    {
        public Transform DiceTransform;
        public float RollDuration = 1.0f;
        public float ThrowHeight = 2.2f;

        Renderer _renderer;
        Vector3 _restPos;
        bool _ready;

        public void EnsureVisual(Vector3 worldPos)
        {
            if (DiceTransform != null)
            {
                _restPos = worldPos;
                DiceTransform.position = worldPos;
                return;
            }

            var dice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dice.name = "DiceCube";
            dice.transform.SetParent(transform, false);
            dice.transform.position = worldPos;
            dice.transform.localScale = Vector3.one * 0.55f;
            _renderer = dice.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                   ?? Shader.Find("Standard"));
            mat.color = new Color(0.95f, 0.95f, 0.98f);
            _renderer.material = mat;
            DiceTransform = dice.transform;
            _restPos = worldPos;
            _ready = true;
        }

        public IEnumerator Roll(System.Action<int> onResult)
        {
            if (!_ready && DiceTransform == null)
                EnsureVisual(transform.position);

            int result = Random.Range(1, 7);
            Vector3 start = _restPos;
            float elapsed = 0f;
            float dur = Mathf.Max(0.3f, RollDuration);

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float arc = 4f * ThrowHeight * t * (1f - t);
                DiceTransform.position = start + Vector3.up * arc;
                DiceTransform.Rotate(Random.Range(400f, 700f) * Time.deltaTime,
                    Random.Range(400f, 700f) * Time.deltaTime,
                    Random.Range(400f, 700f) * Time.deltaTime, Space.World);
                yield return null;
            }

            DiceTransform.position = _restPos;
            DiceTransform.rotation = FaceUpRotation(result);
            TintByResult(result);
            onResult?.Invoke(result);
        }

        /// <summary>
        /// 두 주사위 합 (공성전용).
        /// </summary>
        public IEnumerator RollTwo(System.Action<int, int, int> onResult)
        {
            int a = 0, b = 0;
            yield return Roll(r => a = r);
            yield return new WaitForSeconds(0.15f);
            yield return Roll(r => b = r);
            onResult?.Invoke(a, b, a + b);
        }

        static Quaternion FaceUpRotation(int value)
        {
            // 단순화: 결과값에 따라 다른 오일러로 "면"을 표현
            switch (value)
            {
                case 1: return Quaternion.Euler(0f, 0f, 0f);
                case 2: return Quaternion.Euler(90f, 0f, 0f);
                case 3: return Quaternion.Euler(0f, 0f, 90f);
                case 4: return Quaternion.Euler(0f, 0f, -90f);
                case 5: return Quaternion.Euler(-90f, 0f, 0f);
                default: return Quaternion.Euler(180f, 0f, 0f);
            }
        }

        void TintByResult(int result)
        {
            if (_renderer == null) _renderer = DiceTransform.GetComponent<Renderer>();
            if (_renderer == null) return;
            float v = 0.85f + result * 0.02f;
            _renderer.material.color = new Color(v, v, 0.95f);
        }
    }
}
