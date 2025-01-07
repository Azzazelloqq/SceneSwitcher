using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace SceneSwitcher
{
public interface ISceneSwitcher : IDisposable
{
    public event Action<string> SceneStartedToSwitch;
    public event Action<string> SceneSwitched;

    public TContext SwitchToScene<TContext>(
        string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

    public Task<TContext> SwitchToSceneAsync<TContext>( string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

    public void SwitchToScene(string sceneId, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true);
    public Task SwitchToSceneAsync( string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true);
    
    public void UnloadScene(string sceneId);
    public Task UnloadSceneAsync(string sceneId, CancellationToken token);
}
}