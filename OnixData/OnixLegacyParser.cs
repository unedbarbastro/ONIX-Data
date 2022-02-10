using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using OnixData.Extensions;
using OnixData.Legacy;

namespace OnixData
{
    /// <summary>
    /// 
    /// This class serves as a way to parse files of the ONIX 2.1 standard (and earlier).
    /// You can use this class to either:
    /// 
    /// a.) Deserialize and load the entire file into memory
    /// b.) Enumerate through the file record by record, loading each into memory one at a time
    ///     
    /// </summary>
    public class OnixLegacyParser : IDisposable, IEnumerable
    {
        #region CONSTANTS

        private const int CONST_MSG_REFERENCE_LENGTH = 500;
        private const int CONST_BLOCK_COUNT_SIZE = 50000000;

        private const string CONST_ONIX_MESSAGE_REFERENCE_TAG = "ONIXMessage";
        private const string CONST_ONIX_MESSAGE_SHORT_TAG = "ONIXmessage";

        private const string CONST_ONIX_HEADER_REFERENCE_TAG = "Header";
        private const string CONST_ONIX_HEADER_SHORT_TAG = "header";

        #endregion

        private bool DebugFlag = true;
        private bool ParserRefVerFlag = false;
        private bool ParserRVWFlag = false;
        private bool PerformValidFlag = false;
        private StringBuilder ParserFileContent = null;
        private FileInfo ParserFileInfo = null;
        private readonly SerializerManager _xmlSerializerManager;

        private XmlTextReader LegacyOnixReader = null;
        private OnixLegacyMessage LegacyOnixMessage = null;

        public bool PerformValidation
        {
            get { return this.PerformValidFlag; }
        }

        public bool ReferenceVersion
        {
            get { return this.ParserRefVerFlag; }
        }

        public bool ShouldApplyDefaults { get; set; }

        public bool AlwaysReturnInputXml { get; set; }

