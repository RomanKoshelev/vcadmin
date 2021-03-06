﻿using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Foundation.Search.Schemas;

namespace VirtoCommerce.MerchandisingModule.Web.Services
{
	using VirtoCommerce.CatalogModule.Data.Repositories;
	using VirtoCommerce.Domain.Catalog.Services;
	using VirtoCommerce.Foundation;
	using VirtoCommerce.Foundation.Assets.Services;
	using VirtoCommerce.Foundation.Catalogs;
	using VirtoCommerce.Foundation.Catalogs.Repositories;
	using VirtoCommerce.Foundation.Catalogs.Search;
	using VirtoCommerce.Foundation.Frameworks;
	using VirtoCommerce.Foundation.Frameworks.Extensions;
	using VirtoCommerce.Foundation.Search;
	using VirtoCommerce.MerchandisingModule.Web.Converters;
	using VirtoCommerce.MerchandisingModule.Web.Model;
	using foundation = VirtoCommerce.Foundation.Catalogs.Model;
	using moduleModel = VirtoCommerce.Domain.Catalog.Model;

    public class ItemBrowsingService : IItemBrowsingService
    {
        #region Cache Constants
        public const string SearchCacheKey = "S:{0}:{1}";
        public const string CatalogCacheKey = "C:C:{0}";
        public const string CategoryCacheKey = "C:CT:{0}:{1}";
        public const string CategoryItemRelationCacheKey = "C:CTIREL:{0}";
        public const string CategoryIdCacheKey = "C:CTID:{0}:{1}";
        public const string ChildCategoriesCacheKey = "C:CT:{0}:p:{1}";
        //public const string ItemCacheKey = "C:I:{0}:g:{1}";
        public const string ItemsCacheKey = "C:Is:{0}:g:{1}";
        public const string ItemsCodeCacheKey = "C:Isc:{0}:g:{1}";
        public const string ItemsSearchCacheKey = "C:Is:{0}:{1}";
        public const string ItemsQueryCacheKey = "C:Is:{0}";
        public const string PriceListCacheKey = "C:PL:{0}";
        public const string ItemVariationsCacheKey = "C:V:{0}";
        public const string ItemVariationParentsCacheKey = "C:VP:{0}";
        public const string ItemInvetoriesCacheKey = "C:INV:{0}:{1}";

        public const string PricesCacheKey = "C:P:{0}";
        public const string ItemPricesCacheKey = "C:P:{0}:{1}";
        public const string PricelistAssignmentCacheKey = "C:PLA:{0}";

        public const string PropertiesCacheKey = "C:PR:{0}";
        public const string PropertyValueCacheKey = "C:PRV:{0}:{1}:{2}:{3}";
        #endregion

        #region Private Variables
        private readonly bool _isEnabled;
        private readonly ICacheRepository _cacheRepository;
        private readonly ISearchProvider _searchService;
        private readonly ISearchConnection _searchConnection;
        private readonly IAssetUrl _assetUrl;
        private readonly IItemService _itemService;
        private readonly Func<IFoundationCatalogRepository> _catalogRepositoryFactory;

        #endregion

        public ItemBrowsingService(
            IItemService itemService,
            Func<IFoundationCatalogRepository> catalogRepositoryFactory,
            ISearchProvider searchService,
            ICacheRepository cacheRepository,
            IAssetUrl assetUrl = null,
            ISearchConnection searchConnection = null)
        {
            _searchService = searchService;
            _catalogRepositoryFactory = catalogRepositoryFactory;
            _cacheRepository = cacheRepository;
            _searchConnection = searchConnection;
            _itemService = itemService;
            _assetUrl = assetUrl;
            _isEnabled = CatalogConfiguration.Instance.Cache.IsEnabled;
        }

