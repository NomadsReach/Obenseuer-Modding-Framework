using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ObenseuerCore
{
    public static class WorldLocation
    {
        public static string CurrentScene
        {
            get
            {
                return SceneManager.GetActiveScene().name;
            }
        }

        public static List<string> KnownScenes()
        {
            if (ReadSceneNames.Instance == null || ReadSceneNames.Instance.scenes == null) return new List<string>();
            return new List<string>(ReadSceneNames.Instance.scenes);
        }

        public static List<string> VisitedScenes()
        {
            if (SaveSceneManager.instance == null) return new List<string>();
            return new List<string>(SaveSceneManager.instance.visitedScenes);
        }
    }
}
