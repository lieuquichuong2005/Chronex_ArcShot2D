using Chronex.Services;
using Chronex.Services.Profile;
using Cysharp.Threading.Tasks;
using Game.Session;
using QuiChuong2005;
using UnityEngine;

public static class UserServiceRegister
{
    private static ISessionService _sessionService;

    public static async UniTask RegisterAsync(string uid)
    {
        Debug.Log("Registering user " + uid);

        var locator = ServiceLocator.Instance;

        var dataService = new DataService(
            new FileDataStorage(new UserScopedDataPathProvider(uid)),
            new JsonDataSerializer());
        locator.Register<IDataService>(dataService);

        locator.Register<IPlayerProfileService>(new PlayerProfileService(dataService, uid));

        var storeService = new StoreService(dataService);
        await storeService.InitializeAsync();
        locator.Register<IStoreService>(storeService);

        // Claim session NGAY SAU CÙNG (sau khi mọi service khác đã sẵn sàng) - nếu bị đá trước
        // cả khi các service khác kịp đăng ký, xử lý sẽ phức tạp hơn không cần thiết.
        _sessionService = new SessionService(uid);
        _sessionService.OnKickedFromSession += HandleKickedFromSession;
        await _sessionService.ClaimSessionAsync();
        locator.Register<ISessionService>(_sessionService);
    }

    /// <summary>
    /// Gỡ toàn bộ service gắn với user hiện tại. Gọi TRƯỚC khi Logout / trước khi
    /// RegisterAsync cho user khác - thiếu 1 cái là account sau sẽ dùng nhầm service account trước.
    /// </summary>
    public static void Unregister()
    {
        var locator = ServiceLocator.Instance;

        if (locator.TryGet<IDataService>(out var dataService)) dataService.Save();

        if (_sessionService != null)
        {
            _sessionService.OnKickedFromSession -= HandleKickedFromSession;
            _sessionService.StopListening();
            _sessionService = null;
        }

        locator.Unregister<IDataService>();
        locator.Unregister<IStoreService>();
        locator.Unregister<IPlayerProfileService>();
        locator.Unregister<ISessionService>();
    }

    /// <summary>Chạy khi phát hiện account này vừa đăng nhập ở 1 thiết bị KHÁC (mình bị đá).</summary>
    private static void HandleKickedFromSession()
    {
        Debug.LogWarning("[UserServiceRegister] Bị đăng xuất do tài khoản đăng nhập ở thiết bị khác.");

        Unregister();

        var locator = ServiceLocator.Instance;

        if (locator.TryGet<AuthenticationService>(out var authService)) authService.Logout();

        // TODO: nếu muốn hiện dialog "Tài khoản đã đăng nhập ở thiết bị khác" trước khi chuyển
        // scene, gọi IDialogService ở đây (nếu DialogService vẫn còn sống - nó không phụ thuộc
        // uid nên không bị Unregister ở trên).

        locator.Get<ISceneService>().LoadSceneAsync<LogInScene>(nameof(LogInScene)).Forget();
    }
}