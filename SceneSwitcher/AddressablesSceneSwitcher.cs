using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace SceneSwitcher
{
public class AddressablesSceneSwitcher : ISceneSwitcher
{
    public event Action<string> SceneStartedToSwitch;
    public event Action<string> SceneSwitched;

    public void Dispose()
    {
        SceneSwitched = null;
        SceneStartedToSwitch = null;
    }

    public TContext SwitchToScene<TContext>(string sceneId, LoadSceneMode sceneMode = LoadSceneMode.Single)
        where TContext : ISceneContext
    {
        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode);

        loadOperation.WaitForCompletion();

        var rootGameObjects = loadOperation.Result.Scene.GetRootGameObjects();
        var sceneContext = GetSceneContext<TContext>(rootGameObjects, sceneId);

        return sceneContext;
    }

    public async Task<TContext> SwitchToSceneAsync<TContext>(string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single) where TContext : ISceneContext
    {
        var sceneInstance = await Addressables.LoadSceneAsync(sceneId, sceneMode).Task;

        var rootGameObjects = sceneInstance.Scene.GetRootGameObjects();
        var sceneContext = GetSceneContext<TContext>(rootGameObjects, sceneId);

        return sceneContext;
    }

    public void SwitchToScene(string sceneId, LoadSceneMode sceneMode)
    {
        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode);
    }

    public void OnSceneStartedToSwitch(string sceneId)
    {
        SceneStartedToSwitch?.Invoke(sceneId);
    }

    public void OnSceneSwitched(string sceneId)
    {
        SceneSwitched?.Invoke(sceneId);
    }

    private T GetSceneContext<T>(GameObject[] rootObjects, string sceneId)
    {
        foreach (var rootGameObject in rootObjects)
        {
            if (rootGameObject.TryGetComponent(out T sceneContext))
            {
                return sceneContext;
            }
        }

        Console.Error.Write($"Scene {sceneId} does not have a sceneContext {typeof(T)}");
        return default;
    }
}
}