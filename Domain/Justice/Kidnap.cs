using Basic;
using Logic;

namespace Domain.Justice;

public class Kidnap : Basic.Manager
{
    public static Kidnap Instance { get; private set; } = new();

    public void Do(Life criminal, Life victim)
    {
        if (Agent.HasWitnesses(criminal, out List<Life> witnesses))
        {
            Agent.Witness(criminal, witnesses, Relation.Reason.Kidnap);
            foreach (var witness in witnesses)
            {
                Domain.Talk.Say.Do(witness, Logic.Text.Labels.WitnessKidnap, ("criminal", criminal));
            }
        }

        int jailTime = Agent.Sentencing(criminal, 40, 80);
        Agent.Do(criminal, jailTime, Logic.Life.Crime.Kidnap);
    }

    public bool IsKidnap(Life kidnapper, Life victim)
    {
        if (kidnapper == null || victim == null)
            return false;

        if (kidnapper == victim)
            return false;

        if (victim.State.Is(Life.States.Unconscious))
            return false;

        if (kidnapper.Birthplace != null && victim.Birthplace != null && kidnapper.Birthplace == victim.Birthplace)
            return false;

        if (kidnapper.Leader == victim || victim.Leader == kidnapper)
            return false;

        return true;
    }

}
