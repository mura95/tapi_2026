
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class LoginPasswordToggle : MonoBehaviour
{
    
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image toggleImage;
    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;

    private TMP_InputField _passwordField;
    private bool isPasswordVisible = false;

    private void Awake()
    {
        _passwordField = GetComponent<TMP_InputField>();
    }
    private void Start()
    {
        toggleButton.onClick.AddListener(TogglePasswordVisibility);
        UpdateToggleImage();
    }

    private void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;
        
        _passwordField.inputType = isPasswordVisible ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;

        _passwordField.ForceLabelUpdate();

        UpdateToggleImage();
    }

    private void UpdateToggleImage()
    {
        toggleImage.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
    }
}
