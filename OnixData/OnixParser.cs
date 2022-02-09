﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using OnixData.Extensions;
using OnixData.Version3;
using OnixData.Version3.Header;

namespace OnixData
{
    public class OnixParser : IDisposable, IEnumerable
    {
        #region CONSTANTS

        public const int CONST_DTD_REFERENCE_LENGTH = 500;

        public const string CONST_ONIX_MESSAGE_REFERENCE_TAG = "ONIXMessage";
        public const string CONST_ONIX_MESSAGE_SHORT_TAG = "ONIXmessage";

        public const string CONST_ONIX_HEADER_REFERENCE_TAG = "Header";
        public const string CONST_ONIX_HEADER_SHORT_TAG = "header";

        #endregion

        private bool ParserRefVerFlag = false;
        private bool ParserRVWFlag = false;
        private FileInfo ParserFileInfo = null;
        private StringBuilder ParserFileContent = null;
        //private XmlReader CurrOnixReader = null;
        private OnixMessage CurrOnixMessage = null;

        public bool ReferenceVersion
        {
            get { return this.ParserRefVerFlag; }
        }

        public bool ShouldApplyDefaults { get; set; }

        public OnixParser(FileInfo OnixFilepath,
                              bool ReportValidationWarnings,
                              bool PreprocessOnixFile = true,
                              bool LoadEntireFileIntoMemory = false,
                              bool FilterBadEncodings = false)
        {
            if (!File.Exists(OnixFilepath.FullName))
                throw new Exception("ERROR!  File(" + OnixFilepath + ") does not exist.");

            this.ShouldApplyDefaults = true;

            this.ParserFileInfo = OnixFilepath;
            this.ParserRVWFlag = ReportValidationWarnings;

            bool ReferenceVersion = DetectDtdVersionReference(OnixFilepath);
            string sOnixMsgTag = ReferenceVersion ? CONST_ONIX_MESSAGE_REFERENCE_TAG : CONST_ONIX_MESSAGE_SHORT_TAG;

            this.ParserRefVerFlag = ReferenceVersion;

            if (PreprocessOnixFile)
                OnixFilepath.ReplaceIsoLatinEncodings(true, FilterBadEncodings);

            //this.CurrOnixReader = CreateXmlReader(this.ParserFileInfo, this.ParserRVWFlag);
            using (var currOnixReader = CreateXmlReader(this.ParserFileInfo, this.ParserRVWFlag))
            {
                if (LoadEntireFileIntoMemory)
                {
                    var xmlSerializer = new XmlSerializer(typeof(OnixMessage), new XmlRootAttribute(sOnixMsgTag));
                    this.CurrOnixMessage = xmlSerializer.Deserialize(currOnixReader) as OnixMessage;
                }
                else
                    this.CurrOnixMessage = null;
            }
        }

        public OnixParser(string OnixContent,
                            bool ReportValidationWarnings,
                            bool PreprocessOnixFile = true,
                            bool FilterBadEncodings = false)

        {
            if (String.IsNullOrEmpty(OnixContent))
                throw new Exception("ERROR!  Provided ONIX content is empty.");

            this.ShouldApplyDefaults = true;

            this.ParserFileInfo = null;
            this.ParserRVWFlag = ReportValidationWarnings;

            this.ParserFileContent = new StringBuilder(OnixContent);

            if (PreprocessOnixFile)
                this.ParserFileContent.ReplaceIsoLatinEncodings(FilterBadEncodings);

            bool ReferenceVersion = DetectDtdVersionReference(OnixContent);
            string sOnixMsgTag = ReferenceVersion ? CONST_ONIX_MESSAGE_REFERENCE_TAG : CONST_ONIX_MESSAGE_SHORT_TAG;

            this.ParserRefVerFlag = ReferenceVersion;

            //this.CurrOnixReader = CreateXmlReader(this.ParserFileContent, this.ParserRVWFlag);
            using (var currOnixReader = CreateXmlReader(this.ParserFileContent, this.ParserRVWFlag))
            {
                var rootAttribute = new XmlRootAttribute(sOnixMsgTag);
                var xmlSerializer = new XmlSerializer(typeof(OnixMessage), rootAttribute);
                this.CurrOnixMessage = xmlSerializer.Deserialize(currOnixReader) as OnixMessage;
            }
        }

        public XmlReader CreateXmlReader()
        {
            XmlReader OnixReader =
                (this.ParserFileInfo != null) ? CreateXmlReader(this.ParserFileInfo, this.ParserRVWFlag) : CreateXmlReader(this.ParserFileContent, this.ParserRVWFlag);

            return OnixReader;
        }

