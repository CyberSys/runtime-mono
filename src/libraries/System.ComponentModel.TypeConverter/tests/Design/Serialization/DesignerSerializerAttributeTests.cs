// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class DesignerSerializerAttributeTests
    {
        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(DefaultSerializationProviderAttributeTests), typeof(DesignerSerializerAttribute))]
        public void Ctor_SerializerType_BaseSerializerType(Type serializerType, Type baseSerializerType)
        {
            var attribute = new DesignerSerializerAttribute(serializerType, baseSerializerType);
            Assert.Equal(serializerType.AssemblyQualifiedName, attribute.SerializerTypeName);
            Assert.Equal(baseSerializerType.AssemblyQualifiedName, attribute.SerializerBaseTypeName);
        }

        [Fact]
        public void Ctor_NullSerializerType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>(() => new DesignerSerializerAttribute((Type)null, typeof(int)));
        }

        [Fact]
        public void Ctor_NullBaseSerializerType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("baseSerializerType", () => new DesignerSerializerAttribute(typeof(int), (Type)null));
            AssertExtensions.Throws<ArgumentNullException, NullReferenceException>("baseSerializerType", () => new DesignerSerializerAttribute("int", (Type)null));
        }

        [Theory]
        [InlineData(null, typeof(int))]
        [InlineData("SerializerTypeName", typeof(DesignerSerializerAttribute))]
        public void Ctor_SerializerTypeName_BaseSerializerType(string serializerTypeName, Type baseSerializerType)
        {
            var attribute = new DesignerSerializerAttribute(serializerTypeName, baseSerializerType);
            Assert.Equal(serializerTypeName, attribute.SerializerTypeName);
            Assert.Equal(baseSerializerType.AssemblyQualifiedName, attribute.SerializerBaseTypeName);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("SerializerTypeName", "BaseSerializerTypeName")]
        public void Ctor_SerializerTypeName_BaseSerializerType(string serializerTypeName, string baseSerializerTypeName)
        {
            var attribute = new DesignerSerializerAttribute(serializerTypeName, baseSerializerTypeName);
            Assert.Equal(serializerTypeName, attribute.SerializerTypeName);
            Assert.Equal(baseSerializerTypeName, attribute.SerializerBaseTypeName);
        }

        public static IEnumerable<object[]> TypeId_TestData()
        {
            yield return new object[] { "BaseSerializerTypeName", "System.ComponentModel.Design.Serialization.DesignerSerializerAttributeBaseSerializerTypeName" };
            yield return new object[] { "BaseSerializerTypeName,Other", "System.ComponentModel.Design.Serialization.DesignerSerializerAttributeBaseSerializerTypeName" };
            yield return new object[] { string.Empty, "System.ComponentModel.Design.Serialization.DesignerSerializerAttribute" };
            yield return new object[] { null, "System.ComponentModel.Design.Serialization.DesignerSerializerAttribute" };
        }

        [Theory]
        [MemberData(nameof(TypeId_TestData))]
        public void TypeId_Get_ReturnsExcepted(string serializerBaseTypeName, object expected)
        {
            var attribute = new DesignerSerializerAttribute("SerializerType", serializerBaseTypeName);
            Assert.Equal(expected, attribute.TypeId);
            Assert.Same(attribute.TypeId, attribute.TypeId);
        }
    }
}
