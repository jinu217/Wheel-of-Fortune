using TMPro;
using UnityEngine;

/// <summary>동적 TMP 폰트에 기본 영문과 숫자 글리프를 렌더링 전에 등록합니다.</summary>
public static class RuntimeFontGlyphInitializer
{
    private const string RequiredCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcdefghijklmnopqrstuvwxyz" +
        "0123456789" +
        " !@#$%^&*()-_=+[]{};:'\",.<>/?\\|`~";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterRequiredGlyphs()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null) return;

        font.TryAddCharacters(RequiredCharacters, out string missingCharacters);
        if (!string.IsNullOrEmpty(missingCharacters))
            Debug.LogWarning($"RIDIBatang SDF에서 생성하지 못한 기본 문자가 있습니다: {missingCharacters}", font);
    }
}
