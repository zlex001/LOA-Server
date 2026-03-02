using Basic;
using Data;

namespace Logic.Justice;

public class Assault : Basic.Manager
{
    public static bool Check(Life sub, Life obj)
    {
        if (sub == null)
            return false;
        if (obj == null)
            return false;
        if (!Agent.IsCitizen(obj))
            return false;
        // 攻击受司法保护的种族才算犯罪：Lemurian, Atlantean, Beastman
        if (!IsProtectedCategory(obj.Category))
            return false;
        if (sub is not Player && sub.Config.Tags.Contains("Police"))
            return false;
        if (sub.Birthplace?.Scene == obj.Birthplace?.Scene)
            return false;
        return true;
    }

    private static bool IsProtectedCategory(global::Data.Life.Categories category)
    {
        return category == global::Data.Life.Categories.Lemurian ||
               category == global::Data.Life.Categories.Atlantean ||
               category == global::Data.Life.Categories.Beastman;
    }
    public static void Judge(Life sub, Life obj)
    {
        if (Agent.HasWitnesses(sub, out List<Life> witnesses))
        {
            Agent.Witness(sub, witnesses, Relation.Reason.Assault);
        }
        Agent.Do(sub, Agent.Sentencing(sub, 5, 15), global::Data.Life.Crime.Assault);
    }


}
