using System;
using System.Collections.Generic;
using System.Threading;
#if PROJECT_SUPPORT_UNITASK
using Cysharp.Threading.Tasks;
using SceneSwitcherTask = Cysharp.Threading.Tasks.UniTask;
#else
using System.Threading.Tasks;
using SceneSwitcherTask = System.Threading.Tasks.Task;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneSwitcher
{
public sealed class AddressablesSceneSwitcher : ISceneSwitcher
{
    public event Action<string> SceneStartedToSwitch;
    public event Action<string> SceneSwitched;
    public event Action<string> SceneStartedToUnload;
    public event Action<string> SceneUnloaded;

    [Obsolete("Use SceneStartedToUnload instead.")]
    public event Action<string> SceneUnloadStated
    {
        add => SceneStartedToUnload += value;
        remove => SceneStartedToUnload -= value;
    }

    private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _loadedScenes = new();
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var loadOperation in _loadedScenes.Values)
        {
            if (loadOperation.IsValid())
            {
                UnloadLoadedScene(loadOperation);
            }
        }

        _loadedScenes.Clear();
        SceneSwitched = null;
        SceneStartedToSwitch = null;
        SceneStartedToUnload = null;
        SceneUnloaded = null;
    }

    public void SwitchToScene<TContext>(
        string sceneId,
        Action<TContext> onComplete,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true)
        where TContext : ISceneContext
    {
        if (!CanStartOperation(sceneId))
        {
            return;
        }

        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);
        loadOperation.Completed += operation => CompleteLoad(operation, sceneId, sceneMode, onComplete);
    }

