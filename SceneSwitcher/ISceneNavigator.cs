using System;
using System.Threading;
#if PROJECT_SUPPORT_UNITASK
using SceneSwitcherTask = Cysharp.Threading.Tasks.UniTask;
#else
using System.Threading.Tasks;
using SceneSwitcherTask = System.Threading.Tasks.Task;
#endif
using UnityEngine.SceneManagement;

namespace SceneSwitcher
{
/// <summary>
/// Application-facing contract for navigating between scenes.
/// </summary>
public interface ISceneNavigator : IDisposable
{
    event Action<string> SceneStartedToSwitch;
    event Action<string> SceneSwitched;
    event Action<string> SceneStartedToUnload;
    event Action<string> SceneUnloaded;

    void NavigateTo<TContext>(
        string sceneId,
        Action<TContext> onComplete,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

#if PROJECT_SUPPORT_UNITASK
    Cysharp.Threading.Tasks.UniTask<TContext> NavigateToAsync<TContext>(
#else
    Task<TContext> NavigateToAsync<TContext>(
#endif
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

    void NavigateTo(string sceneId, Action onComplete, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true);
    SceneSwitcherTask NavigateToAsync(string sceneId, CancellationToken token, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true);
    void Unload(string sceneId, Action onComplete);
    SceneSwitcherTask UnloadAsync(string sceneId, CancellationToken token);
}
}
