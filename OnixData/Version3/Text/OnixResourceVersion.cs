namespace OnixData.Version3.Text
{
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class OnixResourceVersion
    {
        private int resourceFormField;
        private int shortResourceFormField;
        private string resourceLinkField;
        private string shortResourceLinkField;

        public OnixResourceVersion()
        {
            ResourceForm = -1;
            ResourceLink = "";
        }

        #region Reference Tags

        /// <remarks/>
        public int ResourceForm
        {
            get
            {
                return this.resourceFormField;
            }
            set
            {
                this.resourceFormField = value;
            }
        }

        /// <remarks/>
        public string ResourceLink
        {
            get
            {
                return this.resourceLinkField;
            }
            set
            {
                this.resourceLinkField = value;
            }
        }

        #endregion

        #region Short Tags

        /// <remarks/>
        public int x441
        {
            get {  return this.shortResourceFormField; }
            set { this.shortResourceFormField = value; }
        }

        /// <remarks/>
        public string x435
        {
            get { return this.shortResourceLinkField; }
            set { this.shortResourceLinkField = value; }
        }

        #endregion
    }
}