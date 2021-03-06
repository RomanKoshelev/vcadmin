﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtoCommerce.Foundation.Frameworks.Workflow.Services
{
	public class ObservableWorkflowService<T> : IWorkflowService, IObservable<T>
	{
		private readonly List<IObserver<T>> _observers;
		public ObservableWorkflowService()
		{
			_observers = new List<IObserver<T>>();
		}

		#region IWorkflowService Members

		public WorkflowResult RunWorkflow(string workflowName, Dictionary<string, object> parameters, object[] extensions)
		{
			WorkflowResult retVal = new WorkflowResult();
			var customerOrder = parameters.Values.OfType<T>().FirstOrDefault();
			if (customerOrder != null)
			{
				foreach (var observer in _observers)
				{
					observer.OnNext(customerOrder);
				}

				foreach (var observer in _observers)
				{
					observer.OnCompleted();
				}
			}
			return retVal;
		}

		#endregion

		#region IObservable<CustomerOrder> Members

		public IDisposable Subscribe(IObserver<T> observer)
		{
			_observers.Add(observer);
			return new Unsubscriber(_observers, observer);
		}

		#endregion

		private class Unsubscriber : IDisposable
		{
			private List<IObserver<T>> _observers;
			private IObserver<T> _observer;

			public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
			{
				this._observers = observers;
				this._observer = observer;
			}

			public void Dispose()
			{
				if (_observer != null && _observers.Contains(_observer))
					_observers.Remove(_observer);
			}
		}
	}
}
