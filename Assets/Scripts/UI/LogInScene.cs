using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LogInScene : MonoBehaviour
{
    public enum AccountTab
    {
        LogIn,
        SignIn,
    }

    [Serializable]
    public class TabConfig
    {
        public AccountTab AccountTab;
        public Button ChangeTabButton;
        public Image ButtonTabSprite;
        public GameObject ContentTab;

        public Sprite TabActive;
        public Sprite TabInactive;
    }

    [Header("Log In")]
    [SerializeField]
    private TMP_InputField _emailInput;

    [SerializeField]
    private TMP_InputField _passwordInput;

    [SerializeField]
    private Button _showPasswordButton_01;

    [SerializeField]
    private Button _loginButton;

    [Space(5)]
    [Header("Sign In")]
    [SerializeField]
    private TMP_InputField _username;

    [SerializeField]
    private TMP_InputField _password;

    [SerializeField]
    private TMP_InputField _confirmPassword;

    [SerializeField]
    private Button _showPasswordButton_02;

    [SerializeField]
    private Button _signInButton;

    [Space(5)]
    [Header("Thông báo")]
    [SerializeField]
    private TMP_Text _messageText;

    [Space(5)]
    [Header("Configs")]
    [SerializeField]
    private List<TabConfig> _tabConfig = new();

    [SerializeField]
    private Sprite _hiddenSprite, _showSprite;

    private AccountTab _currentTab;
    private bool _isLoginPasswordVisible;
    private bool _isSignInPasswordVisible;
    private bool _isProcessing;

    private void Awake()
    {
        _currentTab = AccountTab.SignIn;
        InitButtonTab();
        InitActionButtons();
        SetTab(AccountTab.LogIn);
    }

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

    private void OnDestroy()
    {
        foreach (var config in _tabConfig)
        {
            config.ChangeTabButton.onClick.RemoveAllListeners();
        }

        _loginButton.onClick.RemoveAllListeners();
        _signInButton.onClick.RemoveAllListeners();
        _showPasswordButton_01.onClick.RemoveAllListeners();
        _showPasswordButton_02.onClick.RemoveAllListeners();
    }

    private void InitButtonTab()
    {
        foreach (var config in _tabConfig)
        {
            config.ChangeTabButton.onClick.AddListener(() => SetTab(config.AccountTab));
        }
    }

    private void InitActionButtons()
    {
        _loginButton.onClick.AddListener(OnClickLogin);
        _signInButton.onClick.AddListener(OnClickRegister);
        _showPasswordButton_01.onClick.AddListener(OnClickToggleLoginPassword);
        _showPasswordButton_02.onClick.AddListener(OnClickToggleSignInPassword);
    }

    // Gắn vào nút "Đăng nhập"
    public void OnClickLogin()
    {
        if (_isProcessing) return;

        var email = _emailInput.text.Trim();
        var pass = _passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            _messageText.text = "Vui lòng nhập đầy đủ thông tin.";
            return;
        }

        SetProcessing(true);
        _messageText.text = "Đang đăng nhập...";
        FirebaseAuthManager.Instance.Login(email, pass);
    }

    // Gắn vào nút "Đăng ký"
    public void OnClickRegister()
    {
        if (_isProcessing) return;

        var email = _username.text.Trim();
        var pass = _password.text;
        var confirmPass = _confirmPassword.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(confirmPass))
        {
            _messageText.text = "Vui lòng nhập đầy đủ thông tin.";
            return;
        }

        if (pass != confirmPass)
        {
            _messageText.text = "Mật khẩu nhập lại không khớp.";
            return;
        }

        if (pass.Length < 6)
        {
            _messageText.text = "Mật khẩu phải có ít nhất 6 ký tự.";
            return;
        }

        SetProcessing(true);
        _messageText.text = "Đang đăng ký...";
        FirebaseAuthManager.Instance.Register(email, pass);
    }

    // Gắn vào nút "Đăng xuất"
    public void OnClickLogout()
    {
        FirebaseAuthManager.Instance.Logout();
    }

    // Nút hiện/ẩn mật khẩu bên tab LogIn
    public void OnClickToggleLoginPassword()
    {
        _isLoginPasswordVisible = !_isLoginPasswordVisible;
        ApplyPasswordVisibility(
            _isLoginPasswordVisible,
            _showPasswordButton_01,
            _passwordInput);
    }

    // Nút hiện/ẩn mật khẩu bên tab SignIn (áp dụng cho cả password và confirm password)
    public void OnClickToggleSignInPassword()
    {
        _isSignInPasswordVisible = !_isSignInPasswordVisible;
        ApplyPasswordVisibility(
            _isSignInPasswordVisible,
            _showPasswordButton_02,
            _password,
            _confirmPassword);
    }

    private void ApplyPasswordVisibility(bool isVisible, Button toggleButton, params TMP_InputField[] fields)
    {
        var contentType = isVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        foreach (var field in fields)
        {
            field.contentType = contentType;
            field.ForceLabelUpdate();
        }

        SetToggleButtonSprite(toggleButton, isVisible);
    }

    private void SetToggleButtonSprite(Button button, bool isVisible)
    {
        var image = button.GetComponent<Image>();
        if (image == null) return;

        image.sprite = isVisible ? _showSprite : _hiddenSprite;
    }

    private void HandleLoginSuccess(Firebase.Auth.FirebaseUser user)
    {
        SetProcessing(false);
        _messageText.text = "";
        // TODO: chuyển sang scene chính của game sau khi đăng nhập/đăng ký thành công
        // Ví dụ: UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    private void HandleError(string errorMsg)
    {
        SetProcessing(false);
        _messageText.text = errorMsg;
    }

    private void HandleLogout()
    {
        SetProcessing(false);
        _messageText.text = "";
        ClearInputs();
        SetTab(AccountTab.LogIn);
    }

    private void SetTab(AccountTab tab)
    {
        if (tab == _currentTab)
        {
            return;
        }

        _currentTab = tab;
        _messageText.text = "";

        // Tự động reset mật khẩu về dạng ẩn mỗi khi chuyển tab
        ResetPasswordVisibility();

        foreach (var tabConfig in _tabConfig)
        {
            tabConfig.ButtonTabSprite.sprite =
                tabConfig.AccountTab == tab ? tabConfig.TabActive : tabConfig.TabInactive;
            tabConfig.ContentTab.SetActive(tabConfig.AccountTab == tab);
        }
    }

    private void ResetPasswordVisibility()
    {
        _isLoginPasswordVisible = false;
        _isSignInPasswordVisible = false;

        _passwordInput.contentType = TMP_InputField.ContentType.Password;
        _password.contentType = TMP_InputField.ContentType.Password;
        _confirmPassword.contentType = TMP_InputField.ContentType.Password;

        _passwordInput.ForceLabelUpdate();
        _password.ForceLabelUpdate();
        _confirmPassword.ForceLabelUpdate();

        SetToggleButtonSprite(_showPasswordButton_01, false);
        SetToggleButtonSprite(_showPasswordButton_02, false);
    }

    private void SetProcessing(bool isProcessing)
    {
        _isProcessing = isProcessing;
        _loginButton.interactable = !isProcessing;
        _signInButton.interactable = !isProcessing;
    }

    private void ClearInputs()
    {
        _emailInput.text = "";
        _passwordInput.text = "";
        _username.text = "";
        _password.text = "";
        _confirmPassword.text = "";
    }
}