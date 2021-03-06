using System;
using System.Collections.Generic;
using Inforigami.Regalo.Interfaces;
using NUnit.Framework;

namespace Inforigami.Regalo.Core.Tests.Unit
{
    [TestFixture]
    public class TypeInspectorTests
    {
        [Test]
        [TestCase(null,                                                       new Type[] { })]
        [TestCase(typeof(object),                                             new[] { typeof(object)  })]
        [TestCase(typeof(ClassWithNoSpecificBase),                            new[] { typeof(object), typeof(ClassWithNoSpecificBase)  })]
        [TestCase(typeof(ClassNotDirectlyInheritingObject),                   new[] { typeof(object), typeof(ClassWithNoSpecificBase), typeof(ClassNotDirectlyInheritingObject)                 })]
        [TestCase(typeof(ClassWithNoSpecificBaseThatImplementsAnInterface),   new[] { typeof(object), typeof(IAmAnInterface),          typeof(ClassWithNoSpecificBaseThatImplementsAnInterface) })]
        [TestCase(typeof(VerySpecificSuperClass),                             new[] { typeof(object), typeof(ClassWithNoSpecificBase), typeof(ClassNotDirectlyInheritingObject),                typeof(VerySpecificSuperClass)                             })]
        [TestCase(typeof(ClassWithNoSpecificBaseThatImplementsTwoInterfaces), new[] { typeof(object), typeof(IAmAnInterface),          typeof(IAmAnInterfaceToo),                               typeof(ClassWithNoSpecificBaseThatImplementsTwoInterfaces) })]
        [TestCase(typeof(IAmAnInterfaceThatExtendsTwoInterfaces), new[] { typeof(IAmAnInterface),          typeof(IAmAnInterfaceToo),                               typeof(IAmAnInterfaceThatExtendsTwoInterfaces) })]
        [TestCase(typeof(SimpleEvent), new[] { typeof(object), typeof(IMessage), typeof(Message), typeof(IEvent), typeof(Event), typeof(SimpleEventBase), typeof(SimpleEvent) })]
        public void GivenAType_WhenSearchingForTypeHierarchy_ShouldReturnBaseTypesAndInputType(Type input, IEnumerable<Type> expectedResult)
        {
            var inspector = new TypeInspector();

            var result = inspector.GetTypeHierarchy(input);

            CollectionAssert.AreEqual(expectedResult, result);
        }
    }

    public class ClassWithNoSpecificBaseThatImplementsTwoInterfaces : IAmAnInterface, IAmAnInterfaceToo
    { }

    public class ClassWithNoSpecificBaseThatImplementsAnInterface : IAmAnInterface
    { }

    public interface IAmAnInterface
    { }

    public interface IAmAnInterfaceToo
    { }

    public interface IAmAnInterfaceThatExtendsTwoInterfaces : IAmAnInterface, IAmAnInterfaceToo
    { }

    public class VerySpecificSuperClass : ClassNotDirectlyInheritingObject
    { }

    public class ClassNotDirectlyInheritingObject : ClassWithNoSpecificBase
    { }

    public class ClassWithNoSpecificBase
    { }
}
