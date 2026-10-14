---
title: Packaging
parent: Getting started
nav_order: 8
---

# Packaging

Make a zip whose folders match the game folder. The player extracts it into the game folder, and the files land in place.

```
ExampleHello-0.1.0.zip
  BepInEx\
    plugins\
      ExampleHello\
        ExampleHello.dll
```

1. Put your DLL in a folder named after your mod id, under `BepInEx\plugins\`.
2. Add any other files your mod needs to the same folder.
3. Do not put the framework DLL in your zip. Players get it from its own download.
4. Check the zip. Extract it into an empty folder and confirm the DLL is at `BepInEx\plugins\ExampleHello\ExampleHello.dll`.
5. On the mod page, list the requirements: BepInEx 5.4.23 or later (Mono, x64) or BepInEx 6 Unity.Mono (x64, be.755 or newer), and the Obenseuer Modding Framework 0.10.0-beta or later.

To uninstall a mod, delete its folder under `BepInEx\plugins\`. Its save data stays in the save folders.

## Supporting BepInEx 5 too

A mod DLL runs only on the BepInEx version it was built for. To support both, build the mod twice from the same source, and put both files in the same folder of one zip. Both builds write to `bin\Release\net472\` by default, so send the BepInEx 5 build to its own folder:

1. Build the BepInEx 6 copy: `dotnet build -c Release -p:Loader=BepInEx6`. It writes `bin\Release\net472\MyMod.dll`.
2. Build the BepInEx 5 copy: `dotnet build -c Release -p:Loader=BepInEx5 -p:OutDir=bin\Release\be5\`. It writes `bin\Release\be5\MyMod.dll`. This build needs BepInEx 5's `BepInEx.dll`, in the `BepInEx\core` folder of the game or in the folder given by `-p:BepInExCore=<folder>`.
3. Put both files in `BepInEx\plugins\MyMod\` of the zip: the BepInEx 6 file as `MyMod.dll`, and the BepInEx 5 file renamed to `MyMod.BepInEx5.dll`.

Shipping one version only is fine too.

Only the `using` for the plugin base class and the dependency version differ. The build defines `BEPINEX6` or `BEPINEX5`. A mod that references the framework DLL must have a hard `BepInDependency` on `obenseuer.moddingframework` in both builds, so the right framework copy is loaded first.

```csharp
using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;

[BepInPlugin("example.myfirstmod", "My First Mod", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.10.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.10.0")]
#endif
public class MyFirstMod : BaseUnityPlugin
{
}
```

A mod made from the `Starter` folder gets these settings from the Starter's `Directory.Build.props`, including the loader detection. A mod that is not made from the Starter must add them to its `.csproj`. The BepInEx 5 build needs BepInEx 5's `BepInEx.dll` and the framework's `ObenseuerCore.BepInEx5.dll`, because `CoreMod.For(BaseUnityPlugin)` takes a different type on each loader. This is the minimal part of the Starter's setup:

```xml
<PropertyGroup>
  <Loader Condition="'$(Loader)' == ''">BepInEx6</Loader>
  <DefineConstants Condition="'$(Loader)' == 'BepInEx5'">$(DefineConstants);BEPINEX5</DefineConstants>
  <DefineConstants Condition="'$(Loader)' == 'BepInEx6'">$(DefineConstants);BEPINEX6</DefineConstants>
</PropertyGroup>

<ItemGroup Condition="'$(Loader)' == 'BepInEx5'">
  <Reference Include="BepInEx">
    <HintPath>$(GameDir)\BepInEx\core\BepInEx.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="ObenseuerCore">
    <HintPath>$(GameDir)\BepInEx\plugins\ObenseuerCore\ObenseuerCore.BepInEx5.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

Put the BepInEx 6 references (`BepInEx.Core`, `BepInEx.Unity.Mono` and `ObenseuerCore.dll`) under the condition `'$(Loader)' != 'BepInEx5'`. Then build the BepInEx 5 copy with `-p:Loader=BepInEx5`. `GameDir` must be the game folder.
