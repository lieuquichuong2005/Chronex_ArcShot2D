using UnityEngine;
using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Services.Data;
using QuiChuong2005.Framework.Services.Data.Storage;
using QuiChuong2005.Framework.Services.Data.Serialization;
using QuiChuong2005.Framework.Services.Scenes;

namespace QuiChuong2005.Framework.Bootstrap
{
    public class Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            var pathProvider = new PersistentDataPathProvider();
            var storage = new FileDataStorage(pathProvider);
            var serializer = new JsonDataSerializer();

            ServiceLocator.Register<IDataService>(
                new DataService(storage, serializer));

            ServiceLocator.Register<ISceneService>(
                new SceneService());
        }
    }
}