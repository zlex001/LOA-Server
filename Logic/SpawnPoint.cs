using System.Linq;

namespace Logic
{
    public static class SpawnPoint
    {
        public static Map GetRandomInitialMap()
        {
            return Agent.Instance.Content.RandomGet<Map>(m =>
                m.Scene != null &&
                m.Scene.Type == Scene.Types.City
            );
        }
    }
}

