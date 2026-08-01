using System;
using System.Threading;
#if PROJECT_SUPPORT_UNITASK
using SceneSwitcherTask = Cysharp.Threading.Tasks.UniTask;
#else
using SceneSwitcherTask = System.Threading.Tasks.Task;
#endif

namespace SceneSwitcher
{
public interface IScene : IDisposable
{
    string SceneId { get; }
    string SceneResourceId { get; }
    protected internal ISceneContext SceneContext { get; }
    protected internal ISceneSwitcher SceneSwitcher { get; }

    SceneSwitcherTask InitializeAsync(CancellationToken token);
    void Initialize();
}
}
