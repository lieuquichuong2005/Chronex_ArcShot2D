using System;
using System.Collections.Generic;
using ArcShot2D;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelView : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private CameraFollowController cameraFollow;

    [Header("Spawn")]
    [SerializeField]
    private GameObject playerPrefab;

    [Header("Configs")]
    [SerializeField]
    private MapConfigsManager mapConfigsManager;

    [SerializeField]
    private Transform[] spawnPoints;

    [Header("Turn")]
    [SerializeField]
    private TurnManagerConfig turnConfig;

    [Header("UI")]
    [SerializeField]
    private LevelScene scene;

    [SerializeField]
    private Transform _mapTransform;

    private readonly List<PlayerController> players = new();
    private readonly List<GunController> guns = new();

    private TurnManager turnManager;

    // Gun đang được lắng nghe sự kiện OnShoot (để unsubscribe khi đổi turn)
    private GunController boundGun;

    private void Start()
    {
        SpawnPlayers();

        turnManager = new TurnManager(players, guns, turnConfig);
        turnManager.OnTurnStarted += HandleTurnStarted;

        if (scene.SkipTurnButton != null)
            scene.SkipTurnButton.onClick.AddListener(HandleSkipTurnClicked);

        turnManager.StartGame();
    }

    private void Update()
    {
        turnManager?.Tick(Time.deltaTime);

        UpdateHud();
    }

    private void SpawnPlayers()
    {
        var count = Mathf.Min(turnConfig.playerCount, spawnPoints.Length);

        for (var i = 0; i < count; i++)
        {
            var go = Instantiate(
                playerPrefab,
                spawnPoints[i].position,
                Quaternion.identity);

            players.Add(go.GetComponent<PlayerController>());
            guns.Add(go.GetComponentInChildren<GunController>());
        }
    }

    private void HandleTurnStarted(int playerIndex)
    {
        BindButtons(playerIndex);

        cameraFollow.FollowPlayer(players[playerIndex]);
    }

    private void HandleSkipTurnClicked()
    {
        turnManager.EndCurrentTurn();
    }

    /// <summary>
    /// Gán lại 5 nút di động (trái, phải, ngắm lên, ngắm xuống, bắn)
    /// để điều khiển đúng player/gun đang tới lượt, và lắng nghe sự kiện bắn
    /// để kết thúc turn ngay khi bắn xong (không cần đợi hết giờ).
    /// </summary>
    private void BindButtons(int playerIndex)
    {
        var player = players[playerIndex];
        var gun = guns[playerIndex];

        BindHold(scene.MoveLeftButton, player.MoveLeftDown, player.MoveLeftUp);
        BindHold(scene.MoveRightButton, player.MoveRightDown, player.MoveRightUp);

        BindHold(scene.AimUpButton, gun.AimUpDown, gun.AimUpUp);
        BindHold(scene.AimDownButton, gun.AimDownDown, gun.AimDownUp);

        BindHold(scene.ShootButton, gun.ShootPressed, gun.ShootReleased);

        if (boundGun != null)
            boundGun.OnBulletFired -= HandleBulletFired;

        boundGun = gun;
        boundGun.OnBulletFired += HandleBulletFired;
    }

    /// <summary>
    /// Đạn vừa được bắn ra - khoá TẤT CẢ player (kể cả người vừa bắn) ngay lập tức.
    /// Không ai được di chuyển/hành động cho tới khi đạn được xử lý xong.
    /// </summary>
    private void HandleBulletFired(Bullet bullet)
    {
        turnManager.LockAllPlayers();

        cameraFollow.FollowBullet(bullet);

        bullet.OnResolved += HandleBulletResolved;
    }

    /// <summary>Đạn đã trúng mục tiêu/mặt đất hoặc bay ra khỏi map - giờ mới chuyển turn.</summary>
    private void HandleBulletResolved()
    {
        var currentIndex = turnManager.CurrentPlayerIndex;

        cameraFollow.FollowPlayer(players[currentIndex]);

        turnManager.EndCurrentTurn();
    }

    private void BindHold(EventTrigger trigger, Action onDown, Action onUp)
    {
        if (trigger == null)
            return;

        trigger.triggers.Clear();

        AddEntry(trigger, EventTriggerType.PointerDown, onDown);
        AddEntry(trigger, EventTriggerType.PointerUp, onUp);
    }

    private void AddEntry(EventTrigger trigger, EventTriggerType type, Action callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => callback());

        trigger.triggers.Add(entry);
    }

    private void UpdateHud()
    {
        if (turnManager == null || turnManager.CurrentPlayerIndex < 0)
            return;

        var index = turnManager.CurrentPlayerIndex;

        var gun = guns[index];
        var player = players[index];

        if (scene.FirePower != null)
            scene.FirePower.fillAmount = gun.ChargePercent;

        if (scene.FireAngle != null)
            scene.FireAngle.text = $"{gun.CurrentAngle:0}";

        // Stamina = năng lượng di chuyển còn lại của player đang tới lượt
        if (scene.Stamina != null)
            scene.Stamina.fillAmount = player.StaminaPercent;

        // Thời gian còn lại của turn (đếm ngược, tự kết thúc turn khi về 0)
        if (scene.TimeTurnRemain != null)
            scene.TimeTurnRemain.text = turnConfig.turnDuration > 0f
                ? $"{Mathf.CeilToInt(turnManager.RemainingTime)}s"
                : "∞";
    }
}