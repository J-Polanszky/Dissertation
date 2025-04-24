using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using TMPro;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

public class AuthScript : MonoBehaviour
{
    public AuthScript Instance { get; private set; }
    
    private GameObject loginPanel, menuPanel;
    private TextMeshProUGUI loginStatusText;
    TMP_InputField usernameInput, passwordInput;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        loginPanel = GameObject.FindWithTag("LoginPanel");
        menuPanel = GameObject.FindWithTag("MenuPanel");

        await InitialiseUnityServices();

        Button loginButton = loginPanel.transform.Find("Login").GetComponent<Button>();
        Button registerButton = loginPanel.transform.Find("Register").GetComponent<Button>();

        loginButton.onClick.AddListener(Login);
        registerButton.onClick.AddListener(Register);

        usernameInput = loginPanel.transform.Find("Username").GetComponent<TMP_InputField>();
        passwordInput = loginPanel.transform.Find("Password").GetComponent<TMP_InputField>();
        loginStatusText = loginPanel.transform.Find("LoginStatus").GetComponent<TextMeshProUGUI>();
        loginPanel.transform.Find("Exit").GetComponent<Button>().onClick.AddListener(GameManager.Instance.QuitGame);

        menuPanel.SetActive(false);
    }
    
    async Task InitialiseUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($@"Failed to initialise Unity Services: {e.Message}");
        }
    }
    
    async void Login()
    {
        if (string.IsNullOrEmpty(usernameInput.text))
        {
            if (loginStatusText != null)
                loginStatusText.text = "Username cannot be empty";
            return;
        }

        if (loginStatusText != null)
            loginStatusText.text = "Signing in...";

        try
        {
            // Sign in with username/password
            if (!string.IsNullOrEmpty(passwordInput.text))
            {
                // Use password-based authentication
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(
                    usernameInput.text,
                    passwordInput.text);
            }
            else
            {
                // For demo purposes, use anonymous auth but set the player name
                SignInOptions loginOptions = new SignInOptions
                {
                    CreateAccount = true
                };
                await AuthenticationService.Instance.SignInAnonymouslyAsync(loginOptions);

                // Set player name to match username input
                await AuthenticationService.Instance.UpdatePlayerNameAsync(usernameInput.text);
            }

            Debug.Log($"Signed in to Unity services as: {AuthenticationService.Instance.PlayerId}");
            
            loginPanel.SetActive(false);
            menuPanel.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
            if (loginStatusText != null)
                loginStatusText.text = "Login failed: " + e.Message;
        }
    }

    async void Register()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            if (loginStatusText != null)
                loginStatusText.text = "Username and password are required";
            return;
        }

        if (loginStatusText != null)
            loginStatusText.text = "Creating account...";

        try
        {
            // Sign up with username/password
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(
                usernameInput.text,
                passwordInput.text);

            Debug.Log($"Account created and signed in as: {AuthenticationService.Instance.PlayerId}");
            
            loginPanel.SetActive(false);
            menuPanel.SetActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Registration failed: {e.Message}");
            if (loginStatusText != null)
                loginStatusText.text = "Registration failed: " + e.Message;
        }
    }
}
