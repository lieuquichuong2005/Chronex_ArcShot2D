using Fusion;

namespace ArcShot.Networking
{
    /// <summary>
    /// Input của 1 player mỗi tick network. Client ghi vào đây qua OnInput callback,
    /// Fusion tự gửi lên Host. Host đọc trong FixedUpdateNetwork để mô phỏng player đó.
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        public float MoveAxis;   // -1..1, giống Input.GetAxisRaw("Horizontal") cũ
        public float AimAxis;    // -1..1, W/S hoặc Up/Down cũ
        public NetworkBool ShootHeld; // giữ Space cũ
    }
}