using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using OnixData.Extensions;
using OnixData.Legacy;

namespace OnixData
{
    public class OnixLegacyPlusEnumerator : IDisposable, IEnumerator
    {
        private OnixLegacyPlusParser OnixParser = null;
        private readonly SerializerManager _xmlSerializerManager;
        private XmlReader OnixReader = null;

        private string ProductXmlTag = null;
        private OnixLegacyHeader OnixHeader = null;
        private OnixLegacyProduct CurrentRecord = null;
        private XmlSerializer ProductSerializer = null;

        public OnixLegacyPlusEnumerator(OnixLegacyPlusParser ProvidedParser, FileInfo LegacyOnixFilepath,
            SerializerManager xmlSerializerManager)
        {
            this.ProductXmlTag = ProvidedParser.ReferenceVersion ? "Product" : "product";

            this.OnixParser = ProvidedParser;
            _xmlSerializerManager = xmlSerializerManager;
            this.OnixReader = OnixLegacyPlusParser.CreateXmlReader(LegacyOnixFilepath, false, ProvidedParser.PerformValidation);

            this.OnixReader.MoveToContent();

            ProductSerializer = _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixLegacyProduct), new XmlRootAttribute(this.ProductXmlTag));
        }

        public void Dispose()
        {
            if (this.OnixReader != null)
            {
                this.OnixReader.Close();
                this.OnixReader = null;
            }
        }

        public bool MoveNext()
        {
            bool bResult = false;
            string sProductBody = null;

            if (this.OnixHeader == null)
                this.OnixHeader = OnixParser.MessageHeader;

            do
            {
                if ((this.OnixReader.NodeType == XmlNodeType.Element) && (this.OnixReader.Name == this.ProductXmlTag))
                {
                    // XElement product = XElement.ReadFrom(reader) as XElement;

                    sProductBody = this.OnixReader.ReadOuterXml();
                    break;
                }

            } while (this.OnixReader.Read());

            if (!String.IsNullOrEmpty(sProductBody))
            {
                try
                {
                    CurrentRecord =
                        this.ProductSerializer.Deserialize(new StringReader(sProductBody)) as OnixLegacyProduct;

                    if ((CurrentRecord != null) && OnixParser.ShouldApplyDefaults)
                        CurrentRecord.ApplyHeaderDefaults(this.OnixHeader);

                    bResult = true;
                }
                catch (Exception ex)
                {
                    CurrentRecord = new OnixLegacyProduct();

                    CurrentRecord.SetParsingError(ex);
                    CurrentRecord.SetInputXml(sProductBody);
                }
            }

            return bResult;
        }

        public void Reset()
        {
            return;
        }

        public object Current
        {
            get
            {
                return CurrentRecord;
            }
        }
    }
}
