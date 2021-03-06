﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using VirtoCommerce.Foundation.Search.Schemas;
using VirtoCommerce.Foundation.Stores.Model;

namespace VirtoCommerce.Foundation.Data.Stores
{
	public class SqlStoreSampleDatabaseInitializer : SqlStoreDatabaseInitializer
	{
		private readonly bool _reduced;

		public SqlStoreSampleDatabaseInitializer(bool reduced = false)
		{
			_reduced = reduced;
		}

		protected override void Seed(EFStoreRepository context)
		{
			CreateFulfillmentCenter(context);
			CreateStores(context, _reduced);

			base.Seed(context);
		}

		public static void CreateStores(EFStoreRepository context, bool reduced = false)
		{
			var store = CreateStore("SampleStore", reduced);

			var appleStore = CreateStore("AppleStore");
			appleStore.Name = "Apple Store";
			appleStore.Catalog = "Apple";

			var sonyStore = CreateStore("SonyStore");
			sonyStore.Name = "Sony Store";
			sonyStore.Catalog = "Sony";

			context.Add(store);
			context.Add(appleStore);
			context.Add(sonyStore);
			context.UnitOfWork.Commit();
		}

		private static Store CreateStore(string storeId, bool reduced = false)
		{
			var store = new Store
				{
					StoreId = storeId,
					Name = "Electronics Store",
					Description = "Contains electronics from multiple vendors",
					StoreState = StoreState.Open.GetHashCode(),
					TimeZone = TimeZone.CurrentTimeZone.StandardName,
					Country = "USA",
					Region = "California"
				};

			store.Languages.Add(new StoreLanguage { StoreId = store.StoreId, LanguageCode = "en-US" });
			store.Languages.Add(new StoreLanguage { StoreId = store.StoreId, LanguageCode = "de-DE" });
			if (!reduced)
			{
				store.Languages.Add(new StoreLanguage { StoreId = store.StoreId, LanguageCode = "ru-RU" });
				store.Languages.Add(new StoreLanguage { StoreId = store.StoreId, LanguageCode = "ja-JP" });
			}
			store.Currencies.Add(new StoreCurrency { StoreId = store.StoreId, CurrencyCode = "USD" });
			store.Currencies.Add(new StoreCurrency { StoreId = store.StoreId, CurrencyCode = "EUR" });
			store.DefaultLanguage = "en-US";
			store.DefaultCurrency = "USD";
			store.Catalog = "VendorVirtual";
			store.TaxJurisdictions.Add(new StoreTaxJurisdiction { StoreId = store.StoreId, TaxJurisdiction = "USA" });
			store.TaxCodes.Add(new StoreTaxCode { StoreId = store.StoreId, TaxCode = "Shipping" });
			store.TaxCodes.Add(new StoreTaxCode { StoreId = store.StoreId, TaxCode = "Goods" });
			store.PaymentGateways.Add(new StorePaymentGateway { StoreId = store.StoreId, PaymentGateway = "CreditCard" });
			store.PaymentGateways.Add(new StorePaymentGateway { StoreId = store.StoreId, PaymentGateway = "Phone" });
			store.PaymentGateways.Add(new StorePaymentGateway { StoreId = store.StoreId, PaymentGateway = "Paypal" });

			store.CardTypes.Add(new StoreCardType { StoreId = store.StoreId, CardType = "Visa" });
			store.CardTypes.Add(new StoreCardType { StoreId = store.StoreId, CardType = "MasterCard" });
			store.CardTypes.Add(new StoreCardType { StoreId = store.StoreId, CardType = "Amex" });
			store.CreditCardSavePolicy = CreditCardSavePolicy.None.GetHashCode();
			store.Email = "Vendor Store <vendor-store@virtocommerce.com>";
			store.AdminEmail = "Vendor Store Admin <vendor-store-admin@virtocommerce.com>";
			store.DisplayOutOfStock = true;
			store.FulfillmentCenterId = "vendor-fulfillment";
			store.ReturnsFulfillmentCenterId = "vendor-fulfillment";
			//Need this for testing, otherwise refund will fail 
			store.CreditCardSavePolicy = (int)CreditCardSavePolicy.Full;

			store.Settings.Add(new StoreSetting
				{
					StoreId = store.StoreId,
					ValueType = "xml",
					LongTextValue = CreateFiltersXml(),
					Name = "FilteredBrowsing"
				});
			store.Settings.Add(new StoreSetting
				{
					StoreId = store.StoreId,
					ValueType = "ShortText",
					ShortTextValue = "The store is temporarily closed for maintenance. Please try again later.",
					Name = "StoreClosedMessage"
				});
			store.Settings.Add(new StoreSetting
				{
					StoreId = store.StoreId,
					ValueType = "ShortText",
					ShortTextValue = "You do not have permissions to view this store",
					Name = "StoreRestrictedMessage"
				});
			store.Settings.Add(new StoreSetting
				{
					StoreId = store.StoreId,
					ValueType = "Boolean",
					BooleanValue = false,
					Name = "RequireAccountConfirmation"
				});

			return store;
		}