#if PROJECT_SUPPORT_UNITASK
    public async UniTask<TContext> SwitchToSceneAsync<TContext>(
#else
    public async Task<TContext> SwitchToSceneAsync<TContext>(
#endif
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true)
        where TContext : ISceneContext
    {
        if (!CanStartOperation(sceneId))
        {
            return default;
        }

        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);

        try
        {
            var sceneInstance = await AwaitOperationAsync(loadOperation, token);

            if (_isDisposed)
            {
                UnloadLoadedScene(loadOperation);
                return default;
            }

            TrackLoadedScene(sceneId, loadOperation, sceneMode);
            return GetSceneContext<TContext>(sceneInstance.Scene.GetRootGameObjects(), sceneId);
        }
        catch (OperationCanceledException)
        {
            CleanupCancelledLoad(loadOperation, activateOnLoad);
            return default;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load scene {sceneId}: {exception}");
            return default;
        }
    }

    public void SwitchToScene(
        string sceneId,
        Action onComplete,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true)
    {
        if (!CanStartOperation(sceneId))
        {
            return;
        }

        InvokeSafely(SceneStartedToSwitch, sceneId);

        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);
        loadOperation.Completed += operation => CompleteLoad(operation, sceneId, sceneMode, onComplete);
    }

    public async SceneSwitcherTask SwitchToSceneAsync(
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true)
    {
        if (!CanStartOperation(sceneId))
        {
            return;
        }

        InvokeSafely(SceneStartedToSwitch, sceneId);

        var loadOperation = Addressables.LoadSceneAsync(sceneId, sceneMode, activateOnLoad);

        try
        {
            await AwaitOperationAsync(loadOperation, token);

            if (_isDisposed)
            {
                UnloadLoadedScene(loadOperation);
                return;
            }

            TrackLoadedScene(sceneId, loadOperation, sceneMode);
            InvokeSafely(SceneSwitched, sceneId);
        }
        catch (OperationCanceledException)
        {
            CleanupCancelledLoad(loadOperation, activateOnLoad);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load scene {sceneId}: {exception}");
        }
    }

    public void UnloadScene(string sceneId, Action onComplete)
    {
        if (_isDisposed)
        {
            Debug.LogError($"Cannot unload scene {sceneId}: the scene switcher is disposed");
            return;
        }

        InvokeSafely(SceneStartedToUnload, sceneId);

        if (!_loadedScenes.TryGetValue(sceneId, out var loadOperation))
        {
            Debug.LogError($"Scene {sceneId} is not loaded by this switcher");
            return;
        }

        var unloadOperation = Addressables.UnloadSceneAsync(loadOperation, false);
        unloadOperation.Completed += operation => CompleteUnload(sceneId, loadOperation, operation, onComplete, true);
    }

    public async SceneSwitcherTask UnloadSceneAsync(string sceneId, CancellationToken token)
    {
        if (_isDisposed)
        {
            Debug.LogError($"Cannot unload scene {sceneId}: the scene switcher is disposed");
            return;
        }

        InvokeSafely(SceneStartedToUnload, sceneId);

        if (!_loadedScenes.TryGetValue(sceneId, out var loadOperation))
        {
            Debug.LogError($"Scene {sceneId} is not loaded by this switcher");
            return;
        }

        var unloadOperation = Addressables.UnloadSceneAsync(loadOperation, false);

        try
        {
            await AwaitOperationAsync(unloadOperation, token);
            CompleteUnload(sceneId, loadOperation, unloadOperation, null, true);
        }
        catch (OperationCanceledException)
        {
            unloadOperation.Completed += operation => CompleteUnload(sceneId, loadOperation, operation, null, true);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to unload scene {sceneId}: {exception}");
            ReleaseOperation(unloadOperation);
        }
    }

    private void CompleteLoad<TContext>(AsyncOperationHandle<SceneInstance> loadOperation, string sceneId, LoadSceneMode sceneMode, Action<TContext> onComplete)
        where TContext : ISceneContext
    {
        if (loadOperation.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load scene {sceneId}: {loadOperation.OperationException}");
            InvokeSafely(onComplete, default);
            return;
        }

        if (_isDisposed)
        {
            UnloadLoadedScene(loadOperation);
            return;
        }

        TrackLoadedScene(sceneId, loadOperation, sceneMode);
        var sceneContext = GetSceneContext<TContext>(loadOperation.Result.Scene.GetRootGameObjects(), sceneId);
        InvokeSafely(onComplete, sceneContext);
    }

    private void CompleteLoad(AsyncOperationHandle<SceneInstance> loadOperation, string sceneId, LoadSceneMode sceneMode, Action onComplete)
    {
        if (loadOperation.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load scene {sceneId}: {loadOperation.OperationException}");
            InvokeSafely(onComplete);
            return;
        }

        if (_isDisposed)
        {
            UnloadLoadedScene(loadOperation);
            return;
        }

        TrackLoadedScene(sceneId, loadOperation, sceneMode);
        InvokeSafely(SceneSwitched, sceneId);
        InvokeSafely(onComplete);
    }

    private void CompleteUnload(string sceneId, AsyncOperationHandle<SceneInstance> loadOperation, AsyncOperationHandle<SceneInstance> unloadOperation, Action onComplete, bool notify)
    {
        if (unloadOperation.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to unload scene {sceneId}: {unloadOperation.OperationException}");
            ReleaseOperation(unloadOperation);
            InvokeSafely(onComplete);
            return;
        }

        if (_loadedScenes.TryGetValue(sceneId, out var trackedOperation) && trackedOperation.Equals(loadOperation))
        {
            _loadedScenes.Remove(sceneId);
        }

        if (notify && !_isDisposed)
        {
            InvokeSafely(SceneUnloaded, sceneId);
        }

        InvokeSafely(onComplete);
        ReleaseOperation(unloadOperation);
    }

    private void TrackLoadedScene(string sceneId, AsyncOperationHandle<SceneInstance> loadOperation, LoadSceneMode sceneMode)
    {
        if (sceneMode == LoadSceneMode.Single)
        {
            ReleaseReplacedScenes();
        }
        else if (_loadedScenes.TryGetValue(sceneId, out var previousLoadOperation))
        {
            UnloadLoadedScene(previousLoadOperation);
        }

        _loadedScenes[sceneId] = loadOperation;
    }

    private void ReleaseReplacedScenes()
    {
        foreach (var loadOperation in _loadedScenes.Values)
        {
            if (loadOperation.IsValid())
            {
                Addressables.Release(loadOperation);
            }
        }

        _loadedScenes.Clear();
    }

    private static void CleanupCancelledLoad(AsyncOperationHandle<SceneInstance> loadOperation, bool activateOnLoad)
    {
        if (loadOperation.IsDone)
        {
            CleanupCompletedLoad(loadOperation, activateOnLoad);
            return;
        }

        loadOperation.Completed += operation => CleanupCompletedLoad(operation, activateOnLoad);
    }

    private static void CleanupCompletedLoad(AsyncOperationHandle<SceneInstance> loadOperation, bool activateOnLoad)
    {
        if (loadOperation.Status != AsyncOperationStatus.Succeeded)
        {
            if (loadOperation.IsValid())
            {
                Addressables.Release(loadOperation);
            }

            return;
        }

        if (activateOnLoad)
        {
            UnloadLoadedScene(loadOperation);
            return;
        }

        Addressables.Release(loadOperation);
    }

    private static void UnloadLoadedScene(AsyncOperationHandle<SceneInstance> loadOperation)
    {
        if (loadOperation.IsValid())
        {
            var unloadOperation = Addressables.UnloadSceneAsync(loadOperation, false);
            unloadOperation.Completed += ReleaseOperation;
        }
    }

    private static void ReleaseOperation(AsyncOperationHandle<SceneInstance> operation)
    {
        if (operation.IsValid())
        {
            Addressables.Release(operation);
        }
    }

    private bool CanStartOperation(string sceneId)
    {
        if (!_isDisposed)
        {
            return true;
        }

        Debug.LogError($"Cannot load scene {sceneId}: the scene switcher is disposed");
        return false;
    }

