using System;
using System.Collections.Generic;
using ObenseuerCore.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore
{
    internal enum ModMenuColumn { Side, Body, Footer }

    internal sealed class ModMenuUnit
    {
        internal readonly string Key;
        internal readonly ModMenuColumn Column;
        internal readonly bool Adjusts;
        internal readonly Selectable Target;
        internal readonly Action<int> Adjust;

        internal ModMenuUnit(string key, ModMenuColumn column, bool adjusts, Selectable target)
            : this(key, column, adjusts, target, null)
        {
        }

        internal ModMenuUnit(string key, ModMenuColumn column, bool adjusts, Selectable target, Action<int> adjust)
        {
            Key = key;
            Column = column;
            Adjusts = adjusts;
            Target = target;
            Adjust = adjust;
        }
    }

    internal static class ModMenuOrder
    {
        internal static int First(List<ModMenuUnit> list, ModMenuColumn column)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Column == column) return i;
            }
            return -1;
        }

        internal static int Step(List<ModMenuUnit> list, int index, int delta)
        {
            if (index < 0 || index >= list.Count) return -1;
            ModMenuColumn column = list[index].Column;
            int count = 0;
            int position = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Column != column) continue;
                if (i < index) position++;
                count++;
            }
            if (count == 0) return -1;
            position = (position + delta + count) % count;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Column != column) continue;
                if (position == 0) return i;
                position--;
            }
            return -1;
        }

        internal static int Next(List<ModMenuUnit> list, int index)
        {
            return Step(list, index, 1);
        }

        internal static int Previous(List<ModMenuUnit> list, int index)
        {
            return Step(list, index, -1);
        }

        internal static int Switched(ModMenuColumn from, int horizontal)
        {
            if (horizontal < 0)
            {
                if (from == ModMenuColumn.Side) return -1;
                return from == ModMenuColumn.Body ? (int)ModMenuColumn.Side : (int)ModMenuColumn.Body;
            }
            if (horizontal > 0)
            {
                if (from == ModMenuColumn.Footer) return -1;
                return from == ModMenuColumn.Side ? (int)ModMenuColumn.Body : (int)ModMenuColumn.Footer;
            }
            return -1;
        }

        internal static int Enter(List<ModMenuUnit> list, ModMenuColumn column)
        {
            int index = First(list, column);
            if (index < 0 && column != ModMenuColumn.Body) index = First(list, ModMenuColumn.Body);
            if (index < 0 && column != ModMenuColumn.Footer) index = First(list, ModMenuColumn.Footer);
            if (index < 0 && column != ModMenuColumn.Side) index = First(list, ModMenuColumn.Side);
            return index;
        }

        internal static bool Holds(int position, bool live)
        {
            return position >= 0 || live;
        }

        internal static int Keyed(List<ModMenuUnit> list, string key, int fallback)
        {
            if (key != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Key == key) return i;
                }
                string owner = Owner(key);
                if (owner != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].Key == owner) return i;
                    }
                }
            }
            if (fallback >= 0 && fallback < list.Count) return fallback;
            return -1;
        }

        internal static void Sort(List<ModMenuUnit> list, Func<ModMenuUnit, int[]> path)
        {
            if (list.Count < 2) return;
            List<ModMenuUnit> source = new List<ModMenuUnit>(list);
            List<int> order = new List<int>(list.Count);
            for (int i = 0; i < list.Count; i++) order.Add(i);
            order.Sort((a, b) => Rank(path == null ? null : path(source[a]), path == null ? null : path(source[b]), a, b));
            list.Clear();
            for (int i = 0; i < order.Count; i++) list.Add(source[order[i]]);
        }

        private static string Owner(string key)
        {
            const string suffix = " reset";
            if (key.Length <= suffix.Length || !key.EndsWith(suffix, StringComparison.Ordinal)) return null;
            return key.Substring(0, key.Length - suffix.Length);
        }

        private static int Rank(int[] a, int[] b, int indexA, int indexB)
        {
            if (a == null || b == null)
            {
                if (a == null && b == null) return indexA.CompareTo(indexB);
                return a == null ? 1 : -1;
            }
            int count = Math.Min(a.Length, b.Length);
            for (int i = 0; i < count; i++)
            {
                if (a[i] != b[i]) return a[i].CompareTo(b[i]);
            }
            if (a.Length != b.Length) return a.Length.CompareTo(b.Length);
            return indexA.CompareTo(indexB);
        }

        internal static string SelfCheck()
        {
            List<ModMenuUnit> list = new List<ModMenuUnit>();
            list.Add(new ModMenuUnit("side.a", ModMenuColumn.Side, false, null));
            list.Add(new ModMenuUnit("body.a", ModMenuColumn.Body, false, null));
            list.Add(new ModMenuUnit("body.slider", ModMenuColumn.Body, true, null));
            list.Add(new ModMenuUnit("body.b", ModMenuColumn.Body, false, null));
            list.Add(new ModMenuUnit("footer.a", ModMenuColumn.Footer, false, null));
            list.Add(new ModMenuUnit("footer.b", ModMenuColumn.Footer, false, null));
            if (First(list, ModMenuColumn.Side) != 0 || First(list, ModMenuColumn.Body) != 1 || First(list, ModMenuColumn.Footer) != 4) return "nav first in column";
            if (list[0].Key != "side.a") return "nav key";
            if (Next(list, 1) != 2 || Next(list, 3) != 1) return "nav next";
            if (Previous(list, 1) != 3 || Previous(list, 0) != 0) return "nav previous";
            if (Next(list, 4) != 5 || Previous(list, 4) != 5) return "nav footer wrap";
            if (Next(list, 99) != -1 || Previous(list, -1) != -1) return "nav bad index";
            if (Switched(ModMenuColumn.Side, -1) != -1 || Switched(ModMenuColumn.Side, 1) != (int)ModMenuColumn.Body) return "nav side switch";
            if (Switched(ModMenuColumn.Body, -1) != (int)ModMenuColumn.Side || Switched(ModMenuColumn.Body, 1) != (int)ModMenuColumn.Footer) return "nav body switch";
            if (Switched(ModMenuColumn.Footer, -1) != (int)ModMenuColumn.Body || Switched(ModMenuColumn.Footer, 1) != -1) return "nav footer switch";
            if (Switched(ModMenuColumn.Body, 0) != -1) return "nav no switch";
            if (Enter(list, ModMenuColumn.Footer) != 4 || Enter(list, ModMenuColumn.Side) != 0) return "nav enter";
            if (!list[2].Adjusts || list[1].Adjusts) return "nav adjusts";
            List<ModMenuUnit> empty = new List<ModMenuUnit>();
            if (First(empty, ModMenuColumn.Body) != -1 || Next(empty, 0) != -1 || Enter(empty, ModMenuColumn.Side) != -1) return "nav empty";
            List<ModMenuUnit> onlySide = new List<ModMenuUnit>();
            onlySide.Add(new ModMenuUnit("side.a", ModMenuColumn.Side, false, null));
            if (Enter(onlySide, ModMenuColumn.Footer) != 0) return "nav enter fallback";
            if (!Holds(2, false)) return "keep unit";
            if (!Holds(-1, true)) return "keep live not unit";
            if (Holds(-1, false)) return "keep empty";
            List<ModMenuUnit> keyed = new List<ModMenuUnit>();
            keyed.Add(new ModMenuUnit("a.b", ModMenuColumn.Body, false, null));
            keyed.Add(new ModMenuUnit("a.c", ModMenuColumn.Body, false, null));
            if (Keyed(keyed, "a.c", 0) != 1) return "keyed exact";
            if (Keyed(keyed, "a.b reset", 1) != 0) return "keyed owner";
            if (Keyed(keyed, "gone", 1) != 1) return "keyed fallback";
            if (Keyed(keyed, "gone", 9) != -1) return "keyed no fallback";
            List<ModMenuUnit> ordered = new List<ModMenuUnit>();
            ordered.Add(new ModMenuUnit("g1.header", ModMenuColumn.Body, false, null));
            ordered.Add(new ModMenuUnit("g2.header", ModMenuColumn.Body, false, null));
            ordered.Add(new ModMenuUnit("g2.b", ModMenuColumn.Body, false, null));
            ordered.Add(new ModMenuUnit("g2.a", ModMenuColumn.Body, false, null));
            Sort(ordered, Sample);
            if (ordered[0].Key != "g1.header" || ordered[1].Key != "g2.header" || ordered[2].Key != "g2.a" || ordered[3].Key != "g2.b") return "sort visual order";
            if (Next(ordered, 1) != 2 || Previous(ordered, 2) != 1) return "sort down from a header";
            ordered.Add(new ModMenuUnit("g3.x", ModMenuColumn.Body, false, null));
            ordered.Add(new ModMenuUnit("g3.y", ModMenuColumn.Body, false, null));
            Sort(ordered, Sample);
            if (ordered[4].Key != "g3.x" || ordered[5].Key != "g3.y") return "sort stable";
            return null;
        }

        private static int[] Sample(ModMenuUnit unit)
        {
            if (unit.Key == "g1.header") return new int[] { 0 };
            if (unit.Key == "g2.header") return new int[] { 1 };
            if (unit.Key == "g2.a") return new int[] { 1, 0 };
            if (unit.Key == "g2.b") return new int[] { 1, 1 };
            return new int[] { 2 };
        }
    }

    internal sealed class ModMenuNav : MonoBehaviour
    {
        private const float FirstDelay = 0.4f;
        private const float RepeatDelay = 0.12f;
        private const float DeadZone = 0.5f;
        private const int None = 0;
        private const int Left = 1;
        private const int Right = 2;
        private const int Up = 3;
        private const int Down = 4;
        private readonly List<ModMenuUnit> units = new List<ModMenuUnit>();
        private readonly List<ModMenuUnit> active = new List<ModMenuUnit>();
        private ScrollRect scroll;
        private GameObject last;
        private float nextMove;
        private int lastPosition = -1;
        private string lastKey;
        private bool pending;
        private static bool probed;
        private static bool axes;

        internal void Bind(ScrollRect scrollRect)
        {
            scroll = scrollRect;
        }

        internal void Add(Selectable target, ModMenuColumn column, string key, bool adjusts)
        {
            Add(target, column, key, adjusts, null);
        }

        internal void Add(Selectable target, ModMenuColumn column, string key, bool adjusts, Action<int> adjust)
        {
            if (target == null) return;
            units.Add(new ModMenuUnit(key, column, adjusts, target, adjust));
        }

        internal void ClearBody()
        {
            ModMenuTooltip.Clear();
            units.RemoveAll(IsBody);
            active.Clear();
            lastPosition = -1;
        }

        internal void Clear()
        {
            ModMenuTooltip.Clear();
            units.Clear();
            active.Clear();
            lastPosition = -1;
            lastKey = null;
            last = null;
            pending = false;
        }

        internal void TakeFocus()
        {
            if (EventSystem.current == null || active.Count == 0) return;
            if (Position(EventSystem.current.currentSelectedGameObject) >= 0) return;
            Choose(ModMenuOrder.Enter(active, ModMenuColumn.Body));
        }

        internal void Rebuild()
        {
            Selectable keep = Selected();
            active.Clear();
            for (int i = 0; i < units.Count; i++)
            {
                if (Live(units[i].Target)) active.Add(units[i]);
            }
            ModMenuOrder.Sort(active, Path);
            for (int i = 0; i < active.Count; i++)
            {
                Navigation navigation = new Navigation();
                navigation.mode = Navigation.Mode.Explicit;
                int up = ModMenuOrder.Previous(active, i);
                int down = ModMenuOrder.Next(active, i);
                navigation.selectOnUp = up >= 0 ? active[up].Target : null;
                navigation.selectOnDown = down >= 0 ? active[down].Target : null;
                if (active[i].Adjusts)
                {
                    navigation.selectOnLeft = null;
                    navigation.selectOnRight = null;
                }
                else
                {
                    int left = Enter(active[i].Column, -1);
                    int right = Enter(active[i].Column, 1);
                    navigation.selectOnLeft = left >= 0 ? active[left].Target : null;
                    navigation.selectOnRight = right >= 0 ? active[right].Target : null;
                }
                active[i].Target.navigation = navigation;
            }
            int position = IndexOf(keep);
            if (position >= 0)
            {
                lastPosition = position;
                lastKey = active[position].Key;
                pending = true;
                return;
            }
            Keep();
            pending = true;
        }

        private int IndexOf(Selectable target)
        {
            if (target == null) return -1;
            for (int i = 0; i < active.Count; i++)
            {
                if (ReferenceEquals(active[i].Target, target)) return i;
            }
            return -1;
        }

        private int[] Path(ModMenuUnit unit)
        {
            if (unit == null || unit.Target == null) return null;
            Transform root = transform;
            Transform node = unit.Target.transform;
            List<int> path = new List<int>();
            while (node != null && node != root)
            {
                path.Add(node.GetSiblingIndex());
                node = node.parent;
            }
            if (node != root) return null;
            path.Reverse();
            return path.ToArray();
        }

        private void Update()
        {
            if (units.Count == 0 || EventSystem.current == null) return;
            Track();
            Flush();
            if (UiInput.Locked) return;
            TMP_InputField field = Selected() as TMP_InputField;
            if (field != null && field.isFocused) return;
            Activate();
            int direction = Pad();
            if (direction == None)
            {
                nextMove = 0f;
                return;
            }
            if (nextMove > 0f && Time.unscaledTime < nextMove) return;
            nextMove = Time.unscaledTime + (nextMove > 0f ? RepeatDelay : FirstDelay);
            Move(direction);
        }

        private void Track()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == last) return;
            last = selected;
            int position = Position(selected);
            if (position >= 0)
            {
                lastPosition = position;
                lastKey = active[position].Key;
            }
            pending = true;
        }

        private void Activate()
        {
            if (!Input.GetKeyDown(KeyCode.JoystickButton0) && !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
            Selectable focused = Selected();
            if (focused is Slider) Adjust(focused, 1);
        }

        private void Flush()
        {
            if (!pending) return;
            if (scroll == null)
            {
                pending = false;
                return;
            }
            if (scroll.viewport == null || scroll.viewport.rect.height <= 0f) return;
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            RectTransform rect = selected == null ? null : selected.transform as RectTransform;
            if (rect != null) ModUi.ScrollTo(scroll, rect);
            pending = false;
        }

        private void Move(int direction)
        {
            int position = Position(EventSystem.current.currentSelectedGameObject);
            if (position < 0)
            {
                Choose(lastPosition >= 0 && lastPosition < active.Count ? lastPosition : ModMenuOrder.Enter(active, ModMenuColumn.Body));
                return;
            }
            ModMenuUnit unit = active[position];
            if (direction == Left || direction == Right)
            {
                if (unit.Adjusts)
                {
                    if (unit.Adjust != null) unit.Adjust(direction == Left ? -1 : 1);
                    else Adjust(unit.Target, direction == Left ? -1 : 1);
                    pending = true;
                    return;
                }
                Choose(Enter(unit.Column, direction == Left ? -1 : 1));
                return;
            }
            Choose(direction == Up ? ModMenuOrder.Previous(active, position) : ModMenuOrder.Next(active, position));
        }

        private int Enter(ModMenuColumn column, int horizontal)
        {
            int other = ModMenuOrder.Switched(column, horizontal);
            return other < 0 ? -1 : ModMenuOrder.Enter(active, (ModMenuColumn)other);
        }

        private void Choose(int position)
        {
            if (position < 0 || position >= active.Count || EventSystem.current == null) return;
            Selectable target = active[position].Target;
            if (!Live(target)) return;
            lastPosition = position;
            lastKey = active[position].Key;
            EventSystem.current.SetSelectedGameObject(target.gameObject);
            pending = true;
        }

        private void Keep()
        {
            GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            int position = Position(selected);
            if (position >= 0)
            {
                lastPosition = position;
                lastKey = active[position].Key;
                return;
            }
            bool live = selected != null && selected.activeInHierarchy;
            if (ModMenuOrder.Holds(position, live)) return;
            int fallback = lastPosition >= 0 && lastPosition < active.Count ? lastPosition : ModMenuOrder.Enter(active, ModMenuColumn.Body);
            int next = ModMenuOrder.Keyed(active, lastKey, fallback);
            Choose(next < 0 ? ModMenuOrder.Enter(active, ModMenuColumn.Body) : next);
        }

        private int Position(GameObject selected)
        {
            if (selected == null) return -1;
            for (int i = 0; i < active.Count; i++)
            {
                Selectable target = active[i].Target;
                if (target == null) continue;
                if (ReferenceEquals(target.gameObject, selected)) return i;
            }
            return -1;
        }

        private Selectable Selected()
        {
            GameObject selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            int position = Position(selected);
            if (position < 0) return null;
            return active[position].Target;
        }

        private static bool Live(Selectable target)
        {
            return target != null && target.isActiveAndEnabled;
        }

        private static bool IsBody(ModMenuUnit unit)
        {
            return unit.Column == ModMenuColumn.Body;
        }

        private static void Adjust(Selectable target, int delta)
        {
            Slider slider = target as Slider;
            if (slider != null)
            {
                float step = (slider.maxValue - slider.minValue) * 0.05f;
                if (slider.wholeNumbers) step = Mathf.Max(1f, Mathf.Round(step));
                slider.value = Mathf.Clamp(slider.value + step * delta, slider.minValue, slider.maxValue);
                return;
            }

        }

        private static int Pad()
        {
            if (!Axes()) return None;
            float x = Input.GetAxisRaw("DPadHorizontal");
            float y = Input.GetAxisRaw("DPadVertical");
            if (Mathf.Abs(x) < DeadZone) x = 0f;
            if (Mathf.Abs(y) < DeadZone) y = 0f;
            if (x == 0f && y == 0f) return None;
            if (Mathf.Abs(x) >= Mathf.Abs(y)) return x < 0f ? Left : Right;
            return y > 0f ? Up : Down;
        }

        private static bool Axes()
        {
            if (probed) return axes;
            probed = true;
            try
            {
                Input.GetAxisRaw("DPadHorizontal");
                Input.GetAxisRaw("DPadVertical");
                axes = true;
            }
            catch (Exception e)
            {
                axes = false;
                CoreLog.For(ModMenu.Owner).Warn("d-pad navigation disabled: " + e.GetType().Name + ": " + e.Message);
            }
            return axes;
        }

        internal static string SelfCheck()
        {
            return ModMenuOrder.SelfCheck();
        }
    }

    internal static class ModMenuHint
    {
        private const float IconSize = 20f;

        internal static void Build(RectTransform footer)
        {
            RectTransform line = ModUi.NewRect("Hints", footer);
            HorizontalLayoutGroup group = line.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = UiTheme.Gap;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            LayoutElement size = ModUi.Sizing(line.gameObject);
            size.preferredWidth = 0f;
            size.flexibleWidth = 1f;
            Segment(line, KeyCode.Joystick1Button0, "Apply");
            Segment(line, KeyCode.Joystick1Button1, "Back");
            Segment(line, KeyCode.None, Display("Switch Tab Left") + " / " + Display("Switch Tab Right") + " Page");
            Segment(line, KeyCode.None, "D-pad / Left stick Move");
            Segment(line, KeyCode.None, Display(KeyCode.Escape) + " Close");
        }

        private static void Segment(Transform parent, KeyCode icon, string caption)
        {
            RectTransform item = ModUi.NewRect("Hint", parent);
            HorizontalLayoutGroup group = item.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = UiTheme.HalfGap;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            Sprite sprite = Icon(icon);
            string text = caption;
            if (sprite != null)
            {
                RectTransform glyph = ModUi.NewRect("Glyph", item);
                Image image = glyph.gameObject.AddComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                LayoutElement glyphSize = ModUi.Sizing(glyph.gameObject);
                glyphSize.minWidth = IconSize;
                glyphSize.preferredWidth = IconSize;
                glyphSize.minHeight = IconSize;
                glyphSize.preferredHeight = IconSize;
            }
            else if (icon != KeyCode.None)
            {
                text = Display(icon) + " " + caption;
            }
            ModUi.Label(item, text, UiTheme.MutedSize, UiTheme.Muted, TextAlignmentOptions.MidlineLeft, false);
        }

        private static string Display(KeyCode key)
        {
            InputManager manager = Manager();
            if (manager == null) return key.ToString();
            try
            {
                return manager.GetKeyCodeDisplayText(key);
            }
            catch (Exception)
            {
                return key.ToString();
            }
        }

        private static string Display(string action)
        {
            InputManager manager = Manager();
            if (manager == null) return action;
            try
            {
                return manager.GetKeyCodeDisplayText(action);
            }
            catch (Exception)
            {
                return action;
            }
        }

        private static Sprite Icon(KeyCode key)
        {
            InputManager manager = Manager();
            if (manager == null || manager.inputImages == null || manager.keyBindings == null) return null;
            try
            {
                return manager.inputImages.GetInputImage(key);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static InputManager Manager()
        {
            try
            {
                return InputManager.instance;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
