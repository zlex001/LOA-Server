using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Basic.Tool
{
    /// <summary>
    /// 提供与集合操作相关的通用工具方法。
    /// </summary>
    public static class Collection
    {
        /// <summary>
        /// 随机打乱集合中的元素顺序，并返回一个新的集合。
        /// </summary>
        /// <typeparam name="T">集合中元素的类型。</typeparam>
        /// <param name="source">需要打乱的原始集合。</param>
        /// <returns>一个包含相同元素但顺序被打乱的新集合。</returns>
        /// <remarks>
        /// 此方法使用了 Fisher-Yates 洗牌算法，具有高效性和随机性。
        /// </remarks>
        public static List<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var list = source.ToList();
            Random random = new Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }
    }
}
