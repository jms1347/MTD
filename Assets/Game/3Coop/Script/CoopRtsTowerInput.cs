using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CoopPlayerTowerUnit))]
public class CoopRtsTowerInput : MonoBehaviour
{
    [SerializeField] private float clickDragThreshold = 8f;

    private CoopPlayerTowerUnit towerUnit;
    private Vector2 leftMouseDownScreen;
    private Vector2 rightMouseDownScreen;
    private bool leftMouseDown;
    private bool rightMouseDown;
    private bool attackMoveMode;
    private bool skillTargetingMode;

    private void Awake()
    {
        towerUnit = GetComponent<CoopPlayerTowerUnit>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            attackMoveMode = true;
        if (Input.GetKeyUp(KeyCode.A))
            attackMoveMode = false;

        if (Input.GetKeyDown(KeyCode.Q))
            BeginSkillCast();

        if (skillTargetingMode && Input.GetKeyDown(KeyCode.Escape))
            skillTargetingMode = false;

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
        {
            leftMouseDown = true;
            leftMouseDownScreen = Input.mousePosition;
        }

        if (Input.GetMouseButtonDown(1) && !IsPointerOverUi())
        {
            rightMouseDown = true;
            rightMouseDownScreen = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (rightMouseDown
                && !IsPointerOverUi()
                && !DefenseCameraControlManager.DidPanThisGesture
                && Vector2.Distance(rightMouseDownScreen, Input.mousePosition) <= clickDragThreshold)
            {
                if (TryPickEnemy(Input.mousePosition, out var enemyId))
                    towerUnit.IssueAttackTarget(enemyId);
                else if (TryRaycastWorld(Input.mousePosition, out var point))
                {
                    towerUnit.IssueMove(point);
                    PlayerMoveMarker.Show(point);
                }
            }

            rightMouseDown = false;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (skillTargetingMode
                && leftMouseDown
                && !IsPointerOverUi()
                && !DefenseCameraControlManager.DidPanThisGesture
                && Vector2.Distance(leftMouseDownScreen, Input.mousePosition) <= clickDragThreshold
                && TryRaycastWorld(Input.mousePosition, out var skillPoint))
            {
                towerUnit.IssueSkill(skillPoint);
                skillTargetingMode = false;
            }
            else if (attackMoveMode
                && leftMouseDown
                && !IsPointerOverUi()
                && !DefenseCameraControlManager.DidPanThisGesture
                && Vector2.Distance(leftMouseDownScreen, Input.mousePosition) <= clickDragThreshold)
            {
                if (TryPickEnemy(Input.mousePosition, out var enemyId))
                    towerUnit.IssueAttackTarget(enemyId);
                else if (TryRaycastWorld(Input.mousePosition, out var point))
                {
                    towerUnit.IssueAttackMove(point);
                    PlayerMoveMarker.Show(point);
                }
            }

            leftMouseDown = false;
        }
    }

    private static bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        if (!EventSystem.current.IsPointerOverGameObject())
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        for (var i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }

    public void BeginSkillCast()
    {
        TryBeginSkillCast();
    }

    private void TryBeginSkillCast()
    {
        var session = CoopGameSession.Instance;
        if (session == null || !session.TryGetPlayer(towerUnit.PlayerId, out var player))
            return;

        if (string.IsNullOrEmpty(player.skillId))
            return;

        if (CoopSkillCatalog.RequiresGroundTarget(player.skillId))
        {
            skillTargetingMode = true;
            return;
        }

        towerUnit.IssueSkillAtSelf();
    }

    private static bool TryRaycastWorld(Vector2 screenPosition, out Vector3 point)
    {
        point = default;
        if (Camera.main == null)
            return false;

        var ray = Camera.main.ScreenPointToRay(screenPosition);
        var hits = Physics.RaycastAll(ray, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out var distance))
                return false;

            point = ray.GetPoint(distance);
            point.y = 0f;
            return true;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (var i = 0; i < hits.Length; i++)
        {
            var collider = hits[i].collider;
            if (collider == null)
                continue;

            if (collider.GetComponentInParent<CoopPlayerTowerUnit>() != null)
                continue;

            if (collider.CompareTag("Tower"))
                continue;

            point = hits[i].point;
            point.y = 0f;
            return true;
        }

        var fallbackPlane = new Plane(Vector3.up, Vector3.zero);
        if (!fallbackPlane.Raycast(ray, out var fallbackDistance))
            return false;

        point = ray.GetPoint(fallbackDistance);
        point.y = 0f;
        return true;
    }

    private static bool TryPickEnemy(Vector2 screenPosition, out int enemyId)
    {
        enemyId = -1;
        if (Camera.main == null)
            return false;

        var ray = Camera.main.ScreenPointToRay(screenPosition);
        var hits = Physics.RaycastAll(ray, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (var i = 0; i < hits.Length; i++)
        {
            if (!DefenseEnemyQuery.TryGetEnemyRoot(hits[i].collider, out var enemyRoot))
                continue;

            if (!DefenseEnemyQuery.IsLivingEnemy(enemyRoot))
                continue;

            var synced = enemyRoot.GetComponent<CoopSyncedMonster>();
            if (synced != null)
            {
                enemyId = synced.NetworkId;
                return true;
            }

            var actor = enemyRoot.GetComponent<CoopEnemyActor>();
            if (actor != null)
            {
                enemyId = actor.NetworkId;
                return true;
            }
        }

        return false;
    }
}
