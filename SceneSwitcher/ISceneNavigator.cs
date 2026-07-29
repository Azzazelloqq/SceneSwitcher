using System;
using System.Threading;
using System.Threading.Tasks;
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

    TContext NavigateTo<TContext>(
        string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

    Task<TContext> NavigateToAsync<TContext>(
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

    void NavigateTo(
        string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true);

    Task NavigateToAsync(
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true);

    void Unload(string sceneId);
    Task UnloadAsync(string sceneId, CancellationToken token);
}
}
