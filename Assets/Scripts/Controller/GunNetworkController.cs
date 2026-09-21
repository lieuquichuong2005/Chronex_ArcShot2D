using System;
using ArcShot.Networking;
using Fusion;
using UnityEngine;
using DG.Tweening;

namespace ArcShot
{
    /// <summary>
    /// Thay GunController offline. Chỉ Host tính góc/charge/spawn đạn - Client chỉ đọc property
    /// networked để xoay hình súng đúng góc hiển thị.
    /// </summary>
    public sealed class GunNetworkController : NetworkBehaviour
    {
        public event Action<BulletNetwork> OnBulletFired;

        [Header("Reference")]
        [SerializeField]
        private PlayerNetworkController player;

        [SerializeField]
        private Transform firePoint;

        [SerializeField]
        private NetworkObject bulletPrefab; // Phải đăng ký trong Fusion Prefab Table.

        [Header("Aim")]
        [SerializeField]
        private float rotateSpeed = 80f;

        [SerializeField]
        private float minAngle = -10f;

        [SerializeField]
        private float maxAngle = 65f;

        [Header("Shoot")]
        [SerializeField]
        private float minForce = 2f;

        [SerializeField]
        private float maxForce = 15f;

        [SerializeField]
        private float chargeTime = 4f;

        [Header("Recoil")]
        [SerializeField]
        private Transform _recoilVisual;

        [SerializeField]
        private float _recoilDistance = 0.3f;

        [SerializeField]
        private float _recoilKickDuration = 0.05f;

        [SerializeField]
        private float _recoilReturnDuration = 0.25f;

        [SerializeField]
        private Ease _recoilKickEase = Ease.OutQuad;

        [SerializeField]
        private Ease _recoilReturnEase = Ease.OutBack;

        [Networked] public float CurrentAngle { get; set; } = 45f;
        [Networked] public float CurrentCharge { get; set; }
        [Networked] public NetworkBool IsCharging { get; set; }

        public float ChargePercent => chargeTime > 0f ? Mathf.Clamp01(CurrentCharge / chargeTime) : 0f;

        private ChangeDetector _changeDetector;
        private bool _wasShootHeld;

        private bool _wasCharging;
        private Vector3 _recoilBasePosition;
        private Tween _recoilTween;

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            ApplyRotationVisual();

            if (_recoilVisual != null)
            {
                _recoilBasePosition = _recoilVisual.localPosition;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _recoilTween?.Kill();
        }

        private void OnDestroy()
        {
            _recoilTween?.Kill();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            if (!player.IsMyTurn) return;

            if (GetInput(out NetworkInputData input))
            {
                RotateGun(input.AimAxis);
                HandleShootInput(input.ShootHeld);
            }

            ChargeShot();
        }

        public override void Render()
        {
            if (_changeDetector == null) return;

            foreach (string change in _changeDetector.DetectChanges(this))
            {
                if (change == nameof(CurrentAngle))
                {
                    ApplyRotationVisual();
                }
                else if (change == nameof(IsCharging))
                {
                    if (_wasCharging && !IsCharging)
                    {
                        TriggerRecoil();
                    }

                    _wasCharging = IsCharging;
                }
            }
        }

        private void TriggerRecoil()
        {
            if (_recoilVisual == null) return;

            _recoilTween?.Kill();
            _recoilVisual.localPosition = _recoilBasePosition;

            Vector3 kickPosition = _recoilBasePosition + new Vector3(-_recoilDistance, 0f, 0f);

            _recoilTween = DOTween.Sequence()
                .Append(_recoilVisual.DOLocalMove(kickPosition, _recoilKickDuration).SetEase(_recoilKickEase))
                .Append(
                    _recoilVisual.DOLocalMove(_recoilBasePosition, _recoilReturnDuration).SetEase(_recoilReturnEase));
        }

        private void RotateGun(float aimInput)
        {
            CurrentAngle = Mathf.Clamp(CurrentAngle + aimInput * rotateSpeed * Runner.DeltaTime, minAngle, maxAngle);
        }

        private void ApplyRotationVisual()
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, CurrentAngle);
            // transform.localRotation = player.IsFacingRight()
            //     ? Quaternion.Euler(0f, 0f, CurrentAngle)
            //     : Quaternion.Euler(0f, 0f, CurrentAngle);
        }

        private void HandleShootInput(bool shootHeld)
        {
            if (shootHeld && !_wasShootHeld)
                StartCharge();
            else if (!shootHeld && _wasShootHeld) Shoot();

            _wasShootHeld = shootHeld;
        }

        private void ChargeShot()
        {
            if (!IsCharging) return;

            CurrentCharge = Mathf.Clamp(CurrentCharge + Runner.DeltaTime, 0f, chargeTime);
        }

        private void StartCharge()
        {
            if (IsCharging) return;

            IsCharging = true;
            CurrentCharge = 0f;
        }

        private void Shoot()
        {
            if (!IsCharging) return;

            IsCharging = false;

            var forcePercent = CurrentCharge / chargeTime;
            var shootForce = Mathf.Lerp(minForce, maxForce, forcePercent);

            var spawned = Runner.Spawn(
                bulletPrefab,
                firePoint.position,
                Quaternion.identity);

            var bulletRb = spawned.GetComponent<Rigidbody2D>();
            var direction = player.IsFacingRight() ? firePoint.right : -firePoint.right;
            bulletRb.linearVelocity = direction * shootForce;

            var bullet = spawned.GetComponent<BulletNetwork>();
            bullet.SetShooter(player);

            player.HostIncrementShotsFired();
            OnBulletFired?.Invoke(bullet);
        }
    }
}