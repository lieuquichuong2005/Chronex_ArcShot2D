using System;
using Cysharp.Threading.Tasks;
using QuiChuong2005.Framework.Core.DI;
using QuiChuong2005.Framework.Services.Audio;
using QuiChuong2005.Framework.Services.Scenes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultScene : MonoBehaviour
{
    [Header("UI REFERENCES")]
    [SerializeField]
    private Image _backgroundImage;

    [SerializeField]
    private Image _titleImage;

    [SerializeField]
    private Image _mvp;

    [SerializeField]
    private Image _character;

    [SerializeField]
    private Image _base;

    [SerializeField]
    private TextMeshProUGUI _playerName;

    [SerializeField]
    private TextMeshProUGUI _damageDealtText;

    [SerializeField]
    private TextMeshProUGUI _matchDurationText;

    [SerializeField]
    private TextMeshProUGUI _damageTakenText;

    [SerializeField]
    private TextMeshProUGUI _accuracyText;

    [SerializeField]
    private TextMeshProUGUI _turnsText;

    [SerializeField]
    private TextMeshProUGUI _critText;

    [SerializeField]
    private TextMeshProUGUI _coinBonusText;

    [SerializeField]
    private TextMeshProUGUI _expBonusText;

    [SerializeField]
    private TextMeshProUGUI _levelShapeText;

    [SerializeField]
    private TextMeshProUGUI _levelText;

    [SerializeField]
    private Image _expProcess;

    [SerializeField]
    private TextMeshProUGUI _expProcessText;

    [SerializeField]
    private TextMeshProUGUI _expBonus;

    [SerializeField]
    private Button _backToMenuButton;

    [Header("ASSET REFERENCES")]
    [SerializeField]
    private Sprite _bgVictory;

    [SerializeField]
    private Sprite _bgDefeated;

    [SerializeField]
    private Sprite _titleVictory, _titleDefeated;

    [SerializeField]
    private Sprite _mvpVictory, _mvpDefeated;

    [SerializeField]
    private Sprite _characterVictory, _characterDefeated;

    [SerializeField]
    private Sprite _baseVictory, _baseDefeated;

    [Header("SCENE SETTINGS")]
    [SerializeField]
    private float _expBarAnimationDuration = 0.8f;

    [SerializeField]
    private float _delayBetweenLevelUps = 0.15f;

    [Inject]
    private ISceneService _sceneService;

    [Inject]
    private IAudioService _audioService;

    private MatchResultData _data;

    private void Awake()
    {
        _backToMenuButton.onClick.AddListener(() => { _ = OnBackToMenuClicked(); });
    }

    private void OnDestroy()
    {
        _backToMenuButton.onClick.RemoveListener(() => { _ = OnBackToMenuClicked(); });
    }

    public async UniTask Initialize(MatchResultData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));

        SetupStaticInfo(data);
        await AnimateExpGainAsync(data.CurrentExp, data.ExpBonus, data.RequiredExp, data.CurrentLevel);
    }

    private void SetupStaticInfo(MatchResultData data)
    {
        // Nền + tiêu đề Thắng / Thua
        _backgroundImage.sprite = data.IsVictory ? _bgVictory : _bgDefeated;
        _titleImage.sprite = data.IsVictory ? _titleVictory : _titleDefeated;

        // MVP: hiện tại chỉ có 1vs1 nên mặc định người thắng luôn là MVP
        _mvp.sprite = data.IsVictory ? _mvpVictory : _mvpDefeated;
        _base.sprite = data.IsVictory ? _baseVictory : _baseDefeated;
        _character.sprite = data.IsVictory ? _characterVictory : _characterDefeated;

        _mvp.gameObject.SetActive(data.IsMVP);

        // Tên người chơi lấy đúng từ LevelScene truyền qua
        _playerName.text = data.PlayerName;

        // Các chỉ số trận đấu
        _damageDealtText.text = Mathf.RoundToInt(data.DamageDealt).ToString();
        _damageTakenText.text = Mathf.RoundToInt(data.DamageTaken).ToString();
        _matchDurationText.text = FormatDuration(data.MatchDuration);
        _accuracyText.text = $"{data.Accuracy:0.#}%";
        _turnsText.text = data.Turns.ToString();
        _critText.text = data.CritCount.ToString();

        // Phần thưởng (coin + exp cộng thêm)
        _coinBonusText.text = $"+{data.CoinBonus}";
        _expBonusText.text = $"+{data.ExpBonus}";

        // Icon Level hiện tại, kèm text level bên cạnh
        _levelShapeText.text = data.CurrentLevel.ToString();
        _levelText.text = $"Lv.{data.CurrentLevel}";

        // Trạng thái ban đầu của thanh EXP trước khi chạy animation
        _expProcess.type = Image.Type.Filled; // nhớ set Image Type = Filled trong Inspector
        _expProcess.fillAmount = data.RequiredExp > 0
            ? (float)data.CurrentExp / data.RequiredExp
            : 0f;
        _expProcessText.text = $"{data.CurrentExp}/{data.RequiredExp}";
        _expBonus.text = string.Empty;
    }

    private async UniTask AnimateExpGainAsync(int startExp, int expGain, int requiredExp, int startLevel)
    {
        if (expGain <= 0 || requiredExp <= 0) return;

        var currentExp = startExp;
        var currentLevel = startLevel;
        var remainingGain = expGain;

        while (remainingGain > 0)
        {
            var expNeeded = requiredExp - currentExp;
            var expToAdd = Mathf.Min(remainingGain, expNeeded);
            var targetExp = currentExp + expToAdd;

            await FillExpBarAsync(currentExp, targetExp, requiredExp);

            currentExp = targetExp;
            remainingGain -= expToAdd;

            if (currentExp >= requiredExp && remainingGain > 0)
            {
                currentLevel++;
                currentExp = 0;

                _levelShapeText.text = currentLevel.ToString();
                _levelText.text = $"Lv.{currentLevel}";
                _expProcess.fillAmount = 0f;
                _expProcessText.text = $"0/{requiredExp}";

                await UniTask.Delay(TimeSpan.FromSeconds(_delayBetweenLevelUps));
            }
        }

        _expBonus.text = $"+{expGain} EXP";
    }

    private async UniTask FillExpBarAsync(int fromExp, int toExp, int requiredExp)
    {
        var fromFill = (float)fromExp / requiredExp;
        var toFill = (float)toExp / requiredExp;
        var elapsed = 0f;

        while (elapsed < _expBarAnimationDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / _expBarAnimationDuration);

            _expProcess.fillAmount = Mathf.Lerp(fromFill, toFill, t);

            var displayExp = Mathf.RoundToInt(Mathf.Lerp(fromExp, toExp, t));
            _expProcessText.text = $"{displayExp}/{requiredExp}";

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        _expProcess.fillAmount = toFill;
        _expProcessText.text = $"{toExp}/{requiredExp}";
    }

    private string FormatDuration(float seconds)
    {
        var totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
        var minutes = totalSeconds / 60;
        var secs = totalSeconds % 60;
        return $"{minutes:00}:{secs:00}";
    }

    private async UniTask OnBackToMenuClicked()
    {
        var scene = await _sceneService.LoadSceneAsync<MenuScene>(nameof(MenuScene));
    }
}