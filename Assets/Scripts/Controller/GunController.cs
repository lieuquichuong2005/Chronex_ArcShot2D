using System;
using UnityEngine;

namespace ArcShot2D
{
    public class GunController : MonoBehaviour
    {
        // Bắn ra 1 viên đạn - LevelView lắng nghe để khoá toàn bộ player và đợi đạn được xử lý xong
        // (trúng mục tiêu/mặt đất/bay ra khỏi map) rồi mới chuyển turn, thay vì chuyển ngay khi bắn.
        public event Action<Bullet> OnBulletFired;

        [Header("Reference")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Aim")]
        [SerializeField] private float rotateSpeed = 80f;
        [SerializeField] private float minAngle = -10f;
        [SerializeField] private float maxAngle = 80f;

        [Header("Shoot")]
        [SerializeField] private float minForce = 5f;
        [SerializeField] private float maxForce = 25f;
        [SerializeField] private float chargeTime = 2f;

        private float currentAngle = 45f;

        // Lộ ra cho UI (LevelView) đọc để cập nhật fire power bar và fire angle text
        public float ChargePercent => chargeTime > 0f ? Mathf.Clamp01(currentCharge / chargeTime) : 0f;
        public float CurrentAngle => currentAngle;

        private float keyboardAimInput;
        private float mobileAimInput;

        private bool keyboardShootHolding;
        private bool mobileShootHolding;

        private bool isCharging;
        private float currentCharge;

        private void Update()
        {
            HandleKeyboardInput();

            RotateGun();

            ChargeShot();
        }

        private void HandleKeyboardInput()
        {
            keyboardAimInput = 0;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                keyboardAimInput = 1;

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                keyboardAimInput = -1;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                keyboardShootHolding = true;
                StartCharge();
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                keyboardShootHolding = false;
                Shoot();
            }
        }

        private void RotateGun()
        {
            float finalAimInput =
                mobileAimInput != 0
                    ? mobileAimInput
                    : keyboardAimInput;

            currentAngle += finalAimInput * rotateSpeed * Time.deltaTime;

            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

            if (player.IsFacingRight())
            {
                transform.localRotation =
                    Quaternion.Euler(0f, 0f, currentAngle);
            }
            else
            {
                transform.localRotation =
                    Quaternion.Euler(0f, 180f, currentAngle);
            }
        }

        private void ChargeShot()
        {
            if (!isCharging)
                return;

            currentCharge += Time.deltaTime;

            currentCharge = Mathf.Clamp(currentCharge, 0f, chargeTime);
        }

        private void StartCharge()
        {
            if (isCharging)
                return;

            isCharging = true;
            currentCharge = 0f;
        }

        private void Shoot()
        {
            if (!isCharging)
                return;

            isCharging = false;

            float forcePercent = currentCharge / chargeTime;

            float shootForce = Mathf.Lerp(
                minForce,
                maxForce,
                forcePercent);

            GameObject bulletObj = Instantiate(
                bulletPrefab,
                firePoint.position,
                Quaternion.identity);

            Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();

            rb.linearVelocity = firePoint.right * shootForce;

            Bullet bullet = bulletObj.GetComponent<Bullet>();

            OnBulletFired?.Invoke(bullet);
        }

        #region Mobile Input

        public void AimUpDown()
        {
            mobileAimInput = 1f;
        }

        public void AimUpUp()
        {
            if (mobileAimInput > 0)
                mobileAimInput = 0f;
        }

        public void AimDownDown()
        {
            mobileAimInput = -1f;
        }

        public void AimDownUp()
        {
            if (mobileAimInput < 0)
                mobileAimInput = 0f;
        }

        public void ShootPressed()
        {
            if (mobileShootHolding)
                return;

            mobileShootHolding = true;

            StartCharge();
        }

        public void ShootReleased()
        {
            if (!mobileShootHolding)
                return;

            mobileShootHolding = false;

            Shoot();
        }

        #endregion
    }
}