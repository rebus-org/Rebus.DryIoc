using System;
#if NETSTANDARD1_6
using System.Reflection;
#endif
using DryIoc;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;
using Container = DryIoc.Container;

namespace Rebus.DryIoc.Tests
{
    public class DryIocContainerAdapterFactory : IActivationContext
    {
        public IHandlerActivator CreateActivator(Action<IHandlerRegistry> configureHandlers, out IActivatedContainer container)
        {
            var dryContainer = CreateDryContainer(); 

            configureHandlers.Invoke(new HandlerRegistry(dryContainer));

            container = new ActivatedContainer(dryContainer);

            return new DryIocContainerAdapter(dryContainer);
        }

        public IBus CreateBus(Action<IHandlerRegistry> configureHandlers, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
        {
            var dryContainer = CreateDryContainer();

            configureHandlers.Invoke(new HandlerRegistry(dryContainer));

            container = new ActivatedContainer(dryContainer);

            return configureBus(Configure.With(new DryIocContainerAdapter(dryContainer))).Start();
        }

        public IHandlerActivator CreateContainerAdapter(Action<IHandlerRegistry> configureHandlers)
        {
            var container = CreateDryContainer();
            configureHandlers.Invoke(new HandlerRegistry(container));
            return new DryIocContainerAdapter(container);
        }

        Container CreateDryContainer()
        {
            // allows to register IDisposable transients
            return new Container(rules => rules.WithoutThrowOnRegisteringDisposableTransient());
        }

        class ActivatedContainer : IActivatedContainer
        {
            readonly Container _container;

            public ActivatedContainer(Container container)
            {
                _container = container;
            }

            public void Dispose() => _container.Dispose();

            public IBus ResolveBus() => _container.Resolve<IBus>();
        }

        class HandlerRegistry : IHandlerRegistry
        {
            readonly Container _container;

            public HandlerRegistry(Container container) => _container = container;

            public IHandlerRegistry Register<THandler>() where THandler : class, IHandleMessages
            {
#if NETSTANDARD1_6
            _container.RegisterMany<THandler>(serviceTypeCondition: type =>
            {
                var typeInfo = type.GetTypeInfo();

                return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IHandleMessages<>);
            });
#else
                _container.RegisterMany<THandler>(serviceTypeCondition: type =>
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
#endif

                return this;
            }
        }
    }
}
