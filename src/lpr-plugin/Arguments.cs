using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lpr_plugin
{
    internal class Arguments
    {
        private string ExecutablePath { get; set; }
        private string FilePath { get; set; }
        private double ConfidenceThreshold { get; set; }
    }
}