using System;
using ArcShot.Networking;
using Fusion;
using UnityEngine;

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

        [Networked] public float CurrentAngle { get; set; } = 45f;
        [Networked] public float CurrentCharge { get; set; }
        [Networked] public NetworkBool IsCharging { get; set; }

        public float ChargePercent => chargeTime > 0f ? Mathf.Clamp01(CurrentCharge / chargeTime) : 0f;

        private ChangeDetector _changeDetector;
        private bool _wasShootHeld;

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            ApplyRotationVisual();
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

            foreach (var change in _changeDetector.DetectChanges(this))
                if (change == nameof(CurrentAngle))
                    ApplyRotationVisual();
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
            OnBulletFired?.Invoke(bullet);
        }
    }
}