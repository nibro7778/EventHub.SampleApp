using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsumerApp.Application.Abstractions
{
    public interface IDlqService
    {
        Task SendToDlqAsync(string rawBody, IDictionary<string, object> metadata, Exception ex = null);
    }
}
