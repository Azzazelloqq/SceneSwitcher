using System;
using System.Threading;
#if !PROJECT_SUPPORT_UNITASK
using System.Threading.Tasks;
#endif

namespace SceneSwitcher.Factory
{
public interface ISceneFactory : IDisposable
{
#if PROJECT_SUPPORT_UNITASK
    Cysharp.Threading.Tasks.UniTask<TScene> CreateSceneAsync<TScene>(string sceneId, CancellationToken token) where TScene : IScene;
#else
    Task<TScene> CreateSceneAsync<TScene>(string sceneId, CancellationToken token) where TScene : IScene;
#endif
    TScene CreateScene<TScene>(string sceneId) where TScene : IScene;
}
}
