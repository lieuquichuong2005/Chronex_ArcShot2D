using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoomDialog : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _inputRoomId;

    [SerializeField]
    private Button _leaveButton;

    [SerializeField]
    private Button _joinButton;

    public event Action<string> OnJoinRoom;

    private void Awake()
    {
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
        var roomId = _inputRoomId.text.Trim();

        if (string.IsNullOrEmpty(roomId))
            return;

        SetInteractable(false);

        OnJoinRoom?.Invoke(roomId);
    }
}