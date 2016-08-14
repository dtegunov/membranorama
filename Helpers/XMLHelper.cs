using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.XPath;
using OpenTK;

namespace Warp.Tools
{
    public static class XMLHelper
    {
        public static void WriteAttribute(XmlTextWriter writer, string name, string value)
        {
            writer.WriteStartAttribute(name);
            writer.WriteValue(value);
            writer.WriteEndAttribute();
        }

        public static void WriteAttribute(XmlTextWriter writer, string name, int value)
        {
            writer.WriteStartAttribute(name);
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndAttribute();
        }

        public static void WriteAttribute(XmlTextWriter writer, string name, float value)
        {
            writer.WriteStartAttribute(name);
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndAttribute();
        }

        public static void WriteAttribute(XmlTextWriter writer, string name, double value)
        {
            writer.WriteStartAttribute(name);
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndAttribute();
        }

        public static void WriteAttribute(XmlTextWriter writer, string name, decimal value)
        {
            writer.WriteStartAttribute(name);
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndAttribute();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, string value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", value);
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, bool value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, int value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, long value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, float value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, double value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, decimal value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, Quaternion value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            string ValueString = "";
            ValueString += value.X.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.Y.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.Z.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.W.ToString(CultureInfo.InvariantCulture);
            XMLHelper.WriteAttribute(writer, "Value", ValueString);
            writer.WriteEndElement();
        }

        public static void WriteParamNode(XmlTextWriter writer, string name, Vector3 value)
        {
            writer.WriteStartElement("Param");
            XMLHelper.WriteAttribute(writer, "Name", name);
            XMLHelper.WriteAttribute(writer, "Value", Vector3ToString(value));
            writer.WriteEndElement();
        }

        public static string LoadParamNode(XPathNavigator nav, string name, string defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            return Value;
        }

        public static bool LoadParamNode(XPathNavigator nav, string name, bool defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    return bool.Parse(Value);
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static int LoadParamNode(XPathNavigator nav, string name, int defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    return int.Parse(Value, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static long LoadParamNode(XPathNavigator nav, string name, long defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    return long.Parse(Value, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static float LoadParamNode(XPathNavigator nav, string name, float defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    return float.Parse(Value, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static double LoadParamNode(XPathNavigator nav, string name, double defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    return double.Parse(Value, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static decimal LoadParamNode(XPathNavigator nav, string name, decimal defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    return decimal.Parse(Value, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static Quaternion LoadParamNode(XPathNavigator nav, string name, Quaternion defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    string[] Parts = Value.Split(new[] { ',' });
                    return new Quaternion(float.Parse(Parts[0], CultureInfo.InvariantCulture),
                                          float.Parse(Parts[1], CultureInfo.InvariantCulture),
                                          float.Parse(Parts[2], CultureInfo.InvariantCulture),
                                          float.Parse(Parts[3], CultureInfo.InvariantCulture));
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static Vector3 LoadParamNode(XPathNavigator nav, string name, Vector3 defaultValue)
        {
            XPathNodeIterator Iterator = nav.Select($"//Param[@Name = \"{name}\"]");
            if (Iterator.Count == 0)
                //throw new Exception();
                return defaultValue;

            Iterator.MoveNext();
            string Value = Iterator.Current.GetAttribute("Value", "");
            if (Value.Length > 0)
                try
                {
                    return Vector3FromString(Value);
                }
                catch (Exception)
                { }

            return defaultValue;
        }

        public static string LoadAttribute(XPathNavigator nav, string name, string defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;
            
            try
            {
                return Value;
            }
            catch (Exception)
            { }

            return defaultValue;
        }

        public static int LoadAttribute(XPathNavigator nav, string name, int defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return int.Parse(Value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            { }

            return defaultValue;
        }

        public static long LoadAttribute(XPathNavigator nav, string name, long defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return long.Parse(Value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            { }

            return defaultValue;
        }

        public static float LoadAttribute(XPathNavigator nav, string name, float defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return float.Parse(Value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            { }

            return defaultValue;
        }

        public static double LoadAttribute(XPathNavigator nav, string name, double defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return double.Parse(Value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            { }

            return defaultValue;
        }

        public static bool LoadAttribute(XPathNavigator nav, string name, bool defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return bool.Parse(Value);
            }
            catch (Exception)
            { }

            return defaultValue;
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

        public static Vector3 LoadAttribute(XPathNavigator nav, string name, Vector3 defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return Vector3FromString(Value);
            }
            catch (Exception)
            { }

            return defaultValue;
        }

        public static decimal LoadAttribute(XPathNavigator nav, string name, decimal defaultValue)
        {
            string Value = nav.GetAttribute(name, "");
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            try
            {
                return decimal.Parse(Value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            { }

            return defaultValue;
        }

        public static string Vector3ToString(Vector3 value)
        {
            string ValueString = "";
            ValueString += value.X.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.Y.ToString(CultureInfo.InvariantCulture) + ",";
            ValueString += value.Z.ToString(CultureInfo.InvariantCulture);

            return ValueString;
        }

        public static Vector3 Vector3FromString(string value)
        {
            string[] Parts = value.Split(new[] { ',' });
            return new Vector3(float.Parse(Parts[0], CultureInfo.InvariantCulture),
                               float.Parse(Parts[1], CultureInfo.InvariantCulture),
                               float.Parse(Parts[2], CultureInfo.InvariantCulture));
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
    }
}
