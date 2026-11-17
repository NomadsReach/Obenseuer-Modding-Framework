using System;
using ObenseuerCore.Ui;
using UnityEngine;
using UnityEngine.UIElements;

internal static class Program
{
    private static void Check(bool value) { if (!value) throw new Exception("Toolkit resize check failed"); }
    private static void Main()
    {
        VisualElement focusRoot = new VisualElement(), otherRoot = new VisualElement();
        TextField field = new TextField { parent = focusRoot };
        VisualElement caret = new VisualElement { parent = field }, button = new VisualElement { parent = focusRoot };
        Check(ToolkitTextFocus.Contains(caret, focusRoot) && ToolkitTextFocus.Contains(field, focusRoot));
        Check(!ToolkitTextFocus.Contains(caret, otherRoot) && !ToolkitTextFocus.Contains(button, focusRoot) && !ToolkitTextFocus.Contains(null, focusRoot));
        bool allowed = true;
        VisualElement parent = new VisualElement(), window = new VisualElement { parent = parent, layout = new Rect(600, 450) }, grip = new VisualElement();
        ToolkitResizeHandle resize = new ToolkitResizeHandle(window, grip, 360, 240, () => allowed);
        grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 1, position = new Vector3(100, 100) });
        Check(grip.HasPointerCapture(1));
        grip.Dispatch(new PointerMoveEvent { pointerId = 1, position = new Vector3(-900, 150) });
        Check(window.style.width == 360 && window.style.height == 500);
        grip.Dispatch(new PointerUpEvent { pointerId = 1 }); Check(!grip.HasPointerCapture(1));
        grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 2, position = new Vector3(100, 100) });
        allowed = false; grip.Dispatch(new PointerMoveEvent { pointerId = 2, position = new Vector3(200, 200) });
        Check(!grip.HasPointerCapture(2) && window.style.width == 360);
        allowed = true; grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 3, position = new Vector3(100, 100) });
        window.parent = new VisualElement(); grip.Dispatch(new PointerMoveEvent { pointerId = 3, position = new Vector3(200, 200) });
        Check(!grip.HasPointerCapture(3) && window.style.width == 360);
        window.parent = parent; grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 4, position = new Vector3(100, 100) });
        resize.Cancel(); Check(!grip.HasPointerCapture(4));
        grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 5, position = new Vector3(100, 100) });
        grip.Dispatch(new PointerCancelEvent { pointerId = 5 }); Check(!grip.HasPointerCapture(5));
        grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 6, position = new Vector3(100, 100) });
        grip.ReleasePointer(6); grip.Dispatch(new PointerCaptureOutEvent { pointerId = 6 });
        grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 7, position = new Vector3(100, 100) }); Check(grip.HasPointerCapture(7));
        resize.Dispose(); resize.Dispose(); Check(!grip.HasPointerCapture(7));
        grip.Dispatch(new PointerDownEvent { button = 0, pointerId = 8, position = new Vector3(100, 100) }); Check(!grip.HasPointerCapture(8));
        Console.WriteLine("Toolkit resize lifecycle checks passed");
    }
}
