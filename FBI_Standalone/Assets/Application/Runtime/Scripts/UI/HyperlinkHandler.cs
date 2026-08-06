using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class HyperlinkHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    public string url = "https://www.epfl.ch/labs/lnco/";
    public bool openInBrowser = true;
    public Color hoverColor = new Color(0f, 0.6f, 1f);

    private TMP_Text tmpText;
    private Canvas canvas;
    private Camera cam;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;
        else if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            cam = Camera.main;
        else
            cam = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {

        if (openInBrowser)
        {
            Application.OpenURL(url);
            EventFileManager.Log($"[HyperlinkHandler] Opening link: {url}");
        }

    }


}