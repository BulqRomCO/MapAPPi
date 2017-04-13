using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAPP
{
    public class Trips
    {
        public int RouteID { get; set; }
        public string ServiceID { get; set; }
        public string TripID { get; set; }

        public override string ToString()
        {
            return RouteID + ServiceID + TripID;
        }

    }
}

    

