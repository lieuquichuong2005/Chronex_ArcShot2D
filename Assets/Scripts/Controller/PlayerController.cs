using UnityEngine;

namespace ArcShot2D
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")] [SerializeField] private float moveSpeed = 5f;

        [Header("References")] [SerializeField]
        private Rigidbody2D rb;

        [SerializeField] private Transform visual;

        private float keyboardInput;
        private float mobileInput;

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

            Move(finalInput);
        }

        private void HandleKeyboardInput()
        {
            keyboardInput = Input.GetAxisRaw("Horizontal");
        }

        private void Move(float input)
        {
            rb.linearVelocity = new Vector2(
                input * moveSpeed,
                rb.linearVelocity.y);

            if (input > 0.01f)
            {
                visual.localScale = new Vector3(1, 1, 1);
            }
            else if (input < -0.01f)
            {
                visual.localScale = new Vector3(-1, 1, 1);
            }
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
    }
}