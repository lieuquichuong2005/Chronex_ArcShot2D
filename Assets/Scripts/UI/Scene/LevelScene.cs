using System.Collections.Generic;
using Chronex.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Chỉ chứa reference tới các thành phần UI trong scene.
/// Không xử lý logic game - logic nằm ở LevelView.
/// </summary>
public class LevelScene : MonoBehaviour
{
    [SerializeField]
    private Image _background;

    [Header("HUD")]
    [SerializeField]
    private Image _firePower;

    [SerializeField]
    private Image _stamina;

    [SerializeField]
    private TextMeshProUGUI _fireAngle;

    [SerializeField]
    private TextMeshProUGUI _timeTurnRemain;

    [SerializeField]
    private TextMeshProUGUI _turn;

    [SerializeField]
    private Button _skipTurnButton;

    [SerializeField]
    private Image _windDirection;

    [SerializeField]
    private GameObject[] _windLevelObjects;

    [Header("Movement Buttons")]
    [SerializeField]
    private EventTrigger _moveLeftButton;

    [SerializeField]
    private EventTrigger _moveRightButton;

    [Header("Aim Buttons")]
    [SerializeField]
    private EventTrigger _aimUpButton;

    [SerializeField]
    private EventTrigger _aimDownButton;

    [Header("Shoot Button")]
    [SerializeField]
    private EventTrigger _shootButton;

    [SerializeField]
    private GameObject _inputLayer;

    [SerializeField]
    private GameObject _bottomLayer;

    public Image FirePower => _firePower;
    public Image Stamina => _stamina;
    public TextMeshProUGUI FireAngle => _fireAngle;
    public TextMeshProUGUI TimeTurnRemain => _timeTurnRemain;
    public TextMeshProUGUI Turn => _turn;
    public Button SkipTurnButton => _skipTurnButton;

    public EventTrigger MoveLeftButton => _moveLeftButton;
    public EventTrigger MoveRightButton => _moveRightButton;
    public EventTrigger AimUpButton => _aimUpButton;
    public EventTrigger AimDownButton => _aimDownButton;
    public EventTrigger ShootButton => _shootButton;

    public void SetTurn(bool isActive)
    {
        _inputLayer.SetActive(isActive);
        _bottomLayer.SetActive(isActive);
        _skipTurnButton.gameObject.SetActive(isActive);
    }

    public void SetWind(int direction, int level)
    {
        if (direction != 0)
        {
            var scale = _windDirection.transform.localScale;
            scale.x = direction < 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _windDirection.transform.localScale = scale;
        }

        for (var i = 0; i < _windLevelObjects.Length; i++)
            _windLevelObjects[i].SetActive(i < level);
    }

    public void SetBackground(Sprite background)
    {
        if (background == null) return;
        _background.sprite = background;
    }
}