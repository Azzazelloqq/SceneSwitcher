# Scene Switcher for Unity

> Addressables-based scene loading with synchronous and asynchronous APIs.

`SceneSwitcher` loads and unloads scenes by Addressables key. A loaded scene can
expose a strongly typed `ISceneContext` component from one of its root objects.

## Features

- Load scenes synchronously or asynchronously.
- Support additive and single scene loading modes.
- Retrieve a typed scene context after loading.
- Cancel asynchronous waits with `CancellationToken`.
- Receive start and completed switching events.
- Unload previously loaded scenes by their Addressables key.

## Installation

```bash
git submodule add https://github.com/Azzazelloqq/SceneSwitcher.git Assets/SceneSwitcherModule
```

Or add to `Packages/manifest.json`:

```json
"com.azzazello.sceneswitcher": "https://github.com/Azzazelloqq/SceneSwitcher.git"
```

The module requires `com.unity.addressables` and supports Unity `2020.3` and newer.

## Load a scene context

Add a component implementing `ISceneContext` to a root GameObject in the target scene:

```csharp
using SceneSwitcher;
using UnityEngine;

public sealed class GameSceneContext : MonoBehaviour, ISceneContext
{
}
```

Then load the scene using its Addressables key:

```csharp
using System.Threading;
using SceneSwitcher;
using UnityEngine.SceneManagement;

using var switcher = new AddressablesSceneSwitcher();

GameSceneContext context = await switcher.SwitchToSceneAsync<GameSceneContext>(
    sceneId: "GameScene",
    token: cancellationToken,
    sceneMode: LoadSceneMode.Single);
```

## Scene transitions

```csharp
switcher.SceneStartedToSwitch += sceneId => Debug.Log($"Loading {sceneId}");
switcher.SceneSwitched += sceneId => Debug.Log($"Loaded {sceneId}");

await switcher.SwitchToSceneAsync(
    "MainMenu",
    cancellationToken,
    LoadSceneMode.Additive);

await switcher.UnloadSceneAsync("MainMenu", cancellationToken);
```

## Notes

- `sceneId` must be a valid Addressables scene key.
- The typed overload searches root objects in the loaded scene for the requested
  `ISceneContext` component.
- Dispose the switcher when the owner no longer needs it; it clears event subscriptions.

## API

`ISceneSwitcher` exposes synchronous and asynchronous load/unload operations,
while `ISceneFactory` defines a separate abstraction for creating `IScene`
instances.
