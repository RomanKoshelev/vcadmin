﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.ModelBinding;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Foundation.AppConfig.Model;
using VirtoCommerce.Foundation.Assets.Services;
using VirtoCommerce.Foundation.Catalogs.Search;
using VirtoCommerce.Foundation.Catalogs.Services;
using VirtoCommerce.Foundation.Search;
using VirtoCommerce.Foundation.Search.Schemas;
using VirtoCommerce.Foundation.Stores.Repositories;
using VirtoCommerce.MerchandisingModule.Web.Binders;
using VirtoCommerce.MerchandisingModule.Web.Converters;
using VirtoCommerce.MerchandisingModule.Web.Model;
using VirtoCommerce.MerchandisingModule.Web.Services;
using moduleModel = VirtoCommerce.Domain.Catalog.Model;

namespace VirtoCommerce.MerchandisingModule.Web.Controllers
{
    [RoutePrefix("api/mp/{store}/{language}/products")]
	public class ProductController : BaseController
	{
		private readonly IItemService _itemService;

        private readonly IItemBrowsingService _browseService;
        private readonly IBrowseFilterService _browseFilterService;

        private readonly Func<IFoundationCatalogRepository> _foundationCatalogRepositoryFactory;
	    private readonly Func<IFoundationAppConfigRepository> _foundationAppConfigRepFactory;
	    private readonly Func<ICatalogOutlineBuilder> _catalogOutlineBuilderFactory;

        private readonly IAssetUrl _assetUri;


		public ProductController(IItemService itemService,
                                 IItemBrowsingService browseService,
                                 IBrowseFilterService browseFilterService,
								 Func<IFoundationCatalogRepository> foundationCatalogRepositoryFactory,
                                 Func<IFoundationAppConfigRepository> foundationAppConfigRepFactory,
                                 Func<ICatalogOutlineBuilder> catalogOutlineBuilderFactory,
                                 Func<IStoreRepository> storeRepository,
								 IAssetUrl assetUri) 
            : base(storeRepository)
		{
		    _itemService = itemService;
			_foundationCatalogRepositoryFactory = foundationCatalogRepositoryFactory;
		    _foundationAppConfigRepFactory = foundationAppConfigRepFactory;
		    _catalogOutlineBuilderFactory = catalogOutlineBuilderFactory;
            _assetUri = assetUri;
		    _browseService = browseService;
		    _browseFilterService = browseFilterService;
		}

        /// <summary>
        /// Searches the specified catalog.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="parameters"></param>
        /// <param name="responseGroup">The response group.</param>
        /// <param name="outline">The outline.</param>
        /// <param name="language">The language.</param>
        /// <param name="currency"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
		[ResponseType(typeof(ProductSearchResult))]
		public IHttpActionResult Search(string store, [ModelBinder(typeof(SearchParametersBinder))] SearchParameters parameters, [FromUri]moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemMedium, [FromUri]string outline="", string language = "en-us", string currency = "USD")
        {
            var context = new Dictionary<string, object>
            {
                {"StoreId", store},
            };

            var catalog = GetCatalogId(store);
            string categoryId = null;

            var criteria = new CatalogItemSearchCriteria { Locale = language, Catalog = catalog.ToLowerInvariant() };

            if (!string.IsNullOrWhiteSpace(outline))
            {
                criteria.Outlines.Add(String.Format("{0}/{1}*", catalog, outline));
                categoryId = outline.Split(new[] { '/' }).Last();
                context.Add("CategoryId", categoryId);
            }           

            // Now fill in filters
            var filters = _browseFilterService.GetFilters(context);

            // Add all filters
            foreach (var filter in filters)
            {
                criteria.Add(filter);
            }

            // apply terms
            if (parameters.Terms != null && parameters.Terms.Count > 0)
            {
                foreach (var term in parameters.Terms)
                {
                    var termFilter = new AttributeFilter
                    {
                        Key = term.Key,
                        Values = term.Value.Select(x => new AttributeFilterValue { Id = x.ToLowerInvariant(), Value = x.ToLowerInvariant() }).ToArray()
                    };

                    criteria.Apply(termFilter);
                }
            }

            // apply filters
            var facets = parameters.Facets;
            if (facets.Count != 0)
            {
                foreach (var key in facets.Keys)
                {
                    var filter = filters.SingleOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                        && (!(x is PriceRangeFilter) || ((PriceRangeFilter)x).Currency.Equals(currency, StringComparison.OrdinalIgnoreCase)));

                    var appliedFilter = _browseFilterService.Convert(filter, facets[key]);
                    criteria.Apply(appliedFilter);
                }
            }

            //criteria.ClassTypes.Add("Product");
            criteria.RecordsToRetrieve = parameters.PageSize == 0 ? 10 : parameters.PageSize;
            criteria.StartingRecord = parameters.StartingRecord;
            criteria.Pricelists = null;//UserHelper.CustomerSession.Pricelists;
            criteria.Currency = currency;
            criteria.StartDateFrom = parameters.StartDateFrom;
            criteria.SearchPhrase = parameters.FreeSearch;

            #region sorting

