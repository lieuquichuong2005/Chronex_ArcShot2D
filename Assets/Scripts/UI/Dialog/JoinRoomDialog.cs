using System;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Audio;
using QuiChuong2005.Framework.Services.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoomDialog : DialogBase
{
    [Inject]
    private IAudioService _audioService;

    [SerializeField]
    private TMP_InputField _inputRoomId;

    [SerializeField]
    private Button _leaveButton;

    [SerializeField]
    private Button _joinButton;

    public event Action<string> OnJoinRoom;

    private void Awake()
    {
        ServiceLocator.Instance.Resolve(this);

        _leaveButton.onClick.AddListener(Hide);
        _joinButton.onClick.AddListener(OnClickJoin);
        _inputRoomId.onSubmit.AddListener(_ => OnClickJoin());

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        _inputRoomId.text = "";

        _joinButton.interactable = true;

        _inputRoomId.Select();
        _inputRoomId.ActivateInputField();
    }

    public void Hide()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        gameObject.SetActive(false);
    }

    public void SetInteractable(bool value)
    {
        _joinButton.interactable = value;
        _leaveButton.interactable = value;
        _inputRoomId.interactable = value;
    }

    private void OnClickJoin()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        var roomId = _inputRoomId.text.Trim();

        if (string.IsNullOrEmpty(roomId))
            return;

        SetInteractable(false);

        OnJoinRoom?.Invoke(roomId);
    }
}