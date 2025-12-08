using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsumerApp.Domain
{
    public record MyPayload(string Id, string Type, string Data);
}
