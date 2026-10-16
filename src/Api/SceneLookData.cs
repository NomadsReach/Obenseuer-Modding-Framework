using System.Collections.Generic;

namespace ObenseuerCore
{
    internal static class SceneLookClass
    {
        internal const int GameObject = 1;
        internal const int Transform = 4;
        internal const int Texture2D = 28;
        internal const int Mesh = 43;
        internal const int Shader = 48;
        internal const int Animator = 95;
        internal const int MonoBehaviour = 114;
        internal const int SkinnedMeshRenderer = 137;
    }

    internal struct SceneLookRef
    {
        public int FileId;
        public long PathId;
    }

    internal sealed class SceneLookMemo
    {
        internal readonly Dictionary<long, SceneLookGameObject> GameObjects = new Dictionary<long, SceneLookGameObject>();
        internal readonly Dictionary<long, SceneLookTransform> Transforms = new Dictionary<long, SceneLookTransform>();
        internal readonly Dictionary<long, HashSet<string>> Kinds = new Dictionary<long, HashSet<string>>();
        internal readonly Dictionary<long, string> BehaviourClasses = new Dictionary<long, string>();
        internal readonly Dictionary<long, string> ScriptClasses = new Dictionary<long, string>();
        internal readonly Dictionary<long, string> ShaderLabels = new Dictionary<long, string>();
    }

    internal sealed class SceneLookGameObject
    {
        public long PathId;
        public string Name;
        public List<SceneLookRef> Components;
    }

    internal sealed class SceneLookTransform
    {
        public long PathId;
        public SceneLookRef GameObject;
        public float[] Rotation;
        public float[] Position;
        public float[] Scale;
        public SceneLookRef Father;
        public List<SceneLookRef> Children;
    }

    internal sealed class SceneLookBehaviour
    {
        public long PathId;
        public SceneLookRef GameObject;
        public SceneLookRef Script;
        public string Name;
    }

    internal sealed class SceneLookNpcInfo
    {
        public long PathId;
        public SceneLookRef GameObject;
        public string Guid;
        public bool Global;
        public byte[] SavedTimeAndDay;
        public SceneLookRef Character;
        public SceneLookRef CharacterModel;
    }

    internal sealed class SceneLookCharacter
    {
        public int Id;
        public string Name;
        public string LastName;
        public string GenderString;
        public int Age;
        public string Relationship;
        public string Job;
        public string AssetName;
        public string Addressable;
        public SceneLookRef Sprite;
        public SceneLookRef Prefab;
        public SceneLookRef PrefabWithAi;
    }

    internal sealed class SceneLookAnimator
    {
        public long PathId;
        public SceneLookRef Avatar;
        public SceneLookRef Controller;
    }

    internal sealed class SceneLookAvatarInfo
    {
        public string Kind;
        public int Roles;
        public string ControllerName;
    }

    internal sealed class SceneLookSubmesh
    {
        public uint FirstByte;
        public uint IndexCount;
        public int Topology;
        public uint BaseVertex;
        public uint FirstVertex;
        public uint VertexCount;
    }

    internal sealed class SceneLookChannel
    {
        public int Stream;
        public int Offset;
        public int Format;
        public int Dimension;
    }

    internal sealed class SceneLookBlendShape
    {
        public uint FirstVertex;
        public uint VertexCount;
        public bool HasNormals;
        public bool HasTangents;
    }

    internal sealed class SceneLookBlendShapeChannel
    {
        public int Hash;
        public int FirstFrame;
        public int Frames;
    }

    internal sealed class SceneLookMesh
    {
        public long PathId;
        public string FileName;
        public string Name;
        public List<SceneLookSubmesh> Submeshes;
        public uint VertexCount;
        public List<SceneLookChannel> Channels;
        public List<string> BlendShapeNames;
        public List<SceneLookBlendShape> BlendShapes;
        public List<SceneLookBlendShapeChannel> BlendShapeChannels;
        public float[] BlendShapeFrameWeights;
        public int BlendShapeVertexCount;
        public float[][] BindPoses;
        public int IndexFormat;
        public long IndexDataSize;
        public long VertexDataSize;
        public long ExpectedVertexDataSize;
        public byte[] VertexData;
        public byte[] IndexData;
        public byte[] BlendShapeVertexData;
    }

    internal sealed class SceneLookRenderer
    {
        public long PathId;
        public SceneLookRef GameObject;
        public SceneLookRef Mesh;
        public List<SceneLookRef> Materials;
        public List<SceneLookRef> Bones;
        public SceneLookRef RootBone;
        public float[] BlendShapeWeights;
    }