        public ProductSearchResult SearchItems(CatalogItemSearchCriteria criteria,
            moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemSmall)
        {
            CatalogItemSearchResults results;
            var items = Search<moduleModel.CatalogProduct>(criteria, true, out results, responseGroup);
            var catalogItems = new List<Product>();

            // go through items
            foreach (var item in items)
            {
                var searchTags = results.Items[item.Id.ToLower()];

                var currentOutline = GetItemOutlineUsingContext(searchTags[criteria.OutlineField].ToString(), criteria.Catalog);
                var catalogItem = item.ToWebModel(_assetUrl) as Product;
                catalogItem.Outline = currentOutline;

                int reviewTotal;
                if (searchTags.ContainsKey(criteria.ReviewsTotalField)
                    && int.TryParse(searchTags[criteria.ReviewsTotalField].ToString(), out reviewTotal))
                {
                    catalogItem.ReviewsTotal = reviewTotal;
                }
                double reviewAvg;
                if (searchTags.ContainsKey(criteria.ReviewsAverageField)
                    && double.TryParse(searchTags[criteria.ReviewsAverageField].ToString(), out reviewAvg))
                {
                    catalogItem.Rating = reviewAvg;
                }

                //catalogItem.AddLink(new Link("self", String.Format("{0}", StripCatalogFromOutline(currentOutline, criteria.Catalog)), "view"));
                catalogItems.Add(catalogItem);
            }

            var response = new ProductSearchResult();

            response.Items.AddRange(catalogItems);
            response.TotalCount = results.TotalCount;

            //TODO need better way to find applied filter values
            var appliedFilters = criteria.CurrentFilters.SelectMany(x => x.GetValues()).Select(x=>x.Id).ToArray();
            response.Facets = results.FacetGroups.Select(g => g.ToWebModel(appliedFilters)).ToArray();
            return response;
        }

        /*

        public T GetItem<T>(string productCode, ItemResponseGroups responseGroup) where T : CatalogItem
        {
            var item = GetItem<foundation.Product>(productCode, responseGroup, useCode: true);
            return item != null ? item.AsProduct(_assetUrl) as T : null;
        }
         * */


