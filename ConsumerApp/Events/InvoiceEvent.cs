using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureEventHub.Config;

namespace ConsumerApp.Events
{
    public class InvoiceEvent : EventBase
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Data { get; set; }
    }
}
