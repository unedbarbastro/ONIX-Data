using System;
using System.Collections;
using System.Xml.Serialization;

namespace OnixData
{
    public class SerializerManager
    {
        public Hashtable Serializers { get; set; }

        public SerializerManager()
        {
            Serializers = new Hashtable();
        }

        public XmlSerializer RegisterXmlSerializer(Type type, XmlRootAttribute xmlRoot)
        {
            string key = GenerateKey(type.Name, xmlRoot.ElementName);
            if (Serializers.Contains(key))
            {
                return (XmlSerializer)Serializers[key];
            }

            var xmlSerializer = new XmlSerializer(type, xmlRoot);
            Serializers[key] = xmlSerializer;

            return xmlSerializer;
        }

        public static string GenerateKey(string typeName, string xmlTag)
        {
            return $"{typeName}_{xmlTag}";
        }

        public XmlSerializer GetXmlSerializer(string typeName, string xmlTag)
        {
            string key = GenerateKey(typeName, xmlTag);

            var serializer = Serializers[key];
            if (serializer == null)
            {
                //var xmlSerializer = new XmlSerializer(type, xmlTag);

                //// Cache the serializer.  
                // Serializers[key] = xmlSerializer;
                // return xmlSerializer;

                throw new Exception($"missing xml serialzer for type=[{nameof(typeName)}], xmlTag=[{xmlTag}]");
            }
            return (XmlSerializer)serializer;
        }
    }
}