            if (!string.IsNullOrEmpty(parameters.Sort))
            {

                var isDescending = "desc".Equals(parameters.SortOrder, StringComparison.OrdinalIgnoreCase);

                SearchSort sortObject = null;

                switch (parameters.Sort.ToLowerInvariant())
                {
                    case "price":
                        if (criteria.Pricelists != null)
                        {
                            sortObject = new SearchSort(criteria.Pricelists.Select(priceList =>
                                new SearchSortField(
                                    String.Format("price_{0}_{1}",
                                        criteria.Currency.ToLower(),
                                        priceList.ToLower()))
                                {
                                    IgnoredUnmapped = true,
                                    IsDescending = isDescending,
                                    DataType = SearchSortField.DOUBLE
                                })
                                .ToArray());
                        }
                        break;
                    case "position":
                        sortObject =
                            new SearchSort(
                                new SearchSortField(string.Format("sort{0}{1}", catalog, categoryId).ToLower())
                                {
                                    IgnoredUnmapped = true,
                                    IsDescending = isDescending
                                });
                        break;
                    case "name":
                        sortObject = new SearchSort("name", isDescending);
                        break;
                    case "rating":
                        sortObject = new SearchSort(criteria.ReviewsAverageField, isDescending);
                        break;
                    case "reviews":
                        sortObject = new SearchSort(criteria.ReviewsTotalField, isDescending);
                        break;
                    default:
                        sortObject = CatalogItemSearchCriteria.DefaultSortOrder;
                        break;

                }

                criteria.Sort = sortObject;
            }

            #endregion

            //Load ALL products 
            var searchResults = _browseService.SearchItems(criteria, responseGroup);

            return Ok(searchResults);
		}

		/// GET: api/mp/apple/en-us/products?code='22'
		[HttpGet]
		[ResponseType(typeof(Product))]
		[Route("")]
        public IHttpActionResult GetProductByCode(string store, [FromUri]string code, [FromUri]moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemLarge, string language = "en-us")
		{
            //var catalog = GetCatalogId(store);

			using(var repository = _foundationCatalogRepositoryFactory())
			{
                //Cannot filter by catalogId here because it fails when catalog is virtual
				var itemId = repository.Items.Where(x => x.Code == code).Select(x => x.ItemId).FirstOrDefault();
				if(itemId != null)
				{
					return GetProduct(store, itemId,responseGroup);
				}
			}
			return StatusCode(HttpStatusCode.NotFound);
		}



		[HttpGet]
		[ResponseType(typeof(Product))]
		[Route("{product}")]
        public IHttpActionResult GetProduct(string store, string product, [FromUri]moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemLarge, string language = "en-us")
		{
            var catalog = GetCatalogId(store);
            var result = _itemService.GetById(product, responseGroup);

            if (result == null)
            {
                //Lets treat product as slug
                using (var appConfigRepo = _foundationAppConfigRepFactory())
                {
                    var keyword = appConfigRepo.SeoUrlKeywords.FirstOrDefault(x => x.KeywordType == (int)SeoUrlKeywordTypes.Item
                        && x.Keyword.Equals(product, StringComparison.InvariantCultureIgnoreCase));

                    if (keyword != null)
                    {
                        result = _itemService.GetById(keyword.KeywordValue, responseGroup);
                    }
                }
            }

            if (result != null)
		    {
                var webModelProduct = result.ToWebModel(_assetUri);
                //Build category path outline for requested catalog, can be virtual catalog as well
                webModelProduct.Outline = _catalogOutlineBuilderFactory().BuildCategoryOutline(catalog, result.Id).ToString("/").ToLowerInvariant();
                webModelProduct.Outline = webModelProduct.Outline.Replace(catalog + "/", "");
                return Ok(webModelProduct);
		    }
		    return StatusCode(HttpStatusCode.NotFound);
		}

        /*
        [HttpPost]
        [ResponseType(typeof(ResponseCollection<Product>))]
        [Route("")]
        public IHttpActionResult GetProducts(string store, string[] productIds, [FromUri]moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemLarge, string language = "en-us")
        {
            var catalog = GetCatalogId(store);
            var products = _itemService.GetByIds(productIds, responseGroup);
            var response = new ResponseCollection<Product>();
            if (products == null)
            {
                foreach (var product in products)
                {
                    response.Items.Add(product.ToWebModel(_assetUri));
                }
                //Lets treat product as slug
                using (var appConfigRepo = _foundationAppConfigRepFactory())
                {
                    var keyword = appConfigRepo.SeoUrlKeywords.FirstOrDefault(x => x.KeywordType == (int)SeoUrlKeywordTypes.Item
                        && x.Keyword.Equals(product, StringComparison.InvariantCultureIgnoreCase));

                    if (keyword != null)
                    {
                        result = _itemService.GetById(keyword.KeywordValue, responseGroup);
                    }
                }
            }

            if (result != null)
            {
                var webModelProduct = result.ToWebModel(_assetUri);
                //Build category path outline for requested catalog, can be virtual catalog as well
                webModelProduct.Outline = _catalogOutlineBuilderFactory().BuildCategoryOutline(catalog, result.Id).ToString("/").ToLowerInvariant();
                webModelProduct.Outline = webModelProduct.Outline.Replace(catalog + "/", "");
                return Ok(webModelProduct);
            }
            return StatusCode(HttpStatusCode.NotFound);
        }
         * */
	}
}
