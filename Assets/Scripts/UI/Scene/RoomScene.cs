using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chronex.UI.Room
{
    public sealed class RoomScene : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField]
        private PlayerRoom _playerRoom;

        [SerializeField]
        private Transform _playerParent;

        [Space(5)]
        [SerializeField]
        private Transform _chatParent;

        [SerializeField]
        private TMP_InputField _chatInput;

        [SerializeField]
        private Button _sendButton;

        [Space(5)]
        [Header("Room Settings")]
        [SerializeField]
        private TextMeshProUGUI _roomId;

        [SerializeField]
        private TextMeshProUGUI _maxPlayer;

        [SerializeField]
        private Image _map;

        [Space(5)]
        [Header("Buttons")]
        [SerializeField]
        private Button _leaveRoomButton;

        [SerializeField]
        private Button _inviteButton;

        [SerializeField]
        private Button _readyStartButton;

        private void Awake()
        {
            Debug.Log("[RoomScene] Đã vào phòng.");
            // TODO: hiển thị danh sách player, nút Ready/Start - làm ở bước sau
        }
    }
}