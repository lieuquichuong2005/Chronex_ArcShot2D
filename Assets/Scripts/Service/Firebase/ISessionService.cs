using System;
using Cysharp.Threading.Tasks;

namespace Game.Session
{
    /// <summary>
    /// Đảm bảo 1 account chỉ có đúng 1 phiên đang chơi - mô hình "Đá": login mới LUÔN thành
    /// công, phiên cũ (nếu còn đang mở app) tự phát hiện qua OnKickedFromSession và tự logout.
    /// </summary>
    public interface ISessionService
    {
        /// <summary>Ghi đè session hiện tại thành CỦA MÌNH, bắt đầu lắng nghe realtime để phát hiện bị đá.</summary>
        UniTask ClaimSessionAsync();

        /// <summary>Ngừng lắng nghe - gọi khi chủ động logout, tránh nhận nhầm sự kiện kick của chính lần logout đó.</summary>
        void StopListening();

        /// <summary>Bắn ra khi phát hiện 1 thiết bị khác vừa login CÙNG account này (mình bị đá).</summary>
        event Action OnKickedFromSession;
    }
}
