using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestre la synchronisation bidirectionnelle entre CanvasSetupPointCloudUI (Overlay)
/// et WorldSpacePointCloudUI (World Space).
/// Créé dynamiquement par CanvasSetupPointCloudUI.Awake().
/// </summary>
public class PointCloudUIBridge : MonoBehaviour
{
    private ConfigEditorUI overlayUI;
    private WorldSpacePointCloudUI worldSpaceUI;
    private float switchDelay = 1.5f;

    // Paires cameraId → (overlay entry, ws entry)
    private Dictionary<int, (PointCloudUIEntry overlay, WorldSpacePointCloudEntry ws)> entryPairs
        = new Dictionary<int, (PointCloudUIEntry, WorldSpacePointCloudEntry)>();

    private WorldSpacePointCloudEntry wsActiveEntry;
    private bool isSwitching;

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Initialize(ConfigEditorUI configEditor, WorldSpacePointCloudUI ws, float switchDelay)
    {
        this.overlayUI = configEditor;
        this.worldSpaceUI = ws;
        this.switchDelay = switchDelay;
        Debug.Log("[PointCloudUIBridge] Initialize.");
    }

    // ── Entry pairing ─────────────────────────────────────────────────────────

    public void PairEntries(List<PointCloudUIEntry> overlayEntries,
                            List<WorldSpacePointCloudEntry> wsEntries)
    {
        entryPairs.Clear();
        int count = Mathf.Min(overlayEntries.Count, wsEntries.Count);
        for (int i = 0; i < count; i++)
        {
            int camId = overlayEntries[i].CameraId;
            wsEntries[i].SetPairedEntry(overlayEntries[i]);
            entryPairs[camId] = (overlayEntries[i], wsEntries[i]);
        }
        Debug.Log($"[PointCloudUIBridge] {entryPairs.Count} pair link.");
    }

    // ── Mirror : Overlay → WS ─────────────────────────────────────────────────

    public void MirrorStatus(string message, Color color)
        => worldSpaceUI?.MirrorStatus(message, color);

    public void MirrorFileName(string name)
        => worldSpaceUI?.MirrorFileName(name);

    public void MirrorEntryInteractable(bool interactable)
        => worldSpaceUI?.MirrorEntryInteractable(interactable);

    public void MirrorEntryDisplayState(int cameraId, bool isOn)
        => worldSpaceUI?.MirrorEntryDisplayState(cameraId, isOn);

    public void MirrorEntryData(int index, Vector3 pos, Vector3 rot,
        float depthMin, float depthMax, bool flipX, bool flipY)
        => worldSpaceUI?.MirrorEntryData(index, pos, rot, depthMin, depthMax, flipX, flipY);

    public void MirrorDisplayCanvasUIToggle(bool value)
        => worldSpaceUI?.MirrorDisplayCanvasUIToggle(value);

    public void MirrorCanvasUIPosition(Vector3 pos)
        => worldSpaceUI?.MirrorCanvasUIPosition(pos);

    public void MirrorCanvasUIRotation(Vector3 rot)
        => worldSpaceUI?.MirrorCanvasUIRotation(rot);

    public void MirrorCanvasUIColor(Color color)
        => worldSpaceUI?.MirrorCanvasUIColor(color);

    public void MirrorSceneDropdownOptions(List<string> options, int selectedIndex)
        => worldSpaceUI?.MirrorSceneDropdownOptions(options, selectedIndex);

    public void MirrorSceneDropdownSelection(int index)
        => worldSpaceUI?.MirrorSceneDropdownSelection(index);

    public void MirrorEntryClamp(int index, float xMin, float xMax, float yMin, float yMax)
        => worldSpaceUI?.MirrorEntryClamp(index, xMin, xMax, yMin, yMax);

    public void MirrorEntryReferencePoint(int index, Vector3 center)
        => worldSpaceUI?.MirrorEntryReferencePoint(index, center);

    public void MirrorEntryReferencePointGizmoToggle(int index, bool isOn)
        => worldSpaceUI?.MirrorEntryReferencePointGizmoToggle(index, isOn);

    // ── Requests : WS → Overlay ───────────────────────────────────────────────

    public void RequestClose()
        => overlayUI?.SendMessage("OnButtonCloseClick", SendMessageOptions.DontRequireReceiver);

    public void RequestSave()
    {
        CameraConfigFileManager.Instance.Save();
        MirrorStatus("Saved", Color.green);
    }

    public void RequestOpenFile()
        => overlayUI?.SendMessage("OnOpenConfigButtonClick", SendMessageOptions.DontRequireReceiver);

    public void RequestNewFile()
        => overlayUI?.SendMessage("OnNewConfigButtonClick", SendMessageOptions.DontRequireReceiver);

    public void RequestSaveAs()
        => overlayUI?.SendMessage("OnSaveAsButtonClick", SendMessageOptions.DontRequireReceiver);

    public void RequestCopyToClipboard()
        => overlayUI?.SendMessage("OnCopyConfigButtonClick", SendMessageOptions.DontRequireReceiver);

    public void RequestPasteFromClipboard()
        => overlayUI?.SendMessage("OnPasteConfigButtonClick", SendMessageOptions.DontRequireReceiver);

    public void RequestCanvasUIPosition(float x, float y, float z)
        => overlayUI?.ApplyCanvasUIPositionFromWS(new Vector3(x, y, z));

    public void RequestCanvasUIRotation(float x, float y, float z)
        => overlayUI?.ApplyCanvasUIRotationFromWS(new Vector3(x, y, z));

    public void RequestCanvasUIColor(Color color)
        => overlayUI?.ApplyCanvasUIColorFromWS(color);

    public void RequestDisplayCanvasUI(bool value)
        => overlayUI?.ApplyDisplayCanvasUIFromWS(value);

    /// <summary>
    /// Display toggle d'un point cloud demandé depuis le WS canvas.
    /// Gère l'exclusivité et le switchDelay, puis miroir vers l'overlay.
    /// </summary>
    public void RequestDisplayToggle(int cameraId, bool desiredState)
    {
        if (isSwitching) return;
        if (!entryPairs.TryGetValue(cameraId, out var pair)) return;

        var wsEntry = pair.ws;

        if (!desiredState && wsEntry == wsActiveEntry)
        {
            StartCoroutine(SwitchCoroutine(wsActiveEntry, null));
            return;
        }

        if (desiredState && wsEntry != wsActiveEntry)
            StartCoroutine(SwitchCoroutine(wsActiveEntry, wsEntry));
    }

    private IEnumerator SwitchCoroutine(WorldSpacePointCloudEntry previous,
                                        WorldSpacePointCloudEntry next)
    {
        isSwitching = true;

        if (previous != null)
        {
            previous.ApplyDisplayState(false);
            if (entryPairs.TryGetValue(previous.CameraId, out var prevPair))
                prevPair.overlay.ApplyDisplayState(false);
        }

        wsActiveEntry = null;

        if (next != null)
        {
            yield return new WaitForSeconds(switchDelay);
            next.ApplyDisplayState(true);
            if (entryPairs.TryGetValue(next.CameraId, out var nextPair))
                nextPair.overlay.ApplyDisplayState(true);
            wsActiveEntry = next;
        }

        isSwitching = false;
    }
}