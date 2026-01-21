using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class LocalizeDropDownTMP : MonoBehaviour
{
    public List<LocalizedString> localizedOptions;

    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        var Options = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < localizedOptions.Count; i++)
        {
            localizedOptions[i].GetLocalizedString();
            Options.Add(new TMP_Dropdown.OptionData(localizedOptions[i].GetLocalizedString()));
        }

        dropdown.AddOptions(Options);

    }

}
