using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAPP
{
    public class BussStops
    {
        public int StopID { get; set; }
        public string StopName { get; set; }
        public double LonTitude { get; set; }
        public double Latitude { get; set; }

        
        public override string ToString()
        {
            return "ID " + StopID + " Name " + StopName + " Lon: " + LonTitude + " Lat: " + Latitude; 
        }
    }
}
