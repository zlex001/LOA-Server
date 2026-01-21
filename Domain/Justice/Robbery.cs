using Logic;

namespace Domain.Justice;

public static class Robbery
{
    public static bool Judgment(Life taker, Life owner, Item item)
    {
        if (taker == null || owner == null || item == null)
            return false;

        if (taker == owner)
            return false;

        if (owner.Leader == taker || taker.Leader == owner)
            return false;

        if (owner.Birthplace != null && taker.Birthplace != null && owner.Birthplace == taker.Birthplace)
            return false;

        return true;
    }

    public static void Sentence(Life criminal, Item stolenItem, bool hasWitnesses)
    {
        Relation.Reason reason = hasWitnesses ? Relation.Reason.Robbery : Relation.Reason.Theft;
        
        if (Agent.HasWitnesses(criminal, out List<Life> witnesses))
        {
            Agent.Witness(criminal, witnesses, reason);
            foreach (var witness in witnesses)
            {
                Logic.Text.Labels label = hasWitnesses ? Logic.Text.Labels.WitnessRobbery : Logic.Text.Labels.WitnessTheft;
                Domain.Talk.Say.Do(witness, label, ("criminal", criminal));
            }
        }

        double modifier = stolenItem != null ? 1.0 + (stolenItem.Price / 1000.0) : 1.0;
        Logic.Life.Crime crime = hasWitnesses ? Logic.Life.Crime.Robbery : Logic.Life.Crime.Theft;
        int minJail = hasWitnesses ? 10 : 3;
        int maxJail = hasWitnesses ? 20 : 10;
        
        Agent.Do(criminal, Agent.Sentencing(criminal, minJail, maxJail, modifier), crime);
    }

}

