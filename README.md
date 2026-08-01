# Scene Switcher for Unity

> Addressables-based scene loading with callback and asynchronous APIs.

`SceneSwitcher` loads and unloads scenes by Addressables key. A loaded scene can expose a strongly typed `ISceneContext` component on a root object or any of its children.

## Features

- Load scenes with a completion callback or asynchronously.
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

The module requires `com.unity.addressables` `3.1.0`, `com.cysharp.unitask` `2.5.10`, and Unity `6000.0` or newer.

## Load a scene context

Add a component implementing `ISceneContext` to the target scene:

```csharp
using SceneSwitcher;
using UnityEngine;

public sealed class GameSceneContext : MonoBehaviour, ISceneContext
{
}
```

Then load the scene using its Addressables key:

```csharp
using SceneSwitcher;
using UnityEngine.SceneManagement;

var sceneSwitcher = new AddressablesSceneSwitcher();
using var navigator = new SceneNavigator(sceneSwitcher);

var context = await navigator.NavigateToAsync<GameSceneContext>(
    sceneId: "GameScene",
    token: cancellationToken,
    sceneMode: LoadSceneMode.Single);
```

For a non-awaitable call, receive the result in the completion callback:

```csharp
navigator.NavigateTo<GameSceneContext>("GameScene", context =>
{
    // The scene has completed loading.
});
```

## Scene transitions

```csharp
navigator.SceneStartedToSwitch += sceneId => Debug.Log($"Loading {sceneId}");
navigator.SceneSwitched += sceneId => Debug.Log($"Loaded {sceneId}");

await navigator.NavigateToAsync(
    "MainMenu",
    cancellationToken,
    LoadSceneMode.Additive);

await navigator.UnloadAsync("MainMenu", cancellationToken);
```

## Notes

- `sceneId` must be a valid Addressables scene key.
- The typed overload searches root objects first, then all descendants, for the requested `ISceneContext` component.
- Give application code `ISceneNavigator`; keep `ISceneSwitcher` and its `AddressablesSceneSwitcher` implementation in the composition root.
- `Dispose` starts unloading every scene still tracked by the switcher and clears all event subscriptions.

## API

`ISceneNavigator` is the application-facing navigation API. `ISceneSwitcher` is an implementation-level contract, while `ISceneFactory` defines a separate abstraction for creating `IScene` instances.
