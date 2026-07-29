using System;
using System.Threading;
using System.Threading.Tasks;
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

    public TContext NavigateTo<TContext>(
        string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext =>
        _sceneSwitcher.SwitchToScene<TContext>(sceneId, sceneMode, activateOnLoad);

    public Task<TContext> NavigateToAsync<TContext>(
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext =>
        _sceneSwitcher.SwitchToSceneAsync<TContext>(sceneId, token, sceneMode, activateOnLoad);

    public void NavigateTo(
        string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) =>
        _sceneSwitcher.SwitchToScene(sceneId, sceneMode, activateOnLoad);

    public Task NavigateToAsync(
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) =>
        _sceneSwitcher.SwitchToSceneAsync(sceneId, token, sceneMode, activateOnLoad);

    public void Unload(string sceneId) => _sceneSwitcher.UnloadScene(sceneId);

    public Task UnloadAsync(string sceneId, CancellationToken token) =>
        _sceneSwitcher.UnloadSceneAsync(sceneId, token);

    public void Dispose() => _sceneSwitcher.Dispose();
}
}
