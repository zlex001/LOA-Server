using Basic;
using Data;

namespace Logic.Justice;

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
                Logic.Talk.Say.Do(witness, global::Data.Text.Labels.WitnessAssaultOfficer, ("criminal", criminal));
            }
        }

        int jailTime = Agent.Sentencing(criminal, 10, 20);
        Agent.Do(criminal, jailTime, global::Data.Life.Crime.AssaultOfficer);
    }

}
