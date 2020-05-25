using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Octobroker.Octo_Events
{
    public interface IOctoprintEvent
    {
        //public OctoprintEvent(String name, JToken payload)
        //{
        //    this.Name = name;
        //    this.Payload = payload;
        //}

         string Name { get; }
         JObject Payload { get;  set; }
         JObject GetGenericPayload();
      
    }
}
