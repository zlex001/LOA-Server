using Basic;
using Logic;

namespace Domain.Justice;

public class Murder : Basic.Manager
{
    public static Murder Instance { get; private set; } = new();

    public void Do(Life criminal, Life victim)
    {
        if (Agent.HasWitnesses(criminal, out List<Life> witnesses))
        {
            Agent.Witness(criminal, witnesses, Relation.Reason.Murder);
            foreach (var witness in witnesses)
            {
                Domain.Talk.Say.Do(witness, Logic.Text.Labels.WitnessMurder, ("criminal", criminal));
            }
        }

        int jailTime = Agent.Sentencing(criminal, 30, 60);
        Agent.Do(criminal, jailTime, Logic.Life.Crime.Murder);
    }

    public bool IsMurder(Life killer, Life victim)
    {
        if (killer == null || victim == null)
            return false;

        if (killer == victim)
            return false;

        if (victim.State.Is(Life.States.Unconscious))
            return false;

        if (killer.Birthplace != null && victim.Birthplace != null && killer.Birthplace == victim.Birthplace)
            return false;

        if (killer.Leader == victim || victim.Leader == killer)
            return false;

        return true;
    }

}
