
namespace Logic.Config
{
    public static class MySQL
    {
        public static string Server => "localhost";
        public static int Port => 3306;
        public static string UserId => "root";
        public static string Password => "191316";
        public static int MaxPoolSize => 100;
        public static int MinPoolSize => 0;
        public static int ConnectionTimeout => 30;

        public static string GetConnectionString(string database)
        {
            return $"Server={Server};Port={Port};Database={database};Uid={UserId};Pwd={Password};" +
                   $"Pooling=true;MinimumPoolSize={MinPoolSize};MaximumPoolSize={MaxPoolSize};ConnectionTimeout={ConnectionTimeout};" +
                   $"SslMode=None;AllowPublicKeyRetrieval=True;";
        }

        public static string ConnectionString
        {
            get
            {
                try
                {
                    var instance = Logic.Agent.Instance;
                    if (instance != null && !string.IsNullOrEmpty(instance.ServerId))
                    {
                        var server = Logic.Database.Agent.Instance.GetServerById(instance.ServerId);
                        if (server != null)
                        {
                            return GetConnectionString(server.Id);
                        }
                        else
                        {
                            Utils.Debug.Log.Warning("CONFIG", $"Server '{instance.ServerId}' not found, using default database 'mud'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Error("CONFIG", $"Error getting server config: {ex.Message}, using default database 'mud'");
                }
                
                return GetConnectionString("mud");
            }
        }
    }
}
