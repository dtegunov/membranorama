using OpenTK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using Warp.Tools;

namespace Membranogram.Helpers
{
    public static class OpenGLHelper
    {
        public static float[] ToFloatArray(OpenTK.Matrix4 m)
        {
            return new float[]{ m.M11, m.M12, m.M13, m.M14,
                                m.M21, m.M22, m.M23, m.M24,
                                m.M31, m.M32, m.M33, m.M34,
                                m.M41, m.M42, m.M43, m.M44 };
        }
        public static float[] ToFloatArray(OpenTK.Matrix3 m)
        {
            return new float[]{ m.M11, m.M12, m.M13,
                                m.M21, m.M22, m.M23,
                                m.M31, m.M32, m.M33 };
        }

        public static Vector2 Reciprocal(Vector2 v)
        {
            return new Vector2(1f / v.X, 1f / v.Y);
        }

        public static Vector3 Reciprocal(Vector3 v)
        {
            return new Vector3(1f / v.X, 1f / v.Y, 1f / v.Z);
        }

        public static Vector4 Reciprocal(Vector4 v)
        {
            return new Vector4(1f / v.X, 1f / v.Y, 1f / v.Z, 1f / v.W);
        }

        public static Vector2 Reciprocal(int2 v)
        {
            return new Vector2(1f / v.X, 1f / v.Y);
        }

        public static Vector3 Reciprocal(int3 v)
        {
            return new Vector3(1f / v.X, 1f / v.Y, 1f / v.Z);
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
    }
}
