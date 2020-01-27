﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnixData.Version3.Publishing
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class OnixPubDate
    {
        #region CONSTANTS

        public const string CONST_PUB_DT_ROLE_NORMAL       = "01";
        public const string CONST_PUB_DT_ROLE_ANNOUNCE     = "02";
        public const string CONST_PUB_DT_ROLE_TRADE_ANN    = "10";
        public const string CONST_PUB_DT_ROLE_PUB_FIRST    = "11";
        public const string CONST_PUB_DT_ROLE_LAST_REPRINT = "12";
        public const string CONST_PUB_DT_ROLE_OOP_FIRST    = "13";
        public const string CONST_PUB_DT_ROLE_LAST_REISSUE = "16";

        #endregion

        public OnixPubDate()
        {
            PublishingDateRole = Date = "";
        }

        private string dateField;
        private string pubDateRoleField;

        #region Reference Tags

        public string Date
        {
            get
            {
                return this.dateField;
            }
            set
            {
                this.dateField = value;
            }
        }

        /// <remarks/>
        public string PublishingDateRole
        {
            get
            {
                return this.pubDateRoleField;
            }
            set
            {
                this.pubDateRoleField = value;
            }
        }

        #endregion

        #region Short Tags

        public string b306
        {
            get { return Date; }
            set { Date = value; }
        }        

        /// <remarks/>
        public string x448
        {
            get { return PublishingDateRole; }
            set { PublishingDateRole = value; }
        }

        #endregion
    }
}
