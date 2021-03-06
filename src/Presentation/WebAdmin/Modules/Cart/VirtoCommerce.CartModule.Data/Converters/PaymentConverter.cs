﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Data.Model;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Foundation.Frameworks.Extensions;
using VirtoCommerce.Foundation.Money;
using Omu.ValueInjecter;
using System.Collections.ObjectModel;

namespace VirtoCommerce.CartModule.Data.Converters
{
	public static class PaymentConverter
	{
		public static Payment ToCoreModel(this PaymentEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			var retVal = new Payment();
			retVal.InjectFrom(entity);
			retVal.Currency = (CurrencyCodes)Enum.Parse(typeof(CurrencyCodes), entity.Currency);

			if (entity.Addresses != null && entity.Addresses.Any())
			{
				retVal.BillingAddress = entity.Addresses.First().ToCoreModel();
			}
			return retVal;
		}

		public static PaymentEntity ToEntity(this Payment payment)
		{
			if (payment == null)
				throw new ArgumentNullException("payment");

			var retVal = new PaymentEntity();
			retVal.InjectFrom(payment);

			retVal.Currency = payment.Currency.ToString();

			if (payment.BillingAddress != null)
			{
				retVal.Addresses = new ObservableCollection<AddressEntity>(new AddressEntity[] { payment.BillingAddress.ToEntity() });
			}
			return retVal;
		}


		/// <summary>
		/// Patch CatalogBase type
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		public static void Patch(this PaymentEntity source, PaymentEntity target)
		{
			if (target == null)
				throw new ArgumentNullException("target");

			//Simply properties patch
			target.Amount = source.Amount;

			if (source.GatewayCode != null)
				target.GatewayCode = source.GatewayCode;

			if (!source.Addresses.IsNullCollection())
			{
				source.Addresses.Patch(target.Addresses, new AddressComparer(), (sourceAddress, targetAddress) => sourceAddress.Patch(targetAddress));
			}
		}

	}

}
