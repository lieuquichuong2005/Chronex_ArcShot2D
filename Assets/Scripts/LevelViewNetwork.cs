using System;
using System.Collections.Generic;
using System.Linq;
using ArcShot.Networking;
using Chronex.Networking;
using Cysharp.Threading.Tasks;
using Fusion;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using UnityEngine;

namespace ArcShot
{
    public class LevelViewNetwork : MonoBehaviour // ĐỔI: NetworkBehaviour -> MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField]
        private CameraFollowController cameraFollow;

        [Header("Configs")]
        [SerializeField]
        private MapConfigsManager mapConfigsManager;

        [SerializeField]
        private CharacterManager _characterManager;

        [SerializeField]
        private string selectedMapId;

        [Header("Turn")]
        [SerializeField]
        private TurnManagerConfig turnConfig;

        [SerializeField]
        private NetworkObject _turnManagerNetworkPrefab; // ĐỔI: kéo PREFAB vào đây, không phải object trong scene.

        [Header("UI")]
        [SerializeField]
        private LevelScene scene;

        [SerializeField]
        private Transform _playerTransform;

        [SerializeField]
        private Transform _mapTransform;

        [Inject]
        private Chronex.Services.Networking.INetworkService _networkService;

        private MapConfig currentMapConfig;
        private GunNetworkController boundGun;
        private bool _isHost; // THÊM: lưu lại thay cho Object.HasStateAuthority (không còn dùng được nữa)


        private void Awake()
        {
            ServiceLocator.Instance.Resolve(this);
        }

        private void Start()
        {
            if (!SpawnMap()) return;

            _isHost = _networkService.Runner.IsServer;

            if (_isHost)
            {
                SpawnPlayersHost();
                SpawnTurnManager();
            }

            SubscribeTurnManagerWhenReady().Forget();
            BindLocalPlayer().Forget();

            BulletNetwork.AnyBulletSpawned += HandleAnyBulletSpawned; // THÊM
            BulletNetwork.AnyBulletResolved += HandleAnyBulletResolved; // THÊM

            if (scene.SkipTurnButton != null)
                scene.SkipTurnButton.onClick.AddListener(HandleSkipTurnClicked);
        }


        private void Update()
        {
            UpdateHud();
        }

        private void OnDestroy()
        {
            if (TurnManagerNetwork.Instance != null)
                TurnManagerNetwork.Instance.OnTurnStarted -= HandleTurnStarted;
            if (PlayerNetworkController.LocalPlayer != null)
                PlayerNetworkController.LocalPlayer.OnTurnChanged -= HandleLocalTurnChanged;

            BulletNetwork.AnyBulletSpawned -= HandleAnyBulletSpawned;
            BulletNetwork.AnyBulletResolved -= HandleAnyBulletResolved;
        }


        private void SpawnPlayersHost()
        {
            var players = _networkService.Runner.ActivePlayers.OrderBy(p => p.PlayerId).ToList();

            var totalCount = players.Count;
            var leftCount = Mathf.CeilToInt(totalCount / 2f);
            var rightCount = totalCount - leftCount;

            leftCount = Mathf.Min(leftCount, currentMapConfig.SpawnPoints_Left.Count);
            rightCount = Mathf.Min(rightCount, currentMapConfig.SpawnPoints_Right.Count);

            var turnOrder = 0;

            for (var i = 0; i < leftCount; i++)
            {
                SpawnPlayerAt(players[turnOrder], currentMapConfig.SpawnPoints_Left[i], true, turnOrder);
                turnOrder++;
            }

            for (var i = 0; i < rightCount; i++)
            {
                SpawnPlayerAt(players[turnOrder], currentMapConfig.SpawnPoints_Right[i], false, turnOrder);
                turnOrder++;
            }
        }

        private void SpawnPlayerAt(PlayerRef owner, Vector2 localSpawnPos, bool facingRight, int turnOrderIndex)
        {
            var characterGo = _characterManager.GetRandomCharacter();
            if (characterGo == null)
            {
                Debug.LogError("[LevelViewNetwork] GetRandomCharacter() trả về null.");
                return;
            }

            var characterPrefab = characterGo.GetComponent<NetworkObject>();
            if (characterPrefab == null)
            {
                Debug.LogError($"[LevelViewNetwork] Prefab '{characterGo.name}' chưa có NetworkObject.");
                return;
            }

            var worldPos = _mapTransform.TransformPoint(localSpawnPos);

            // ĐỔI: Runner.Spawn -> _networkService.Runner.Spawn (property Runner của NetworkBehaviour không còn nữa)
            var spawned = _networkService.Runner.Spawn(characterPrefab, worldPos, Quaternion.identity, owner);

            var playerCtrl = spawned.GetComponent<PlayerNetworkController>();
            if (playerCtrl == null)
            {
                Debug.LogError($"[LevelViewNetwork] Prefab '{characterGo.name}' chưa có PlayerNetworkController.");
                return;
            }

            playerCtrl.TurnOrderIndex = turnOrderIndex;
            playerCtrl.FacingRight = facingRight;

            var gun = spawned.GetComponentInChildren<GunNetworkController>();
            if (gun == null)
            {
                Debug.LogError($"[LevelViewNetwork] Prefab '{characterGo.name}' chưa có GunNetworkController.");
                return;
            }

            gun.OnBulletFired += HandleBulletFired;
        }

