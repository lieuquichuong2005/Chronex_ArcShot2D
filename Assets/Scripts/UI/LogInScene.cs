using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogInScene : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField]
    private TMP_InputField _emailInput;

    [SerializeField]
    private TMP_InputField _passwordInput;

    [Header("UI Panels")]
    [SerializeField]
    private GameObject _loginPanel;

    [SerializeField]
    private GameObject _gamePanel;

    [Header("Thông báo")]
    [SerializeField]
    private TMP_Text _messageText;

    private void OnEnable()
    {
        FirebaseAuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        FirebaseAuthManager.Instance.OnAuthError += HandleError;
        FirebaseAuthManager.Instance.OnLogout += HandleLogout;
    }

    private void OnDisable()
    {
        if (FirebaseAuthManager.Instance == null) return;
        FirebaseAuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
        FirebaseAuthManager.Instance.OnAuthError -= HandleError;
        FirebaseAuthManager.Instance.OnLogout -= HandleLogout;
    }

    // Gắn vào nút "Đăng nhập"
    public void OnClickLogin()
    {
        var email = _emailInput.text.Trim();
        var pass = _passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            _messageText.text = "Vui lòng nhập đầy đủ thông tin.";
            return;
        }

        _messageText.text = "Đang đăng nhập...";
        FirebaseAuthManager.Instance.Login(email, pass);
    }

    // Gắn vào nút "Đăng ký"
    public void OnClickRegister()
    {
        var email = _emailInput.text.Trim();
        var pass = _passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            _messageText.text = "Vui lòng nhập đầy đủ thông tin.";
            return;
        }

        _messageText.text = "Đang đăng ký...";
        FirebaseAuthManager.Instance.Register(email, pass);
    }

    // Gắn vào nút "Đăng xuất"
    public void OnClickLogout()
    {
        FirebaseAuthManager.Instance.Logout();
    }

    private void HandleLoginSuccess(Firebase.Auth.FirebaseUser user)
    {
        _messageText.text = "";
        _loginPanel.SetActive(false);
        _gamePanel.SetActive(true);
    }

    private void HandleError(string errorMsg)
    {
        _messageText.text = errorMsg;
    }

    private void HandleLogout()
    {
        _gamePanel.SetActive(false);
        _loginPanel.SetActive(true);
    }
}