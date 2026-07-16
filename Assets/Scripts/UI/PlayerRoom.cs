using System;
using Chronex.Networking;
using Fusion;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View hiển thị 1 dòng player trong RoomScene. KHÔNG networked - đây là UI thuần,
/// dữ liệu thật lấy từ RoomPlayerNetworkObject qua Bind(). Local player mới cho phép
/// bấm để đổi Ready (client) - các dòng player khác chỉ hiển thị, không tương tác được.
/// </summary>
public class PlayerRoom : MonoBehaviour
{
    [Inject]
    private IAudioService _audioService;

    [SerializeField]
    private TextMeshProUGUI _playerName;

    [SerializeField]
    private TextMeshProUGUI _playerLevel;

    [SerializeField]
    private Image _playerRank;

    [SerializeField]
    private Image _player;

    [SerializeField]
    private TextMeshProUGUI _stateText;

    [SerializeField]
    private Image _buttonImage;

    [SerializeField]
    private Image _iconState;

    [SerializeField]
    private Sprite _readyButtonSprite;

    [SerializeField]
    private Sprite _tickSprite;

    [SerializeField]
    private Sprite _prepareButtonSprite;

    [SerializeField]
    private Sprite _xSprite;

    private RoomPlayerNetworkObject _data;
    private NetworkBehaviour.ChangeDetector _changeDetector;
    private bool _isLocalPlayer;

    public Action PressedCallback;

    public bool IsReady => _data != null && _data.IsReady;

    private void Awake()
    {
        ServiceLocator.Instance.Resolve(this);
    }

    /// <summary>
    /// Gọi bởi RoomSceneController ngay sau khi Instantiate prefab này cho 1 player.
    /// </summary>
    public void Bind(RoomPlayerNetworkObject data, bool isLocalPlayer)
    {
        _data = data;
        _isLocalPlayer = isLocalPlayer;
        _changeDetector = data.GetChangeDetector(NetworkBehaviour.ChangeDetector.Source.SimulationState);

        _data.OnDespawned += HandleDespawn;

        _playerLevel.text = "Lv. 1";

        Refresh();
    }


    private void HandleDespawn()
    {
        if (_data != null)
            _data.OnDespawned -= HandleDespawn;

        _data = null;
        _changeDetector = null;
    }

    private void Update()
    {
        if (_data == null)
            return;

        if (_data.Object == null)
            return;

        foreach (var _ in _changeDetector.DetectChanges(_data))
        {
            Refresh();
        }
    }

    public void OnPressed()
    {
        if (!_isLocalPlayer || _data.IsHost) return; // Host không tự bấm Ready cho chính mình.

        _audioService.PlaySfx(Audio.SFX_Click);
        PressedCallback?.Invoke();
    }

    private void Refresh()
    {
        _playerName.text = _data.PlayerName.ToString();
        UpdateReadyState();
    }

    private void UpdateReadyState()
    {
        bool ready = _data.IsReady;
        bool isHost = _data.IsHost;
        if (isHost)
        {
            _buttonImage.sprite = _readyButtonSprite;
            _iconState.sprite = _tickSprite;
            _stateText.text = "Host";
            return;
        }

        _buttonImage.sprite = ready ? _readyButtonSprite : _prepareButtonSprite;
        _iconState.sprite = ready ? _tickSprite : _xSprite;
        _stateText.text = ready ? "Ready" : "Prepare";
    }
}