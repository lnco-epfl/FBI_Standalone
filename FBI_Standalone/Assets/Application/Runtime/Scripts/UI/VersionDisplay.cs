using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class VersionDisplay : MonoBehaviour
{

    private TMP_Text versionText;

    public LocalizedString prefix;

    private void Awake()
    {
        if (versionText == null)
        {
            versionText = GetComponent<TMP_Text>();
        }

        UpdateVersionText();
    }

    private void UpdateVersionText()
    {

        string version = Application.version;

        versionText.text = prefix.GetLocalizedString() + version;

    }
}