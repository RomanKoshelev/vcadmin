﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;
using VirtoCommerce.Foundation.Money;
using VirtoCommerce.Foundation.Frameworks;
using System.Collections.ObjectModel;
using VirtoCommerce.Foundation.Frameworks.ConventionInjections;
using VirtoCommerce.Foundation.Frameworks.Extensions;
using foundationModel = VirtoCommerce.Foundation.Customers.Model;
using coreModel = VirtoCommerce.Domain.Customer.Model;

namespace VirtoCommerce.CustomerModule.Data.Converters
{
	public static class ContactConverter
	{
		/// <summary>
		/// Converting to model type
		/// </summary>
		/// <param name="catalogBase"></param>
		/// <returns></returns>
		public static coreModel.Contact ToCoreModel(this foundationModel.Contact dbEntity)
		{
			if (dbEntity == null)
				throw new ArgumentNullException("dbEntity");

			var retVal = new coreModel.Contact();
			retVal.InjectFrom(dbEntity);
			retVal.Id = dbEntity.MemberId;

			retVal.CreatedDate = dbEntity.Created.Value;
			retVal.ModifiedDate = dbEntity.LastModified;

			retVal.Addresses = dbEntity.Addresses.Select(x => x.ToCoreModel()).ToList();
			retVal.Emails = dbEntity.Emails.Select(x => x.Address).ToList();
			retVal.Notes = dbEntity.Notes.Select(x => x.ToCoreModel()).ToList();
			retVal.Phones = dbEntity.Phones.Select(x => x.Number).ToList();
			retVal.Properties = dbEntity.ContactPropertyValues.Select(x => x.ToCoreModel()).ToList();
			return retVal;

		}


		public static foundationModel.Contact ToFoundation(this coreModel.Contact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			var retVal = new foundationModel.Contact();

			retVal.InjectFrom(contact);

			if (contact.Id != null)
			{
				retVal.MemberId = contact.Id;
			}

			retVal.Phones = new NullCollection<foundationModel.Phone>();
			if(contact.Phones != null)
			{
				retVal.Phones = new ObservableCollection<foundationModel.Phone>(contact.Phones.Select(x => new foundationModel.Phone
				{
					 Number = x,
					 MemberId = contact.Id
				}));
			}

			retVal.Emails = new NullCollection<foundationModel.Email>();
			if (contact.Emails != null)
			{
				retVal.Emails = new ObservableCollection<foundationModel.Email>(contact.Emails.Select(x => new foundationModel.Email
				{
					Address = x,
					MemberId = contact.Id
				}));
			}
			retVal.ContactPropertyValues = new NullCollection<foundationModel.ContactPropertyValue>();
			if (contact.Properties != null)
			{
				retVal.ContactPropertyValues = new ObservableCollection<foundationModel.ContactPropertyValue>(contact.Properties.Select(x => x.ToFoundation()));
				foreach (var property in retVal.ContactPropertyValues)
				{
					property.ContactId = contact.Id;
				}
			}
			retVal.Addresses = new NullCollection<foundationModel.Address>();
			if (contact.Addresses != null)
			{
				retVal.Addresses = new ObservableCollection<foundationModel.Address>(contact.Addresses.Select(x => x.ToFoundation()));
				foreach (var address in retVal.Addresses)
				{
					address.MemberId = contact.Id;
				}
			}

			retVal.Notes = new NullCollection<foundationModel.Note>();
			if (contact.Notes != null)
			{
				retVal.Notes = new ObservableCollection<foundationModel.Note>(contact.Notes.Select(x => x.ToFoundation()));
				foreach (var note in retVal.Notes)
				{
					note.MemberId = contact.Id;
				}
			}
			return retVal;
		}

		/// <summary>
		/// Patch changes
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		public static void Patch(this foundationModel.Contact source, foundationModel.Contact target)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			var patchInjection = new PatchInjection<foundationModel.Contact>(x => x.BirthDate, x => x.DefaultLanguage,
																		   x => x.FullName, x => x.Salutation,
																		   x => x.TimeZone);
			target.InjectFrom(patchInjection, source);
		
			if (!source.Phones.IsNullCollection())
			{
				var phoneComparer = AnonymousComparer.Create((foundationModel.Phone x) => x.Number);
				source.Phones.Patch(target.Phones, phoneComparer, (sourcePhone, targetPhone) => targetPhone.Number = sourcePhone.Number);
			}
			if (!source.Emails.IsNullCollection())
			{
				var addressComparer = AnonymousComparer.Create((foundationModel.Email x) => x.Address);
				source.Emails.Patch(target.Emails, addressComparer, (sourceEmail, targetEmail) => targetEmail.Address = sourceEmail.Address);
			}
			if (!source.Addresses.IsNullCollection())
			{
				source.Addresses.Patch(target.Addresses, new AddressComparer(), (sourceAddress, targetAddress) => sourceAddress.Patch(targetAddress));
			}
			if (!source.ContactPropertyValues.IsNullCollection())
			{
				var propertyComparer = AnonymousComparer.Create((foundationModel.ContactPropertyValue x) => x.PropertyValueId);
				source.ContactPropertyValues.Patch(target.ContactPropertyValues, propertyComparer, (sourceProperty, targetProperty) => sourceProperty.Patch(targetProperty));
			}
			if (!source.Notes.IsNullCollection())
			{
				var noteComparer = AnonymousComparer.Create((foundationModel.Note x) => x.NoteId);
				source.Notes.Patch(target.Notes, noteComparer, (sourceNote, targetNote) => sourceNote.Patch(targetNote));
			}
		} 


	}
}
