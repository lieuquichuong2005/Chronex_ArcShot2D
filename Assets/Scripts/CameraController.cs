using ArcShot.Networking;
using Unity.Cinemachine;
using UnityEngine;

namespace ArcShot
{
    /// <summary>
    /// Điều khiển target camera theo dõi: player đang tới lượt, hoặc viên đạn khi đang bay.
    /// LevelView gọi các method public để đổi target theo diễn biến turn.
    /// </summary>
    public class CameraFollowController : MonoBehaviour
    {
        [SerializeField]
        private CinemachineCamera vcam;

        public void FollowPlayer(PlayerNetworkController playerNetwork)
        {
            if (playerNetwork == null)
                return;

            SetTarget(playerNetwork.transform);
        }

        public void FollowBullet(BulletNetwork bullet)
        {
            if (bullet == null)
                return;

            SetTarget(bullet.transform);
        }

        private void SetTarget(Transform target)
        {
            if (vcam == null)
                return;

            vcam.Follow = target;
        }
    }
}