using System;
using System.Collections.Generic;

namespace Utils
{
    public static class Random
    {
        private static readonly System.Random _random = new System.Random();
        public static System.Random Instance => _random;

        public static int Range(int min, int max) => _random.Next(min, max);

        public static T GetElement<T>(IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List is null or empty.");
            return list[_random.Next(list.Count)];
        }
        public static int GetIndex<T>(IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List is null or empty.");
            return Range(0, list.Count);
        }

        public static T[] Sample<T>(IList<T> list, int count)
        {
            if (list == null || count > list.Count)
                throw new ArgumentException("Invalid count or list.");

            List<T> copy = new List<T>(list);
            List<T> result = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                int index = _random.Next(copy.Count);
                result.Add(copy[index]);
                copy.RemoveAt(index);
            }
            return result.ToArray();
        }

        public static void Shuffle<T>(T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = _random.Next(n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }
        public static T[] WithSum<T>(T sum, int count, T minValue, T maxValue)
        {
            if (typeof(T) != typeof(int))
                throw new NotSupportedException("Only int type is supported currently.");

            int targetSum = Convert.ToInt32(sum);
            int min = Convert.ToInt32(minValue);
            int max = Convert.ToInt32(maxValue);

            if (count <= 0 || max < min)
                throw new ArgumentException("Invalid input parameters.");

            int[] result;
            int currentSum;

            do
            {
                result = new int[count];
                currentSum = 0;
                for (int i = 0; i < count; i++)
                {
                    result[i] = _random.Next(min, max + 1);
                    currentSum += result[i];
                }
            }
            while (currentSum != targetSum);

            Shuffle(result);

            return result.Select(x => (T)Convert.ChangeType(x, typeof(T))).ToArray();
        }

    }
}
