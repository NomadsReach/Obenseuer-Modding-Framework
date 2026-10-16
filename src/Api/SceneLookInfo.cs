namespace ObenseuerCore
{
    public sealed class SceneLookInfo
    {
        public string Scene { get; internal set; }
        public string Level { get; internal set; }
        public string Name { get; internal set; }
        public string Path { get; internal set; }
        public int Renderers { get; internal set; }
        public int Bones { get; internal set; }
        public string RigKind { get; internal set; }
        public string AvatarKind { get; internal set; }
        public bool HasAnimator { get; internal set; }
        public bool IsNpc { get; internal set; }
        public bool Clean { get; internal set; }
        public bool HasCharacter { get; internal set; }
        public int CharacterId { get; internal set; }
        public string CharacterName { get; internal set; }
        internal long Root { get; set; }
    }

    public sealed class SceneLook
    {
        public string Scene { get; internal set; }
        public string Path { get; internal set; }
        public string Name { get; internal set; }
        public bool Ready { get; internal set; }
        public bool Failed { get; internal set; }
        public string Reason { get; internal set; }
        public int MeshCount { get; internal set; }
        public int VertexCount { get; internal set; }
        public string[] BoneNames { get; internal set; }
        internal SceneLookLook Look { get; set; }
    }
}
