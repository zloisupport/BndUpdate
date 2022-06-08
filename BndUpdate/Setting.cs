using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace BndUpdate
{
   public class Setting
    {
        public string Title { get; set; }
        public string Version { get; set; }
        public HashSum Hashsum  { get; set; }
        public string CreatedDate  { get; set; }
    }

    public class HashSum
    {
        //Dictionary<string,string> Hash { get; set; }
          public List<string> name { get; set; }
          public List<string> hash { get; set; }
    }
}
