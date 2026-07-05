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
    [Header("HUD")] [SerializeField] private Image _firePower;
    [SerializeField] private Image _stamina;
    [SerializeField] private TextMeshProUGUI _fireAngle;
    [SerializeField] private TextMeshProUGUI _timeTurnRemain;
    [SerializeField] private Button _skipTurnButton;


    [Header("Movement Buttons")] [SerializeField]
    private EventTrigger _moveLeftButton;

    [SerializeField] private EventTrigger _moveRightButton;

    [Header("Aim Buttons")] [SerializeField]
    private EventTrigger _aimUpButton;

    [SerializeField] private EventTrigger _aimDownButton;

    [Header("Shoot Button")] [SerializeField]
    private EventTrigger _shootButton;

    public Image FirePower => _firePower;
    public Image Stamina => _stamina;
    public TextMeshProUGUI FireAngle => _fireAngle;
    public TextMeshProUGUI TimeTurnRemain => _timeTurnRemain;
    public Button SkipTurnButton => _skipTurnButton;

    public EventTrigger MoveLeftButton => _moveLeftButton;
    public EventTrigger MoveRightButton => _moveRightButton;
    public EventTrigger AimUpButton => _aimUpButton;
    public EventTrigger AimDownButton => _aimDownButton;
    public EventTrigger ShootButton => _shootButton;
}