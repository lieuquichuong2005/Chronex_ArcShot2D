using System;
using System.Collections.Generic;

namespace QuiChuong2005.Framework.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service)
        {
            _services[typeof(T)] = service;
        }

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;

            throw new Exception($"Service {typeof(T).Name} is not registered.");
        }
    }
}