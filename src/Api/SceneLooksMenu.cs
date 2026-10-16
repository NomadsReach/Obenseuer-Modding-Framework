namespace ObenseuerCore
{
    internal static class SceneLooksMenu
    {
        internal const string Action = "scene-looks-checks";
        internal const string Section = "Scene looks";
        internal const string Title = "Run scene looks checks";
        internal const string Description = "Read only. Lists the scene look entries of the active scene, characters only and all roots, then the NPC and character names.";

        internal static void Init()
        {
            ModMenuActions.Register(ModIds.CoreId, Action, Section, Title, Description, SceneLooksConsole.RunChecks);
        }
    }
}
