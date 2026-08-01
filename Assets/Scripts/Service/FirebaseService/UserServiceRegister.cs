using Chronex.Services.Profile;
using Cysharp.Threading.Tasks;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Services.Data;
using QuiChuong2005.Framework.Services.Data.Serialization;
using QuiChuong2005.Framework.Services.Data.Storage;
using QuiChuong2005.Framework.Services.Store;
using UnityEngine;

public static class UserServiceRegister
{
    public static UniTask RegisterAsync(string uid)
    {
        Debug.Log("Registering user " + uid);

        var locator = ServiceLocator.Instance;

        var dataService = new DataService(
            new FileDataStorage(new UserScopedDataPathProvider(uid)),
            new JsonDataSerializer());
        locator.Register<IDataService>(dataService);

        locator.Register<IPlayerProfileService>(new PlayerProfileService(dataService));

        var storeService = new StoreService(dataService);
        locator.Register<IStoreService>(storeService);

        return storeService.InitializeAsync();
    }

    /// <summary>
    /// Gỡ toàn bộ service gắn với user hiện tại. BẮT BUỘC gọi trước khi Logout /
    /// trước khi RegisterAsync cho user khác - nếu không ServiceLocator sẽ giữ
    /// instance cũ, khiến Register lần 2 bị lỗi hoặc dùng nhầm data của user cũ.
    /// </summary>
    public static void Unregister()
    {
        var locator = ServiceLocator.Instance;

        // Flush mọi thay đổi đang chờ (Set với autoSave=false) trước khi huỷ,
        // tránh mất dữ liệu của user vừa logout.
        if (locator.TryGet<IDataService>(out var dataService))
        {
            dataService.Save();
        }

        locator.Unregister<IDataService>();
        locator.Unregister<IPlayerProfileService>();
        locator.Unregister<IStoreService>();
    }
}