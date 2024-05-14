using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace SceneSwitcher
{
public interface ISceneSwitcher : IDisposable
{
    public event Action<string> SceneStartedToSwitch;
    public event Action<string> SceneSwitched;

    public TContext SwitchToScene<TContext>(string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single) where TContext : ISceneContext;

    public Task<TContext> SwitchToSceneAsync<TContext>(string sceneId,
        LoadSceneMode sceneMode = LoadSceneMode.Single) where TContext : ISceneContext;

    public void SwitchToScene(string sceneId, LoadSceneMode sceneMode);

    public void OnSceneStartedToSwitch(string sceneId);
    public void OnSceneSwitched(string sceneId);
}
}