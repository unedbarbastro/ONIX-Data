using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using OnixData.Extensions;
using OnixData.Version3;
using OnixData.Version3.Header;

namespace OnixData
{
    public class OnixEnumerator : IDisposable, IEnumerator
    {
        private OnixParser OnixParser = null;
        private readonly SerializerManager _xmlSerializerManager;
        private XmlReader OnixReader = null;

        private int CurrentIndex = -1;
        private string ProductXmlTag = null;
        private XmlDocument OnixDoc = null;
        private XmlNodeList ProductList = null;
        private OnixHeader OnixHeader = null;
        private OnixProduct CurrentRecord = null;

        #region Properties 

        public List<OnixTextContent> CurrentCommList { get; set; }

        #endregion 

        public OnixEnumerator(OnixParser ProvidedParser, FileInfo OnixFilepath, SerializerManager xmlSerializerManager)
        {
            this.ProductXmlTag = ProvidedParser.ReferenceVersion ? "Product" : "product";

            this.OnixParser = ProvidedParser;
            this.OnixReader = OnixParser.CreateXmlReader(OnixFilepath, false);

            _xmlSerializerManager = xmlSerializerManager;
            _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixProduct), new XmlRootAttribute(this.ProductXmlTag));

            CurrentCommList = new List<OnixTextContent>();
        }

        public OnixEnumerator(OnixParser ProvidedParser, StringBuilder OnixContent, SerializerManager xmlSerializerManager)
        {
            this.ProductXmlTag = ProvidedParser.ReferenceVersion ? "Product" : "product";

            this.OnixParser = ProvidedParser;
            this.OnixReader = OnixParser.CreateXmlReader(OnixContent, false);

            _xmlSerializerManager = xmlSerializerManager;
            _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixProduct), new XmlRootAttribute(this.ProductXmlTag));

            CurrentCommList = new List<OnixTextContent>();
        }

        public void Dispose()
        {
            if (this.OnixReader is not null)
            {
                this.OnixReader.Close();
                this.OnixReader.Dispose();
            }
        }

        public bool MoveNext()
        {
            bool bResult = true;

            if (OnixDoc is null)
            {
                this.OnixDoc = new XmlDocument();
                this.OnixDoc.Load(this.OnixReader);

                this.ProductList = this.OnixDoc.GetElementsByTagName(this.ProductXmlTag);
            }

            if (this.OnixHeader is null)
                this.OnixHeader = OnixParser.MessageHeader;

            if (++CurrentIndex < this.ProductList.Count)
            {
                string sInputXml = this.ProductList[CurrentIndex].OuterXml;

                try
                {
                    using (var stringReader = new StringReader(sInputXml))
                    {
                        var productSerializer = _xmlSerializerManager.GetXmlSerializer(nameof(OnixProduct), this.ProductXmlTag);
                        CurrentRecord = productSerializer.Deserialize(stringReader) as OnixProduct;

                        if ((CurrentRecord is not null) && OnixParser.ShouldApplyDefaults)
                            CurrentRecord.ApplyHeaderDefaults(this.OnixHeader);

                        CurrentCommList.Clear();

                        if ((CurrentRecord is not null) &&
                            (CurrentRecord.CollateralDetail is not null) &&
                            (CurrentRecord.CollateralDetail.OnixTextContentList is not null) &&
                            (CurrentRecord.CollateralDetail.OnixTextContentList.Length > 0))
                        {
                            CurrentCommList.AddRange(CurrentRecord.CollateralDetail.OnixTextContentList);
                        }
                        CurrentRecord.RawXmlNode = sInputXml;
                    }
                }
                catch (Exception ex)
                {
                    CurrentRecord = new OnixProduct();

                    CurrentRecord.SetParsingError(ex);
                    CurrentRecord.SetInputXml(sInputXml);
                }
            }
            else
                bResult = false;

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
