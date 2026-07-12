using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance;

    private FirebaseAuth auth;
    private FirebaseUser user;

    public event Action<FirebaseUser> OnLoginSuccess;
    public event Action<string> OnAuthError;
    public event Action OnLogout;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitFirebase();
    }

    private void InitFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                user = auth.CurrentUser;
                Debug.Log("Firebase đã sẵn sàng.");
            }
            else
            {
                Debug.LogError("Không thể khởi tạo Firebase: " + task.Result);
            }
        });
    }

    // ---------------- ĐĂNG KÝ ----------------
    public void Register(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                OnAuthError?.Invoke(GetErrorMessage(task.Exception));
                return;
            }

            user = task.Result.User;
            Debug.Log("Đăng ký thành công: " + user.Email);
            OnLoginSuccess?.Invoke(user);
        });
    }

    // ---------------- ĐĂNG NHẬP ----------------
    public void Login(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                OnAuthError?.Invoke(GetErrorMessage(task.Exception));
                return;
            }

            user = task.Result.User;
            Debug.Log("Đăng nhập thành công: " + user.Email);
            OnLoginSuccess?.Invoke(user);
        });
    }

    // ---------------- ĐĂNG XUẤT ----------------
    public void Logout()
    {
        auth.SignOut();
        user = null;
        OnLogout?.Invoke();
        Debug.Log("Đã đăng xuất.");
    }

    // ---------------- KIỂM TRA ĐANG ĐĂNG NHẬP ----------------
    public bool IsLoggedIn()
    {
        return auth != null && auth.CurrentUser != null;
    }

    public FirebaseUser GetCurrentUser()
    {
        return auth?.CurrentUser;
    }

    // ---------------- XỬ LÝ LỖI ----------------
    private string GetErrorMessage(AggregateException exception)
    {
        var firebaseEx = exception.GetBaseException() as FirebaseException;
        if (firebaseEx == null) return "Có lỗi xảy ra.";

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