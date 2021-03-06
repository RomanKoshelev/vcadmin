﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Foundation.Frameworks;
using VirtoCommerce.Foundation.Money;

namespace VirtoCommerce.Domain.Cart.Model
{
	public class ShipmentMethod : ValueObject<ShipmentMethod>
	{
		public string ShipmentMethodCode { get; set; }
		public string Name { get; set; }
		public string LogoUrl { get; set; }
		public CurrencyCodes Currency { get; set; }
		public Money Price { get; set; }
		public ICollection<Discount> Discounts { get; set; }
	}
}
