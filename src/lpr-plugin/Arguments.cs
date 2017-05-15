using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lpr_plugin
{
    public class Arguments
    {
        public string ExecutablePath { get; set; }
        public string FilePath { get; set; }
        public double ConfidenceThreshold { get; set; }
    }
}