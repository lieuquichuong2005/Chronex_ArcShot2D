using System;
using ArcShot.Networking;
using Fusion;
using UnityEngine;

namespace ArcShot
{
    /// <summary>
    /// Thay PlayerController offline. Chỉ Host (State Authority) thực sự simulate di chuyển
    /// trong FixedUpdateNetwork - đọc input qua GetInput(), không đọc Input.GetAxisRaw trực tiếp.
    /// NetworkRigidbody2D (bắt buộc có trên prefab) tự sync vị trí sang Client để hiển thị.
    /// </summary>
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(HealthNetworkController))]
    public sealed class PlayerNetworkController : NetworkBehaviour, ITurnParticipant
    {
        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 5f;

        [Header("Stamina")]
        [SerializeField]
        private float maxStamina = 10f;

        [SerializeField]
        private float staminaCostPerUnit = 1f;

        [SerializeField]
        private GameObject _turnTransform;

        [Header("References")]
        [SerializeField]
        private Rigidbody2D rb;

        [SerializeField]
        private Transform visual;

        [SerializeField]
        private SpriteRenderer _character;

        [SerializeField]
        private SpriteRenderer _tire;

        [SerializeField]
        private SpriteRenderer _cannon;

        // Đăng ký/gỡ đăng ký static để LevelViewNetwork tìm đúng player theo TurnOrderIndex
        // mà không cần giữ reference thủ công qua network (Spawned() chạy trên MỌI peer).
        public static readonly System.Collections.Generic.List<PlayerNetworkController> AllPlayers = new();

        [Networked] public int TurnOrderIndex { get; set; } = -1;
        [Networked] public NetworkBool IsMyTurn { get; set; }
        [Networked] public NetworkBool FacingRight { get; set; } = true;
        [Networked] public float CurrentStamina { get; set; }

        [Networked] public int TeamId { get; set; }
        [Networked] public float DamageDealt { get; set; }
        [Networked] public float DamageTaken { get; set; }
        [Networked] public int ShotsFired { get; set; }
        [Networked] public int ShotsHit { get; set; }
        public float Accuracy => ShotsFired > 0 ? (float)ShotsHit / ShotsFired * 100f : 0f;

        public static PlayerNetworkController LocalPlayer { get; private set; }

        public float StaminaPercent => maxStamina > 0f ? Mathf.Clamp01(CurrentStamina / maxStamina) : 0f;
        public event Action<bool> OnTurnChanged;
        public bool IsLocalPlayer => Object.HasInputAuthority;

        private ChangeDetector _changeDetector;
        private HealthNetworkController _health;

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            _health = GetComponent<HealthNetworkController>();
            _health.OnDeath += HandleDeath;

            AllPlayers.Add(this);

            if (Object.HasInputAuthority)
            {
                LocalPlayer = this;
            }


            if (Object.HasStateAuthority)
                CurrentStamina = maxStamina;

            ApplyFacingVisual(FacingRight);
            if (_turnTransform != null) _turnTransform.SetActive(IsMyTurn);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            AllPlayers.Remove(this);

            AllPlayers.Remove(this);

            if (LocalPlayer == this)
                LocalPlayer = null;

            if (_health != null)
                _health.OnDeath -= HandleDeath;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;
            if (!IsMyTurn) return;

            if (GetInput(out NetworkInputData input))
            {
                var moveInput = CurrentStamina <= 0f ? 0f : input.MoveAxis;
                Move(input.MoveAxis, moveInput);
            }
        }

        public override void Render()
        {
            if (_changeDetector == null)
                return;

            foreach (var change in _changeDetector.DetectChanges(this))
                switch (change)
                {
                    case nameof(FacingRight):
                        ApplyFacingVisual(FacingRight);
                        break;

                    case nameof(IsMyTurn):

                        if (_turnTransform != null)
                            _turnTransform.SetActive(IsMyTurn);

                        OnTurnChanged?.Invoke(IsMyTurn);

                        break;
                }
        }

        private void Move(float faceInput, float moveInput)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            if (faceInput > 0.01f) FacingRight = true;
            else if (faceInput < -0.01f) FacingRight = false;

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                var distance = Mathf.Abs(rb.linearVelocity.x) * Runner.DeltaTime;
                CurrentStamina = Mathf.Max(CurrentStamina - distance * staminaCostPerUnit, 0f);
            }
        }

        private void ApplyFacingVisual(bool facingRight)
        {
            visual.localScale = facingRight ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
        }

        private void HandleDeath()
        {
            gameObject.SetActive(false);

            if (Object.HasStateAuthority)
            {
                Chronex.Networking.MatchStatsTracker.Instance?.HostCheckMatchEnd();
            }
        }

        /// <summary>Chỉ Host gọi - từ LevelViewNetwork khi bắt đầu turn mới.</summary>
        public void HostSetTurnActive(bool active)
        {
            if (!Object.HasStateAuthority)
                return;

            IsMyTurn = active;

            if (active)
                CurrentStamina = maxStamina;
        }

        public bool IsFacingRight()
        {
            return FacingRight;
        }

        public void SetTurnActive(bool active)
        {
            HostSetTurnActive(active);
        }

        /// <summary>Chỉ Host gọi - từ GunNetworkController khi bắn ra 1 viên đạn.</summary>
        public void HostIncrementShotsFired()
        {
            if (Object.HasStateAuthority) ShotsFired++;
        }

        /// <summary>Chỉ Host gọi - từ BulletNetwork khi đạn TRÚNG mục tiêu (không phải hết giờ/ra khỏi map).</summary>
        public void HostRegisterHit(float damage)
        {
            if (Object.HasStateAuthority)
            {
                ShotsHit++;
                DamageDealt += damage;
            }
        }

        /// <summary>Chỉ Host gọi - từ HealthNetworkController.HostTakeDamage của CHÍNH player này.</summary>
        public void HostRegisterDamageTaken(float damage)
        {
            if (Object.HasStateAuthority) DamageTaken += damage;
        }
    }
}