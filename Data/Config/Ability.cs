using System.Collections.Generic;

namespace Data.Config
{

    public interface IName
    {
        int Name { get; }
    }
    public interface IDescription
    {
        int Description { get; }
    }
    public interface ICharacter : IName, IDescription
    {

    }

    public class Ability : Basic.Data
    {
        public int Id { get; set; }

        public Dictionary<string, List<string>> text;
    }
}


