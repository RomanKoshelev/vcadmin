﻿
namespace VirtoCommerce.MerchandisingModule.Web.Model.Store
{
    public class Store
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public int StoreState { get; set; }
  
        public string TimeZone { get; set; }

        public string Country { get; set; }

        public string Region { get; set; }

        public string DefaultLanguage { get; set; }

        public string DefaultCurrency { get; set; }

        public string Catalog { get; set; }

        public string SecureUrl { get; set; }

        public string[] Languages { get; set; }

        public string[] Currencies { get; set; }
 
        public StoreSetting[] Settings { get; set; }

        public string[] LinkedStores { get; set; }

        public SeoKeyword[] SeoKeywords { get; set; }

    }
}
