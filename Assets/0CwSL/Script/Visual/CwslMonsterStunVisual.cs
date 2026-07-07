using UnityEngine;

/// <summary>몬스터 스턴 — 플레이어와 동일한 폭발 + 머리 위 별 연출.</summary>
public class CwslMonsterStunVisual : MonoBehaviour
{
    private Transform starsAnchor;
    private GameObject starsInstance;
    private float stunEndTime;
    private bool stunActive;

    public static CwslMonsterStunVisual Ensure(GameObject root)
    {
        if (root == null)
            return null;

        var visual = root.GetComponent<CwslMonsterStunVisual>();
        if (visual == null)
            visual = root.AddComponent<CwslMonsterStunVisual>();

        return visual;
    }

    public void PlayStun(Vector3 worldPosition, float durationSeconds)
    {
        if (durationSeconds <= 0f)
            return;

        stunActive = true;
        stunEndTime = Time.time + durationSeconds;
        CwslRammerStunFeedback.PlaySound(worldPosition);
        CwslVfxSpawner.SpawnRammerStunExplosion(worldPosition);
        EnsureStarsAnchor();
        EnsureStars();
    }

    public void EndStun()
    {
        stunActive = false;
        stunEndTime = 0f;
        ClearStars();
    }

    private void Update()
    {
        if (!stunActive)
            return;

        if (Time.time >= stunEndTime)
            EndStun();
    }

    private void EnsureStarsAnchor()
    {
        if (starsAnchor != null)
            return;

        var visual = transform.Find("Visual");
        var headY = 1.35f;
        if (visual != null)
        {
            var helm = visual.Find("Helm");
            if (helm == null)
                helm = visual.Find("HeadPivot");
            if (helm == null)
                helm = visual.Find("HorseRoot/RiderPivot/HeadPivot");

            if (helm != null)
            {
                starsAnchor = new GameObject("MonsterStunStarsAnchor").transform;
                starsAnchor.SetParent(helm, false);
                starsAnchor.localPosition = new Vector3(0f, 0.42f, 0f);
                return;
            }

            headY = visual.localPosition.y + 1.2f;
        }

        starsAnchor = new GameObject("MonsterStunStarsAnchor").transform;
        starsAnchor.SetParent(transform, false);
        starsAnchor.localPosition = new Vector3(0f, headY, 0f);
    }

    private void EnsureStars()
    {
        if (!stunActive || starsInstance != null)
            return;

        EnsureStarsAnchor();
        if (starsAnchor == null)
            return;

        starsInstance = CwslVfxSpawner.AttachRammerStunStars(starsAnchor);
    }

    private void ClearStars()
    {
        if (starsInstance == null)
            return;

        Destroy(starsInstance);
        starsInstance = null;
    }

    private void OnDestroy()
    {
        ClearStars();
    }
}
