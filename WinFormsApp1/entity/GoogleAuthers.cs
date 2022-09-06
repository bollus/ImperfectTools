using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperfectTools.entity
{
    internal class GoogleAuthers
    {
        public List<GoogleAuther>? Auths { get; set; }
    }

    internal class GoogleAuther
    {
        public string? Name { get; set; }
        public string? Account { get; set; }
        public string? Secret { get; set; }
    }
}
