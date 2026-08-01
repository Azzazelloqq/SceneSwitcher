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
/// Adapts a scene-switching implementation to the application navigation API.
/// </summary>
public sealed class SceneNavigator : ISceneNavigator
{
    private readonly ISceneSwitcher _sceneSwitcher;

    public SceneNavigator(ISceneSwitcher sceneSwitcher)
    {
        _sceneSwitcher = sceneSwitcher ?? throw new ArgumentNullException(nameof(sceneSwitcher));
    }

    public event Action<string> SceneStartedToSwitch
    {
        add => _sceneSwitcher.SceneStartedToSwitch += value;
        remove => _sceneSwitcher.SceneStartedToSwitch -= value;
    }

    public event Action<string> SceneSwitched
    {
        add => _sceneSwitcher.SceneSwitched += value;
        remove => _sceneSwitcher.SceneSwitched -= value;
    }

    public event Action<string> SceneStartedToUnload
    {
        add => _sceneSwitcher.SceneStartedToUnload += value;
        remove => _sceneSwitcher.SceneStartedToUnload -= value;
    }

    public event Action<string> SceneUnloaded
    {
        add => _sceneSwitcher.SceneUnloaded += value;
        remove => _sceneSwitcher.SceneUnloaded -= value;
    }

    public void NavigateTo<TContext>(string sceneId, Action<TContext> onComplete, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true)
        where TContext : ISceneContext =>
        _sceneSwitcher.SwitchToScene(sceneId, onComplete, sceneMode, activateOnLoad);

#if PROJECT_SUPPORT_UNITASK
    public Cysharp.Threading.Tasks.UniTask<TContext> NavigateToAsync<TContext>(
#else
    public Task<TContext> NavigateToAsync<TContext>(
#endif
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true)
        where TContext : ISceneContext =>
        _sceneSwitcher.SwitchToSceneAsync<TContext>(sceneId, token, sceneMode, activateOnLoad);

    public void NavigateTo(string sceneId, Action onComplete, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true) =>
        _sceneSwitcher.SwitchToScene(sceneId, onComplete, sceneMode, activateOnLoad);

    public SceneSwitcherTask NavigateToAsync(string sceneId, CancellationToken token, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true) =>
        _sceneSwitcher.SwitchToSceneAsync(sceneId, token, sceneMode, activateOnLoad);

    public void Unload(string sceneId, Action onComplete) => _sceneSwitcher.UnloadScene(sceneId, onComplete);

    public SceneSwitcherTask UnloadAsync(string sceneId, CancellationToken token) =>
        _sceneSwitcher.UnloadSceneAsync(sceneId, token);

    public void Dispose() => _sceneSwitcher.Dispose();
}
}
