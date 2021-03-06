﻿namespace VirtoCommerce.ApiClient.Caching
{
    internal class CacheEntry
    {
        private readonly object _Lock;

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <value>The lock.</value>
        public object Lock
        {
            get { return _Lock; }
        }

        internal CacheEntry()
        {
            _Lock = new object();
        }
    }

}
