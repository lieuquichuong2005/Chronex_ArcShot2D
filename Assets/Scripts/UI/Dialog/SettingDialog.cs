using QuiChuong2005.Framework.Core;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Audio;
using QuiChuong2005.Framework.Services.Data;
using QuiChuong2005.Framework.Services.Dialog;
using UnityEngine;
using UnityEngine.UI;

public class SettingDialog : DialogBase
{
    [Inject]
    private IAudioService _audioService;

    [Inject]
    private IDataService _dataService;

    [SerializeField]
    private Slider _musicVolumnSlider;

    [SerializeField]
    private Slider _sfxVolumnSlider;

    private bool _isInitializing;

    private void Awake()
    {
        ServiceLocator.Instance.Resolve(this);
    }

    private void OnEnable()
    {
        // Đặt giá trị slider theo volume hiện tại TRƯỚC khi add listener - tránh
        // onValueChanged bắn ngay khi set SetValueWithoutNotify không dùng, dẫn tới
        // vòng lặp gọi ngược lại AudioService không cần thiết ngay lúc mở dialog.
        _isInitializing = true;
        _musicVolumnSlider.value = _audioService.MusicVolume;
        _sfxVolumnSlider.value = _audioService.SfxVolume;
        _isInitializing = false;

        _musicVolumnSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        _sfxVolumnSlider.onValueChanged.AddListener(OnSfxSliderChanged);
    }

    private void OnDisable()
    {
        _musicVolumnSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        _sfxVolumnSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
    }

    private void OnMusicSliderChanged(float value)
    {
        if (_isInitializing) return;

        _audioService.SetMusicVolume(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        if (_isInitializing) return;

        _audioService.SetSfxVolume(value);
    }

    public void OnCloseButtonPressed()
    {
        _audioService.PlaySfx(Audio.SFX_Click);
        SaveSettings();

        // TODO: đổi sang IPopupService.Close(this) khi Framework có Popup Service,
        _ = HideAsync();
    }

    private void SaveSettings()
    {
        var settings = _dataService.Get(DataKeys.Settings, new AudioSettingsData());

        settings.MusicVolume = _audioService.MusicVolume;
        settings.SfxVolume = _audioService.SfxVolume;

        _dataService.Set(DataKeys.Settings, settings);
    }
}