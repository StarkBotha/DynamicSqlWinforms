using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dynaforms1
{
    public class ListedScript
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string ConnString { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
