using Chronex.Services.Profile;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Services.Data;
using QuiChuong2005.Framework.Services.Data.Serialization;
using QuiChuong2005.Framework.Services.Data.Storage;
using UnityEngine;

public static class UserServiceRegister
{
    public static void Register(string uid)
    {
        Debug.Log("Registering user " + uid);
        
        var locator = ServiceLocator.Instance;

        var dataService = new DataService(
            new FileDataStorage(
                new UserScopedDataPathProvider(uid)),
            new JsonDataSerializer());

        locator.Register<IDataService>(dataService);

        locator.Register<IPlayerProfileService>(
            new PlayerProfileService(dataService));
    }
}