        public OnixLegacyParser(FileInfo LegacyOnixFilepath,
                                    SerializerManager xmlSerializerManager,
                                    bool ExecuteValidation,
                                    bool PreprocessOnixFile = true,
                                    bool LoadEntireFileIntoMemory = false)
        {
            if (!File.Exists(LegacyOnixFilepath.FullName))
                throw new Exception("ERROR!  File(" + LegacyOnixFilepath + ") does not exist.");

            AlwaysReturnInputXml = false;

            this.ParserFileInfo = LegacyOnixFilepath;
            _xmlSerializerManager = xmlSerializerManager;
            this.ParserRVWFlag = true;
            this.ShouldApplyDefaults = true;
            this.PerformValidFlag = ExecuteValidation;

            if (PreprocessOnixFile)
                LegacyOnixFilepath.ReplaceIsoLatinEncodings(true);

            // this.LegacyOnixReader = CreateXmlReader(this.ParserFileInfo, this.ParserRVWFlag, this.PerformValidFlag);
            this.LegacyOnixReader = CreateXmlTextReader(this.ParserFileInfo);

            bool ReferenceVersion = DetectVersionReference(LegacyOnixFilepath);
            string sOnixMsgTag = ReferenceVersion ? CONST_ONIX_MESSAGE_REFERENCE_TAG : CONST_ONIX_MESSAGE_SHORT_TAG;

            this.ParserRefVerFlag = ReferenceVersion;

            if (LoadEntireFileIntoMemory)
            {
                var xmlSerializer = _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixLegacyMessage), new XmlRootAttribute(sOnixMsgTag));
                this.LegacyOnixMessage = xmlSerializer.Deserialize(this.LegacyOnixReader) as OnixLegacyMessage;
            }
            else
                this.LegacyOnixMessage = null;
        }

        public OnixLegacyParser(bool ExecuteValidation,
                            FileInfo LegacyOnixFilepath,
                            SerializerManager xmlSerializerManager,
                                bool ReferenceVersion,
                                bool PreprocessOnixFile = true,
                                bool LoadEntireFileIntoMemory = false)
        {
            string sOnixMsgTag = ReferenceVersion ? CONST_ONIX_MESSAGE_REFERENCE_TAG : CONST_ONIX_MESSAGE_SHORT_TAG;

            AlwaysReturnInputXml = false;

            if (!File.Exists(LegacyOnixFilepath.FullName))
                throw new Exception("ERROR!  File(" + LegacyOnixFilepath + ") does not exist.");

            this.ParserRefVerFlag = ReferenceVersion;
            this.ParserFileInfo = LegacyOnixFilepath;
            _xmlSerializerManager = xmlSerializerManager;
            this.ParserRVWFlag = true;
            this.ShouldApplyDefaults = true;
            this.PerformValidFlag = ExecuteValidation;

            if (PreprocessOnixFile)
                LegacyOnixFilepath.ReplaceIsoLatinEncodings(true);

            // this.LegacyOnixReader = CreateXmlReader(this.ParserFileInfo, this.ParserRVWFlag, this.PerformValidFlag);
            this.LegacyOnixReader = CreateXmlTextReader(this.ParserFileInfo);

            if (LoadEntireFileIntoMemory)
            {
                var xmlSerializer = _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixLegacyMessage), new XmlRootAttribute(sOnixMsgTag));
                this.LegacyOnixMessage = xmlSerializer.Deserialize(this.LegacyOnixReader) as OnixLegacyMessage;
            }
            else
                this.LegacyOnixMessage = null;
        }

        public OnixLegacyParser(string LegacyOnixContent,
                                    SerializerManager xmlSerializerManager,
                                    bool ExecuteValidation,
                                    bool PreprocessOnixFile = true,
                                    bool LoadEntireFileIntoMemory = false)
        {
            if (String.IsNullOrEmpty(LegacyOnixContent))
                throw new Exception("ERROR!  ONIX content provided is empty.");

            AlwaysReturnInputXml = false;

            this.ParserFileContent = new StringBuilder(LegacyOnixContent);
            this.ParserRVWFlag = true;
            this.ShouldApplyDefaults = true;
            _xmlSerializerManager = xmlSerializerManager;
            this.PerformValidFlag = ExecuteValidation;

            if (PreprocessOnixFile)
            {
                ParserFileContent.ReplaceIsoLatinEncodings();
                ParserFileContent.FilterIncompleteEncodings();
            }

            // this.LegacyOnixReader = CreateXmlReader(this.ParserFileContent, this.ParserRVWFlag, this.PerformValidFlag);
            this.LegacyOnixReader = CreateXmlTextReader(this.ParserFileContent);

            bool ReferenceVersion = DetectVersionReference(ParserFileContent);
            string sOnixMsgTag = ReferenceVersion ? CONST_ONIX_MESSAGE_REFERENCE_TAG : CONST_ONIX_MESSAGE_SHORT_TAG;

            this.ParserRefVerFlag = ReferenceVersion;

            if (LoadEntireFileIntoMemory)
            {
                var xmlSerializer = _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixLegacyMessage), new XmlRootAttribute(sOnixMsgTag));
                this.LegacyOnixMessage = xmlSerializer.Deserialize(this.LegacyOnixReader) as OnixLegacyMessage;
            }
            else
                this.LegacyOnixMessage = null;
        }

        public OnixLegacyParser(bool ExecuteValidation,
                              string LegacyOnixContent,
                              SerializerManager xmlSerializerManager,
                                bool ReferenceVersion,
                                bool PreprocessOnixFile = true,
                                bool LoadEntireFileIntoMemory = false)
        {
            string sOnixMsgTag = ReferenceVersion ? CONST_ONIX_MESSAGE_REFERENCE_TAG : CONST_ONIX_MESSAGE_SHORT_TAG;

            AlwaysReturnInputXml = false;

            if (String.IsNullOrEmpty(LegacyOnixContent))
                throw new Exception("ERROR!  ONIX content provided is empty.");

            this.ParserRefVerFlag = ReferenceVersion;
            this.ParserFileContent = new StringBuilder(LegacyOnixContent);
            this.ParserRVWFlag = true;
            this.ShouldApplyDefaults = true;
            this.PerformValidFlag = ExecuteValidation;
            _xmlSerializerManager = xmlSerializerManager;
            if (PreprocessOnixFile)
            {
                ParserFileContent.ReplaceIsoLatinEncodings();
                ParserFileContent.FilterIncompleteEncodings();
            }

            // this.LegacyOnixReader = CreateXmlReader(this.ParserFileContent, this.ParserRVWFlag, this.PerformValidFlag);
            this.LegacyOnixReader = CreateXmlTextReader(this.ParserFileContent);

            if (LoadEntireFileIntoMemory)
            {
                var xmlSerializer = _xmlSerializerManager.RegisterXmlSerializer(typeof(OnixLegacyMessage), new XmlRootAttribute(sOnixMsgTag));
                this.LegacyOnixMessage = xmlSerializer.Deserialize(this.LegacyOnixReader) as OnixLegacyMessage;
            }
            else
                this.LegacyOnixMessage = null;
        }

        public XmlReader CreateXmlReader()
        {
            XmlReader OnixReader =
                (this.ParserFileInfo != null) ?
                    CreateXmlReader(this.ParserFileInfo, this.ParserRVWFlag, false) : CreateXmlReader(this.ParserFileContent, this.ParserRVWFlag, false);

            return OnixReader;
        }

        /// <summary>
        /// 
        /// This method will prepare the XmlReader that we will use to read the ONIX XML file.
        /// 
        /// <param name="CurrOnixFilepath">The path to the ONIX file</param>
        /// <param name="ReportValidationWarnings">The indicator for whether we should report validation warnings to the caller</param>
        /// <param name="ExecutionValidation">The indicator for whether or not the ONIX file should be validated</param>
        /// <returns>The XmlReader that will be used to read the ONIX file</returns>
        /// </summary>
        static public XmlReader CreateXmlReader(FileInfo LegacyOnixFilepath, bool ReportValidationWarnings, bool ExecutionValidation)
        {
            bool bUseXSD = true;
            bool bUseDTD = false;
            StringBuilder InvalidErrMsg = new StringBuilder();
            XmlReader OnixXmlReader = null;

            XmlReaderSettings settings = new XmlReaderSettings();

            settings.ValidationType = ValidationType.None;
            settings.DtdProcessing = DtdProcessing.Ignore;

            if (ExecutionValidation)
            {
                settings.ConformanceLevel = ConformanceLevel.Document;

                if (bUseXSD)
                {
                    /*
                     * NOTE: XSD Validation does not appear to be working correctly yet
                     * 
                    XmlSchemaSet schemas = new XmlSchemaSet();

                    string XsdFilepath = "";

                    if (ParserRefVerFlag)
                        XsdFilepath = @"C:\ONIX_XSD\2.1\ONIX_BookProduct_Release2.1_reference.xsd";
                    else
                        XsdFilepath = @"C:\ONIX_XSD\2.1\ONIX_BookProduct_Release2.1_short.xsd";

                    schemas.Add(null, XmlReader.Create(new StringReader(File.ReadAllText(XsdFilepath))));

                    settings.Schemas        = schemas;
                    settings.ValidationType = ValidationType.Schema;                

                    if (ReportValidationWarnings)
                        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                    else
                        settings.ValidationFlags &= ~(XmlSchemaValidationFlags.ReportValidationWarnings);

                    // settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
                    */
                }

                /*
                 * NOTE: DTD Validation does not appear that it will ever work correctly on the .NET platform
                 * 
                if (bUseDTD)
                {
                    settings.DtdProcessing  = DtdProcessing.Parse;
                    settings.ValidationType = ValidationType.DTD;

                    settings.ValidationFlags |= XmlSchemaValidationFlags.AllowXmlAttributes;

                    System.Xml.XmlResolver newXmlResolver = new System.Xml.XmlUrlResolver();
                    newXmlResolver.ResolveUri(new Uri("C:\\ONIX_DTD\\2.1\\short"), "onix-international.dtd");
                    settings.XmlResolver = newXmlResolver;

                    settings.ValidationEventHandler += new ValidationEventHandler(delegate(object sender, ValidationEventArgs args)
                    {
                        // InvalidErrMsg.AppendLine(args.Message);
                        throw new Exception(args.Message);
                    });
                }
                */
            }

            OnixXmlReader = XmlReader.Create(LegacyOnixFilepath.FullName, settings);

            return OnixXmlReader;
        }

        static public XmlReader CreateXmlReader(StringBuilder LegacyOnixContent, bool ReportValidationWarnings, bool ExecutionValidation)
        {
            bool bUseXSD = true;

            StringBuilder InvalidErrMsg = new StringBuilder();
            XmlReader OnixXmlReader = null;

            XmlReaderSettings settings = new XmlReaderSettings();

            settings.ValidationType = ValidationType.None;
            settings.DtdProcessing = DtdProcessing.Ignore;

            if (ExecutionValidation)
            {
                settings.ConformanceLevel = ConformanceLevel.Document;

                if (bUseXSD)
                { /* NOTE: XSD Validation does not appear to be working correctly yet */ }

                /* NOTE: DTD Validation does not appear that it will ever work correctly on the .NET platform */
            }

            OnixXmlReader = XmlReader.Create(new StringReader(LegacyOnixContent.ToString()), settings);

            return OnixXmlReader;
        }

        static public XmlTextReader CreateXmlTextReader(FileInfo LegacyOnixFilepath)
        {
            XmlTextReader OnixXmlReader = new XmlTextReader(LegacyOnixFilepath.FullName);
            OnixXmlReader.XmlResolver = null;
            OnixXmlReader.DtdProcessing = DtdProcessing.Ignore;
            OnixXmlReader.Namespaces = false;

            return OnixXmlReader;
        }

        static public XmlTextReader CreateXmlTextReader(StringBuilder LegacyOnixContent)
        {
            XmlTextReader OnixXmlReader = new XmlTextReader(new StringReader(LegacyOnixContent.ToString()));
            OnixXmlReader.XmlResolver = null;
            OnixXmlReader.DtdProcessing = DtdProcessing.Ignore;
            OnixXmlReader.Namespaces = false;

            return OnixXmlReader;
        }

        /// <summary>
        /// 
        /// This property will return the header of an ONIX file.  If the file has already been loaded 
        /// into memory, it will extract the header from the internal member reader.  If not, it will 
        /// open the file temporarily and extract it from there.
        /// 
        /// <returns>The header of the ONIX file</returns>
        /// </summary>
        public OnixLegacyHeader MessageHeader
        {
            get
            {
                string sOnixHdrTag =
                    this.ParserRefVerFlag ? CONST_ONIX_HEADER_REFERENCE_TAG : CONST_ONIX_HEADER_SHORT_TAG;

                OnixLegacyHeader LegacyHeader = new OnixLegacyHeader();

                if (this.LegacyOnixMessage != null)
                    LegacyHeader = this.LegacyOnixMessage.Header;
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

                            var xmlSerializer = _xmlSerializerManager.GetXmlSerializer(nameof(OnixLegacyHeader), sOnixHdrTag);
                            LegacyHeader = xmlSerializer.Deserialize(new StringReader(sHeaderBody)) as OnixLegacyHeader;
                        }
                    }
                }

                return LegacyHeader;
            }
        }

        public OnixLegacyMessage Message
        {
            get
            {
                return LegacyOnixMessage;
            }
        }

        public void Dispose()
        {
            if (this.LegacyOnixReader != null)
            {
                this.LegacyOnixReader.Close();
                this.LegacyOnixReader = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public OnixLegacyEnumerator GetEnumerator()
        {
            if (this.ParserFileInfo != null)
                return new OnixLegacyEnumerator(this, this.ParserFileInfo, _xmlSerializerManager);
            else if (this.ParserFileContent != null)
                return new OnixLegacyEnumerator(this, this.ParserFileContent, _xmlSerializerManager);
            else
                return null;
        }

        /// <summary>
        /// 
        /// This method was intended to provide the functionality of validating a legacy ONIX file against its 
        /// respective DTD/XSD.  Unfortunately, though, the .NET platform does not seem to be compatible with 
        /// parsing and understanding a complex schema as specified in the ONIX standard.  So, it seems that this
        /// method will never come into fruition.
        /// 
        /// <returns>The Boolean that indicates whether or not the ONIX file is valid</returns>
        /// </summary>
        public bool ValidateFile()
        {
            bool ValidOnixFile = true;

            /*
             * NOTE: XSD Validation is proving to be consistently problematic
             * 
            try
            {
                XmlReaderSettings TempReaderSettings = new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore };

                using (XmlReader TempXmlReader = XmlReader.Create(ParserFileInfo.FullName, TempReaderSettings))
                {
                    XDocument OnixDoc = XDocument.Load(TempXmlReader);

                    XmlSchemaSet schemas = new XmlSchemaSet();

                    string XsdFilepath = "";

                    if (ParserRefVerFlag)
                        XsdFilepath = @"C:\ONIX_XSD\2.1\ONIX_BookProduct_Release2.1_reference.xsd";
                    else
                        XsdFilepath = @"C:\ONIX_XSD\2.1\ONIX_BookProduct_Release2.1_short.xsd";

                    schemas.Add(null, XmlReader.Create(new StringReader(File.ReadAllText(XsdFilepath))));

                    schemas.Compile();

                    OnixDoc.Validate(schemas, (o, e) =>
                    {
                        Console.WriteLine("{0}", e.Message);
                        ValidOnixFile = false;
                    });
                }
            }
            catch (Exception ex)
            {
                ValidOnixFile = false;
            }
            */

            return ValidOnixFile;
        }

        #region Support Methods

        /// <summary>
        /// 
        /// This method will help determine whether the XML structure of an ONIX file belongs to the 'Reference' type 
        /// of ONIX (i.e., verbose tags) or the 'Short' type of ONIX (i.e., alphanumeric tags).
        /// 
        /// <param name="LegacyOnixFilepath">The path to the ONIX file</param>
        /// <returns>The Boolean that indicates whether or not the ONIX file belongs to the 'Reference' type</returns>
        /// </summary>
        public bool DetectVersionReference(FileInfo LegacyOnixFileInfo)
        {
            bool bReferenceVersion = true;

            if (LegacyOnixFileInfo.Length < CONST_MSG_REFERENCE_LENGTH)
                throw new Exception("ERROR!  ONIX File is smaller than expected!");

            byte[] buffer = new byte[CONST_MSG_REFERENCE_LENGTH];
            using (FileStream fs = new FileStream(LegacyOnixFileInfo.FullName, FileMode.Open, FileAccess.Read))
            {
                fs.Read(buffer, 0, buffer.Length);
                fs.Close();
            }

            string sRefMsgTag = "<" + CONST_ONIX_MESSAGE_REFERENCE_TAG;
            string sFileHead = Encoding.Default.GetString(buffer);
            if (sFileHead.Contains(sRefMsgTag))
                bReferenceVersion = true;
            else
                bReferenceVersion = false;

            return bReferenceVersion;
        }

        public bool DetectVersionReference(StringBuilder LegacyOnixContent)
        {
            bool bReferenceVersion = true;

            if (LegacyOnixContent.Length < CONST_MSG_REFERENCE_LENGTH)
                throw new Exception("ERROR!  ONIX File is smaller than expected!");

            string sRefMsgTag = "<" + CONST_ONIX_MESSAGE_REFERENCE_TAG;
            string sFileHead = LegacyOnixContent.ToString().Substring(0, CONST_MSG_REFERENCE_LENGTH);
            if (sFileHead.Contains(sRefMsgTag))
                bReferenceVersion = true;
            else
                bReferenceVersion = false;

            return bReferenceVersion;
        }

        #endregion
    }
}