        private void SpawnTurnManager()
        {
            _networkService.Runner.Spawn(_turnManagerNetworkPrefab);

            var participants = PlayerNetworkController.AllPlayers
                .OrderBy(p => p.TurnOrderIndex)
                .Cast<ITurnParticipant>()
                .ToList();

            WaitAndInitializeTurnManager(participants).Forget();
        }

        private async UniTaskVoid WaitAndInitializeTurnManager(List<ITurnParticipant> participants)
        {
            while (TurnManagerNetwork.Instance == null)
                await UniTask.Yield();

            TurnManagerNetwork.Instance.HostInitialize(participants, turnConfig);
        }

        private async UniTaskVoid SubscribeTurnManagerWhenReady()
        {
            while (TurnManagerNetwork.Instance == null)
                await UniTask.Yield();

            TurnManagerNetwork.Instance.OnTurnStarted += HandleTurnStarted;

            if (TurnManagerNetwork.Instance.CurrentPlayerIndex >= 0)
                HandleTurnStarted(TurnManagerNetwork.Instance.CurrentPlayerIndex);
        }

        private void HandleTurnStarted(int playerIndex)
        {
            HandleTurnStartedAsync(playerIndex).Forget();
        }

        private async UniTaskVoid HandleTurnStartedAsync(int playerIndex)
        {
            PlayerNetworkController target = null;
            var elapsed = 0f;
            const float timeoutSeconds = 3f;

            while (target == null && elapsed < timeoutSeconds)
            {
                target = PlayerNetworkController.AllPlayers
                    .FirstOrDefault(p => p.TurnOrderIndex == playerIndex);

                if (target == null)
                {
                    await UniTask.Yield();
                    elapsed += Time.deltaTime;
                }
            }

            if (target == null)
            {
                Debug.LogError(
                    $"[LevelViewNetwork] Không tìm thấy player với TurnOrderIndex={playerIndex} sau {timeoutSeconds}s.");
                return;
            }

            if (_isHost)
                foreach (var p in PlayerNetworkController.AllPlayers)
                {
                    p.HostSetTurnActive(p == target);
                    // HandleLocalTurnChanged(p == target);
                }

            cameraFollow.FollowPlayer(target);
            boundGun = target.GetComponentInChildren<GunNetworkController>();
        }

        private void HandleSkipTurnClicked()
        {
            if (_isHost)
            {
                TurnManagerNetwork.Instance?.HostEndCurrentTurn();
            }
            else
            {
                TurnManagerNetwork.Instance?.RPC_RequestEndTurn();
            }
        }

        private void HandleBulletFired(BulletNetwork bullet)
        {
            if (_isHost)
            {
                TurnManagerNetwork.Instance?.HostLockAllPlayers();
                bullet.OnResolved += HandleBulletResolved;
            }
        }

        private void HandleBulletResolved()
        {
            if (_isHost)
                TurnManagerNetwork.Instance?.HostEndCurrentTurn();
        }

        private bool SpawnMap()
        {
            currentMapConfig = mapConfigsManager.GetMapConfig(selectedMapId);

            if (currentMapConfig == null || currentMapConfig.MapPrefab == null)
            {
                Debug.LogError($"[LevelViewNetwork] Không tìm thấy MapConfig cho '{selectedMapId}'.");
                return false;
            }

            Instantiate(currentMapConfig.MapPrefab, _mapTransform);
            return true;
        }

        private async UniTaskVoid RegisterLocalPlayer()
        {
            PlayerNetworkController localPlayer = null;

            while (localPlayer == null)
            {
                localPlayer = PlayerNetworkController.AllPlayers
                    .FirstOrDefault(p => p.Object.HasInputAuthority);

                await UniTask.Yield();
            }

            localPlayer.OnTurnChanged += HandleLocalTurnChanged;
        }

        private void HandleLocalTurnChanged(bool isMyTurn)
        {
            scene.SetTurn(isMyTurn);
        }

        private async UniTaskVoid BindLocalPlayer()
        {
            while (PlayerNetworkController.LocalPlayer == null)
                await UniTask.Yield();

            var player = PlayerNetworkController.LocalPlayer;
            player.OnTurnChanged += HandleLocalTurnChanged;

            HandleLocalTurnChanged(player.IsMyTurn);
        }

        private void UpdateHud()
        {
            var player = PlayerNetworkController.LocalPlayer;

            if (player == null)
                return;

            var gun = player.GetComponentInChildren<GunNetworkController>();

            if (gun == null)
                return;

            if (scene.FirePower != null)
                scene.FirePower.fillAmount = gun.ChargePercent;

            if (scene.FireAngle != null)
                scene.FireAngle.text = $"{gun.CurrentAngle:0}";

            if (scene.Stamina != null)
                scene.Stamina.fillAmount = player.StaminaPercent;

            if (scene.TimeTurnRemain != null &&
                TurnManagerNetwork.Instance != null)
                scene.TimeTurnRemain.text =
                    $"{Mathf.CeilToInt(TurnManagerNetwork.Instance.RemainingTime)}s";
        }

        private void HandleAnyBulletSpawned(BulletNetwork bullet)
        {
            cameraFollow.FollowBullet(bullet);
        }

        private void HandleAnyBulletResolved()
        {
            if (TurnManagerNetwork.Instance == null) return;

            var current = PlayerNetworkController.AllPlayers
                .FirstOrDefault(p => p.TurnOrderIndex == TurnManagerNetwork.Instance.CurrentPlayerIndex);

            if (current != null)
            {
                cameraFollow.FollowPlayer(current);
            }
        }
    }
}