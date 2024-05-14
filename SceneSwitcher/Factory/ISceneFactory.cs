using System;
using System.Threading;
using System.Threading.Tasks;

namespace SceneSwitcher.Factory
{
public interface ISceneFactory : IDisposable
{
    public Task<TScene> CreateSceneAsync<TScene>(string sceneId, CancellationToken token) where TScene : IScene;
    public TScene CreateScene<TScene>(string sceneId) where TScene : IScene;
}
}