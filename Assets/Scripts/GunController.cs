using ArtShot2D;
using UnityEngine;

namespace ArcShot2D
{
    public class GunController : MonoBehaviour
    {
        [Header("Reference")] [SerializeField] private PlayerController player;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Aim")] [SerializeField] private float rotateSpeed = 80f;
        [SerializeField] private float minAngle = -10f;
        [SerializeField] private float maxAngle = 80f;

        [Header("Shoot")] [SerializeField] private float minForce = 5f;
        [SerializeField] private float maxForce = 25f;
        [SerializeField] private float chargeTime = 2f;

        private float currentAngle = 45f;

        private float aimInput;

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
            aimInput = 0;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                aimInput = 1;

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                aimInput = -1;

            if (Input.GetKeyDown(KeyCode.Space))
                StartCharge();

            if (Input.GetKeyUp(KeyCode.Space))
                Shoot();
        }

        private void RotateGun()
        {
            currentAngle += aimInput * rotateSpeed * Time.deltaTime;

            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

            if (player.IsFacingRight())
            {
                transform.localRotation =
                    Quaternion.Euler(0, 0, currentAngle);
            }
            else
            {
                transform.localRotation =
                    Quaternion.Euler(0, 180, currentAngle);
            }
        }

        private void ChargeShot()
        {
            if (!isCharging)
                return;

            currentCharge += Time.deltaTime;

            currentCharge = Mathf.Clamp(currentCharge, 0, chargeTime);
        }

        private void StartCharge()
        {
            isCharging = true;
            currentCharge = 0;
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

            GameObject bullet =
                Instantiate(bulletPrefab,
                    firePoint.position,
                    Quaternion.identity);

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

            rb.linearVelocity =
                firePoint.right * shootForce;
        }

        #region Mobile Input

        public void AimUp(bool pressed)
        {
            aimInput = pressed ? 1 : 0;
        }

        public void AimDown(bool pressed)
        {
            aimInput = pressed ? -1 : 0;
        }

        public void ShootPressed()
        {
            StartCharge();
        }

        public void ShootReleased()
        {
            Shoot();
        }

        #endregion
    }
}