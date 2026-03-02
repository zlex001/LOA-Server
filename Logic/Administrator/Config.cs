namespace Logic.Administrator
{
    public class Config
    {
        private static Config instance;
        public static Config Instance { get { if (instance == null) { instance = new Config(); } return instance; } }
    }
}

