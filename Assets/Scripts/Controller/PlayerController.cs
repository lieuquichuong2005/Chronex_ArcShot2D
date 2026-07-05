using UnityEngine;

namespace ArcShot2D
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private HealthController _healthController;

        [Header("Movement")] [SerializeField] private float moveSpeed = 5f;

        [Header("Stamina")] [SerializeField] private float maxStamina = 10f;

        [Tooltip("Số stamina bị trừ trên mỗi 1 unit khoảng cách di chuyển")] [SerializeField]
        private float staminaCostPerUnit = 1f;

        [SerializeField] private GameObject _turnTransform;

        [Header("References")] [SerializeField]
        private Rigidbody2D rb;

        [SerializeField] private Transform visual;

        private float keyboardInput;
        private float mobileInput;

        private float currentStamina;

        public float CurrentStamina => currentStamina;
        public float StaminaPercent => maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;

        private bool IsAlive { get; set; }

        private void Awake()
        {
            IsAlive = true;
            currentStamina = maxStamina;

            _healthController.OnDeath += OnDeath;
        }


        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
            visual = transform;
        }

        private void Update()
        {
            HandleKeyboardInput();
        }

        private void FixedUpdate()
        {
            float finalInput = mobileInput != 0 ? mobileInput : keyboardInput;

            float moveInput = currentStamina <= 0f ? 0f : finalInput;

            Move(finalInput, moveInput);
        }

        private void HandleKeyboardInput()
        {
            keyboardInput = Input.GetAxisRaw("Horizontal");
        }

        private void Move(float faceInput, float moveInput)
        {
            rb.linearVelocity = new Vector2(
                moveInput * moveSpeed,
                rb.linearVelocity.y);

            if (faceInput > 0.01f)
            {
                visual.localScale = new Vector3(1, 1, 1);
            }
            else if (faceInput < -0.01f)
            {
                visual.localScale = new Vector3(-1, 1, 1);
            }

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                float distance = Mathf.Abs(rb.linearVelocity.x) * Time.fixedDeltaTime;

                ConsumeStamina(distance * staminaCostPerUnit);
            }
        }

        private void ConsumeStamina(float amount)
        {
            currentStamina -= amount;

            currentStamina = Mathf.Max(currentStamina, 0f);
        }

        /// <summary>Gọi bởi TurnManager khi tới lượt player này, hồi đầy stamina.</summary>
        public void ResetStamina()
        {
            currentStamina = maxStamina;
        }

        #region Mobile Input

        public void MoveLeftDown()
        {
            mobileInput = -1f;
        }

        public void MoveLeftUp()
        {
            if (mobileInput < 0)
                mobileInput = 0f;
        }

        public void MoveRightDown()
        {
            mobileInput = 1f;
        }

        public void MoveRightUp()
        {
            if (mobileInput > 0)
                mobileInput = 0f;
        }

        #endregion

        public bool IsFacingRight()
        {
            return visual.localScale.x > 0;
        }

        public void SetTurnIndicator(bool active)
        {
            if (_turnTransform != null)
                _turnTransform.SetActive(active);
        }

        private void OnDestroy()
        {
            _healthController.OnDeath -= OnDeath;
        }

        private void OnDeath()
        {
            IsAlive = false;
            gameObject.SetActive(false);
        }
    }
}