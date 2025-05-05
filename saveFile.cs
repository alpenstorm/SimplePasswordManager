using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplePasswordManager
{
    internal class SaveFile
    {
        public Config config {  get; set; }
        public string version { get; set; }
    }

    internal class Config
    {
        // these are loaded into memory in
        // init.cs, starting from line 94

        public bool isFirstLoad { get; set; }
        public bool debugMode { get; set; }
        public int timeout { get; set; }
    }
}
