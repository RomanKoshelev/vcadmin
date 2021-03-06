﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Data.Converters;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Foundation.Frameworks;
using VirtoCommerce.Foundation.Frameworks.Caching;
using foundation = VirtoCommerce.Foundation.Catalogs.Model;
using module = VirtoCommerce.Domain.Catalog.Model;

namespace VirtoCommerce.CatalogModule.Data.Services
{
	public class CatalogServiceImpl : ModuleServiceBase,  ICatalogService
	{
		private readonly Func<IFoundationCatalogRepository> _catalogRepositoryFactory;
		private readonly CacheManager _cacheManager;
		public CatalogServiceImpl(Func<IFoundationCatalogRepository> catalogRepositoryFactory, CacheManager cacheManager)
		{
			_catalogRepositoryFactory = catalogRepositoryFactory;
			_cacheManager = cacheManager;
		}

		#region ICatalogService Members

		public module.Catalog GetById(string catalogId)
		{
			module.Catalog retVal = null;
			using (var repository = _catalogRepositoryFactory())
			{
				var dbCatalogBase = repository.GetCatalogById(catalogId);
				retVal = dbCatalogBase.ToModuleModel();
			}
			return retVal;
		}

		public module.Catalog Create(module.Catalog catalog)
		{
			var dbCatalog = catalog.ToFoundation();
			module.Catalog retVal = null;
			using (var repository = _catalogRepositoryFactory())
			{
				repository.Add(dbCatalog);
				CommitChanges(repository);
			}
			retVal = GetById(dbCatalog.CatalogId);
			return retVal;
		}

		public void Update(module.Catalog[] catalogs)
		{
			using (var repository = _catalogRepositoryFactory())
			using (var changeTracker = base.GetChangeTracker(repository))
			{
				foreach (var catalog in catalogs)
				{
					var dbCatalog = repository.GetCatalogById(catalog.Id);
					if (dbCatalog == null)
					{
						throw new NullReferenceException("dbCatalog");
					}
					var dbCatalogChanged = catalog.ToFoundation();

					changeTracker.Attach(dbCatalog);
					dbCatalogChanged.Patch(dbCatalog);
				}

				CommitChanges(repository);
			}
		}

		public void Delete(string[] catalogIds)
		{
			using (var repository = _catalogRepositoryFactory())
			{
				foreach (var catalogId in catalogIds)
				{
					var dbCatalog = repository.GetCatalogById(catalogId);
					repository.Remove(dbCatalog);
				}
				CommitChanges(repository);
			}
		}

		#endregion
		
	}
	
}
