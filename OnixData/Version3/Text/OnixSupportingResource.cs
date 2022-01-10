namespace OnixData.Version3.Text
{
    public partial class OnixSupportingResource
    {
        private int resourceContentTypeField;
        private int contentAudienceField;
        private int resourceModeField;

        private OnixResourceVersion[] resourceVersionField;
        private OnixResourceVersion[] shortResourceVersionField;

        public OnixSupportingResource()
        {
            ResourceContentType = ContentAudience = ResourceMode = - 1;
            resourceVersionField = shortResourceVersionField = new OnixResourceVersion[0];
        }

        #region ONIX Lists

        public OnixResourceVersion[] OnixResourceVersionList
        {
            get
            {
                OnixResourceVersion[] ResourceVersions = null;

                if (this.resourceVersionField != null)
                    ResourceVersions = this.resourceVersionField;
                else if (this.shortResourceVersionField != null)
                    ResourceVersions = this.shortResourceVersionField;
                else
                    ResourceVersions = new OnixResourceVersion[0];

                return ResourceVersions;
            }
        }

        #endregion

        #region Reference Tags

        /// <remarks/>
        public int ResourceContentType
        {
            get
            {
                return this.resourceContentTypeField;
            }
            set
            {
                this.resourceContentTypeField = value;
            }
        }

        /// <remarks/>
        public int ContentAudience
        {
            get
            {
                return this.contentAudienceField;
            }
            set
            {
                this.contentAudienceField = value;
            }
        }

         /// <remarks/>
        public int ResourceMode
        {
            get
            {
                return this.resourceModeField;
            }
            set
            {
                this.resourceModeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ResourceVersion", IsNullable = false)]
        public OnixResourceVersion[] OnixResourceVersion
        {
            get
            {
                return this.resourceVersionField;
            }
            set
            {
                this.resourceVersionField = value;
            }
        }

        #endregion

        #region Short Tags

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("resourceVersion", IsNullable = false)]
        public OnixResourceVersion[] onixResourceVersion
        {
            get
            {
                return this.shortResourceVersionField;
            }
            set
            {
                this.shortResourceVersionField = value;
            }
        }

        #endregion
    }
}