using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static WinLiveManage.NewLoginfo;

namespace OeipCommon
{
    public static class SettingHelper
    {
        public static void SaveSetting<T>(this T seting, string path) where T : IXmlSerializable, new()
        {
            try
            {
                using (var write = new StreamWriter(path, false, Encoding.UTF8))
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    using (XmlWriter xw = XmlWriter.Create(write, settings))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        serializer.Serialize(xw, seting);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogMessageEx("save xml setting error.", ex);
                LogUtility.Log("¥Ê¥¢Xml Setting¥ÌŒÛ:--- save xml setting error." + ex,LogType.Error);
            }
        }

        public static T ReadSetting<T>(string path) where T : IXmlSerializable, new()
        {
            T setting = new T();
            try
            {
                if (File.Exists(path))
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        setting = (T)serializer.Deserialize(stream);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogMessageEx("read xml setting error.", e);
                LogUtility.Log("∂¡»° Xml Setting Error"+e,LogType.Error);
            }
            return setting;
        }

        public static void ReadElement<T>(this XmlReader reader, string name, ref T t)
        {
            reader.ReadStartElement(name);
            var xmlSerial = new XmlSerializer(typeof(T));
            t = (T)xmlSerial.Deserialize(reader);
            reader.ReadEndElement();
        }

        public static void WriteElement<T>(this XmlWriter writer, string name, T t)
        {
            writer.WriteStartElement(name);
            var xmlSerial = new XmlSerializer(typeof(T));
            xmlSerial.Serialize(writer, t);
            writer.WriteEndElement();
        }
    }
}
