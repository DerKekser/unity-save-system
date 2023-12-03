# Save System

[Save System](https://github.com/DerKekser/unity-save-system) is a simple save system for Unity.
It allows you to save and load scene objects and their components.

## Contents
- [Saving and Loading](#saving-and-loading)
- [Using the Saveable Component](#using-the-saveable-component)
- [Prefab Registry](#prefab-registry)
- [Create your own Saveable Component](#create-your-own-saveable-component)
- [Data Types](#data-types)
- [Supported Types](#supported-types)
- [Install via git URL](#install-via-git-url)
- [License](#license)

### Saving and Loading

You can save and load the current scene by using the `SaveLoadManager` class.

```csharp
using Kekser.SaveSystem;

// ...

SaveLoadManager.Save(path);
SaveLoadManager.Load(path);
```
### Using the Saveable Component

You can save and load a scene object by adding the `Savable` component to it.
You can use it on any scene object, but it is recommended to use it on Prefabs.

This package contains a `SavableTransform` and a `SavableRigidbody` component.
You can use them to save and load the transform and the rigidbody of a scene object.

![Savable Component](/Assets/Kekser/Screenshots/components.png)

### Prefab Registry

To be able to save and load prefabs, you have to register them in the `PrefabRegistry`.
You can create and update the prefab registry by clicking on `Tools/Save System/Update Prefabs`.

This is a manual process, and needs to be done every time you add a new prefab to your project.

![Prefab Registry](/Assets/Kekser/Screenshots/create_update_prefab_registry.png)

### Create your own Saveable Component

You can create your own saveable component by using the `Savable`, `Save` or `Load` attribute.

#### Fields
```csharp
using Kekser.SaveSystem;

[Savable]
private int _score = 100;
```
#### Methods
```csharp
using Kekser.SaveSystem;

private int _score = 100;

[Save]
private void Save(DataObject data)
{
    dataObject.Add("Score", new DataElement(_score));
}

[Load]
private void Load(DataObject data)
{
    _score = dataObject.Get<DataElement>("Score").ToObject<int>();
}
```
### Data Types
#### DataObject
The `DataObject` class is a dictionary that contains other `DataObject`, `DataArray` and `DataElement` objects.
#### DataArray
The `DataArray` class is a list that contains other `DataObject`, `DataArray` and `DataElement` objects.
#### DataElement
The `DataElement` class is a wrapper for any supported objects.

### Supported Types
- `byte[]`
- `int`
- `float`
- `bool`
- `string`
- `Vector2`
- `Vector3`
- `Vector4`
- `Quaternion`
- `Color`
- `Type`
- `Guid`
- `IList`
- `IDictionary`
- `Array`
- `Enum`
- `Class`
- `Struct`
- `GameObject`

### Install via git URL

You can add this package to your project by adding this git URL

```
https://github.com/DerKekser/unity-save-system.git?path=Assets/Kekser/SaveSystem
```
### License

This library is under the MIT License.