        static public XmlReader CreateXmlReader(FileInfo CurrOnixFilepath, bool ReportValidationWarnings)
        {
            /*
             * OLD WAY
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ConformanceLevel  = ConformanceLevel.Document;
            settings.ValidationType    = ValidationType.Schema;
            settings.DtdProcessing     = DtdProcessing.Parse;
            settings.ValidationFlags  |= XmlSchemaValidationFlags.ProcessInlineSchema;

            if (ReportValidationWarnings)
                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            else
                settings.ValidationFlags &= ~(XmlSchemaValidationFlags.ReportValidationWarnings);

            return XmlReader.Create(CurrOnixFilepath.FullName, settings);
            */

            XmlTextReader OnixXmlReader = new XmlTextReader(CurrOnixFilepath.FullName);
            OnixXmlReader.XmlResolver = null;
            OnixXmlReader.DtdProcessing = DtdProcessing.Ignore;
            OnixXmlReader.Namespaces = false;

            return OnixXmlReader;
        }

        static public XmlReader CreateXmlReader(StringBuilder OnixContent, bool ReportValidationWarnings)
        {
            XmlTextReader OnixXmlReader = new XmlTextReader(new StringReader(OnixContent.ToString()));
            OnixXmlReader.XmlResolver = null;
            OnixXmlReader.DtdProcessing = DtdProcessing.Ignore;
            OnixXmlReader.Namespaces = false;

            return OnixXmlReader;
        }

        public OnixHeader MessageHeader
        {
            get
            {
                string sOnixHdrTag =
                    this.ParserRefVerFlag ? CONST_ONIX_HEADER_REFERENCE_TAG : CONST_ONIX_HEADER_SHORT_TAG;

                OnixHeader OnixHeader = new OnixHeader();

                if (this.CurrOnixMessage != null)
                    OnixHeader = this.CurrOnixMessage.Header;
                else
                {
                    using (XmlReader OnixReader = CreateXmlReader())
                    {
                        XmlDocument XMLDoc = new XmlDocument();
                        XMLDoc.Load(OnixReader);

                        XmlNodeList HeaderList = XMLDoc.GetElementsByTagName(sOnixHdrTag);

                        if ((HeaderList != null) && (HeaderList.Count > 0))
                        {
                            XmlNode HeaderNode = HeaderList.Item(0);
                            string sHeaderBody = HeaderNode.OuterXml;

                            OnixHeader =
                                new XmlSerializer(typeof(OnixHeader), new XmlRootAttribute(sOnixHdrTag))
                                .Deserialize(new StringReader(sHeaderBody)) as OnixHeader;
                        }
                    }
                }

                return OnixHeader;
            }
        }

        public OnixMessage Message
        {
            get
            {
                return this.CurrOnixMessage;
            }
        }

        public void Dispose()
        {
            //if (this.CurrOnixReader != null)
            //{
            //    this.CurrOnixReader.Close();
            //    this.CurrOnixReader.Dispose();
            //}
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public OnixEnumerator GetEnumerator()
        {
            if (this.ParserFileInfo != null)
                return new OnixEnumerator(this, this.ParserFileInfo);
            else if (this.ParserFileContent != null)
                return new OnixEnumerator(this, this.ParserFileContent);
            else
                return null;

        }

        public bool DetectDtdVersionReference(FileInfo LegacyOnixFilepath)
        {
            if (LegacyOnixFilepath.Length < CONST_DTD_REFERENCE_LENGTH)
                throw new Exception("ERROR!  ONIX File is smaller than expected!");

            byte[] buffer = new byte[CONST_DTD_REFERENCE_LENGTH];
            using (FileStream fs = new FileStream(LegacyOnixFilepath.FullName, FileMode.Open, FileAccess.Read))
            {
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
            }

            string sOnixMsgTop = Encoding.Default.GetString(buffer);

            return DetectDtdVersionReference(sOnixMsgTop);
        }

        public bool DetectDtdVersionReference(string psOnixMsg)
        {
            bool bReferenceVersion = true;

            string sRefMsgTag = "<" + CONST_ONIX_MESSAGE_REFERENCE_TAG + " ";

            if (psOnixMsg.Contains(sRefMsgTag))
                bReferenceVersion = true;
            else
                bReferenceVersion = false;

            return bReferenceVersion;
        }

    }
}
