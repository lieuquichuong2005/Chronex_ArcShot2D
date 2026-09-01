using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

namespace Game.Session
{
    /// <summary>
    /// Cài đặt ISessionService dùng Firebase Realtime Database làm nơi lưu "ai đang là phiên
    /// hiện tại" cho mỗi account, dưới node <c>sessions/{uid}/sessionId</c>.
    /// </summary>
    public sealed class SessionService : ISessionService
    {
        private readonly string _uid;
        private readonly string _sessionId;
        private DatabaseReference _sessionIdRef;

        public event Action OnKickedFromSession;

        public SessionService(string uid)
        {
            _uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _sessionId = Guid.NewGuid().ToString("N");
        }

        public async UniTask ClaimSessionAsync()
        {
            DatabaseReference sessionRef = FirebaseDatabase.DefaultInstance
                .RootReference.Child("sessions").Child(_uid);

            var payload = new Dictionary<string, object>
            {
                { "sessionId", _sessionId },
                { "deviceName", SystemInfo.deviceName },
                { "timestamp", ServerValue.Timestamp },
            };

            await sessionRef.SetValueAsync(payload).AsUniTask();

            // Tự xoá node khi app đóng đột ngột (crash, force-quit, mất mạng, rớt kết nối) -
            // Firebase phát hiện mất kết nối ở phía SERVER, không cần app tự polling/heartbeat.
            // Không ảnh hưởng mô hình "Đá" (login mới luôn ghi đè được dù có dòng này hay không),
            // chỉ để dọn dữ liệu Database cho sạch.
            sessionRef.OnDisconnect().RemoveValue();

            _sessionIdRef = sessionRef.Child("sessionId");
            _sessionIdRef.ValueChanged += HandleSessionIdChanged;
        }

        public void StopListening()
        {
            if (_sessionIdRef != null)
            {
                _sessionIdRef.ValueChanged -= HandleSessionIdChanged;
                _sessionIdRef = null;
            }
        }

        private void HandleSessionIdChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[SessionService] Lỗi lắng nghe session: {args.DatabaseError.Message}");
                return;
            }

            string serverSessionId = args.Snapshot.Value as string;

            // serverSessionId khác _sessionId của mình -> 1 thiết bị khác vừa ghi đè -> mình bị đá.
            // (Lần listener tự bắn ngay lúc mới đăng ký ở ClaimSessionAsync sẽ thấy ĐÚNG session
            // của chính mình -> không match điều kiện này -> không tự kick nhầm chính mình.)
            if (!string.IsNullOrEmpty(serverSessionId) && serverSessionId != _sessionId)
            {
                StopListening();
                OnKickedFromSession?.Invoke();
            }
        }
    }
}
