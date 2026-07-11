using System;
using System.Threading;
using Chronex.Services;
using Cysharp.Threading.Tasks;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Services.Audio;
using QuiChuong2005.Framework.Services.Scenes;
using UnityEngine;


namespace QuiChuong2005.Framework.Services
{
    /// <summary>
    /// Sống trên GameObject "Bootstrap" riêng, DontDestroyOnLoad.
    /// Chỉ có duy nhất trách nhiệm: khởi tạo & đăng ký service vào ServiceLocator.
    /// KHÔNG chứa logic UI - SplashSceneController (ở SplashScene) mới lo phần đó.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        public static Bootstrap Instance { get; private set; }

        [SerializeField]
        private AudioLibrary _audioLibrary; // kéo asset vào đây trong Inspector

        public bool IsInitialized { get; private set; }

        public event Action<string> OnStatusChanged;
        public event Action OnCompleted;
        public event Action<string> OnError;

        private CancellationTokenSource _cts;
        private UniTask _runTask;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            _cts?.Cancel();
            _cts?.Dispose();
            Instance = null;
        }

        /// <summary>
        /// Gọi bởi SplashSceneController. An toàn khi gọi nhiều lần -
        /// nếu đã init xong hoặc đang init dở thì trả về task hiện có, không chạy lại từ đầu.
        /// </summary>
        public UniTask RunAsync()
        {
            if (IsInitialized)
            {
                OnCompleted?.Invoke();
                return UniTask.CompletedTask;
            }

            return _runTask.Status == UniTaskStatus.Pending
                ? _runTask
                : (_runTask = RunInternalAsync());
        }

        private async UniTask RunInternalAsync()
        {
            try
            {
                await InitializeCoreServicesAsync(_cts.Token);
                IsInitialized = true;
                OnCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Bị huỷ do app thoát/domain reload - không phải lỗi nghiệp vụ.
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrap] Khởi tạo thất bại: {ex}");
                OnError?.Invoke(ex.Message);
            }
        }

        private async UniTask InitializeCoreServicesAsync(CancellationToken token)
        {
            var locator = ServiceLocator.Instance;

            var audioService = new AudioService();
            audioService.RegisterLibrary<global::Audio>(_audioLibrary);
            locator.Register<IAudioService>(audioService);

            // --- Phase 0: Scene service (không phụ thuộc, không async) ---
            locator.Register<ISceneService>(new SceneService());

            // --- Phase 1: Firebase SDK ---
            OnStatusChanged?.Invoke("Đang khởi tạo Firebase...");
            var firebaseService = new FirebaseService();
            locator.Register(firebaseService);
            await firebaseService.InitializeAsync(token);

            // --- Phase 2: Authentication (phụ thuộc Firebase) ---
            OnStatusChanged?.Invoke("Đang xác thực người dùng...");
            var authService = new AuthenticationService();
            locator.Register(authService);
            locator.Resolve(authService);
            await authService.InitializeAsync(token);

            OnStatusChanged?.Invoke("Hoàn tất khởi tạo.");
        }
    }
}