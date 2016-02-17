using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightRail;

public class DataResponseMessage : IMessage
{
    public Guid DataId { get; set; }
    public string String { get; set; }
}
