using System;
using NUnit.Framework;

namespace LightRail.Client.Reflection
{
    [TestFixture]
    public class ReflectionMessageMapperTests
    {
        IMessageMapper mapper;

        [SetUp]
        public void SetUp()
        {
            mapper = new ReflectionMessageMapper();
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

            var result = mapper.CreateInstance<InterfaceWithProperties>();

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

        [Test]
        public void Generated_Concrete_Type_Should_Map_To_Interface()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithProperties) });

            var classType = mapper.GetMappedTypeFor(typeof(InterfaceWithProperties));

            Assert.AreEqual(typeof(InterfaceWithProperties), mapper.GetMappedTypeFor(classType));
        }
    }

    public interface InterfaceWithProperties
    {
        string SomeProperty { get; set; }
    }

    public interface InterfaceWithMethods
    {
        string SomeProperty { get; set; }
        void MethodOnInterface();
    }
}
