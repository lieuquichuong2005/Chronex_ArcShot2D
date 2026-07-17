using System;
using Chronex.Services.Networking;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArcShot.Networking
{
    /// <summary>
    /// Đọc input local (bàn phím + nút mobile) mỗi frame, ghi kết quả cuối cùng vào
    /// NetworkRunnerCallbacks - nơi duy nhất Fusion lấy dữ liệu gửi lên qua OnInput().
    /// Mobile input được ưu tiên hơn bàn phím khi cả 2 cùng có giá trị (giống PlayerController/GunController offline cũ).
    /// </summary>
    public sealed class LocalInputReader : MonoBehaviour
    {
        [SerializeField]
        private LevelScene _scene;

        [Inject]
        private INetworkService _networkService;

        private NetworkRunnerCallbacks _callbacks;

        private float _mobileMoveInput;
        private float _mobileAimInput;
        private bool _mobileShootHolding;

        private void Awake()
        {
            ServiceLocator.Instance.Resolve(this);
        }

        private void Start()
        {
            _callbacks = _networkService.Runner.GetComponent<NetworkRunnerCallbacks>();

            if (_callbacks == null)
            {
                Debug.LogError("[LocalInputReader] Không tìm thấy NetworkRunnerCallbacks trên Runner.");
                return;
            }

            BindMobileButtons();
        }

        private void Update()
        {
            if (_callbacks == null) return;

            float keyboardMove = Input.GetAxisRaw("Horizontal");
            _callbacks.LocalMoveAxis = _mobileMoveInput != 0f ? _mobileMoveInput : keyboardMove;

            float keyboardAim = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyboardAim = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyboardAim = -1f;
            _callbacks.LocalAimAxis = _mobileAimInput != 0f ? _mobileAimInput : keyboardAim;

            bool keyboardShoot = Input.GetKey(KeyCode.Space);
            _callbacks.LocalShootHeld = _mobileShootHolding || keyboardShoot;
        }

        private void BindMobileButtons()
        {
            BindHold(_scene.MoveLeftButton, () => _mobileMoveInput = -1f, () =>
            {
                if (_mobileMoveInput < 0) _mobileMoveInput = 0f;
            });
            BindHold(_scene.MoveRightButton, () => _mobileMoveInput = 1f, () =>
            {
                if (_mobileMoveInput > 0) _mobileMoveInput = 0f;
            });

            BindHold(_scene.AimUpButton, () => _mobileAimInput = 1f, () =>
            {
                if (_mobileAimInput > 0) _mobileAimInput = 0f;
            });
            BindHold(_scene.AimDownButton, () => _mobileAimInput = -1f, () =>
            {
                if (_mobileAimInput < 0) _mobileAimInput = 0f;
            });

            BindHold(_scene.ShootButton, () => _mobileShootHolding = true, () => _mobileShootHolding = false);
        }

        private void BindHold(EventTrigger trigger, Action onDown, Action onUp)
        {
            if (trigger == null) return;

            trigger.triggers.Clear();

            AddEntry(trigger, EventTriggerType.PointerDown, onDown);
            AddEntry(trigger, EventTriggerType.PointerUp, onUp);
        }

        private void AddEntry(EventTrigger trigger, EventTriggerType type, Action callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }
    }
}