		public static void CreateFulfillmentCenter(EFStoreRepository context)
		{
			var center = new FulfillmentCenter
				{
					FulfillmentCenterId = "vendor-fulfillment",
					Name = "Vendor Fulfillment Center",
					PickDelay = 30,
					Line1 = "1232 Wilshire Blvd",
					MaxReleasesPerPickBatch = 20,
					PostalCode = "90234",
					StateProvince = "California",
					City = "Los Angeles",
					CountryName = "United States",
					CountryCode = "USA",
					DaytimePhoneNumber = "3232323232"
				};

			context.Add(center);
			context.UnitOfWork.Commit();
		}

		#region Setup Filters

		private static string CreateFiltersXml()
		{
			var browsing = new FilteredBrowsing();
			CreateFilters(browsing);
			CreateColorFilters(browsing);
			CreatePriceFilters(browsing);

			var serializer = new XmlSerializer(typeof(FilteredBrowsing));
			var writer = new StringWriter();
			serializer.Serialize(writer, browsing);
			var filtersXml = writer.ToString();
			return filtersXml;
		}

		private static void CreateColorFilters(FilteredBrowsing browsing)
		{
			var filters = browsing.Attributes != null ? new List<AttributeFilter>(browsing.Attributes) : new List<AttributeFilter>();

			var filter = new AttributeFilter { Key = "color", IsLocalized = false };

			var colors = new[] { "White", "Black", "Red", "Yellow", "Green", "Blue", "Pink", "Magenta" };

			//var index = 0;

			filter.Values = colors.Select(color => new AttributeFilterValue { Id = color.ToLower(), Value = color.ToLower() }).ToArray();
			filters.Add(filter);
			browsing.Attributes = filters.ToArray();
		}

		/*
				private void CreateDisplayFilters(FilteredBrowsing browsing)
				{
					List<RangeFilter> filters = null;

					if (browsing.AttributeRanges != null)
					{
						filters = new List<RangeFilter>(browsing.AttributeRanges);
					}
					else
					{
						filters = new List<RangeFilter>();
					}

					var filter = new RangeFilter();
					filter.Key = "DisplaySize";

					var vals = new List<RangeFilterValue>();

					vals.Add(CreateRange("20 Inches & Under", "under-i20", String.Empty, "20", "en"));
					vals.Add(CreateRange("21 to 29 Inches", "i21-29", "21", "29", "en"));
					vals.Add(CreateRange("30 to 39 Inches", "i21-29", "30", "39", "en"));
					vals.Add(CreateRange("40 to 49 Inches", "i21-29", "40", "49", "en"));
					vals.Add(CreateRange("50 Inches & Up", "over-i50", "50", String.Empty, "en"));

					filters.Add(filter);

					browsing.AttributeRanges = filters.ToArray();
				}
		*/

		private static void CreateFilters(FilteredBrowsing browsing)
		{
			var filters = browsing.Attributes != null ? new List<AttributeFilter>(browsing.Attributes) : new List<AttributeFilter>();

			var vals = new List<AttributeFilterValue>();

			var filter = new AttributeFilter { Key = "Brand" };

			var val = new AttributeFilterValue { Id = "samsung", Value = "samsung" };
			var val2 = new AttributeFilterValue { Id = "sony", Value = "sony" };
			var val3 = new AttributeFilterValue { Id = "apple", Value = "apple" };

			vals.Add(val);
			vals.Add(val2);
			vals.Add(val3);

			filter.Values = vals.ToArray();
			filters.Add(filter);

			browsing.Attributes = filters.ToArray();
		}

		private static void CreatePriceFilters(FilteredBrowsing browsing)
		{
			var filters = browsing.Prices != null ? new List<PriceRangeFilter>(browsing.Prices) : new List<PriceRangeFilter>();

			var vals = new List<RangeFilterValue>();

			var filter = new PriceRangeFilter { Currency = "USD", IsLocalized = false };

			vals.Add(CreateRange("Under $100", "under-100", String.Empty, "100", "en"));

			vals.Add(CreateRange("$100 - $200", "100-200", "100", "200", "en"));
			vals.Add(CreateRange("$200 - $600", "200-600", "200", "600", "en"));
			vals.Add(CreateRange("$600 - $1000", "600-1000", "600", "1000", "en"));
			vals.Add(CreateRange("Over $1000", "over-1000", "1000", String.Empty, "en"));

			filter.Values = vals.ToArray();
			filters.Add(filter);

			vals = new List<RangeFilterValue>();

			filter = new PriceRangeFilter { Currency = "EUR", IsLocalized = false };

			vals.Add(CreateRange("Under 100€", "under-100", String.Empty, "100", "en"));

			vals.Add(CreateRange("100€ - 200€", "100-200", "100", "200", "en"));
			vals.Add(CreateRange("200€ - 600€", "200-600", "200", "600", "en"));
			vals.Add(CreateRange("600€ - 1000€", "600-1000", "600", "1000", "en"));
			vals.Add(CreateRange("Over 1000€", "over-1000", "1000", String.Empty, "en"));

			filter.Values = vals.ToArray();
			filters.Add(filter);

			browsing.Prices = filters.ToArray();
		}

		private static RangeFilterValue CreateRange(string desciption, string key, string lower, string upper, string lang)
		{
			var val = new RangeFilterValue { Id = key };
			if (lower != null)
			{
				val.Lower = lower;
			}
			if (!String.IsNullOrEmpty(upper))
			{
				val.Upper = upper;
			}

			var disp = new FilterValueDisplay { Value = desciption, Language = lang };
			val.Displays = new[] { disp };
			return val;
		}

		#endregion
	}
}
