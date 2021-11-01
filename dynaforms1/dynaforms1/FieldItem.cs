using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dynaforms1
{
    public class FieldItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public Control Control { get; set; }
    }
}
