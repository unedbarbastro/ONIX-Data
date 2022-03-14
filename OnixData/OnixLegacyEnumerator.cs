using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using OnixData.Extensions;
using OnixData.Legacy;

namespace OnixData
{
    /// <summary>
    ///
    /// This class can be useful in the case that one wants to iterate through an ONIX file, even if it has a bad record due to:
    /// 
    /// a.) incorrect XML syntax
    /// b.) invalid text within a XML document (like certain hexadecimal Unicode)
    /// d.) improper tag placement
    /// d.) invalid data types
    /// 
    /// In that way, the user of the class can investigate each record on a case-by-case basis, and the file can be processed
    /// without a sole record preventing the rest of the file from being handled.
    /// 
    /// NOTE: It is still recommended that the files be validated through an alternate process before using this class.
    /// 
    /// </summary> 
    public class OnixLegacyEnumerator : IDisposable, IEnumerator
    {
        private OnixLegacyParser OnixParser = null;
        private readonly SerializerManager _xmlSerializerManager;
        private XmlTextReader OnixReader = null;

        private int CurrentIndex = -1;
        private string ProductXmlTag = null;
        private List<string> OtherTextList = new List<string>();
        private XmlDocument OnixDoc = null;
        private XmlNodeList ProductList = null;
        private OnixLegacyHeader OnixHeader = null;
        private OnixLegacyProduct CurrentRecord = null;
        private XmlSerializer ProductSerializer = null;

        public OnixLegacyEnumerator(OnixLegacyParser ProvidedParser, FileInfo LegacyOnixFilepath, SerializerManager xmlSerializerManager)
        {
            this.ProductXmlTag = ProvidedParser.ReferenceVersion ? "Product" : "product";

            this.OnixParser = ProvidedParser;
            _xmlSerializerManager = xmlSerializerManager;

            // this.OnixReader = OnixLegacyParser.CreateXmlReader(LegacyOnixFilepath, false, ProvidedParser.PerformValidation);
            this.OnixReader = OnixLegacyParser.CreateXmlTextReader(LegacyOnixFilepath);

            ProductSerializer = _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixLegacyProduct), new XmlRootAttribute(this.ProductXmlTag));

            ProductSerializer.UnknownElement += (s, e) =>
            {
                if (((e.Element.LocalName == "Text") || (e.Element.LocalName == "d104")) &&
                    e.ObjectBeingDeserialized is OnixLegacyOtherText)
                {
                    OtherTextList.Add(e.Element.InnerText);
                }
            };
        }

        public OnixLegacyEnumerator(OnixLegacyParser ProvidedParser, StringBuilder LegacyOnixContent, SerializerManager xmlSerializerManager)
        {
            this.ProductXmlTag = ProvidedParser.ReferenceVersion ? "Product" : "product";

            this.OnixParser = ProvidedParser;
            _xmlSerializerManager = xmlSerializerManager;

            // this.OnixReader = OnixLegacyParser.CreateXmlReader(LegacyOnixContent, false, ProvidedParser.PerformValidation);
            this.OnixReader = OnixLegacyParser.CreateXmlTextReader(LegacyOnixContent);
            ProductSerializer = _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixLegacyProduct), new XmlRootAttribute(this.ProductXmlTag));
        }

        public void Dispose()
        {
            if (this.OnixReader is not null)
            {
                this.OnixReader.Close();
                this.OnixReader = null;
            }
        }

        public bool MoveNext()
        {
            bool bResult = true;

            if (this.OnixDoc is null)
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
                    CurrentRecord =
                        this.ProductSerializer.Deserialize(new StringReader(sInputXml)) as OnixLegacyProduct;

                    if ((CurrentRecord is not null) && OnixParser.AlwaysReturnInputXml)
                        CurrentRecord.SetInputXml(sInputXml);

                    if ((CurrentRecord is not null) && OnixParser.ShouldApplyDefaults)
                        CurrentRecord.ApplyHeaderDefaults(this.OnixHeader);

                    if ((CurrentRecord is not null) &&
                        (CurrentRecord.OnixOtherTextList is not null) &&
                        (CurrentRecord.OnixOtherTextList.Length > 0))
                    {
                        int nOTCount = CurrentRecord.OnixOtherTextList.Length;

                        for (int nIdx = 0; nIdx < OtherTextList.Count; ++nIdx)
                        {
                            if (nIdx < nOTCount)
                            {
                                OnixLegacyOtherText TempText = CurrentRecord.OnixOtherTextList[nIdx];
                                TempText.Text = OtherTextList[nIdx];
                            }
                        }
                    }

                    OtherTextList.Clear();
                }
                catch (Exception ex)
                {
                    CurrentRecord = new OnixLegacyProduct();

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
