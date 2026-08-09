using System;
using Chronex.Services.Profile;
using Cysharp.Threading.Tasks;
using QuiChuong2005;
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

    [Inject]
    private IPlayerProfileService _playerProfileService;

    [Inject]
    private Chronex.Services.Networking.INetworkService _networkService;

    private MatchResultData _data;
    private bool _hasAppliedExp;

    private void Awake()
    {
        ServiceLocator.Instance.Resolve(this);

        _backToMenuButton.onClick.AddListener(() => { _ = OnBackToMenuClicked(); });
    }

    private void OnDestroy()
    {
        _backToMenuButton.onClick.RemoveListener(() => { _ = OnBackToMenuClicked(); });
    }

    public async UniTask Initialize(MatchResultData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));

        var startExp = _playerProfileService.CurrentExp;
        var startLevel = _playerProfileService.Level;
        var requiredExp = _playerProfileService.RequiredExp;

        SetupStaticInfo(data, startExp, startLevel, requiredExp);

        if (!_hasAppliedExp)
        {
            _hasAppliedExp = true;
            _playerProfileService.AddExp(data.ExpBonus);
        }

        await AnimateExpGainAsync(startExp, data.ExpBonus, requiredExp, startLevel);
    }

    private void SetupStaticInfo(MatchResultData data, int startExp, int startLevel, int requiredExp)
    {
        _backgroundImage.sprite = data.IsVictory ? _bgVictory : _bgDefeated;
        _titleImage.sprite = data.IsVictory ? _titleVictory : _titleDefeated;

        _mvp.sprite = data.IsVictory ? _mvpVictory : _mvpDefeated;
        _base.sprite = data.IsVictory ? _baseVictory : _baseDefeated;
        _character.sprite = data.IsVictory ? _characterVictory : _characterDefeated;

        _mvp.gameObject.SetActive(data.IsMVP);

        _playerName.text = _playerProfileService.PlayerName;

        _damageDealtText.text = Mathf.RoundToInt(data.DamageDealt).ToString();
        _damageTakenText.text = Mathf.RoundToInt(data.DamageTaken).ToString();
        _matchDurationText.text = FormatDuration(data.MatchDuration);
        _accuracyText.text = $"{data.Accuracy:0.#}%";
        _turnsText.text = data.Turns.ToString();
        _critText.text = data.CritCount.ToString();

        _coinBonusText.text = $"+{data.CoinBonus}";
        _expBonusText.text = $"+{data.ExpBonus}";

        _levelShapeText.text = startLevel.ToString();
        _levelText.text = $"Lv.{startLevel}";

        _expProcess.type = Image.Type.Filled; // nhớ set Image Type = Filled trong Inspector
        _expProcess.fillAmount = requiredExp > 0
            ? (float)startExp / requiredExp
            : 0f;
        _expProcessText.text = $"{startExp}/{requiredExp}";
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
        await _networkService.LeaveRoomAsync();

        var scene = await _sceneService.LoadSceneAsync<MenuScene>(nameof(MenuScene));
    }
}