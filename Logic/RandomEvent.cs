using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Basic
{
    public class RandomEvent
    {
        public delegate void Delegate( params object[]args);
        public static Random random = new Random();
        public static Delegate ByProbability(Dictionary<Delegate, double> delegatesWithProbability)
        {
            double sum = delegatesWithProbability.Values.Sum();
            double rand = random.NextDouble();
            double accumulate = 0;
            foreach (var kvp in delegatesWithProbability)
            {
                accumulate += kvp.Value;
                if (accumulate >= rand * sum)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        public static Delegate ByWeight(Dictionary<Delegate, double> delegatesWithWeights)
        {
            double totalWeight = delegatesWithWeights.Values.Sum();
            double randomValue = random.NextDouble() * totalWeight;
            double currentWeight = 0f;

            foreach (KeyValuePair<Delegate, double> delegateWithWeight in delegatesWithWeights)
            {
                currentWeight += delegateWithWeight.Value;
                if (currentWeight >= randomValue)
                {
                    return delegateWithWeight.Key;
                }
            }
            return null;
        }

    }
}
