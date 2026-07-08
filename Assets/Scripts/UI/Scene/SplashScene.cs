using Cysharp.Threading.Tasks;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Services.Scenes;
using UnityEngine;

namespace Chronex.Bootstrap
{
    public sealed class SplashScene : MonoBehaviour
    {
        [SerializeField]
        private Bootstrap _bootstrapPrefab; // kéo prefab GO "Bootstrap" vào đây

        private Bootstrap _bootstrap;

        private void Awake()
        {
            // Nếu Bootstrap đã tồn tại từ trước (persist qua lần chạy trước) thì dùng lại,
            // chưa có thì instantiate mới từ prefab.
            _bootstrap = Bootstrap.Instance != null
                ? Bootstrap.Instance
                : Instantiate(_bootstrapPrefab);
        }

        private void OnEnable()
        {
            _bootstrap.OnStatusChanged += HandleStatusChanged;
            _bootstrap.OnCompleted += HandleCompleted;
            _bootstrap.OnError += HandleError;
        }

        private void OnDisable()
        {
            // _bootstrap không bị huỷ theo scene này nên luôn còn sống để unsubscribe an toàn.
            _bootstrap.OnStatusChanged -= HandleStatusChanged;
            _bootstrap.OnCompleted -= HandleCompleted;
            _bootstrap.OnError -= HandleError;
        }

        private void Start()
        {
            _bootstrap.RunAsync().Forget();
        }

        private void HandleStatusChanged(string message)
        {
            Debug.Log($"[Splash] {message}");
            // TODO: cập nhật SplashUIView (progress bar/text)
        }

        private void HandleCompleted()
        {
            var sceneService = ServiceLocator.Instance.Get<ISceneService>();
            sceneService.LoadSceneAsync<LogInScene>(nameof(LogInScene)).Forget();
        }

        private void HandleError(string message)
        {
            Debug.LogError($"[Splash] Lỗi: {message}");
            // TODO: show ErrorPopupController kèm nút Retry
        }
    }
}