// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeAttributeArgumentCollectionTests : CodeCollectionTestBase<CodeAttributeArgumentCollection, CodeAttributeArgument>
    {
        protected override CodeAttributeArgumentCollection Ctor() => new CodeAttributeArgumentCollection();
        protected override CodeAttributeArgumentCollection CtorArray(CodeAttributeArgument[] array) => new CodeAttributeArgumentCollection(array);
        protected override CodeAttributeArgumentCollection CtorCollection(CodeAttributeArgumentCollection collection) => new CodeAttributeArgumentCollection(collection);

        protected override int Count(CodeAttributeArgumentCollection collection) => collection.Count;

        protected override CodeAttributeArgument GetItem(CodeAttributeArgumentCollection collection, int index) => collection[index];
        protected override void SetItem(CodeAttributeArgumentCollection collection, int index, CodeAttributeArgument value) => collection[index] = value;

        protected override void AddRange(CodeAttributeArgumentCollection collection, CodeAttributeArgument[] array) => collection.AddRange(array);
        protected override void AddRange(CodeAttributeArgumentCollection collection, CodeAttributeArgumentCollection value) => collection.AddRange(value);

        protected override object Add(CodeAttributeArgumentCollection collection, CodeAttributeArgument obj) => collection.Add(obj);

        protected override void Insert(CodeAttributeArgumentCollection collection, int index, CodeAttributeArgument value) => collection.Insert(index, value);

        protected override void Remove(CodeAttributeArgumentCollection collection, CodeAttributeArgument value) => collection.Remove(value);

        protected override int IndexOf(CodeAttributeArgumentCollection collection, CodeAttributeArgument value) => collection.IndexOf(value);
        protected override bool Contains(CodeAttributeArgumentCollection collection, CodeAttributeArgument value) => collection.Contains(value);

        protected override void CopyTo(CodeAttributeArgumentCollection collection, CodeAttributeArgument[] array, int index) => collection.CopyTo(array, index);
    }
}
