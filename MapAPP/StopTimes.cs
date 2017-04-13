using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAPP
{
    public class StopTimes
    {
            public int StopID { get; set; }
            public string StopTime { get; set; }
            public int Sequence { get; set; }


        public override string ToString()
        {
            return StopID + StopTime + Sequence;
        }

    }
    }


