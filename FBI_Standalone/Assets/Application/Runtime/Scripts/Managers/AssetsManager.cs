using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class AssetsManager : MonoBehaviour
{
    private static AssetsManager instance;
    public static AssetsManager Instance { get { return instance; } }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }

        Initialize();
    }

    [Header("Folders")]
    [SerializeField] private string imagesFolderName = "Images";
    [SerializeField] private string audiosFolderName = "Audios";
    [SerializeField] private string videoFolderName = "Videos";

    private Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, AudioClip> loadedAudioClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, string> loadedVideoPaths = new Dictionary<string, string>();

    // Action called when all assets (including async audio coroutines) are fully loaded
    public event Action OnAllAssetsLoaded;

    private int pendingCoroutines = 0;
    private bool loadingComplete = false;

    public string ImagesPath { get; private set; }
    public string AudiosPath { get; private set; }
    public string VideosPath { get; private set; }

    private void Initialize()
    {
        SetupPaths();
        CreateFoldersIfNeeded();
        LoadAllAssets();
    }

    private void SetupPaths()
    {

        string basePath = Path.Combine(Application.dataPath, "..", "Input");

        ImagesPath = Path.Combine(basePath, imagesFolderName);
        AudiosPath = Path.Combine(basePath, audiosFolderName);
        VideosPath = Path.Combine(basePath, videoFolderName);
    }

    private void CreateFoldersIfNeeded()
    {
        if (!Directory.Exists(ImagesPath))
            Directory.CreateDirectory(ImagesPath);

        if (!Directory.Exists(AudiosPath))
            Directory.CreateDirectory(AudiosPath);

        if (!Directory.Exists(VideosPath))
            Directory.CreateDirectory(VideosPath);
    }

    public void LoadAllAssets()
    {
        loadingComplete = false;
        pendingCoroutines = 0;

        LoadAllImages();
        LoadAllAudio();
        LoadAllVideos();

        // If no audio coroutines were started, fire immediately
        CheckAllAssetsLoaded();
    }

    private void CheckAllAssetsLoaded()
    {
        if (pendingCoroutines == 0 && !loadingComplete)
        {
            loadingComplete = true;
            EventFileManager.Log($"[AssetsManager] All assets loaded. Sprites: {loadedSprites.Count}, Audios: {loadedAudioClips.Count}, Videos: {loadedVideoPaths.Count}");
            OnAllAssetsLoaded?.Invoke();
        }
    }

    #region Images

    public void LoadAllImages()
    {
        loadedSprites.Clear();

        if (!Directory.Exists(ImagesPath))
        {
            EventFileManager.Warning($"[AssetsManager] Images folder not found: {ImagesPath}");
            return;
        }


        string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tga" };

        foreach (string extension in extensions)
        {
            string[] files = Directory.GetFiles(ImagesPath, extension, SearchOption.AllDirectories);

            foreach (string filePath in files)
            {
                LoadImage(filePath);
            }
        }

        EventFileManager.Log($"[AssetsManager] Loaded {loadedSprites.Count} images");
    }

    private void LoadImage(string filePath)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                texture.name = Path.GetFileNameWithoutExtension(filePath);
                texture.filterMode = FilterMode.Bilinear;


                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                sprite.name = texture.name;


                string relativePath = GetRelativePath(filePath, ImagesPath);
                string key = Path.GetFileNameWithoutExtension(relativePath);

                loadedSprites[key] = sprite;

                EventFileManager.Log($"[AssetsManager] Loaded image: {key}");
            }
            else
            {
                EventFileManager.Error($"[AssetsManager] Failed to load image: {filePath}");
            }
        }
        catch (Exception e)
        {
            EventFileManager.Log($"[AssetsManager] Error loading image {filePath}: {e.Message}");
        }
    }

    public Sprite GetSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return null;

        if (loadedSprites.TryGetValue(spriteName, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[AssetsManager] Sprite not found: {spriteName}");
        return null;
    }

    public Texture2D GetTexture(string textureName)
    {
        if (string.IsNullOrEmpty(textureName))
            return null;

        Debug.LogWarning($"[AssetsManager] Texture not found: {textureName}");
        return null;
    }

    #endregion

    #region Audio

    public void LoadAllAudio()
    {
        loadedAudioClips.Clear();

        if (!Directory.Exists(AudiosPath))
        {
            EventFileManager.Warning($"[AssetsManager] Audios folder not found: {AudiosPath}");
            return;
        }

        // Extensions supportées par Unity
        string[] extensions = { "*.wav", "*.ogg", "*.mp3" };

        foreach (string extension in extensions)
        {
            string[] files = Directory.GetFiles(AudiosPath, extension, SearchOption.AllDirectories);

            foreach (string filePath in files)
            {
                LoadAudioFile(filePath);
            }
        }

        EventFileManager.Log($"[AssetsManager] Loaded {loadedAudioClips.Count} audio clips");
    }

    private void LoadAudioFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        string relativePath = GetRelativePath(filePath, AudiosPath);
        string key = Path.GetFileNameWithoutExtension(relativePath);

        // WAV files peuvent être chargés directement
        if (extension == ".wav")
        {
            pendingCoroutines++;
            StartCoroutine(LoadWavFile(filePath, key));
        }
        // Pour OGG et MP3, utiliser UnityWebRequest
        else if (extension == ".ogg" || extension == ".mp3")
        {
            pendingCoroutines++;
            StartCoroutine(LoadAudioWithWebRequest(filePath, key));
        }
    }

    private System.Collections.IEnumerator LoadWavFile(string filePath, string key)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip($"file://{filePath}", AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                clip.name = key;
                loadedAudioClips[key] = clip;
                EventFileManager.Log($"[AssetsManager] Loaded audio: {key}");
            }
            else
            {
                EventFileManager.Error($"[AssetsManager] Failed to load audio {filePath}: {www.error}");
            }
        }

        pendingCoroutines--;
        CheckAllAssetsLoaded();
    }

    private System.Collections.IEnumerator LoadAudioWithWebRequest(string filePath, string key)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        AudioType audioType = extension == ".ogg" ? AudioType.OGGVORBIS : AudioType.MPEG;

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip($"file://{filePath}", audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                clip.name = key;
                loadedAudioClips[key] = clip;
                EventFileManager.Log($"[AssetsManager] Loaded audio: {key}");
            }
            else
            {
                EventFileManager.Error($"[AssetsManager] Failed to load audio {filePath}: {www.error}");
            }
        }

        pendingCoroutines--;
        CheckAllAssetsLoaded();
    }

    public AudioClip GetAudioClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return null;

        if (loadedAudioClips.TryGetValue(clipName, out AudioClip clip))
        {
            return clip;
        }

        EventFileManager.Warning($"[AssetsManager] Audio clip not found: {clipName}");
        return null;
    }

    #endregion

    #region Videos

    public void LoadAllVideos()
    {
        loadedVideoPaths.Clear();

        if (!Directory.Exists(VideosPath))
        {
            EventFileManager.Warning($"[AssetsManager] Videos folder not found: {VideosPath}");
            return;
        }

        string[] extensions = { "*.mp4", "*.webm", "*.mov" };

        foreach (string extension in extensions)
        {
            string[] files = Directory.GetFiles(VideosPath, extension, SearchOption.AllDirectories);

            foreach (string filePath in files)
            {
                string relativePath = GetRelativePath(filePath, VideosPath);
                string key = Path.GetFileNameWithoutExtension(relativePath);
                loadedVideoPaths[key] = filePath;
                EventFileManager.Log($"[AssetsManager] Registered video: {key}");
            }
        }

        EventFileManager.Log($"[AssetsManager] Registered {loadedVideoPaths.Count} videos");
    }

    public string GetVideoPath(string videoName)
    {
        if (string.IsNullOrEmpty(videoName))
            return null;

        if (loadedVideoPaths.TryGetValue(videoName, out string path))
            return path;

        EventFileManager.Warning($"[AssetsManager] Video not found: {videoName}");
        return null;
    }

    public List<string> GetAllVideoNames()
    {
        return new List<string>(loadedVideoPaths.Keys);
    }

    #endregion

    #region Utilities

    private string GetRelativePath(string fullPath, string basePath)
    {
        Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
        Uri fullUri = new Uri(fullPath);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
    }

    public void ReloadAllAssets()
    {
        LoadAllAssets();
    }

    public void OpenImagesFolder()
    {
        Application.OpenURL($"file://{ImagesPath}");
    }

    public void OpenAudioFolder()
    {
        Application.OpenURL($"file://{AudiosPath}");
    }

    public List<string> GetAllSpriteNames()
    {
        return new List<string>(loadedSprites.Keys);
    }

    public List<string> GetAllAudioClipNames()
    {
        return new List<string>(loadedAudioClips.Keys);
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Reload All Assets")]
    public void EditorReloadAllAssets()
    {
        ReloadAllAssets();
    }

    [ContextMenu("Print Loaded Assets")]
    public void PrintLoadedAssets()
    {
        Debug.Log($"=== Loaded Assets ===");
        Debug.Log($"Sprites: {loadedSprites.Count}");
        foreach (var key in loadedSprites.Keys)
        {
            Debug.Log($"  - {key}");
        }

        Debug.Log($"Audio Clips: {loadedAudioClips.Count}");
        foreach (var key in loadedAudioClips.Keys)
        {
            Debug.Log($"  - {key}");
        }
    }

    [ContextMenu("Open Images Folder")]
    public void EditorOpenImagesFolder()
    {
        OpenImagesFolder();
    }

    [ContextMenu("Open Audio Folder")]
    public void EditorOpenAudioFolder()
    {
        OpenAudioFolder();
    }
#endif
}