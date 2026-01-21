using Basic;
using Logic;

namespace Domain.Justice;

public class AssaultOfficer : Basic.Manager
{
    public static AssaultOfficer Instance { get; private set; } = new();

    public void Do(Life criminal, Life officer)
    {
        if (Agent.HasWitnesses(criminal, out List<Life> witnesses))
        {
            Agent.Witness(criminal, witnesses, Relation.Reason.AssaultOfficer);
            foreach (var witness in witnesses)
            {
                Domain.Talk.Say.Do(witness, Logic.Text.Labels.WitnessAssaultOfficer, ("criminal", criminal));
            }
        }

        int jailTime = Agent.Sentencing(criminal, 10, 20);
        Agent.Do(criminal, jailTime, Logic.Life.Crime.AssaultOfficer);
    }

}
