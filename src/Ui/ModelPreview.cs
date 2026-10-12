using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ObenseuerCore.Ui
{
    public enum PreviewFraming
    {
        FullBody,
        Face
    }

    public sealed class ModelPreview : MonoBehaviour, IDragHandler, IScrollHandler
    {
        private const int PreviewLayer = 31;
        private const int PreviewBit = 1 << PreviewLayer;
        private const int MaxWidth = 1536;
        private const int MaxHeight = 1536;
        private const float Fov = 30f;
        private const float Elevation = 4f;
        private const float DegreesPerPixel = 0.4f;
        private const float ZoomStep = 0.1f;
        private const float MinZoom = 0.3f;
        private const float MaxZoom = 2.5f;
        private const float FullFill = 0.85f;
        private const float FaceCenter = 0.11f;
        private const float FaceHalf = 0.165f;
        private const int ReportNames = 10;
        private const float SwitchMargin = 8f;
        private static readonly Vector3[] LampFrom =
        {
            new Vector3(-0.55f, 0.6f, -0.6f),
            new Vector3(0.75f, 0.1f, -0.65f),
            new Vector3(0.3f, 0.7f, 0.65f),
            new Vector3(0f, 0f, -1f)
        };
        private static readonly Color[] LampColor =
        {
            new Color(1f, 0.93f, 0.82f),
            new Color(0.78f, 0.87f, 1f),
            new Color(0.85f, 0.92f, 1f),
            Color.white
        };
        private static readonly float[] LampIntensity = { 1.25f, 0.5f, 0.8f, 0.3f };
        private static readonly string[] FramingNames = { "Body", "Face" };
        private static int open;
        private readonly Dictionary<Renderer, bool> enabledBefore = new Dictionary<Renderer, bool>();
        private readonly Dictionary<GameObject, int> layerBefore = new Dictionary<GameObject, int>();
        private readonly Dictionary<GameObject, bool> rigActive = new Dictionary<GameObject, bool>();
        private readonly Dictionary<SkinnedMeshRenderer, bool> offscreenBefore = new Dictionary<SkinnedMeshRenderer, bool>();
        private readonly Dictionary<SkinnedMeshRenderer, Bounds> boundsBefore = new Dictionary<SkinnedMeshRenderer, Bounds>();
        private readonly Dictionary<SkinnedMeshRenderer, Mesh> meshBefore = new Dictionary<SkinnedMeshRenderer, Mesh>();
        private readonly Dictionary<Camera, int> maskBefore = new Dictionary<Camera, int>();
        private readonly Dictionary<Light, int> lightMaskBefore = new Dictionary<Light, int>();
        private RawImage raw;
        private Transform target;
        private PreviewFraming framing;
        private GameObject cameraHost;
        private GameObject lightHost;
        private Camera lens;
        private Light[] lamps;
        private RenderTexture texture;
        private string modId;
        private float yaw;
        private float zoom = 1f;
        private int settleFrame = -1;
        private bool reported;
        private bool counted;
        private bool disposed;
        private ModSegmented framingSwitch;
        private bool showSwitch = true;

        public PreviewFraming Framing
        {
            get { return framing; }
            set
            {
                framing = value;
                if (framingSwitch != null) framingSwitch.Set(value == PreviewFraming.Face ? 1 : 0);
                Reframe();
            }
        }

        public RenderTexture Texture
        {
            get { return texture; }
        }

        public int Renderers
        {
            get { return enabledBefore.Count; }
        }

        public bool ShowFramingSwitch
        {
            get { return showSwitch; }
            set
            {
                showSwitch = value;
                if (framingSwitch != null) framingSwitch.Rect.gameObject.SetActive(value);
            }
        }

        public void Refresh()
        {
            if (disposed) return;
            Record();
            Reframe();
            settleFrame = Time.frameCount + 1;
        }

        public int Dispose()
        {
            if (disposed) return 0;
            disposed = true;
            int recorded = enabledBefore.Count;
            if (modId != null)
            {
                ModWindow.Quiet(modId, RestoreRenderers);
                ModWindow.Quiet(modId, RestoreLayers);
                ModWindow.Quiet(modId, RestoreSkins);
                ModWindow.Quiet(modId, RestoreRigs);
                ModWindow.Quiet(modId, ReassertPlayerModel);
                ModWindow.Quiet(modId, RestoreMasks);
                ModWindow.Quiet(modId, RestoreLightMasks);
                ModWindow.Quiet(modId, ReleaseTarget);
            }
            if (counted)
            {
                counted = false;
                open--;
            }
            return recorded;
        }

        internal void Init(UiScope owner, Transform subject, PreviewFraming start)
        {
            if (open > 0) throw new InvalidOperationException("Only one model preview can be open at a time");
            open++;
            counted = true;
            modId = owner.ModId;
            target = subject;
            framing = start;
            raw = GetComponent<RawImage>();
            framingSwitch = ModUi.Segmented(transform, FramingNames, start == PreviewFraming.Face ? 1 : 0, OnFramingPick);
            float switchWidth = FramingNames.Length * UiTheme.SegmentWidth;
            ModUi.Place(framingSwitch.Rect, new Vector2(1f, 1f), Vector2.one, new Vector2(-switchWidth - SwitchMargin, -UiTheme.SegmentHeight - SwitchMargin), new Vector2(-SwitchMargin, -SwitchMargin));
            framingSwitch.Rect.gameObject.SetActive(showSwitch);
            CreateRig();
            SetRig(isActiveAndEnabled);
            Record();
            MaskCameras();
            MaskLights();
            Canvas.ForceUpdateCanvases();
            Resize();
            Reframe();
            settleFrame = Time.frameCount + 1;
        }

        private void OnFramingPick(int index)
        {
            Framing = index == 1 ? PreviewFraming.Face : PreviewFraming.FullBody;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (lens != null && !disposed) Resize();
        }

        private void LateUpdate()
        {
            if (settleFrame < 0 || disposed || Time.frameCount < settleFrame) return;
            settleFrame = -1;
            Resize();
            Reframe();
            if (reported) return;
            reported = true;
            Report();
        }

        private void OnEnable()
        {
            SetRig(true);
        }

        private void OnDisable()
        {
            SetRig(false);
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void SetRig(bool on)
        {
            if (lens != null) lens.enabled = on;
            if (lamps == null) return;
            for (int i = 0; i < lamps.Length; i++) lamps[i].enabled = on;
        }

        void IDragHandler.OnDrag(PointerEventData data)
        {
            yaw += data.delta.x * DegreesPerPixel;
            Reframe();
        }

        void IScrollHandler.OnScroll(PointerEventData data)
        {
            zoom = Mathf.Clamp(zoom * (1f - data.scrollDelta.y * ZoomStep), MinZoom, MaxZoom);
            Reframe();
        }

        private void CreateRig()
        {
            cameraHost = new GameObject("ObenseuerCore.ModelPreview.Camera");
            lens = cameraHost.AddComponent<Camera>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = new Color(0f, 0f, 0f, 0f);
            lens.cullingMask = PreviewBit;
            lens.fieldOfView = Fov;
            lens.nearClipPlane = 0.05f;
            lens.farClipPlane = 50f;
            lightHost = new GameObject("ObenseuerCore.ModelPreview.Lights");
            lamps = new Light[LampFrom.Length];
            for (int i = 0; i < lamps.Length; i++)
            {
                GameObject host = new GameObject("Lamp", typeof(Light));
                host.transform.SetParent(lightHost.transform, false);
                Light lamp = host.GetComponent<Light>();
                lamp.type = LightType.Directional;
                lamp.color = LampColor[i];
                lamp.intensity = LampIntensity[i];
                lamp.cullingMask = PreviewBit;
                lamp.shadows = LightShadows.None;
                lamps[i] = lamp;
            }
        }

        private void Record()
        {
            if (target == null) return;
            if (PlayerLocator.instance != null)
            {
                RecordRig(PlayerLocator.instance.playerRig);
                RecordRig(PlayerLocator.instance.playerRigUnrigified);
            }
            Adopt(target);
        }

        private void RecordRig(GameObject rig)
        {
            if (rig == null) return;
            if (!rigActive.ContainsKey(rig))
            {
                rigActive.Add(rig, rig.activeSelf);
                rig.SetActive(true);
            }
            Adopt(rig.transform);
        }

        private void Adopt(Transform root)
        {
            MeshRenderer[] meshes = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++) Claim(meshes[i]);
            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++) Claim(skins[i]);
        }

        private void Claim(Renderer item)
        {
            GameObject host = item.gameObject;
            if (!host.activeInHierarchy) return;
            if (!enabledBefore.ContainsKey(item)) enabledBefore.Add(item, item.enabled);
            if (!layerBefore.ContainsKey(host)) layerBefore.Add(host, host.layer);
            item.enabled = true;
            host.layer = PreviewLayer;
            SkinnedMeshRenderer skin = item as SkinnedMeshRenderer;
            if (skin == null || skin.sharedMesh == null) return;
            if (!offscreenBefore.ContainsKey(skin))
            {
                offscreenBefore.Add(skin, skin.updateWhenOffscreen);
                boundsBefore.Add(skin, skin.localBounds);
                meshBefore.Add(skin, skin.sharedMesh);
            }
            skin.updateWhenOffscreen = true;
            skin.localBounds = skin.sharedMesh.bounds;
        }

        private void MaskCameras()
        {
            Camera[] all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++)
            {
                Camera other = all[i];
                if (other == lens || (other.cullingMask & PreviewBit) == 0) continue;
                maskBefore.Add(other, other.cullingMask);
                other.cullingMask = other.cullingMask & ~PreviewBit;
            }
        }

        private void MaskLights()
        {
            Light[] all = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                Light other = all[i];
                if (other.transform.IsChildOf(lightHost.transform) || (other.cullingMask & PreviewBit) == 0) continue;
                lightMaskBefore.Add(other, other.cullingMask);
                other.cullingMask = other.cullingMask & ~PreviewBit;
            }
        }

        private void Resize()
        {
            RectTransform rect = (RectTransform)transform;
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.rect.width), 64, MaxWidth);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.rect.height), 64, MaxHeight);
            if (texture != null && texture.width == width && texture.height == height) return;
            if (texture != null) Destroy(texture);
            texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
            lens.targetTexture = texture;
            raw.texture = texture;
        }

        private void Reframe()
        {
            if (lens == null || target == null) return;
            Bounds bounds = Measure();
            float height = Mathf.Max(bounds.size.y, 0.2f);
            Vector3 focus = bounds.center;
            float half = height * 0.5f / FullFill;
            if (framing == PreviewFraming.Face)
            {
                focus = new Vector3(bounds.center.x, bounds.max.y - height * FaceCenter, bounds.center.z);
                half = height * FaceHalf;
            }
            half = Mathf.Max(half, 0.05f);
            float distance = half / Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad) * zoom;
            Vector3 front = target.forward;
            front.y = 0f;
            if (front.sqrMagnitude < 0.0001f) front = Vector3.forward;
            Vector3 offset = Quaternion.Euler(-Elevation, yaw, 0f) * front.normalized;
            lens.transform.position = focus + offset * distance;
            lens.transform.LookAt(focus);
            Aim();
        }

        private void Aim()
        {
            Transform eye = lens.transform;
            for (int i = 0; i < lamps.Length; i++)
            {
                Vector3 from = eye.TransformDirection(LampFrom[i]).normalized;
                lamps[i].transform.rotation = Quaternion.LookRotation(-from);
            }
        }

        private void Report()
        {
            int active = 0;
            int visible = 0;
            int named = 0;
            string hidden = "";
            foreach (Renderer item in enabledBefore.Keys)
            {
                if (item == null || !item.enabled || !item.gameObject.activeInHierarchy) continue;
                active++;
                if (item.isVisible)
                {
                    visible++;
                }
                else if (named < ReportNames)
                {
                    hidden += (named > 0 ? ", " : "") + item.name;
                    named++;
                }
            }
            CoreLog.For(modId).Info("model preview: texture " + texture.width + "x" + texture.height + ", " + enabledBefore.Count + " renderers recorded, " + active + " active, " + visible + " visible, " + rigActive.Count + " rigs recorded" + (named > 0 ? ", not visible: " + hidden : ""));
        }

        private Bounds Measure()
        {
            Bounds bounds = new Bounds(target.position, Vector3.one * 0.2f);
            bool first = true;
            foreach (Renderer item in enabledBefore.Keys)
            {
                if (item == null || !item.enabled || !item.gameObject.activeInHierarchy) continue;
                Bounds box = item.bounds;
                if (box.size == Vector3.zero) continue;
                if (first)
                {
                    bounds = box;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(box);
                }
            }
            return bounds;
        }

        private void RestoreRenderers()
        {
            foreach (KeyValuePair<Renderer, bool> pair in enabledBefore)
            {
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            }
            enabledBefore.Clear();
        }

        private void RestoreLayers()
        {
            foreach (KeyValuePair<GameObject, int> pair in layerBefore)
            {
                if (pair.Key != null) pair.Key.layer = pair.Value;
            }
            layerBefore.Clear();
        }

        private void RestoreSkins()
        {
            foreach (KeyValuePair<SkinnedMeshRenderer, bool> pair in offscreenBefore)
            {
                if (pair.Key != null) pair.Key.updateWhenOffscreen = pair.Value;
            }
            foreach (KeyValuePair<SkinnedMeshRenderer, Bounds> pair in boundsBefore)
            {
                if (pair.Key != null && pair.Key.sharedMesh == meshBefore[pair.Key]) pair.Key.localBounds = pair.Value;
            }
            offscreenBefore.Clear();
            boundsBefore.Clear();
            meshBefore.Clear();
        }

        private void RestoreRigs()
        {
            foreach (KeyValuePair<GameObject, bool> pair in rigActive)
            {
                if (pair.Key != null) pair.Key.SetActive(pair.Value);
            }
            rigActive.Clear();
        }

        private void ReassertPlayerModel()
        {
            ThirdPersonCameraCollision view = ThirdPersonCameraCollision.instance;
            if (view == null) return;
            if (view.firstperson) view.HidePlayerModel();
            else view.ShowPlayerModel();
        }

        private void RestoreMasks()
        {
            foreach (KeyValuePair<Camera, int> pair in maskBefore)
            {
                if (pair.Key == null) continue;
                pair.Key.cullingMask = (pair.Key.cullingMask & ~PreviewBit) | (pair.Value & PreviewBit);
            }
            maskBefore.Clear();
        }

        private void RestoreLightMasks()
        {
            foreach (KeyValuePair<Light, int> pair in lightMaskBefore)
            {
                if (pair.Key == null) continue;
                pair.Key.cullingMask = (pair.Key.cullingMask & ~PreviewBit) | (pair.Value & PreviewBit);
            }
            lightMaskBefore.Clear();
        }

        private void ReleaseTarget()
        {
            if (raw != null) raw.texture = null;
            if (lens != null) lens.targetTexture = null;
            if (texture != null) Destroy(texture);
            texture = null;
            if (cameraHost != null) Destroy(cameraHost);
            if (lightHost != null) Destroy(lightHost);
            cameraHost = null;
            lightHost = null;
            lamps = null;
            lens = null;
        }
    }
}
