using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SamgukMarble
{
    /// <summary>
    /// 턴 진행, 주사위, 공성전, 교차로 홀/짝 분기, 100칸 데이터 통합 컨트롤러.
    /// </summary>
    public class GameManager3D : MonoBehaviour
    {
        public static GameManager3D Instance { get; private set; }

        public enum TurnPhase
        {
            WaitingRoll,
            Moving,
            TileAction,
            Building,
            SiegeChoice,
            WaitingCard,
            TurnEnd
        }

        [Header("Refs")]
        public BoardBuilder3D Board;
        public Dice3D Dice;
        public BuildingManager Buildings;
        public TreasuryManager Treasury;
        public CardUI Cards;

        [Header("Players")]
        public int PlayerCount = 2;
        public string[] DefaultNames = { "유비", "관우", "장비", "조조" };
        public Color[] DefaultColors =
        {
            new Color(0.3f, 0.85f, 0.4f),
            new Color(0.9f, 0.25f, 0.25f),
            new Color(0.3f, 0.5f, 1f),
            new Color(0.7f, 0.35f, 0.9f)
        };

        [Header("Rules")]
        public int StartGold = 1500;
        public int PassStartBonus = 200;
        public int MarketBonus = 100;
        public int HospitalHealCost = 50;

        public List<Player3D> Players { get; private set; } = new List<Player3D>();
        public int CurrentPlayerIndex { get; private set; }
        public TurnPhase Phase { get; private set; } = TurnPhase.WaitingRoll;
        public int LastDice { get; private set; }
        public string StatusMessage { get; private set; } = "삼국마블 시작";
        public string LogMessage { get; private set; } = "";

        Player3D Current => Players.Count > 0 ? Players[CurrentPlayerIndex] : null;
        Tile3D PendingSiegeTile;
        bool _busy;
        bool _inCenterPath; // 현재 플레이어가 중원(81-100) 루프에 있는지 — 플레이어별 플래그로 관리

        readonly Dictionary<Player3D, bool> _centerFlags = new Dictionary<Player3D, bool>();

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            EnsureSystems();
            if (Board.Tiles == null || Board.Tiles.Length < 101)
                Board.Build();
            SetupCamera();
            SpawnPlayers();
            Phase = TurnPhase.WaitingRoll;
            SetStatus($"{Current.PlayerName}의 턴 — 주사위를 굴리세요 (Space)");
        }

        void EnsureSystems()
        {
            if (Board == null) Board = GetComponent<BoardBuilder3D>() ?? gameObject.AddComponent<BoardBuilder3D>();
            if (Buildings == null) Buildings = GetComponent<BuildingManager>() ?? gameObject.AddComponent<BuildingManager>();
            if (Treasury == null) Treasury = GetComponent<TreasuryManager>() ?? gameObject.AddComponent<TreasuryManager>();
            if (Cards == null) Cards = GetComponent<CardUI>() ?? gameObject.AddComponent<CardUI>();
            if (Dice == null)
            {
                var diceGo = new GameObject("Dice3D");
                diceGo.transform.SetParent(transform, false);
                Dice = diceGo.AddComponent<Dice3D>();
            }
            Dice.EnsureVisual(new Vector3(0f, 1.5f, 0f));
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.tag = "MainCamera";
                go.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 28f, -18f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            cam.orthographic = false;
            cam.fieldOfView = 50f;
        }

        void SpawnPlayers()
        {
            Players.Clear();
            _centerFlags.Clear();
            int count = Mathf.Clamp(PlayerCount, 2, 4);
            var startTile = Board.GetTile(1);

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Player_{i}_{DefaultNames[i]}");
                go.transform.SetParent(transform, false);
                var p = go.AddComponent<Player3D>();
                Vector3 pos = startTile != null
                    ? startTile.GetStandPosition(i, count)
                    : Board.GetTileWorldPosition(1);
                p.Initialize(DefaultNames[i], i, DefaultColors[i], pos);
                p.Gold = StartGold;
                Players.Add(p);
                _centerFlags[p] = false;
            }
            CurrentPlayerIndex = 0;
        }

        void Update()
        {
            if (_busy) return;
            if (Phase == TurnPhase.WaitingRoll && Input.GetKeyDown(KeyCode.Space))
                StartCoroutine(DoRollAndMove());
            if (Phase == TurnPhase.TileAction || Phase == TurnPhase.Building)
            {
                if (Input.GetKeyDown(KeyCode.B)) TryBuyCurrent();
                if (Input.GetKeyDown(KeyCode.Alpha1)) TryBuild(BuildingType.Barracks);
                if (Input.GetKeyDown(KeyCode.Alpha2)) TryBuild(BuildingType.TaxOffice);
                if (Input.GetKeyDown(KeyCode.Alpha3)) TryBuild(BuildingType.Watchtower);
                if (Input.GetKeyDown(KeyCode.Alpha4)) TryBuild(BuildingType.Landmark);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    EndTurn();
            }
            if (Phase == TurnPhase.SiegeChoice)
            {
                if (Input.GetKeyDown(KeyCode.P)) StartCoroutine(PayToll(false));
                if (Input.GetKeyDown(KeyCode.S)) StartCoroutine(ResolveSiege());
            }
        }

        IEnumerator DoRollAndMove()
        {
            _busy = true;
            Phase = TurnPhase.Moving;
            var player = Current;

            if (player.IsInExile)
            {
                player.ExileTurnsLeft--;
                if (player.ExileTurnsLeft > 0)
                {
                    SetStatus($"{player.PlayerName} 유배 중 (남은 {player.ExileTurnsLeft}턴)");
                    _busy = false;
                    EndTurn();
                    yield break;
                }
                player.IsInExile = false;
                SetStatus($"{player.PlayerName} 유배 해제!");
            }

            int diceResult = 0;
            yield return Dice.Roll(r => diceResult = r);
            LastDice = diceResult;
            Log($"{player.PlayerName} 주사위: {diceResult}");

            // 교차로에 서 있으면 홀/짝으로 중원 분기 후 이동
            var standing = Board.GetTile(player.TileIndex);
            if (standing != null && standing.Type == TileType.Crossroad)
            {
                ApplyCrossroadBranch(player, diceResult);
                var entry = Board.GetTile(player.TileIndex);
                if (entry != null)
                    player.transform.position = entry.GetStandPosition(player.PlayerIndex, Players.Count);
            }

            yield return MovePlayerSteps(player, diceResult);

            yield return ResolveLanding(player);
            _busy = false;
        }

        void ApplyCrossroadBranch(Player3D player, int dice)
        {
            bool odd = dice % 2 == 1;
            if (odd)
            {
                _centerFlags[player] = true;
                player.TileIndex = 81;
                SetStatus($"교차로 홀수 → 중원 진입 (낙양관문)");
                Log($"{player.PlayerName} 중원 루트 진입");
            }
            else
            {
                _centerFlags[player] = false;
                SetStatus($"교차로 짝수 → 외곽 유지");
            }
        }

        IEnumerator MovePlayerSteps(Player3D player, int steps)
        {
            var path = new List<Vector3>();
            int from = player.TileIndex;

            for (int s = 0; s < steps; s++)
            {
                int next = GetNextTileId(player, from);
                // 출발지 통과 보너스
                if (!_centerFlags[player] && from > next && next == 1)
                {
                    player.AddGold(PassStartBonus);
                    Log($"{player.PlayerName} 출발지 통과 +{PassStartBonus}");
                }
                // 중원 완주(100→외곽) 시 플래그 해제
                if (_centerFlags[player] && from == 100)
                    _centerFlags[player] = false;

                from = next;
                var tile = Board.GetTile(from);
                Vector3 stand = tile != null
                    ? tile.GetStandPosition(player.PlayerIndex, Players.Count)
                    : Board.GetTileWorldPosition(from);
                path.Add(stand);
            }

            yield return player.JumpAlongPath(path);
            player.TileIndex = from;
        }

        int GetNextTileId(Player3D player, int current)
        {
            bool inCenter = _centerFlags.TryGetValue(player, out var c) && c;

            if (inCenter)
            {
                if (current < 81) return 81;
                if (current >= 100) return 1; // 옥좌 다음 → 출발지
                return current + 1;
            }

            // 외곽 1~80 루프
            if (current >= 80) return 1;
            if (current < 1) return 1;
            // 중원에 잘못 있으면 외곽으로
            if (current > 80) return 1;
            return current + 1;
        }

        IEnumerator ResolveLanding(Player3D player)
        {
            Phase = TurnPhase.TileAction;
            var tile = Board.GetTile(player.TileIndex);
            if (tile == null)
            {
                EndTurn();
                yield break;
            }

            SetStatus($"{player.PlayerName} → [{tile.TileId}] {tile.TileName}");

            switch (tile.Type)
            {
                case TileType.Treasury:
                    int got = Treasury.CollectAll(player);
                    SetStatus($"낙양 국고 수령: {got} 금화");
                    Phase = TurnPhase.Building;
                    break;

                case TileType.LuckyCard:
                    yield return DrawCard(player, CardKind.Lucky);
                    break;
                case TileType.UnluckyCard:
                    yield return DrawCard(player, CardKind.Unlucky);
                    break;
                case TileType.ChanceCard:
                    yield return DrawCard(player, CardKind.Chance);
                    break;

                case TileType.Exile:
                    player.IsInExile = true;
                    player.ExileTurnsLeft = 2;
                    SetStatus($"{player.PlayerName} 유배지로 이송 (2턴)");
                    Phase = TurnPhase.TurnEnd;
                    EndTurn();
                    yield break;

                case TileType.Market:
                    player.AddGold(MarketBonus);
                    SetStatus($"마시장: +{MarketBonus} 금화");
                    Phase = TurnPhase.Building;
                    break;

                case TileType.Special:
                    if (tile.TileId == 60) // 화타 의원
                    {
                        if (player.TrySpend(HospitalHealCost))
                            SetStatus("화타의 의원: 치료 완료");
                        else
                            SetStatus("치료비 부족");
                    }
                    else if (tile.IsPurchasable)
                        yield return HandleOwnableTile(player, tile);
                    else
                        Phase = TurnPhase.Building;
                    break;

                case TileType.Start:
                    player.AddGold(PassStartBonus / 2);
                    SetStatus($"출발지 도착 보너스 +{PassStartBonus / 2}");
                    Phase = TurnPhase.Building;
                    break;

                case TileType.Crossroad:
                    SetStatus($"교차로 대기 중 — 다음 주사위 홀/짝으로 중원 분기");
                    Phase = TurnPhase.Building;
                    break;

                case TileType.Castle:
                case TileType.Gate:
                case TileType.Throne:
                    yield return HandleOwnableTile(player, tile);
                    break;

                default:
                    Phase = TurnPhase.Building;
                    break;
            }

            if (Phase == TurnPhase.Building || Phase == TurnPhase.TileAction)
                SetStatus(StatusMessage + " | B:매입 1~4:건설 Enter:턴종료");
        }

        IEnumerator HandleOwnableTile(Player3D player, Tile3D tile)
        {
            if (tile.Owner == null)
            {
                SetStatus($"{tile.TileName} 공성 — B키로 {tile.BasePrice}에 매입 / Enter 패스");
                Phase = TurnPhase.TileAction;
                yield break;
            }

            if (tile.Owner == player)
            {
                SetStatus($"내 성 {tile.TileName} — 1~4 건설 / Enter 종료");
                Phase = TurnPhase.Building;
                yield break;
            }

            // 타인 성
            if (tile.HasLandmark)
            {
                SetStatus($"랜드마크 성 — 공성/강제매입 불가. 통행료 지불(P)");
                PendingSiegeTile = tile;
                Phase = TurnPhase.SiegeChoice;
                yield return PayToll(false);
                yield break;
            }

            int toll = Buildings.CalculateToll(tile);
            SetStatus($"{tile.Owner.PlayerName}의 {tile.TileName} — P:통행료({toll}) / S:공성전");
            PendingSiegeTile = tile;
            Phase = TurnPhase.SiegeChoice;
        }

        public void TryBuyCurrent()
        {
            if (_busy) return;
            var player = Current;
            var tile = Board.GetTile(player.TileIndex);
            if (tile == null || !tile.IsPurchasable || tile.Owner != null) return;
            if (!player.TrySpend(tile.BasePrice))
            {
                SetStatus("금화 부족");
                return;
            }
            Buildings.TransferOwnership(tile, player, true);
            SetStatus($"{player.PlayerName}이(가) {tile.TileName} 매입!");
            Phase = TurnPhase.Building;
        }

        public void TryBuild(BuildingType type)
        {
            if (_busy) return;
            var player = Current;
            var tile = Board.GetTile(player.TileIndex);
            if (!Buildings.CanBuild(tile, type, player, out string reason))
            {
                SetStatus(reason);
                return;
            }
            Buildings.TryBuild(tile, type, player);
            SetStatus($"{type} 건설 완료 (통행료 {Buildings.CalculateToll(tile)})");
            Phase = TurnPhase.Building;
        }

        IEnumerator PayToll(bool doubleToll)
        {
            _busy = true;
            var player = Current;
            var tile = PendingSiegeTile ?? Board.GetTile(player.TileIndex);
            if (tile == null || tile.Owner == null || tile.Owner == player)
            {
                _busy = false;
                Phase = TurnPhase.Building;
                yield break;
            }

            int toll = Buildings.CalculateToll(tile);
            if (doubleToll) toll *= 2;

            if (!player.TrySpend(toll))
            {
                // 파산 처리 (단순)
                int paid = player.Gold;
                player.Gold = 0;
                tile.Owner.AddGold(paid);
                SetStatus($"{player.PlayerName} 파산 직전 — {paid}만 지불");
                CheckBankrupt(player);
            }
            else
            {
                tile.Owner.AddGold(toll);
                SetStatus($"통행료 {toll} 지불 → {tile.Owner.PlayerName}");
            }

            PendingSiegeTile = null;
            _busy = false;
            Phase = TurnPhase.Building;
            yield return null;
        }

        IEnumerator ResolveSiege()
        {
            _busy = true;
            var attacker = Current;
            var tile = PendingSiegeTile ?? Board.GetTile(attacker.TileIndex);
            if (tile == null || tile.Owner == null || tile.HasLandmark)
            {
                SetStatus("공성 불가");
                _busy = false;
                yield return PayToll(false);
                yield break;
            }

            int atk = 0, defRoll = 0;
            yield return Dice.Roll(r => atk = r);
            yield return new WaitForSeconds(0.2f);
            yield return Dice.Roll(r => defRoll = r);

            int defBonus = Buildings.GetDefenseBonus(tile);
            int defTotal = defRoll + defBonus;

            Log($"공성전 {attacker.PlayerName}({atk}) vs {tile.Owner.PlayerName}({defRoll}+{defBonus}={defTotal})");

            if (atk > defTotal)
            {
                var prev = tile.Owner;
                Buildings.TransferOwnership(tile, attacker, true);
                SetStatus($"공성 승리! {tile.TileName} 탈취 ({prev.PlayerName} → {attacker.PlayerName})");
                Phase = TurnPhase.Building;
            }
            else
            {
                SetStatus("공성 패배 — 통행료 2배 지불");
                yield return PayToll(true);
            }

            PendingSiegeTile = null;
            _busy = false;
        }

        IEnumerator DrawCard(Player3D player, CardKind kind)
        {
            Phase = TurnPhase.WaitingCard;
            _busy = true;
            bool closed = false;

            string title;
            string body;
            System.Action effect;

            RollCardContent(player, kind, out title, out body, out effect);

            Cards.ShowCard(kind, title, body, () =>
            {
                effect?.Invoke();
                closed = true;
            });

            while (!closed) yield return null;

            _busy = false;
            Phase = TurnPhase.Building;
            SetStatus($"{title} 적용됨 — Enter로 턴 종료");
        }

        void RollCardContent(Player3D player, CardKind kind, out string title, out string body, out System.Action effect)
        {
            int roll = Random.Range(0, 4);
            title = kind == CardKind.Lucky ? "🌟 행운" : kind == CardKind.Unlucky ? "🌩️ 불행" : "🎴 찬스";
            body = "";
            effect = null;

            if (kind == CardKind.Lucky)
            {
                switch (roll)
                {
                    case 0:
                        body = "황제의 하사금\n+200 금화";
                        effect = () => player.AddGold(200);
                        break;
                    case 1:
                        body = "전리품 발견\n+150 금화";
                        effect = () => player.AddGold(150);
                        break;
                    case 2:
                        body = "민심 확보\n소유 성마다 +30";
                        effect = () => player.AddGold(30 * Mathf.Max(1, player.OwnedTiles.Count));
                        break;
                    default:
                        body = "국고 기부금 회수\n국고에서 +100 적립 후 수령 가능";
                        effect = () => Treasury.Deposit(100);
                        break;
                }
            }
            else if (kind == CardKind.Unlucky)
            {
                switch (roll)
                {
                    case 0:
                        body = "벌금 납부\n-100 → 국고 적립";
                        effect = () =>
                        {
                            int pay = Mathf.Min(100, player.Gold);
                            player.Gold -= pay;
                            Treasury.Deposit(pay);
                        };
                        break;
                    case 1:
                        body = "세금 독촉\n-150 → 국고";
                        effect = () =>
                        {
                            int pay = Mathf.Min(150, player.Gold);
                            player.Gold -= pay;
                            Treasury.Deposit(pay);
                        };
                        break;
                    case 2:
                        body = "유배 명령\n유배지로 이동";
                        effect = () =>
                        {
                            player.TileIndex = 20;
                            player.IsInExile = true;
                            player.ExileTurnsLeft = 2;
                            var t = Board.GetTile(20);
                            if (t != null)
                                player.transform.position = t.GetStandPosition(player.PlayerIndex, Players.Count);
                        };
                        break;
                    default:
                        body = "군량 손실\n-80 금화";
                        effect = () => player.TrySpend(Mathf.Min(80, player.Gold));
                        break;
                }
            }
            else // Chance
            {
                switch (roll)
                {
                    case 0:
                        body = "첩보 성공\n+120 금화";
                        effect = () => player.AddGold(120);
                        break;
                    case 1:
                        body = "뇌물 스캔들\n-100 → 국고";
                        effect = () =>
                        {
                            int pay = Mathf.Min(100, player.Gold);
                            player.Gold -= pay;
                            Treasury.Deposit(pay);
                        };
                        break;
                    case 2:
                        body = "낙양 국고 습격!\n국고 전액 수령";
                        effect = () => Treasury.CollectAll(player);
                        break;
                    default:
                        body = "강제 행군 준비금\n+80 금화";
                        effect = () => player.AddGold(80);
                        break;
                }
            }
        }

        void CheckBankrupt(Player3D player)
        {
            if (player.Gold <= 0 && player.OwnedTiles.Count == 0)
            {
                player.IsBankrupt = true;
                SetStatus($"{player.PlayerName} 파산!");
            }
        }

        public void EndTurn()
        {
            if (_busy && Phase == TurnPhase.Moving) return;
            if (Phase == TurnPhase.WaitingCard) return;

            // 다음 생존 플레이어
            int safety = Players.Count;
            do
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
                safety--;
            } while (Players[CurrentPlayerIndex].IsBankrupt && safety > 0);

            PendingSiegeTile = null;
            Phase = TurnPhase.WaitingRoll;
            SetStatus($"{Current.PlayerName}의 턴 — Space로 주사위");
        }

        void SetStatus(string msg)
        {
            StatusMessage = msg;
            Debug.Log($"[삼국마블] {msg}");
        }

        void Log(string msg)
        {
            LogMessage = msg;
            Debug.Log($"[삼국마블] {msg}");
        }

        void OnGUI()
        {
            const int pad = 12;
            GUI.Box(new Rect(pad, pad, 420, 220), "삼국마블 (Samguk Marble)");
            GUILayout.BeginArea(new Rect(pad + 8, pad + 28, 400, 180));
            GUILayout.Label($"턴: {(Current != null ? Current.PlayerName : "-")} | 단계: {Phase}");
            GUILayout.Label($"주사위: {LastDice} | 국고: {(Treasury != null ? Treasury.NationalTreasury : 0)}");
            if (Current != null)
            {
                var tile = Board != null ? Board.GetTile(Current.TileIndex) : null;
                string tName = tile != null ? $"{tile.TileId}.{tile.TileName}" : "?";
                GUILayout.Label($"{Current.PlayerName} 금화:{Current.Gold} 위치:{tName} 성:{Current.OwnedTiles.Count}");
            }
            GUILayout.Label(StatusMessage);
            GUILayout.Label(LogMessage);
            GUILayout.Label("Space:주사위 | B:매입 | 1병영 2조세 3망루 4랜드 | P통행 S공성 | Enter종료");
            GUILayout.EndArea();

            // 플레이어 요약
            float y = 250f;
            foreach (var p in Players)
            {
                GUI.Label(new Rect(pad, y, 400, 22),
                    $"{p.PlayerName}: {p.Gold}G / 칸{p.TileIndex} / 성{p.OwnedTiles.Count}" +
                    (p.IsInExile ? " [유배]" : "") + (p.IsBankrupt ? " [파산]" : ""));
                y += 20f;
            }
        }
    }
}
