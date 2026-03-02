using System;
using System.Collections.Generic;
using Utils;

namespace Data.Database
{
    public partial class Manager : Basic.MySql
    {
        public delegate void FixDelegate();
        private Dictionary<string, FixDelegate> fix = new Dictionary<string, FixDelegate>();
        private Dictionary<string, List<string>> FixPlayer { get; set; } = new Dictionary<string, List<string>>();
        private static Manager instance;
        public static Manager Instance { get { if (instance == null) { instance = new Manager(); } return instance; } }
        
        public override void Init(params object[] args)
        {
            Load<Card>(Config.MySQL.ConnectionString);
            Load<Device>(Config.MySQL.ConnectionString);
            Load<Player>(Config.MySQL.ConnectionString);
        }

        private void OnTurnOff(params object[] args)
        {
            Save<Device>(global::Data.Config.MySQL.ConnectionString);
            Save<Player>(global::Data.Config.MySQL.ConnectionString);
        }
        public void Delete(Basic.Data data)
        {
            Delete(global::Data.Config.MySQL.ConnectionString, data);
        }

        public void ExecuteNonQuery(string connectionString, string query, Dictionary<string, object> parameters)
        {
            using var connection = Connection(connectionString);
            connection.Open();
            using var command = new MySql.Data.MySqlClient.MySqlCommand(query, connection);
            
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }
            
            command.ExecuteNonQuery();
        }
    }
}
