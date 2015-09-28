using System;
using LightRail;
using NUnit.Framework;

namespace LightRail.Reflection
{
    [TestFixture]
    public class MessageMapperTests
    {
        IMessageMapper mapper;

        [SetUp]
        public void SetUp()
        {
            mapper = new MessageMapper(new MessageTypeConventions());
        }

        [Test]
        public void Interfaces_with_only_properties_should_be_mapped()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithProperties) });

            Assert.IsNotNull(mapper.GetMappedTypeFor(typeof(InterfaceWithProperties)));
        }

        [Test]
        public void Interface_should_be_created()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithProperties) });

            var result = mapper.CreateInstance<InterfaceWithProperties>(null);

            Assert.IsNotNull(result);
        }

        [Test]
        public void Interfaces_with_methods_should_be_ignored()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithMethods) });

            Assert.IsNull(mapper.GetMappedTypeFor(typeof(InterfaceWithMethods)));
        }


        [Test]
        public void Generated_type_should_preserve_namespace_to_make_it_easier_for_users_to_define_custom_conventions()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithProperties) });

            Assert.AreEqual(typeof(InterfaceWithProperties).Namespace, mapper.CreateInstance(typeof(InterfaceWithProperties)).GetType().Namespace);
        }
    }

    public interface InterfaceWithProperties : IMessage
    {
        string SomeProperty { get; set; }
    }

    public interface InterfaceWithMethods : IMessage
    {
        string SomeProperty { get; set; }
        void MethodOnInterface();
    }
}
