using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ArcShot
{
    /// <summary>
    /// Text damage bay lên tại vị trí bị bắn trúng. Component thuần local (không networked) -
    /// mỗi client tự spawn khi nhận RPC broadcast từ BulletNetwork.
    /// </summary>
    public class DamageFloatingText : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro _text;

        [Header("Timing")]
        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private float riseHeight = 1.5f;

        [SerializeField]
        private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField]
        private AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Header("Bounce")]
        [SerializeField]
        private float bounceHeight = 0.3f;

        [SerializeField]
        private float bounceDuration = 0.15f;

        [Header("Colors")]
        [SerializeField]
        private Color normalColor = Color.red;

        [SerializeField]
        private Color critColor = new(1f, 0.15f, 0.1f);

        [Header("Crit")]
        [SerializeField]
        private float normalFontSize = 4f;

        [SerializeField]
        private float critFontSize = 6.5f;

        [SerializeField]
        private float critPunchScale = 1.4f;

        [SerializeField]
        private float critShakeStrength = 0.08f;

        private Vector3 _startPos;
        private Camera _cam;
        private CancellationToken _ct;

        public void Setup(int damage, bool isCritical)
        {
            _cam = Camera.main;
            _startPos = transform.position;
            _ct = this.GetCancellationTokenOnDestroy();

            _text.text = damage.ToString();
            _text.color = isCritical ? critColor : normalColor;
            _text.fontSize = isCritical ? critFontSize : normalFontSize;

            PlayAsync(isCritical).Forget();
        }

        private void LateUpdate()
        {
            if (_cam != null)
                transform.rotation = _cam.transform.rotation;
        }

        private async UniTaskVoid PlayAsync(bool isCritical)
        {
            if (isCritical)
            {
                await ScalePunch();
                await Shake();
            }

            await Bounce();
            await RiseAndFade();

            if (this != null) Destroy(gameObject);
        }

        private async UniTask ScalePunch()
        {
            var t = 0f;
            const float punchDuration = 0.12f;
            var baseScale = transform.localScale;

            while (t < punchDuration)
            {
                t += Time.deltaTime;
                var p = t / punchDuration;
                var s = Mathf.Lerp(0.3f, critPunchScale, p);
                transform.localScale = baseScale * s;
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }

            transform.localScale = baseScale;
        }

        private async UniTask Shake()
        {
            var t = 0f;
            const float shakeDuration = 0.12f;
            var origin = transform.position;

            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                var offset = Random.insideUnitCircle * critShakeStrength;
                transform.position = origin + (Vector3)offset;
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }

            transform.position = origin;
            _startPos = origin;
        }

        private async UniTask Bounce()
        {
            var t = 0f;

            while (t < bounceDuration)
            {
                t += Time.deltaTime;
                var p = t / bounceDuration;
                var y = Mathf.Sin(p * Mathf.PI) * bounceHeight;
                transform.position = _startPos + new Vector3(0f, y, 0f);
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }

            transform.position = _startPos;
        }

        private async UniTask RiseAndFade()
        {
            var t = 0f;
            var baseColor = _text.color;

            while (t < duration)
            {
                t += Time.deltaTime;
                var p = Mathf.Clamp01(t / duration);

                var y = riseCurve.Evaluate(p) * riseHeight;
                transform.position = _startPos + new Vector3(0f, y, 0f);

                var alpha = fadeCurve.Evaluate(p);
                _text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }
        }
    }
}