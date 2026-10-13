# Unity - Save System

[Save System](https://github.com/DerKekser/unity-save-system) is a simple save system for Unity.
It allows you to save and load scene objects and their components.

## Contents
- [Saving and Loading](#saving-and-loading)
- [Save Meta Data](#save-meta-data)
- [Using the Savable Component](#using-the-savable-component)
- [Prefab Registry](#prefab-registry)
- [Create your own Savable Component](#create-your-own-savable-component)
- [Data Types](#data-types)
- [Reading Values Safely](#reading-values-safely)
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

// load into a freshly loaded scene, even if it is already the active one
SaveLoadManager.Load(path, reloadScene: true);
```
A save is written to a temporary file first and then moved into place, so a crash while saving never
leaves a broken file behind. `SaveLoadManager.IsBusy` is true while a save or load runs.

### Save Meta Data

You can store meta data with a save (a version, what a save slot shows, ...) and read it back without loading the save.

```csharp
DataObject meta = new DataObject();
meta.SetValue("Version", 2);
meta.SetValue("Level", "Harbour");
SaveLoadManager.Save(path, meta);

DataObject saved = SaveLoadManager.LoadMeta(path); // null if there is none
int version = saved.GetValue("Version", 0);
```
### Using the Savable Component

You can save and load a scene object by adding the `Savable` component to it.
You can use it on any scene object, but it is recommended to use it on Prefabs.

This package contains a `SavableTransform` and a `SavableRigidbody` component.
You can use them to save and load the transform and the rigidbody of a scene object.

Savable fields and methods are also found on base classes, so a component inherits what its base class saves.
Children of a `Savable` are only written when they (or their children) have something to save.

#### Fixed Guids

Scene objects get a random guid. If your scenes are generated (and so recreated with new guids), give them a fixed
one, e.g. derived from the object's name, so older saves still find them:

```csharp
savable.SetGuid(myStableGuid); // in the editor, mark the component dirty afterwards
```

![Savable Component](/Assets/Kekser/Screenshots/components.png)

### Prefab Registry

To be able to save and load prefabs, you have to register them in the `PrefabRegistry`.
You can create and update the prefab registry by clicking on `Tools/Save System/Update Prefabs`.

This is a manual process, and needs to be done every time you add a new prefab to your project.

A prefab is found again by the guid its `Savable` keeps, so older saves rely on it staying the same. Editing a prefab
(in Prefab Mode or through `PrefabUtility.LoadPrefabContents`) keeps it; before 1.3.1 saving such an edit gave the
prefab a new guid.

![Prefab Registry](/Assets/Kekser/Screenshots/create_update_prefab_registry.png)

### Create your own Savable Component

You can create your own savable component by using the `Savable`, `Save` or `Load` attribute.\
#### Fields
```csharp
using Kekser.SaveSystem.Attributes;

[Savable]
private int _score = 100;
```
#### Methods
```csharp
using Kekser.SaveSystem.Attributes;
using Kekser.SaveSystem.Data;

private int _score = 100;

[Save]
private void Save(DataObject data)
{
    data.Add("Score", new DataElement(_score));
}

[Load]
private void Load(DataObject data)
{
    _score = data.Get<DataElement>("Score").ToObject<int>();
}
```
*These attributes are also usable on static fields and methods.*
### Data Types
#### DataObject
The `DataObject` class is a dictionary that contains other `DataObject`, `DataArray` and `DataElement` objects.
#### DataArray
The `DataArray` class is a list that contains other `DataObject`, `DataArray` and `DataElement` objects.
#### DataElement
The `DataElement` class is a wrapper for any [supported types](#supported-types).

### Reading Values Safely

Saves outlive code changes. `SetValue`, `GetValue` and `TryGetValue` on a `DataObject` make reading tolerant of
missing keys (e.g. a value added in a later version):

```csharp
[Save]
private void Save(DataObject data)
{
    data.SetValue("Score", _score);
}

[Load]
private void Load(DataObject data)
{
    _score = data.GetValue("Score", 100); // 100 if the save has no "Score"
}
```
A saved field that no longer exists, a component type that was renamed and a failing load method are logged and
skipped; the rest of the save still loads.

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

You can add this package to your project by adding this git URL to the package manager:
```
https://github.com/DerKekser/unity-save-system.git?path=Assets/Kekser/SaveSystem
```
![Package Manager](/Assets/Kekser/Screenshots/package_manager.png)
### License

This library is under the MIT License.
