using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using System.Linq;
using UnityEditor;

[CreateAssetMenu(fileName = "MapConfig", menuName = "ArcShot2D/Configs/MapConfig")]
public class MapConfig : ScriptableObject
{
    public string MapId;
    public Sprite IconMap;
    public GameObject MapPrefab;
    public bool CanDig;
    public List<Vector2> SpawnPoints_Left = new List<Vector2>();
    public List<Vector2> SpawnPoints_Right = new List<Vector2>();

    [Button]
    public void AutoGetSpawnPoints()
    {
        if (MapPrefab == null)
        {
            Debug.LogWarning($"[MapConfig] {name}: MapPrefab chưa được gán.");
            return;
        }

        Transform spawnPointsRoot = FindDeepChild(MapPrefab.transform, "SpawnPoints");

        if (spawnPointsRoot == null)
        {
            Debug.LogWarning($"[MapConfig] {name}: Không tìm thấy object 'SpawnPoints' trong MapPrefab.");
            return;
        }

        var leftPoints = new List<(int index, Vector2 pos)>();
        var rightPoints = new List<(int index, Vector2 pos)>();

        foreach (Transform child in spawnPointsRoot)
        {
            // Quy đổi về toạ độ tương đối gốc MapPrefab, không phải tương đối SpawnPoints,
            // để đúng bất kể map được đặt ở vị trí nào trong scene.
            Vector2 relativePos = MapPrefab.transform.InverseTransformPoint(child.position);

            if (child.name.StartsWith("Left_") && TryGetIndex(child.name, out int leftIndex))
            {
                leftPoints.Add((leftIndex, relativePos));
            }
            else if (child.name.StartsWith("Right_") && TryGetIndex(child.name, out int rightIndex))
            {
                rightPoints.Add((rightIndex, relativePos));
            }
        }

        SpawnPoints_Left.Clear();
        SpawnPoints_Right.Clear();

        SpawnPoints_Left.AddRange(leftPoints.OrderBy(p => p.index).Select(p => p.pos));
        SpawnPoints_Right.AddRange(rightPoints.OrderBy(p => p.index).Select(p => p.pos));

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        Debug.Log(
            $"[MapConfig] {name}: Lấy được {SpawnPoints_Left.Count} spawn Left, {SpawnPoints_Right.Count} spawn Right.");
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private static bool TryGetIndex(string objName, out int index)
    {
        index = 0;

        int underscoreIndex = objName.LastIndexOf('_');

        if (underscoreIndex < 0 || underscoreIndex == objName.Length - 1)
            return false;

        string suffix = objName.Substring(underscoreIndex + 1);

        return int.TryParse(suffix, out index);
    }
}

[CreateAssetMenu(fileName = "MapConfigsManager", menuName = "ArcShot2D/Manager/MapConfigsManager")]
public class MapConfigsManager : ScriptableObject
{
    public List<MapConfig> MapConfigs = new();

    private Dictionary<string, MapConfig> mapConfigMap = new Dictionary<string, MapConfig>();

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