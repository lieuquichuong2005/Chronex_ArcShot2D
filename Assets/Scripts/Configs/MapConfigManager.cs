using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "MapConfigsManager", menuName = "ArcShot/Manager/MapConfigsManager")]
public class MapConfigsManager : ScriptableObject
{
    public List<MapConfig> MapConfigs = new();

    private Dictionary<string, MapConfig> mapConfigMap = new();

    private void InitMap()
    {
        mapConfigMap = new Dictionary<string, MapConfig>();
        foreach (var mapConfig in MapConfigs)
        {
            mapConfigMap.Add(mapConfig.MapId, mapConfig);
        }
    }

    public MapConfig GetMapConfig(string mapId)
    {
        if (mapConfigMap == null || mapConfigMap.Count == 0)
        {
            InitMap();
        }

        return mapConfigMap[mapId];
    }

    [Button]
    public void ClearMapConfigs()
    {
        MapConfigs.Clear();
    }
}