using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.DryIoc.Tests
{
    [TestFixture]
    public class DryIocAssumptions
    {
        [Test]
        public void RegisterWorks()
        {
            var factory = new DryIocContainerAdapterFactory();
            var activator = factory.CreateContainerAdapter(r =>
            {
                r.Register<SomeHandler>();
                r.Register<AnotherHandler>();

            });

            using (var scope = new RebusTransactionScope())
            {
                const string stringMessage = "bimse";
                
                var handlers = activator.GetHandlers(stringMessage, scope.TransactionContext).Result.ToList();
                
                Assert.That(handlers.Count, Is.EqualTo(2));
            }
        }

        class SomeHandler : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new NotImplementedException();
            }
        }

        class AnotherHandler : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new NotImplementedException();
            }
        }

    }
}
