/// <summary>
/// Data lưu cài đặt âm thanh của người chơi qua DataService (key = DataKeys.Settings).
/// Nếu project đã có sẵn 1 class SettingsData khác (gộp cả Graphics, Language...),
/// thay class này bằng field MusicVolume/SfxVolume trong class đó, không cần tách riêng.
/// </summary>
public sealed class AudioSettingsData
{
    public float MasterVolume = 1f;
    public float MusicVolume = 1f;
    public float SfxVolume = 1f;
    public bool IsMuted;
}