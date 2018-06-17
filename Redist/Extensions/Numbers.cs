using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public static class Calculatios
    {
        public static bool Within (this double number, double max, double min)
        {
            if (number > max)
                return false;
            if (number < min)
                return false;

            return true;
        }

        public static bool NotWithin(this double number, double max, double min)
        {
            return !number.Within(max, min);
        }
    }
}
