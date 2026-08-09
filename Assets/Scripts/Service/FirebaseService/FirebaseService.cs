using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using QuiChuong2005;
using UnityEngine;

namespace Chronex.Services
{
    /// <summary>
    /// Chỉ chịu trách nhiệm khởi tạo Firebase SDK và expose instance dùng chung.
    /// Không chứa nghiệp vụ Auth (xem AuthenticationService).
    /// </summary>
    public sealed class FirebaseService : IInitializableService
    {
        public FirebaseApp App { get; private set; }
        public FirebaseAuth Auth { get; private set; }
        public bool IsReady { get; private set; }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            DependencyStatus status = await FirebaseApp
                .CheckAndFixDependenciesAsync()
                .AsUniTask();

            cancellationToken.ThrowIfCancellationRequested();

            if (status != DependencyStatus.Available)
            {
                throw new InvalidOperationException(
                    $"Firebase dependency không khả dụng: {status}");
            }

            App = FirebaseApp.DefaultInstance;
            Auth = FirebaseAuth.DefaultInstance;
            IsReady = true;

            Debug.Log("[FirebaseService] Sẵn sàng.");
        }
    }
}
