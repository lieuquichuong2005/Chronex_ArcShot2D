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

    public Action OnClick;

    private void Awake()
    {
        ServiceLocator.Instance.Resolve(this);
    }

    public void OnPressed()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        OnClick?.Invoke();
    }
}