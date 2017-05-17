using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DryIoc;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.DryIoc
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that uses DryIoC to get handler instances
    /// </summary>
    public class DryIocContainerAdapter : IContainerAdapter
    {
        readonly IContainer _container;

        /// <summary>
        /// Constructs the adapter, using the specified container
        /// </summary>
        public DryIocContainerAdapter(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlerInstances = _container.Resolve<IList<IHandleMessages<TMessage>>>();

            transactionContext.OnDisposed(() =>
            {
                handlerInstances
                    .OfType<IDisposable>()
                    .ForEach(disposable =>
                    {
                        disposable.Dispose();
                    });
            });

            return handlerInstances;
        }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            if (_container.IsRegistered<IBus>())
            {
                throw new InvalidOperationException("This container instance already has an IBus registration. If you want to host multiple Rebus instance in a single process, please do so in separate container instances.");
            }

            _container.Register(Made.Of(() => GetCurrentMessageContext()));
            _container.Register(typeof(ISyncBus), new DelegateFactory(r => r.Resolve<IBus>().Advanced.SyncBus));

            _container.RegisterInstance(bus);
        }

        /// <summary>
        /// Returns the current message context and ensures it is not null
        /// </summary>
        /// <returns>IMessageContext</returns>
        internal static IMessageContext GetCurrentMessageContext()
        {
            var currentMessageContext = MessageContext.Current;
            if (currentMessageContext == null)
            {
                throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
            }
            return currentMessageContext;
        }
    }
}
