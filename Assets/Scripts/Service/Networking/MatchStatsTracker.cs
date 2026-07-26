using System.Linq;
using ArcShot;
using Fusion;
using UnityEngine;

namespace Chronex.Networking
{
    /// <summary>
    /// Theo dõi tiến trình trận đấu: thời gian, số turn, và điều kiện kết thúc (chỉ còn 1 team).
    /// Spawn runtime bởi Host giống TurnManagerNetwork, KHÔNG đặt sẵn trong scene (lý do tương tự
    /// đã giải thích ở TurnManagerNetwork - Fusion không nhận scene-baked NetworkObject ở đây).
    /// </summary>
    public sealed class MatchStatsTracker : NetworkBehaviour
    {
        public static MatchStatsTracker Instance { get; private set; }

        [Networked] public float MatchStartTime { get; set; }
        [Networked] public NetworkBool MatchEnded { get; set; }

        public System.Action<int> OnMatchEnded; // param: winningTeamId

        public override void Spawned()
        {
            Instance = this;

            if (Object.HasStateAuthority)
            {
                MatchStartTime = Runner.SimulationTime;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this) Instance = null;
        }

        public float GetMatchDuration() => Runner.SimulationTime - MatchStartTime;


        /// <summary>
        /// Chỉ Host gọi - từ PlayerNetworkController.HandleDeath. Kiểm tra còn bao nhiêu team
        /// còn người sống; nếu chỉ còn 1 -> kết thúc trận, báo toàn bộ client qua RPC.
        /// </summary>
        public void HostCheckMatchEnd()
        {
            if (!Object.HasStateAuthority || MatchEnded) return;

            var aliveTeams = PlayerNetworkController.AllPlayers
                .Where(p => p.gameObject.activeSelf)
                .Select(p => p.TeamId)
                .Distinct()
                .ToList();

            if (aliveTeams.Count <= 1)
            {
                MatchEnded = true;
                int winningTeam = aliveTeams.Count == 1 ? aliveTeams[0] : -1; // -1 = hoà (hiếm khi xảy ra)
                RPC_MatchEnded(winningTeam);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_MatchEnded(int winningTeamId)
        {
            OnMatchEnded?.Invoke(winningTeamId);
        }
    }
}