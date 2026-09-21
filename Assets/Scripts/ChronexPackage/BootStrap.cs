using System;
using System.Threading;
using Chronex.Services;
using Cysharp.Threading.Tasks;
using QuiChuong2005;
using QuiChuong2005.Framework.Services.Audio;
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
        private AudioLibrary _audioLibrary;

        [SerializeField]
        private EntityCatalogSO _entityFactory;

        public bool IsInitialized { get; private set; }

        public event Action<string> OnStatusChanged;
        public event Action OnCompleted;
        public event Action<string> OnError;
        public string SelectedMapId { get; set; }

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
        public UniTask RunAsync(DialogCatalogSO dialogCatalogSo)
        {
            if (IsInitialized)
            {
                OnCompleted?.Invoke();
                return UniTask.CompletedTask;
            }

            return _runTask.Status == UniTaskStatus.Pending
                ? _runTask
                : _runTask = RunInternalAsync(dialogCatalogSo);
        }

        private async UniTask RunInternalAsync(DialogCatalogSO dialogCatalogSo)
        {
            try
            {
                await InitializeCoreServicesAsync(dialogCatalogSo, _cts.Token);
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

        private async UniTask InitializeCoreServicesAsync(DialogCatalogSO dialogCatalogSo, CancellationToken token)
        {
            var locator = ServiceLocator.Instance;

            // --- Phase 0: SceneService + AudioService ---
            locator.Register<ISceneService>(new SceneService());

            var audioService = new AudioService();
            audioService.RegisterLibrary<global::Audio>(_audioLibrary);
            locator.Register<IAudioService>(audioService);

            var entityService = new EntityService(_entityFactory.Entities);
            locator.Register<IEntityService>(entityService);

            // --- Phase 1: Firebase SDK ---
            OnStatusChanged?.Invoke("Đang khởi tạo Firebase...");
            var firebaseService = new FirebaseService();
            locator.Register(firebaseService);
            await firebaseService.InitializeAsync(token);

            // --- Phase 2: Authentication (chỉ để có UserId TẠM cho Photon, KHÔNG dùng để tạo DataService) ---
            OnStatusChanged?.Invoke("Đang xác thực người dùng...");
            var authService = new AuthenticationService();
            locator.Register(authService);
            locator.Resolve(authService);
            await authService.InitializeAsync(token);

            var dialogService = new DialogService(dialogCatalogSo.Dialogs);
            locator.Register<IDialogService>(dialogService);

            OnStatusChanged?.Invoke("Hoàn tất khởi tạo.");
        }
    }
}