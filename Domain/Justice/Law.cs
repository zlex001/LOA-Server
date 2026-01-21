using Basic;
using Logic;

namespace Domain.Justice;

public class Law : Basic.Manager
{
    public static Law Instance { get; private set; } = new();



}
