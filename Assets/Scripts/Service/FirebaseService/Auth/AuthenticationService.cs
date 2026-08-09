using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using QuiChuong2005;
using UnityEngine;

namespace Chronex.Services
{
    /// <summary>
    /// Toàn bộ nghiệp vụ đăng nhập/đăng ký/đăng xuất.
    /// Phụ thuộc FirebaseService qua field injection ([Inject]),
    /// được ServiceLocator.Resolve() gán trước khi InitializeAsync chạy.
    /// </summary>
    public sealed class AuthenticationService : IInitializableService
    {
        [Inject]
        private FirebaseService _firebaseService;

        private FirebaseUser _currentUser;

        public event Action<FirebaseUser> OnLoginSuccess;
        public event Action<string> OnAuthError;
        public event Action OnLogout;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_firebaseService == null || !_firebaseService.IsReady)
            {
                throw new InvalidOperationException(
                    "AuthenticationService yêu cầu FirebaseService phải khởi tạo trước. " +
                    "Kiểm tra thứ tự Register/Resolve trong Bootstrap.");
            }

            _currentUser = _firebaseService.Auth.CurrentUser;

            // FPS online thường cần 1 UserId ngay tại splash để Photon connect được,
            // nên nếu chưa có session cũ thì auto anonymous login.
            if (_currentUser == null)
            {
                await SignInAnonymouslyAsync(cancellationToken);
            }
        }

        private async UniTask SignInAnonymouslyAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Lưu ý: kiểu trả về (AuthResult vs FirebaseUser) tuỳ version Firebase SDK,
                // kiểm tra lại theo version bạn đang dùng.
                AuthResult result = await _firebaseService.Auth
                    .SignInAnonymouslyAsync()
                    .AsUniTask();

                cancellationToken.ThrowIfCancellationRequested();

                _currentUser = result.User;
                // Không fire OnLoginSuccess ở đây vì đây chỉ là login tạm cho Photon,
                // không phải login thực sự của user. SplashScene sẽ xử lý auto-login sau.
            }
            catch (Exception ex)
            {
                string message = GetErrorMessage(ex);
                OnAuthError?.Invoke(message);
                throw; // để Bootstrap biết Phase này fail và dừng flow
            }
        }

        public async UniTask RegisterAsync(string email, string password)
        {
            try
            {
                AuthResult result = await _firebaseService.Auth
                    .CreateUserWithEmailAndPasswordAsync(email, password)
                    .AsUniTask();

                _currentUser = result.User;
                OnLoginSuccess?.Invoke(_currentUser);
            }
            catch (Exception ex)
            {
                OnAuthError?.Invoke(GetErrorMessage(ex));
            }
        }

        public async UniTask LoginAsync(string email, string password)
        {
            try
            {
                AuthResult result = await _firebaseService.Auth
                    .SignInWithEmailAndPasswordAsync(email, password)
                    .AsUniTask();

                _currentUser = result.User;
                OnLoginSuccess?.Invoke(_currentUser);
            }
            catch (Exception ex)
            {
                OnAuthError?.Invoke(GetErrorMessage(ex));
            }
        }

        public async UniTask<bool> TryAutoLoginAsync()
        {
            Debug.Log("TryAutoLoginAsync");

            if (_firebaseService == null || _firebaseService.Auth == null)
            {
                Debug.LogError("FirebaseService chưa sẵn sàng.");
                return false;
            }

            var user = _firebaseService.Auth.CurrentUser;

            if (user == null)
            {
                _currentUser = null;
                return false;
            }

            try
            {
                await user.ReloadAsync();

                _currentUser = user;
                await UserServiceRegister.RegisterAsync(user.UserId);
                OnLoginSuccess?.Invoke(user);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Auto-login thất bại: {ex.Message}");
                _currentUser = null;
                return false;
            }
        }

        public void Logout()
        {
            _firebaseService.Auth.SignOut();
            _currentUser = null;
            OnLogout?.Invoke();
        }

        public bool IsLoggedIn() => _currentUser != null;

        public FirebaseUser GetCurrentUser() => _currentUser;

        private string GetErrorMessage(Exception exception)
        {
            var firebaseEx = exception as FirebaseException
                             ?? (exception as AggregateException)?.GetBaseException() as FirebaseException;

            if (firebaseEx == null)
                return "Có lỗi xảy ra.";

            var errorCode = (AuthError)firebaseEx.ErrorCode;

            return errorCode switch
            {
                AuthError.WrongPassword => "Sai mật khẩu.",
                AuthError.InvalidEmail => "Email không hợp lệ.",
                AuthError.UserNotFound => "Tài khoản không tồn tại.",
                AuthError.EmailAlreadyInUse => "Email đã được sử dụng.",
                AuthError.WeakPassword => "Mật khẩu quá yếu (tối thiểu 6 ký tự).",
                AuthError.MissingEmail => "Vui lòng nhập email.",
                AuthError.MissingPassword => "Vui lòng nhập mật khẩu.",
                _ => $"Lỗi: {errorCode}"
            };
        }
    }
}