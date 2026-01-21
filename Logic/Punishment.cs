using Basic;
using System.Collections.Generic;

namespace Logic;

public class Punishment : Basic.Element
{
    public int Jail { get; set; }
    public int Fine => (int)Math.Pow(Jail, 2);
    public HashSet<Life.Crime> Crimes { get; set; } = new HashSet<Life.Crime>();

    public override void Init(params object[] args)
    {
        Jail = (int)args[0];
        Crimes.Add((Life.Crime)args[1]);
    }

}
