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
        [SerializeField] private CameraFollowController cameraFollow;

        [Header("Configs")]
        [SerializeField] private MapConfigsManager mapConfigsManager;
        [SerializeField] private CharacterManager _characterManager;
        [SerializeField] private string selectedMapId;

        [Header("Turn")]
        [SerializeField] private TurnManagerConfig turnConfig;

        [SerializeField]
        private NetworkObject _turnManagerNetworkPrefab; // ĐỔI: kéo PREFAB vào đây, không phải object trong scene.

        [Header("UI")]
        [SerializeField] private LevelScene scene;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Transform _mapTransform;

        [Inject] private Chronex.Services.Networking.INetworkService _networkService;

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

            _isHost = _networkService.Runner.IsServer; // THÊM

            if (_isHost)
            {
                SpawnPlayersHost();
                SpawnTurnManager(); // ĐỔI: gọi hàm mới thay vì HostInitialize trực tiếp
            }

            SubscribeTurnManagerWhenReady().Forget(); // ĐỔI: thay dòng _turnManagerNetwork.OnTurnStarted += ...

            if (scene.SkipTurnButton != null)
                scene.SkipTurnButton.onClick.AddListener(HandleSkipTurnClicked);
        }

        private void OnDestroy()
        {
            if (TurnManagerNetwork.Instance != null) // ĐỔI: dùng Instance thay vì field _turnManagerNetwork
                TurnManagerNetwork.Instance.OnTurnStarted -= HandleTurnStarted;
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
            GameObject characterGo = _characterManager.GetRandomCharacter();
            if (characterGo == null)
            {
                Debug.LogError("[LevelViewNetwork] GetRandomCharacter() trả về null.");
                return;
            }

            NetworkObject characterPrefab = characterGo.GetComponent<NetworkObject>();
            if (characterPrefab == null)
            {
                Debug.LogError($"[LevelViewNetwork] Prefab '{characterGo.name}' chưa có NetworkObject.");
                return;
            }

            Vector3 worldPos = _mapTransform.TransformPoint(localSpawnPos);

            // ĐỔI: Runner.Spawn -> _networkService.Runner.Spawn (property Runner của NetworkBehaviour không còn nữa)
            NetworkObject spawned = _networkService.Runner.Spawn(characterPrefab, worldPos, Quaternion.identity, owner);

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

        // THÊM MỚI - thay cho đoạn gọi _turnManagerNetwork.HostInitialize trực tiếp trong Start()
        private void SpawnTurnManager()
        {
            _networkService.Runner.Spawn(_turnManagerNetworkPrefab);

            var participants = PlayerNetworkController.AllPlayers
                .OrderBy(p => p.TurnOrderIndex)
                .Cast<ITurnParticipant>()
                .ToList();

            WaitAndInitializeTurnManager(participants).Forget();
        }

        // THÊM MỚI
        private async UniTaskVoid WaitAndInitializeTurnManager(List<ITurnParticipant> participants)
        {
            while (TurnManagerNetwork.Instance == null)
                await UniTask.Yield();

            TurnManagerNetwork.Instance.HostInitialize(participants, turnConfig);
        }

        // THÊM MỚI - thay cho dòng _turnManagerNetwork.OnTurnStarted += HandleTurnStarted trực tiếp
        private async UniTaskVoid SubscribeTurnManagerWhenReady()
        {
            while (TurnManagerNetwork.Instance == null)
                await UniTask.Yield();

            TurnManagerNetwork.Instance.OnTurnStarted += HandleTurnStarted;
        }

        private void HandleTurnStarted(int playerIndex)
        {
            var target = PlayerNetworkController.AllPlayers
                .FirstOrDefault(p => p.TurnOrderIndex == playerIndex);

            if (target == null) return;

            if (_isHost) // ĐỔI: Object.HasStateAuthority -> _isHost
                foreach (var p in PlayerNetworkController.AllPlayers)
                    p.HostSetTurnActive(p == target);

            cameraFollow.FollowPlayer(target);
            boundGun = target.GetComponentInChildren<GunNetworkController>();
        }

        private void HandleSkipTurnClicked()
        {
            if (_isHost) // ĐỔI
                TurnManagerNetwork.Instance?.HostEndCurrentTurn(); // ĐỔI: dùng Instance
        }

        private void HandleBulletFired(BulletNetwork bullet)
        {
            if (_isHost) // ĐỔI
                TurnManagerNetwork.Instance?.HostLockAllPlayers(); // ĐỔI

            cameraFollow.FollowBullet(bullet);
            bullet.OnResolved += HandleBulletResolved;
        }

        private void HandleBulletResolved()
        {
            if (_isHost) // ĐỔI
                TurnManagerNetwork.Instance?.HostEndCurrentTurn(); // ĐỔI
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
    }
}