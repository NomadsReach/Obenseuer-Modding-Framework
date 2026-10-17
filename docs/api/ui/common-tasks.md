---
title: Common tasks
parent: UI
grand_parent: Screen and UI
nav_order: 1
---

# Common tasks

This page uses `Transform` from Unity. The [glossary](../../glossary.md#types-that-come-from-unity) says what each one is.
{: .unity-types}

Group controls in a titled panel (a card), and put a row of tabs at the top. Each tab has its own page to fill.

```csharp
RectTransform card = ModUi.Card(window.Body, "Options");
ModUi.Toggle(card, "Show hints", true, on => { });

ModTabs tabs = ModUi.Tabs(window.Body, new[] { "First", "Second" }, index => { });
ModUi.Text(tabs.Page(0), "This is the first page.", false);
ModUi.Text(tabs.Page(1), "This is the second page.", false);
```

Let the player pick from a list of choices. `Stepper` shows one choice with arrows, and `Segmented` shows all of them in a row.

```csharp
ModStepper size = ModUi.Stepper(window.Body, "Size", new[] { "Small", "Medium", "Large" }, 1, index => { });
ModSegmented mode = ModUi.Segmented(window.Body, new[] { "Easy", "Hard" }, 0, index => { });
size.Set(2);
mode.Set(1);
```

Ask for a line of text.

```csharp
ModTextField name = ModUi.TextField(window.Body, "Name", "Alex", text => { });
```

Show the player's model in the window. Only one model preview can be open at a time.

```csharp
ModelPreview preview = ModUi.ModelPreview(window.Body, PlayerLocator.instance.playerRig.transform, PreviewFraming.Face);
```

In these snippets, `window` is the `ModWindow` that `Open` gives to your build code.
