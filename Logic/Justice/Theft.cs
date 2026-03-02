using Basic;
using Data;
using System.Linq;

namespace Logic.Justice;

public static class Theft
{
    public static bool Judgment(Life sub, Item obj)
    {
        if (sub == null) return false;
        if (obj == null) return false;
        if (Agent.Owner(obj) is not Map map) return false;
        if (Agent.IsPublic(map)) return false;
        if (sub.Birthplace == null) return true;
        return sub.Birthplace.Scene != map.Scene;
    }



    public static void Sentence(Life criminal, Item stolenItem = null)
    {
        if (Agent.HasWitnesses(criminal, out List<Life> witnesses))
        {
            Agent.Witness(criminal, witnesses, Relation.Reason.Theft);
            foreach (var witness in witnesses)
            {
                Logic.Talk.Say.Do(witness, global::Data.Text.Labels.WitnessTheft, ("criminal", criminal));
            }
        }
        double modifier = stolenItem != null ? 1.0 + (stolenItem.Price / 1000.0) : 1.0;
        Agent.Do(criminal, Agent.Sentencing(criminal, 3, 10, modifier), global::Data.Life.Crime.Theft);
    }
}
