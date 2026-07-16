using System;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private bool _isReady;

    public Action PressedCallback;

    public bool IsReady
    {
        get => _isReady;
        set
        {
            _isReady = value;
            UpdateReadyState();
        }
    }

    private void Awake()
    {
        ServiceLocator.Instance.Resolve(this);
    }

    public void OnPressed()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        PressedCallback?.Invoke();
    }

    private void UpdateReadyState()
    {
        _buttonImage.sprite = IsReady ? _readyButtonSprite : _prepareButtonSprite;
        _iconState.sprite = IsReady ? _tickSprite : _prepareButtonSprite;
    }
}