    internal sealed class SceneLookNamed<T>
    {
        public string Name;
        public T Value;
    }

    internal sealed class SceneLookTexEnv
    {
        public string Name;
        public SceneLookRef Texture;
        public SceneLookTexture Resolved;
        public float[] Scale;
        public float[] Offset;
    }

    internal sealed class SceneLookMaterial
    {
        public long PathId;
        public string FileName;
        public string Name;
        public SceneLookRef Shader;
        public int RenderQueue;
        public List<SceneLookTexEnv> TexEnvs;
        public List<SceneLookNamed<int>> Ints;
        public List<SceneLookNamed<float>> Floats;
        public List<SceneLookNamed<float[]>> Colors;
    }

    internal sealed class SceneLookTexture
    {
        public long PathId;
        public string FileName;
        public string Name;
        public int Width;
        public int Height;
        public int Format;
        public int Mips;
        public long CompleteSize;
        public bool Inline;
        public bool InlineValid;
        public string StreamPath;
        public long StreamOffset;
        public long StreamSize;
    }

    internal sealed class SceneLookNode
    {
        public long GameObject;
        public long Transform;
        public string Name;
        public SceneLookRef Father;
        public List<SceneLookRef> Children;
        public float[] Rotation;
        public float[] Position;
        public float[] Scale;
    }

    internal sealed class SceneLookBone
    {
        public string Name;
        public SceneLookRef Transform;
    }

    internal sealed class SceneLookLookRenderer
    {
        public string Name;
        public SceneLookRenderer Source;
        public SceneLookMesh Mesh;
        public List<SceneLookMaterial> Materials;
        public List<SceneLookBone> Bones;
        public SceneLookBone RootBone;
    }

    internal sealed class SceneLookLook
    {
        public string FileName;
        public long Root;
        public List<SceneLookNode> Nodes;
        public List<SceneLookLookRenderer> Renderers;
        public List<SceneLookTexture> Textures;
        public SceneLookFiles Files;
    }

    internal sealed class SceneLookRoot
    {
        public long GameObject;
        public string Name;
        public string Path;
        public bool IsNpc;
        public bool HasAnimator;
        public int Renderers;
        public List<string> BoneNames;
        public string Rig;
        public string Avatar;
        public List<SceneLookAvatarInfo> Avatars;
        public long Vertices;
        public long Triangles;
        public bool Clean;
        public List<string> Reasons;
        public List<int> Formats;
        public List<string> Shaders;
        public List<string> Hierarchy;
        public SceneLookCharacter Character;
        public string CharacterNote;
    }

    internal sealed class SceneLookNpc
    {
        public long PathId;
        public long GameObject;
        public string Name;
        public string Path;
        public SceneLookCharacter Character;
        public string CharacterNote;
        public long ModelGameObject;
        public long Body;
    }

    internal sealed class SceneLookFileScan
    {
        public string FileName;
        public string Error;
        public double Seconds;
        public int Renderers;
        public int CleanRenderers;
        public SceneLookCounter<string> ReasonCounts = new SceneLookCounter<string>();
        public int Unrooted;
        public SceneLookCounter<string> UnrootedRigs = new SceneLookCounter<string>();
        public SceneLookCounter<int> FormatsUsed = new SceneLookCounter<int>();
        public SceneLookCounter<string> ShadersUsed = new SceneLookCounter<string>();
        public SceneLookCounter<int> TextureFormats = new SceneLookCounter<int>();
        public SceneLookCounter<string> ShaderObjects = new SceneLookCounter<string>();
        public List<SceneLookRoot> Roots = new List<SceneLookRoot>();
        public List<SceneLookNpc> Npcs = new List<SceneLookNpc>();
    }

    internal sealed class SceneLookCounter<T>
    {
        private readonly List<T> keys = new List<T>();
        private readonly Dictionary<T, int> counts = new Dictionary<T, int>();

        internal void Add(T key)
        {
            Add(key, 1);
        }

        internal void Add(T key, int amount)
        {
            int count;
            if (counts.TryGetValue(key, out count))
            {
                counts[key] = count + amount;
                return;
            }
            counts[key] = amount;
            keys.Add(key);
        }

        internal int this[T key]
        {
            get
            {
                int count;
                return counts.TryGetValue(key, out count) ? count : 0;
            }
        }

        internal IList<T> Keys
        {
            get { return keys; }
        }
    }
}
