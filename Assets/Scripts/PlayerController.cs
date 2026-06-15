using UnityEngine;

namespace ArtShot2D
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")] [SerializeField] private float moveSpeed = 5f;

        [Header("Reference")] [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Transform visual;

        private float moveInput;

        private void Update()
        {
            HandleKeyboardInput();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void HandleKeyboardInput()
        {
            moveInput = Input.GetAxisRaw("Horizontal");
        }

        private void Move()
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            if (moveInput > 0.01f)
            {
                visual.localScale = new Vector3(1, 1, 1);
            }
            else if (moveInput < -0.01f)
            {
                visual.localScale = new Vector3(-1, 1, 1);
            }
        }

        #region Mobile Input

        public void MoveLeft(bool isPressed)
        {
            moveInput = isPressed ? -1f : 0f;
        }

        public void MoveRight(bool isPressed)
        {
            moveInput = isPressed ? 1f : 0f;
        }

        #endregion

        public bool IsFacingRight()
        {
            return visual.localScale.x > 0;
        }
    }
}