        /// <summary>
        /// Gets the context item outline based on what customer is browsing
        /// </summary>
        /// <param name="itemOutline"></param>
        /// <returns></returns>
        private string GetItemOutlineUsingContext(string itemOutline, string prefixOutline)
        {
            if (String.IsNullOrEmpty(itemOutline))
            {
                return String.Empty;
            }

            //var session = UserHelper.CustomerSession;
            //var prefixOutline = String.Format("{0}/{1}", session.CatalogId, session.CategoryId);

            var outline = itemOutline.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.StartsWith(prefixOutline, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

            return outline;
        }

        private string StripCatalogFromOutline(string outline, string catalog)
        {
            if (String.IsNullOrEmpty(outline))
            {
                return null;
            }


            if (outline.Length > catalog.Length + 1)
                return outline.Substring(catalog.Length + 1);

            return String.Empty;
        }

        private IEnumerable<T> Search<T>(CatalogItemSearchCriteria criteria,
            bool cacheResults, out CatalogItemSearchResults results,
            moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemSmall) where T : moduleModel.CatalogProduct
        {
            var items = new List<T>();
            var itemsOrderedList = new List<string>();

            int foundItemCount;
            var dbItemCount = 0;
            var searchRetry = 0;

            var myCriteria = criteria.Clone();

            do
            {
                // Search using criteria, it will only return IDs of the items
                results = SearchItems(myCriteria, cacheResults);
                searchRetry++;

                //Get only new found itemIds
                var uniqueKeys = results.Items.Keys.Except(itemsOrderedList).ToArray();
                foundItemCount = uniqueKeys.Length;

                if (!results.Items.Any()) continue;

                itemsOrderedList.AddRange(uniqueKeys);

                // Now load items from repository
                var currentItems = GetItems(uniqueKeys.ToArray(), cacheResults, responseGroup);

                var orderedList = currentItems.OrderBy(i => itemsOrderedList.IndexOf(i.Id));
                items.AddRange((IEnumerable<T>)orderedList);
                dbItemCount = currentItems.Length;

                //If some items where removed and search is out of sync try getting extra items
                if (foundItemCount > dbItemCount)
                {
                    //Retrieve more items to fill missing gap
                    myCriteria.RecordsToRetrieve += (foundItemCount - dbItemCount);
                }
            } while (foundItemCount > dbItemCount && results.Items.Any() && searchRetry <= 3 &&
                (myCriteria.RecordsToRetrieve + myCriteria.StartingRecord) < results.TotalCount);

            return items;
        }

        private CatalogItemSearchResults SearchItems(CatalogItemSearchCriteria criteria, bool useCache)
        {
            var scope = _searchConnection.Scope;

            return Helper.Get(
                CacheHelper.CreateCacheKey(Constants.CatalogCachePrefix, string.Format(SearchCacheKey, scope, criteria.CacheKey)),
                () => SearchItems(scope, criteria),
                SearchConfiguration.Instance.Cache.FiltersTimeout,
                _isEnabled);
        }

        public CatalogItemSearchResults SearchItems(string scope, CatalogItemSearchCriteria criteria)
        {
            var results = _searchService.Search(scope, criteria) as SearchResults;
            var items = results.GetKeyAndOutlineFieldValueMap<string>();

            var r = new CatalogItemSearchResults(criteria, items, results);
            return r;
        }

        #region Item Methods

        /*
        /// <summary>
        /// Gets the item. Additionally filters by catalog
        /// </summary>
        /// <param name="id">The id of item.</param>
        /// <param name="responseGroup">The response group.</param>
        /// <param name="useCache">if set to <c>true</c> uses cache.</param>
        /// <returns></returns>
        public T GetItem<T>(string id, ItemResponseGroups responseGroup, bool useCode = false, bool useCache = true) where T : foundation.Item
        {
            var items = useCode ? this.GetItemsByCode<T>(new[] { id }, useCache, responseGroup) : this.GetItems<T>(new[] { id }, useCache, responseGroup);

            if (items == null || !items.Any()) return null;

            return items[0];
        }
         * */

        private moduleModel.CatalogProduct[] GetItems(string[] ids, bool useCache = true,
    moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemSmall)
        {
            return Helper.Get(
                CacheHelper.CreateCacheKey(Constants.CatalogCachePrefix, string.Format(ItemsCacheKey, CacheHelper.CreateCacheKey(ids), responseGroup)),
                () => (_itemService.GetByIds(ids, responseGroup)).ToArray(),
                CatalogConfiguration.Instance.Cache.ItemTimeout,
                _isEnabled && useCache);
        }

        /*
        private T[] GetItemsByCode<T>(string[] codes, bool useCache = true,
moduleModel.ItemResponseGroup responseGroup = moduleModel.ItemResponseGroup.ItemSmall) where T : foundation.Item
        {
            return Helper.Get(
                CacheHelper.CreateCacheKey(Constants.CatalogCachePrefix, string.Format(ItemsCacheKey, CacheHelper.CreateCacheKey(codes), responseGroup)),
                () => (ItemHelper.GetItemsByCode<T>(_catalogRepository, codes, responseGroup)).ToArray(),
                CatalogConfiguration.Instance.Cache.ItemTimeout,
                _isEnabled && useCache);
        }
         * */
        #endregion

        CacheHelper _cacheHelper;

        public CacheHelper Helper
        {
            get { return _cacheHelper ?? (_cacheHelper = new CacheHelper(_cacheRepository)); }
        }


        /*
        public ResponseCollection<Category> GetCategories(string outline, string language)
        {
            var catalogId = outline.Substring(0, outline.IndexOf("/", StringComparison.OrdinalIgnoreCase));
            var categories = GetCategoriesInternal(catalogId, outline);

            var categoryModels = categories.Select(x => x.AsCategory()).ToArray();
            var response = new ResponseCollection<Category>();

            response.Items.AddRange(categoryModels);
            response.TotalCount = categories.Count();
            return response;
        }

        private foundation.CategoryBase[] GetCategoriesInternal(string catalogId, string outline, bool useCache = true)
        {
            return Helper.Get(
                CacheHelper.CreateCacheKey(Constants.CatalogCachePrefix, string.Format(ChildCategoriesCacheKey, catalogId, outline)),
                () => CategoryHelper.GetCategories<foundation.CategoryBase>(_catalogRepository, catalogId, outline),
                CatalogConfiguration.Instance.Cache.CategoryCollectionTimeout, true);


        }
         * */
    }
}