#if PROJECT_SUPPORT_UNITASK
    private static UniTask<T> AwaitOperationAsync<T>(AsyncOperationHandle<T> operation, CancellationToken token) =>
        operation.ToUniTask<T>(cancellationToken: token);
#else
    private static Task<T> AwaitOperationAsync<T>(AsyncOperationHandle<T> operation, CancellationToken token)
    {
        if (operation.IsDone)
        {
            return operation.Status == AsyncOperationStatus.Succeeded
                ? Task.FromResult(operation.Result)
                : Task.FromException<T>(operation.OperationException ?? new InvalidOperationException("Addressables operation failed"));
        }

        var completionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        var completionHandled = false;

        void OnCompleted(AsyncOperationHandle<T> completedOperation)
        {
            if (completionHandled)
            {
                return;
            }

            completionHandled = true;
            completedOperation.Completed -= OnCompleted;
            cancellationRegistration.Dispose();

            if (completedOperation.Status == AsyncOperationStatus.Succeeded)
            {
                completionSource.TrySetResult(completedOperation.Result);
                return;
            }

            completionSource.TrySetException(completedOperation.OperationException ?? new InvalidOperationException("Addressables operation failed"));
        }

        cancellationRegistration = token.Register(() =>
        {
            if (completionHandled)
            {
                return;
            }

            completionHandled = true;
            operation.Completed -= OnCompleted;
            completionSource.TrySetCanceled(token);
        });

        if (completionSource.Task.IsCompleted)
        {
            cancellationRegistration.Dispose();
            return completionSource.Task;
        }

        operation.Completed += OnCompleted;

        if (operation.IsDone)
        {
            OnCompleted(operation);
        }

        return completionSource.Task;
    }
#endif

    private static T GetSceneContext<T>(GameObject[] rootObjects, string sceneId)
    {
        foreach (var rootObject in rootObjects)
        {
            if (TryGetSceneContext(rootObject.GetComponents<Component>(), out T sceneContext))
            {
                return sceneContext;
            }
        }

        foreach (var rootObject in rootObjects)
        {
            foreach (var component in rootObject.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.gameObject != rootObject && component is T sceneContext)
                {
                    return sceneContext;
                }
            }
        }

        Debug.LogError($"Scene {sceneId} does not have a sceneContext {typeof(T).FullName}");
        return default;
    }

    private static bool TryGetSceneContext<T>(IEnumerable<Component> components, out T sceneContext)
    {
        foreach (var component in components)
        {
            if (component is T typedContext)
            {
                sceneContext = typedContext;
                return true;
            }
        }

        sceneContext = default;
        return false;
    }

    private static void InvokeSafely(Action callback)
    {
        if (callback == null)
        {
            return;
        }

        foreach (Action handler in callback.GetInvocationList())
        {
            try
            {
                handler();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static void InvokeSafely<T>(Action<T> callback, T value)
    {
        if (callback == null)
        {
            return;
        }

        foreach (Action<T> handler in callback.GetInvocationList())
        {
            try
            {
                handler(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
}
