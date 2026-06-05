using Eflatun.SceneReference;
using PrimeTween;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderManager : MonoBehaviour
{

    [Header("Scenes")]
    [SerializeField] private SceneReference DefaultScene;

    public event Action<Scene> OnSceneLoaded;
    public event Action<string> OnSceneLoad;

    private Scene currentScene;
    public Scene CurrentScene => currentScene;

    AsyncOperation asyncLoad;
    public AsyncOperation AsyncLoad => asyncLoad;

    #region Singelton
    private static SceneLoaderManager instance;
    public static SceneLoaderManager Instance { get { return instance; } }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this.gameObject); }
        else
        {
            instance = this;
        }
    }
    #endregion

    private void Start()
    {
        LoadDefaultScene();
    }

    private void OnEnable()
    {
        ExperimentManager.Instance.OnStop += OnGameManagerExperimentStop;
    }

    private void OnDisable()
    {
        ExperimentManager.Instance.OnStop -= OnGameManagerExperimentStop;
    }

    public IEnumerator LoadAsyncScene(SceneReference sceneRef)
    {
        if (sceneRef.BuildIndex != SceneLoaderManager.Instance.currentScene.buildIndex)
        {

            OnSceneLoad?.Invoke(sceneRef.Name);

            yield return new WaitForSeconds(0.25f);

            asyncLoad = SceneManager.LoadSceneAsync(sceneRef.BuildIndex, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;

            yield return new WaitUntil(() => asyncLoad.isDone);

            currentScene = SceneManager.GetActiveScene();

            OnSceneLoaded?.Invoke(currentScene);

            EventFileManager.Log($"[SceneLoaderManager] Scene loaded {currentScene.name}");

        }
    }


    public void LoadDefaultScene()
    {
        if(Fader.Instance != null)
        {
            Fader.Instance.FadeToBlack();
        }
        
        StartCoroutine(LoadAsyncScene(DefaultScene));

        Tween.Delay(1.0f, () => Fader.Instance?.FadeToClear());
    }

    private void OnGameManagerExperimentStop()
    {
        LoadDefaultScene();
    }

}
