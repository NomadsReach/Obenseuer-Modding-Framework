using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 618

namespace ObenseuerCore
{
    internal static class ModMenuTab
    {
        private const string TabTitle = "Mods";
        private const int CheckFrames = 3;
        internal static UIPageButtons Owner;
        internal static UIPageButtons FailedFor;
        private static MenuPageNames id;
        private static MenuPageNames lastPage;
        private static bool arrived;
        private static int checks;

        internal static void Build(UIPageButtons pb)
        {
            GameObject panel = null;
            GameObject tab = null;
            MenuPage entry = null;
            try
            {
                MenuPage[] pages = Pages(pb);
                MenuPage character = Find(pages, MenuPageNames.Character);
                if (character == null || character.Panel == null || character.Button == null) throw new InvalidOperationException("Character page not found in MenuPages");
                Transform strip = FindStrip(pages);
                MenuPage source = character;
                if (character.Selected) source = FirstIdle(pages) ?? character;
                Transform sourceSlot = SlotRoot(strip, source.Button.transform);
                Transform anchor = SlotRoot(strip, character.Button.transform);
                if (sourceSlot == null || anchor == null) throw new InvalidOperationException("tab slot not found in the tab strip");
                MenuPageNames tabId = Allocate(pages);

                panel = UnityEngine.Object.Instantiate(character.Panel, character.Panel.transform.parent);
                panel.name = "ModMenuPagePanel";
                panel.SetActive(false);
                CharacterPanel stale = panel.GetComponent<CharacterPanel>();
                if (stale != null) UnityEngine.Object.DestroyImmediate(stale);
                ModMenuWidgets.Clear(panel.transform);

                tab = UnityEngine.Object.Instantiate(sourceSlot.gameObject, strip);
                tab.name = "ModMenuPageTab";
                Transform button = Counterpart(sourceSlot, source.Button.transform, tab.transform);
                if (button == null) throw new InvalidOperationException("could not locate the button inside the cloned tab");
                StripLocalization(tab);
                Rewire(tab);
                SetLabels(source, sourceSlot, tab, TabTitle);
                ResetVisual(button.gameObject);
                tab.transform.SetSiblingIndex(anchor.GetSiblingIndex() + 1);

                entry = new MenuPage();
                entry.Name = tabId;
                entry.Panel = panel;
                entry.Button = button.gameObject;
                entry.Text = null;
                entry.Key = "";
                entry.Selected = false;
                entry.Disabled = false;
                MenuPage[] next = new MenuPage[pages.Length + 1];
                Array.Copy(pages, next, pages.Length);
                next[pages.Length] = entry;
                id = tabId;
                Owner = pb;
                checks = CheckFrames;
                ModMenuReflect.Set(pb, "MenuPages", next);
                CoreLog.For(ModMenu.Owner).Info("Mods tab added as menu page " + (int)tabId);
            }
            catch (Exception e)
            {
                FailedFor = pb;
                if (Owner == pb) Owner = null;
                if (entry != null) Unregister(pb, entry);
                if (panel != null) UnityEngine.Object.Destroy(panel);
                if (tab != null) UnityEngine.Object.Destroy(tab);
                CoreLog.For(ModMenu.Owner).Error("Mods tab not added: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void Unregister(UIPageButtons pb, MenuPage entry)
        {
            try
            {
                MenuPage[] pages = Pages(pb);
                int index = Array.IndexOf(pages, entry);
                if (index < 0) return;
                MenuPage[] kept = new MenuPage[pages.Length - 1];
                Array.Copy(pages, 0, kept, 0, index);
                Array.Copy(pages, index + 1, kept, index, pages.Length - index - 1);
                ModMenuReflect.Set(pb, "MenuPages", kept);
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Error("Mods tab entry not removed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        internal static void Tick()
        {
            UIPageButtons pb = UIPageButtons.instance;
            if (pb != null && pb == Owner) Follow(pb);
            if (checks == 0) return;
            if (--checks > 0) return;
            try
            {
                VerifySpacing();
            }
            catch (Exception e)
            {
                CoreLog.For(ModMenu.Owner).Warn("Mods tab spacing check failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        internal static bool OnPage(UIPageButtons pb)
        {
            return pb != null && pb == Owner && pb.currentPage == id;
        }

        private static void Follow(UIPageButtons pb)
        {
            if (pb.currentPage != id)
            {
                lastPage = pb.currentPage;
                arrived = false;
                return;
            }
            if (!arrived)
            {
                arrived = true;
                ModMenuWindow.Open();
            }
            else if (!ModMenuWindow.IsOpen)
            {
                pb.Select(lastPage);
            }
        }

        private static void VerifySpacing()
        {
            if (Owner == null) return;
            MenuPage[] pages = Pages(Owner);
            Transform strip = FindStrip(pages);
            Transform mine = SlotRoot(strip, ButtonOf(pages, id));
            Transform anchor = SlotRoot(strip, ButtonOf(pages, MenuPageNames.Character));
            if (mine == null || anchor == null) return;
            bool overlap = false;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == null || pages[i].Button == null) continue;
                Transform slot = SlotRoot(strip, pages[i].Button.transform);
                if (slot == null || slot == mine) continue;
                Vector3 p = slot.localPosition;
                if (Mathf.Abs(p.x - mine.localPosition.x) < 1f && Mathf.Abs(p.y - mine.localPosition.y) < 1f) overlap = true;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            if (!overlap) return;
            bool horizontal = maxX - minX >= maxY - minY;
            RectTransform rect = anchor as RectTransform;
            float size = rect == null ? 100f : (horizontal ? rect.rect.width : rect.rect.height);
            Vector3 target = anchor.localPosition;
            if (horizontal) target.x = maxX + size + 10f;
            else target.y = minY - size - 10f;
            mine.localPosition = target;
            CoreLog.For(ModMenu.Owner).Info("Mods tab moved to " + target + " to clear the tab strip");
        }

        private static void OpenTab()
        {
            ModMenuWindow.Open();
        }

        private static MenuPage[] Pages(UIPageButtons pb)
        {
            MenuPage[] pages = ModMenuReflect.Get<MenuPage[]>(pb, "MenuPages");
            if (pages == null) throw new InvalidOperationException("UIPageButtons.MenuPages is empty");
            return pages;
        }

        private static MenuPage Find(MenuPage[] pages, MenuPageNames name)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null && pages[i].Name == name) return pages[i];
            }
            return null;
        }

        private static MenuPage FirstIdle(MenuPage[] pages)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null && pages[i].Button != null && !pages[i].Selected && !pages[i].Disabled) return pages[i];
            }
            return null;
        }

        private static Transform ButtonOf(MenuPage[] pages, MenuPageNames name)
        {
            MenuPage page = Find(pages, name);
            return page == null || page.Button == null ? null : page.Button.transform;
        }

        private static MenuPageNames Allocate(MenuPage[] pages)
        {
            int next = (int)MenuPageNames.Items + 1;
            while (Find(pages, (MenuPageNames)next) != null) next++;
            return (MenuPageNames)next;
        }

        private static Transform FindStrip(MenuPage[] pages)
        {
            Transform strip = null;
            for (int i = 0; i < pages.Length && strip == null; i++)
            {
                if (pages[i] != null && pages[i].Button != null) strip = pages[i].Button.transform.parent;
            }
            while (strip != null)
            {
                bool all = true;
                for (int j = 0; j < pages.Length; j++)
                {
                    if (pages[j] != null && pages[j].Button != null && !pages[j].Button.transform.IsChildOf(strip))
                    {
                        all = false;
                        break;
                    }
                }
                if (all) return strip;
                strip = strip.parent;
            }
            throw new InvalidOperationException("could not find the tab strip");
        }

        private static Transform SlotRoot(Transform strip, Transform button)
        {
            Transform slot = button;
            while (slot != null && slot.parent != strip) slot = slot.parent;
            return slot;
        }

        private static Transform Counterpart(Transform srcRoot, Transform srcNode, Transform dstRoot)
        {
            if (srcNode == null) return null;
            List<int> path = new List<int>();
            Transform walk = srcNode;
            while (walk != null && walk != srcRoot)
            {
                path.Add(walk.GetSiblingIndex());
                walk = walk.parent;
            }
            if (walk == null) return null;
            Transform node = dstRoot;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (path[i] >= node.childCount) return null;
                node = node.GetChild(path[i]);
            }
            return node;
        }

        private static void StripLocalization(GameObject root)
        {
            MenuLocalization[] found = root.GetComponentsInChildren<MenuLocalization>(true);
            for (int i = 0; i < found.Length; i++) UnityEngine.Object.DestroyImmediate(found[i]);
        }

        private static void Rewire(GameObject tab)
        {
            UnityEngine.UI.Button[] buttons = tab.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            if (buttons.Length == 0) throw new InvalidOperationException("the cloned tab has no Button");
            for (int i = 0; i < buttons.Length; i++) ModMenuWidgets.Strip(buttons[i].onClick);
            buttons[0].onClick.AddListener(OpenTab);
        }

        private static void SetLabels(MenuPage source, Transform sourceSlot, GameObject tab, string title)
        {
            Transform key = source.Text == null ? null : Counterpart(sourceSlot, source.Text.transform, tab.transform);
            string upper = title.ToUpperInvariant();
            TextMeshProUGUI[] texts = tab.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (key != null && texts[i].transform == key)
                {
                    texts[i].text = "";
                    continue;
                }
                string current = texts[i].text ?? "";
                texts[i].text = current.Length > 0 && current == current.ToUpperInvariant() ? upper : title;
            }
        }

        private static void ResetVisual(GameObject button)
        {
            DefaultUIButton style = button.GetComponent<DefaultUIButton>();
            if (style != null) style.ChangePressed(false);
            Animator animator = button.GetComponent<Animator>();
            if (animator != null) animator.SetBool("MoveUp", false);
        }
    }
}
