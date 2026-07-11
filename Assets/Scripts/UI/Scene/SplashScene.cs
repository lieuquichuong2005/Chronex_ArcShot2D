using System.Threading;
using Cysharp.Threading.Tasks;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Services;
using QuiChuong2005.Framework.Services.Scenes;
using TMPro;
using UnityEngine;

namespace QuiChuong2005.Arcshot
{
    public sealed class SplashScene : MonoBehaviour
    {
        [SerializeField]
        private QuiChuong2005.Framework.Services.Bootstrap _bootstrapPrefab;

        [SerializeField]
        private TextMeshProUGUI _loadingText;

        [SerializeField, Tooltip("Số giây giữa mỗi lần đổi số chấm")]
        private float _dotIntervalSeconds = 0.4f;

        [SerializeField, Tooltip("Số chấm tối đa trước khi lặp lại")]
        private int _maxDotCount = 3;

        private QuiChuong2005.Framework.Services.Bootstrap _bootstrap;
        private string _currentBaseMessage = "Loading";
        private CancellationTokenSource _dotAnimationCts;

        private void Awake()
        {
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
            _bootstrap.OnStatusChanged -= HandleStatusChanged;
            _bootstrap.OnCompleted -= HandleCompleted;
            _bootstrap.OnError -= HandleError;

            StopDotAnimation();
        }

        private void Start()
        {
            StartDotAnimation();
            _bootstrap.RunAsync().Forget();
        }

        private void HandleStatusChanged(string message)
        {
            Debug.Log($"[Splash] {message}");
            _currentBaseMessage = message;
        }

        private void HandleCompleted()
        {
            StopDotAnimation();
            if (_loadingText != null)
            {
                _loadingText.text = "Load Complete";
            }

            var sceneService = ServiceLocator.Instance.Get<ISceneService>();
            sceneService.LoadSceneAsync<LogInScene>(nameof(LogInScene)).Forget();
        }

        private void HandleError(string message)
        {
            StopDotAnimation();
            Debug.LogError($"[Splash] Lỗi: {message}");

            if (_loadingText != null)
            {
                _loadingText.text = message;
            }
            // TODO: show ErrorPopupController kèm nút Retry
        }

        private void StartDotAnimation()
        {
            StopDotAnimation();
            _dotAnimationCts = new CancellationTokenSource();
            AnimateDotsAsync(_dotAnimationCts.Token).Forget();
        }

        private void StopDotAnimation()
        {
            _dotAnimationCts?.Cancel();
            _dotAnimationCts?.Dispose();
            _dotAnimationCts = null;
        }

        private async UniTaskVoid AnimateDotsAsync(CancellationToken token)
        {
            int dotCount = 0;

            while (!token.IsCancellationRequested)
            {
                if (_loadingText != null)
                {
                    _loadingText.text = _currentBaseMessage + new string('.', dotCount);
                }

                dotCount = (dotCount + 1) % (_maxDotCount + 1);

                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(_dotIntervalSeconds),
                    cancellationToken: token,
                    cancelImmediately: true
                ).SuppressCancellationThrow();
            }
        }
    }
}