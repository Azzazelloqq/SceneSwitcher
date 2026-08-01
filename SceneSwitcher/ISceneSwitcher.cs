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
public interface ISceneSwitcher : IDisposable
{
    event Action<string> SceneStartedToSwitch;
    event Action<string> SceneSwitched;
    event Action<string> SceneStartedToUnload;
    event Action<string> SceneUnloaded;

    void SwitchToScene<TContext>(
        string sceneId,
        Action<TContext> onComplete,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

#if PROJECT_SUPPORT_UNITASK
    Cysharp.Threading.Tasks.UniTask<TContext> SwitchToSceneAsync<TContext>(
#else
    Task<TContext> SwitchToSceneAsync<TContext>(
#endif
        string sceneId,
        CancellationToken token,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool activateOnLoad = true) where TContext : ISceneContext;

    void SwitchToScene(string sceneId, Action onComplete, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true);
    SceneSwitcherTask SwitchToSceneAsync(string sceneId, CancellationToken token, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true);
    void UnloadScene(string sceneId, Action onComplete);
    SceneSwitcherTask UnloadSceneAsync(string sceneId, CancellationToken token);
}
}
