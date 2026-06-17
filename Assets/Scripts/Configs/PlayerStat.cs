using System;
using UnityEngine;

namespace ArcShot2D
{
    /// <summary>
    /// Lưu các thông số (stat) của một nhân vật/player trong game bắn súng tọa độ
    /// kiểu Gunny: lực bắn, tầm bắn, sát thương, máu, giáp, gió ảnh hưởng, v.v.
    /// Tạo asset qua menu: Assets > Create > Game > Stats > Player Stat
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStat", menuName = "ArcShot2D/Configs/Player Stat", order = 0)]
    public class PlayerStat : ScriptableObject
    {
        [Header("Identity")] [SerializeField] private string statId = "player_default";
        [SerializeField] private string displayName = "Player";

        [Header("Health")] [SerializeField] [Min(1)]
        private int maxHealth = 100;

        [SerializeField] [Min(0)] private int armor = 0;

        [Tooltip("Tỉ lệ % giảm sát thương nhận vào, 0 - 1")] [SerializeField] [Range(0f, 1f)]
        private float damageReduction = 0f;

        [Header("Movement")] [SerializeField] [Min(0f)]
        private float moveSpeed = 5f;

        [SerializeField] [Min(0)] private int maxMoveSteps = 3;

        [Tooltip("Số bước di chuyển hồi lại mỗi turn")] [SerializeField] [Min(0)]
        private int moveStepsPerTurn = 3;

        [Header("Aiming / Shooting")] [Tooltip("Lực bắn tối đa (kéo chuột / thanh lực)")] [SerializeField] [Min(1f)]
        private float maxPower = 100f;

        [Tooltip("Góc bắn tối thiểu (độ)")] [SerializeField] [Range(0f, 90f)]
        private float minAngle = 0f;

        [Tooltip("Góc bắn tối đa (độ)")] [SerializeField] [Range(0f, 90f)]
        private float maxAngle = 90f;

        [Tooltip("Hệ số nhân lực bắn thực tế = power * powerMultiplier")] [SerializeField] [Min(0.01f)]
        private float powerMultiplier = 1f;

        [Header("Weapon Defaults")] [SerializeField]
        private WeaponData defaultWeapon;

        [SerializeField] [Min(0)] private int baseDamage = 10;

        [Tooltip("Bán kính nổ cơ bản (vùng ảnh hưởng splash damage)")] [SerializeField] [Min(0f)]
        private float explosionRadius = 1.5f;

        [Header("Wind Resistance")]
        [Tooltip("Mức độ ảnh hưởng của gió lên đạn, 0 = không bị ảnh hưởng, 1 = ảnh hưởng đầy đủ")]
        [SerializeField]
        [Range(0f, 1f)]
        private float windResistance = 1f;

        [Header("Turn / Resource")] [SerializeField] [Min(0)]
        private int maxAmmo = -1; // -1 = vô hạn

        [SerializeField] [Min(0f)] private float turnTimeLimit = 30f;

        // ----- Public read-only accessors -----

        public string StatId => statId;
        public string DisplayName => displayName;

        public int MaxHealth => maxHealth;
        public int Armor => armor;
        public float DamageReduction => damageReduction;

        public float MoveSpeed => moveSpeed;
        public int MaxMoveSteps => maxMoveSteps;
        public int MoveStepsPerTurn => moveStepsPerTurn;

        public float MaxPower => maxPower;
        public float MinAngle => minAngle;
        public float MaxAngle => maxAngle;
        public float PowerMultiplier => powerMultiplier;

        public WeaponData DefaultWeapon => defaultWeapon;
        public int BaseDamage => baseDamage;
        public float ExplosionRadius => explosionRadius;

        public float WindResistance => windResistance;

        public int MaxAmmo => maxAmmo;
        public float TurnTimeLimit => turnTimeLimit;

        /// <summary>
        /// Tính sát thương thực tế sau khi áp dụng giáp + damage reduction.
        /// </summary>
        public int CalculateFinalDamage(int rawDamage)
        {
            float afterArmor = Mathf.Max(0, rawDamage - armor);
            var afterReduction = afterArmor * (1f - damageReduction);
            return Mathf.RoundToInt(afterReduction);
        }

        /// <summary>
        /// Quy đổi % lực bắn (0-1, ví dụ từ thanh kéo lực) sang lực thực tế dùng cho physics.
        /// </summary>
        public float GetActualPower(float normalizedPower01)
        {
            normalizedPower01 = Mathf.Clamp01(normalizedPower01);
            return normalizedPower01 * maxPower * powerMultiplier;
        }

        /// <summary>
        /// Clamp góc bắn người chơi nhập vào trong khoảng cho phép.
        /// </summary>
        public float ClampAngle(float angle)
        {
            return Mathf.Clamp(angle, minAngle, maxAngle);
        }

        private void OnValidate()
        {
            if (maxAngle < minAngle) maxAngle = minAngle;
        }
    }

    /// <summary>
    /// Placeholder cho dữ liệu vũ khí. Thay bằng ScriptableObject WeaponData riêng nếu đã có.
    /// </summary>
    [Serializable]
    public class WeaponData
    {
        public string weaponId;
        public string weaponName;
        public Sprite icon;
        public GameObject projectilePrefab;
    }
}