using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVDiff
{
    internal static class Helpers
    {
        public static System.Windows.Media.Color GetNewRandomColor(int index, double saturation = 1, double value = 1)
        {
            const double GoldenRatioConjugate = 0.618033988749895;
            const double HueMaximum = 360.0;

            // http://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
            double hue = /*24.89 +*/ GoldenRatioConjugate * HueMaximum * index;
            hue %= HueMaximum;

            return ColorFromHSV(hue, saturation, value);
        }

        public static System.Windows.Media.Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => System.Windows.Media.Color.FromArgb(255, v, t, p),
                1 => System.Windows.Media.Color.FromArgb(255, q, v, p),
                2 => System.Windows.Media.Color.FromArgb(255, p, v, t),
                3 => System.Windows.Media.Color.FromArgb(255, p, q, v),
                4 => System.Windows.Media.Color.FromArgb(255, t, p, v),
                _ => System.Windows.Media.Color.FromArgb(255, v, p, q),
            };
        }
    }
}
