using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

    public TContext SwitchToScene<TContext>(string sceneId, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true)
        where TContext : ISceneContext
    {
        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);

        loadOperation.WaitForCompletion();

        var sceneInstance = loadOperation.Result;
        var rootGameObjects = sceneInstance.Scene.GetRootGameObjects();
        var sceneContext = GetSceneContext<TContext>(rootGameObjects, sceneId);

        _loadedScenes[sceneId] = sceneInstance;
        
        return sceneContext;
    }

    public async Task<TContext> SwitchToSceneAsync<TContext>(
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext
    {
        var loadSceneTask = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);

        while (loadSceneTask.Status != AsyncOperationStatus.Succeeded)
        {
            if (loadSceneTask.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"Failed to load scene {sceneId}");
                return default;
            }
            
            if (token.IsCancellationRequested)
            {
                Addressables.Release(loadSceneTask);
                return default;
            }
            
            await Task.Yield();
        }

        var sceneInstance = loadSceneTask.Result;
        _loadedScenes[sceneId] = sceneInstance;
        var rootGameObjects = sceneInstance.Scene.GetRootGameObjects();
        var sceneContext = GetSceneContext<TContext>(rootGameObjects, sceneId);

        _loadedScenes[sceneId] = sceneInstance;
        
        return sceneContext;
    }

    public void SwitchToScene(string sceneId, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true)
    {
        SceneStartedToSwitch?.Invoke(sceneId);
        
        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);

        loadOperation.WaitForCompletion();

        var sceneInstance = loadOperation.Result;
        _loadedScenes[sceneId] = sceneInstance;
        
        SceneSwitched?.Invoke(sceneId);
    }

    public async Task SwitchToSceneAsync(
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true)
    {
        SceneStartedToSwitch?.Invoke(sceneId);

        var loadSceneTask = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);

        while (loadSceneTask.Status != AsyncOperationStatus.Succeeded)
        {
            if (loadSceneTask.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"Failed to load scene {sceneId}");
                return;
            }
            
            if (token.IsCancellationRequested)
            {
                Addressables.Release(loadSceneTask);
                return;
            }
            
            await Task.Yield();
        }

        var sceneInstance = loadSceneTask.Result;
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

    public async Task UnloadSceneAsync(string sceneId, CancellationToken token)
    {
        SceneUnloadStated?.Invoke(sceneId);
        var sceneInstance = _loadedScenes[sceneId];
        
        var unloadSceneTask = Addressables.UnloadSceneAsync(sceneInstance);

        while (unloadSceneTask.Status != AsyncOperationStatus.Succeeded)
        {
            if (unloadSceneTask.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"Failed to unload scene {sceneId}");
                return;
            }
            
            if (token.IsCancellationRequested)
            {
                Addressables.Release(unloadSceneTask);
                return;
            }
            
            await Task.Yield();
        }
        
        SceneUnloaded?.Invoke(sceneId);
    }

    private static T GetSceneContext<T>(GameObject[] rootObjects, string sceneId)
    {
        foreach (var rootGameObject in rootObjects)
        {
            if (rootGameObject.TryGetComponent(out T sceneContext))
            {
                return sceneContext;
            }
        }

        Debug.LogError($"Scene {sceneId} does not have a sceneContext {typeof(T).FullName}");
        return default;
    }
}
}