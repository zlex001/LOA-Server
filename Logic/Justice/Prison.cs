using Basic;
using Data;

namespace Logic.Justice;

public class Prison : Basic.Manager
{
    public static Prison Instance { get; private set; } = new();

    public void DoJailBreak(Life criminal)
    {
        if (Agent.HasWitnesses(criminal, out List<Life> witnesses))
        {
            Agent.Witness(criminal, witnesses, Relation.Reason.JailBreak);
            foreach (var witness in witnesses)
            {
                Logic.Talk.Say.Do(witness, global::Data.Text.Labels.WitnessJailBreak, ("criminal", criminal));
            }
        }

        int jailTime = Agent.Sentencing(criminal, 25, 50);
        Agent.Do(criminal, jailTime, global::Data.Life.Crime.PrisonBreak);
    }

    public void DoPrisonBreak(Life criminal, int prisonerJailTime)
    {
        if (Agent.HasWitnesses(criminal, out List<Life> witnesses))
        {
            Agent.Witness(criminal, witnesses, Relation.Reason.PrisonBreak);
            foreach (var witness in witnesses)
            {
                Logic.Talk.Say.Do(witness, global::Data.Text.Labels.WitnessPrisonBreak, ("criminal", criminal));
            }
        }

        double rescueDifficultyModifier = 1.0 + (prisonerJailTime * 0.02);
        int jailTime = Agent.Sentencing(criminal, 20, 40, rescueDifficultyModifier);
        Agent.Do(criminal, jailTime, global::Data.Life.Crime.JailBreak);
    }


}
