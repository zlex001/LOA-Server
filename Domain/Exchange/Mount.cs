using Logic;
using Org.BouncyCastle.Asn1.X509;

namespace Domain.Exchange
{
    public static class Mount
    {
        private static bool Can(Item sub, Item obj)
        {
            if (sub == null) return false;
            if (obj == null) return false;
            foreach (var tag in obj.Config.Tags)
            {
                if (sub.Container.TryGetValue(tag, out int max))
                {
                    if (max > sub.Content.Count<Item>(e => e.Config.Tags.Contains(tag)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
