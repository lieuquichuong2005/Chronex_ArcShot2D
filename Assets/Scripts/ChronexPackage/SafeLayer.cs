using UnityEngine;

namespace Game.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeLayerArea : MonoBehaviour
    {
        private RectTransform rectTransform;

        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (NeedRefresh()) ApplySafeArea();
        }

        private bool NeedRefresh()
        {
            return lastSafeArea != Screen.safeArea
                   || lastScreenSize.x != Screen.width
                   || lastScreenSize.y != Screen.height
                   || lastOrientation != Screen.orientation;
        }

        private void ApplySafeArea()
        {
            var safeArea = Screen.safeArea;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;

            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}