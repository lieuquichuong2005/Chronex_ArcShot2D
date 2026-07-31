using Chronex.Services.Profile;
using Cysharp.Threading.Tasks;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Services.Data;
using QuiChuong2005.Framework.Services.Data.Serialization;
using QuiChuong2005.Framework.Services.Data.Storage;
using QuiChuong2005.Framework.Services.Store;

public static class UserServiceRegister
{
    public static async UniTask RegisterAsync(string uid)
    {
        var locator = ServiceLocator.Instance;

        var dataService = new DataService(
            new FileDataStorage(new UserScopedDataPathProvider(uid)),
            new JsonDataSerializer());
        locator.Register<IDataService>(dataService);

        locator.Register<IPlayerProfileService>(new PlayerProfileService(dataService));

        var storeService = new StoreService(dataService);
        await storeService.InitializeAsync();
        locator.Register<IStoreService>(storeService);
    }
}