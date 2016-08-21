using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.XPath;

namespace Membranogram.Helpers
{
    public static class ColorHelper
    {
        public static Color SpectrumColor(int w, float alpha)
        {
            float r = 0.0f;
            float g = 0.0f;
            float b = 0.0f;

            w += 2; // Don't start with pink!
            w *= 15;
            w = w % 100;

            if (w < 17)
            {
                r = -(w - 17.0f) / 17.0f;
                b = 1.0f;
            }
            else if (w < 33)
            {
                g = (w - 17.0f) / (33.0f - 17.0f);
                b = 1.0f;
            }
            else if (w < 50)
            {
                g = 1.0f;
                b = -(w - 50.0f) / (50.0f - 33.0f);
            }
            else if (w < 67)
            {
                r = (w - 50.0f) / (67.0f - 50.0f);
                g = 1.0f;
            }
            else if (w < 83)
            {
                r = 1.0f;
                g = -(w - 83.0f) / (83.0f - 67.0f);
            }
            else
            {
                r = 1.0f;
                b = (w - 83.0f) / (100.0f - 83.0f);
            }

            return Color.FromScRgb(alpha, r, g, b);
        }

        public static Vector4 ColorToVector(Color color, bool ignoreAlpha = false)
        {
            return new Vector4(color.ScR, color.ScG, color.ScB, ignoreAlpha ? 1.0f : color.ScA);
        }
        public static string ColorToString(Color value)
        {
            string ValueString = "";
            ValueString += value.A.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.R.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.G.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.B.ToString(CultureInfo.InvariantCulture);

            return ValueString;
        }

        public static Color ColorFromString(string value)
        {
            string[] Parts = value.Split(new[] { ',' });
            return Color.FromArgb(byte.Parse(Parts[0], CultureInfo.InvariantCulture),
                                  byte.Parse(Parts[1], CultureInfo.InvariantCulture),
                                  byte.Parse(Parts[2], CultureInfo.InvariantCulture),
                                  byte.Parse(Parts[3], CultureInfo.InvariantCulture));
        }

        public static Color LoadAttribute(XPathNavigator nav, string name, Color defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return ColorFromString(Value);
            }
            catch (Exception)
            { }

            return defaultValue;
        }
    }
}
