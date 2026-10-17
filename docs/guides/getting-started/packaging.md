---
title: Packaging
parent: Getting started
nav_order: 8
---

# Packaging

Packaging is turning your finished mod into one zip file that other players can install. Make a zip whose folders match the game folder. The player extracts it into the game folder, and the files land in place.

This page uses a mod named `ExampleHello` as the example. Use your own mod's name, which is the name of the DLL your build made.

```
ExampleHello-0.1.0.zip
  BepInEx\
    plugins\
      ExampleHello\
        ExampleHello.dll
```

1. Build your mod in release mode: run `dotnet build -c Release` in your mod folder, as in [Your first mod](first-mod.md). Your DLL is now at `bin\Release\net472\ExampleHello.dll`.
2. Make a new empty folder to build the zip in, for example `C:\Mods\Package`. Inside it, make the folders `BepInEx\plugins\ExampleHello`. The last folder is named after your mod.
3. Copy your DLL into `BepInEx\plugins\ExampleHello`. Add any other files your mod needs, such as pictures or `.json` files, to the same folder.
4. Do not put the framework DLL in your zip. Players get it from its own download.
5. Make the zip. In a terminal opened in `C:\Mods\Package`, run this line, which zips the `BepInEx` folder:

   ```
   powershell -Command "Compress-Archive -Path BepInEx -DestinationPath ExampleHello-0.1.0.zip"
   ```

   You can also right-click the `BepInEx` folder in File Explorer and choose Send to, then Compressed (zipped) folder. The `BepInEx` folder itself must be at the top of the zip, not a folder that holds it.
6. Check the zip. Extract it into an empty folder and confirm the DLL is at `BepInEx\plugins\ExampleHello\ExampleHello.dll`.
7. On the mod page, list the requirements: BepInEx 5.4.23 or later (Mono, x64) or BepInEx 6 Unity.Mono (x64, [be.788](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.Mono-win-x64-6.0.0-be.788%2B5b766a3.zip), the tested build), and the Obenseuer Modding Framework 0.11.0 or later for a framework mod, or 0.10.0-beta or later for a BepInEx plugin.

To uninstall a mod, delete its folder under `BepInEx\plugins\`. Its save data stays in the save folders.

## Where to go next

You now know how to make, test and share a mod. To add something specific, such as an item or a Mods menu page, see the [Cookbook](../cookbook/index.md). Before you share your mod, read [Using the framework alongside other mods](../multiple-mods.md).

## BepInEx plugins (advanced)

Use a BepInEx plugin only when you need a feature of BepInEx itself, such as `KeyboardShortcut`. A framework mod is one DLL and needs no second build, see [Framework mods](../../api/framework-mods.md). Skip this section if you are following the guide for the first time.

### Supporting BepInEx 5 too

A plugin DLL runs only on the BepInEx version it was built for. To support both, build the plugin twice from the same source, and put both files in the same folder of one zip. Both builds write to `bin\Release\net472\` by default, so send the BepInEx 5 build to its own folder:

1. Build the BepInEx 6 copy: `dotnet build -c Release -p:Loader=BepInEx6`. It writes `bin\Release\net472\MyMod.dll`.
2. Build the BepInEx 5 copy: `dotnet build -c Release -p:Loader=BepInEx5 -p:OutDir=bin\Release\be5\`. It writes `bin\Release\be5\MyMod.dll`. This build needs BepInEx 5's `BepInEx.dll`, in the `BepInEx\core` folder of the game or in the folder given by `-p:BepInExCore=<folder>`.
3. Put both files in `BepInEx\plugins\MyMod\` of the zip: the BepInEx 6 file as `MyMod.dll`, and the BepInEx 5 file renamed to `MyMod.BepInEx5.dll`.

Shipping one version only is fine too.

Only the `using` for the plugin base class and the dependency version differ. The build defines `BEPINEX6` or `BEPINEX5`. A mod that references the framework DLL must have a hard `BepInDependency` on `obenseuer.moddingframework` in both builds, so the right framework copy is loaded first. Replace the code in `Plugin.cs` with this plugin.

```csharp
using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;

[BepInPlugin("example.myfirstmod", "My First Mod", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.11.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
#endif
public class MyFirstMod : BaseUnityPlugin
{
}
```

The `Starter` folder makes a framework mod, with no loader switch. For a BepInEx plugin, change its `.csproj` as follows. The BepInEx 5 build needs BepInEx 5's `BepInEx.dll` and the framework's `ObenseuerCore.BepInEx5.dll`, because `CoreMod.For(BaseUnityPlugin)` takes a different type on each loader. First add this block right after the first `PropertyGroup`. It picks the loader, sets the `BEPINEX5` or `BEPINEX6` name your code tests with `#if`, and points the framework reference at the BepInEx 5 copy when needed:

```xml
<PropertyGroup>
  <Loader Condition="'$(Loader)' == ''">BepInEx6</Loader>
  <DefineConstants Condition="'$(Loader)' == 'BepInEx5'">$(DefineConstants);BEPINEX5</DefineConstants>
  <DefineConstants Condition="'$(Loader)' != 'BepInEx5'">$(DefineConstants);BEPINEX6</DefineConstants>
  <FrameworkDll Condition="'$(Loader)' == 'BepInEx5'">$(GameDir)\BepInEx\plugins\ObenseuerCore\ObenseuerCore.BepInEx5.dll</FrameworkDll>
</PropertyGroup>
```

Then add these two `ItemGroup` blocks next to the other references. They add the BepInEx files for the loader you build for:

```xml
<ItemGroup Condition="'$(Loader)' != 'BepInEx5'">
  <Reference Include="BepInEx.Core">
    <HintPath>$(BepInExCore)\BepInEx.Core.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="BepInEx.Unity.Mono">
    <HintPath>$(BepInExCore)\BepInEx.Unity.Mono.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>

<ItemGroup Condition="'$(Loader)' == 'BepInEx5'">
  <Reference Include="BepInEx">
    <HintPath>$(BepInExCore)\BepInEx.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

Build the BepInEx 5 copy with `-p:Loader=BepInEx5`. If your game has BepInEx 6 installed, also pass `-p:BepInExCore=<folder>`, where the folder is the `BepInEx\core` folder of a BepInEx 5 install, because the default `BepInExCore` is the game's own `BepInEx\core` folder.
