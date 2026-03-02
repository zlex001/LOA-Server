using Newtonsoft.Json;

namespace Data.Database
{
    public class Server : Basic.MySql.Data
    {
        public int id;
        public int name;
        public string ip;
        public int port;

        public override Dictionary<string, object> ToDictionary => new()
        {
            ["Id"] = id,
            ["Name"] = name,
            ["Ip"] = ip,
            ["Port"] = port,
        };

        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            Id = Get<string>(dict, "id");
            name = Get<int>(dict, "name");
            ip = Get<string>(dict, "ip");
            port = Get<int>(dict, "port");
        }

        public Server() { }

        public Server(string id, int name, string ip, int port)
        {
            Id = id;
            this.name = name;
            this.ip = ip;
            this.port = port;
        }
    }
}



