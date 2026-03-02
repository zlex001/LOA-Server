using Data;

namespace Logic.Justice;

public class Agent : Basic.Manager
{
    public static bool IsPublic(Map map)
    {
        if (map == null) return false;
        if (map.Type == Map.Types.Room) return false;
        if (map.Type == Map.Types.Restaurant) return false;
        if (map.Type == Map.Types.HeavyGearShop) return false;
        if (map.Type == Map.Types.LightGearShop) return false;
        if (map.Type == Map.Types.MagicShop) return false;
        if (map.Type == Map.Types.PotionShop) return false;
        return true;
    }
    public static bool IsCitizen(Life life)
    {
        if (life == null)
            return false;
        if (life is Player)
            return true;
        if (life.Birthplace.Scene.Content.Has<Map>(m => m.Type == Map.Types.PoliceStation))
            return true;
        return false;
    }
    public static void Do(Life life, int jailTime, global::Data.Life.Crime crime)
    {
        if (life.Content.Has<Punishment>())
        {
            Reoffend(life.Content.Get<Punishment>(), jailTime, crime);
        }
        else
        {
            life.Create<Punishment>(jailTime, crime);
        }
    }

    private static void Reoffend(Punishment punishment, int newJailTime, global::Data.Life.Crime crimeType)
    {
        punishment.Jail += newJailTime + (int)(newJailTime * 0.5);
        punishment.Crimes.Add(crimeType);
    }
    public static bool HasWitnesses(Life criminal, out List<Life> witnesses)
    {
        witnesses = new List<Life>();

        if (criminal?.Map != null)
        {
            foreach (var life in criminal.Map.Content.Gets<Life>())
            {
                if (IsValidWitness(life, criminal))
                {
                    witnesses.Add(life);
                }
            }
        }

        return witnesses.Count > 0;
    }
    public static bool IsValidWitness(Life potential, Life criminal)
    {
        if (potential == criminal)
            return false;

        if (potential.State.Is(Life.States.Unconscious))
            return false;

        if (potential.Birthplace != null && criminal.Birthplace != null && potential.Birthplace == criminal.Birthplace)
            return false;

        if (potential.Leader == criminal || criminal.Leader == potential)
            return false;

        return true;
    }
    public static int Sentencing(Life criminal, int minJailTime, int maxJailTime, double modifier = 1.0)
    {
        int baseJailTime = Utils.Random.Instance.Next(minJailTime, maxJailTime + 1);
        baseJailTime = (int)(baseJailTime * modifier);

        double levelMultiplier = 1.0 + (criminal.Level * 0.01);
        int finalJailTime = (int)(baseJailTime * levelMultiplier);
        int randomAdjustment = Utils.Random.Instance.Next(-2, 3);

        return Math.Max(1, finalJailTime + randomAdjustment);
    }
    public static void Witness(Life criminal, List<Life> witnesses, Relation.Reason reason)
    {
        foreach (var witness in witnesses)
        {
            Relation.Do(witness, criminal, reason);
            Logic.Talk.Say.Do(witness, global::Data.Text.Labels.WitnessAssault, ("criminal", criminal));
        }
    }
    public static Ability Owner(Item item)
    {
        if (item == null) return null;
        if (item.Parent is Part part) return part.Parent as Life;
        if (item.Parent is Item container) return Owner(container);
        if (item.Parent is Map map) return map;
        return null;
    }

}