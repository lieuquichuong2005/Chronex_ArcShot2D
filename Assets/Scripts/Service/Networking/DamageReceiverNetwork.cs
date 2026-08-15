using ArcShot;
using ArcShot.Networking;
using Fusion;
using UnityEngine;

namespace Arcshot.Networking
{
    public sealed class DamageReceiverNetwork : NetworkBehaviour
    {
        [SerializeField]
        private HealthNetworkController _health;

        public void HostReceiveDamage(int damage)
        {
            Debug.Log(
                $"[DamageReceiverNetwork] HostReceiveDamage gọi, damage={damage}, HasStateAuthority={Object.HasStateAuthority}, _health null={_health == null}");

            if (Object.HasStateAuthority) _health.HostTakeDamage(damage);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ShowDamageText(int damage, bool isCritical)
        {
            if (DamageTextSpawner.Instance != null)
                DamageTextSpawner.Instance.Spawn(transform.position, damage, isCritical);
        }
    }
}