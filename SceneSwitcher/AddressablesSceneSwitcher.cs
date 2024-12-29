using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneSwitcher
{
public class AddressablesSceneSwitcher : ISceneSwitcher
{
    public event Action<string> SceneStartedToSwitch;
    public event Action<string> SceneSwitched;
    public event Action<string> SceneUnloadStated;
    public event Action<string> SceneUnloaded;

    private readonly Dictionary<string, SceneInstance> _loadedScenes = new();
    
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

        var sceneInstance = loadOperation.Result;
        var rootGameObjects = sceneInstance.Scene.GetRootGameObjects();
        var sceneContext = GetSceneContext<TContext>(rootGameObjects, sceneId);

        _loadedScenes[sceneId] = sceneInstance;
        
        return sceneContext;
    }

    public async Task<TContext> SwitchToSceneAsync<TContext>(string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single) where TContext : ISceneContext
    {
        var sceneInstance = await Addressables.LoadSceneAsync(sceneId, sceneMode).Task;

        var rootGameObjects = sceneInstance.Scene.GetRootGameObjects();
        var sceneContext = GetSceneContext<TContext>(rootGameObjects, sceneId);

        _loadedScenes[sceneId] = sceneInstance;
        
        return sceneContext;
    }

    public void SwitchToScene(string sceneId, LoadSceneMode sceneMode = LoadSceneMode.Single)
    {
        SceneStartedToSwitch?.Invoke(sceneId);
        
        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode);

        loadOperation.WaitForCompletion();

        var sceneInstance = loadOperation.Result;
        _loadedScenes[sceneId] = sceneInstance;
        
        SceneSwitched?.Invoke(sceneId);
    }

    public async Task SwitchToSceneAsync(string sceneId, LoadSceneMode sceneMode = LoadSceneMode.Single)
    {
        SceneStartedToSwitch?.Invoke(sceneId);

        var sceneInstance = await Addressables.LoadSceneAsync(sceneId, sceneMode).Task;

        _loadedScenes[sceneId] = sceneInstance;
        
        SceneSwitched?.Invoke(sceneId);
    }
    
    public void UnloadScene(string sceneId)
    {
        SceneUnloadStated?.Invoke(sceneId);
        
        var sceneInstance = _loadedScenes[sceneId];
        
        var unloadOperation = Addressables.UnloadSceneAsync(sceneInstance);
        
        unloadOperation.WaitForCompletion();
        
        SceneUnloaded?.Invoke(sceneId);
    }

    public async Task UnloadSceneAsync(string sceneId)
    {
        SceneUnloadStated?.Invoke(sceneId);
        var sceneInstance = _loadedScenes[sceneId];
        
        await Addressables.UnloadSceneAsync(sceneInstance).Task;
        
        SceneUnloaded?.Invoke(sceneId);
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

        Console.Error.Write($"Scene {sceneId} does not have a sceneContext {typeof(T).FullName}");
        return default;
    }
}
}