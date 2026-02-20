using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Pet/Input/StrictAlphanumericValidator", fileName = "StrictAlphanumericValidator")]
public class StrictAlphanumericValidator : TMP_InputValidator
{
    private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{}|;:'\",.<>?/`~";
    
    public override char Validate(ref string text, ref int pos, char ch)
    {
        // 英数字のみ許可
        if (AllowedCharacters.Contains(ch))
        {
            text = text.Insert(pos, ch.ToString());
            pos++;
            return ch;
        }
        return '\0';
    }
}