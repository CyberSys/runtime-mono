// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource First<TSource>(this IEnumerable<TSource> source)
        {
            TSource first = source.TryGetFirst(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return first!;
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource first = source.TryGetFirst(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return first!;
        }

        [return: MaybeNull]
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source) =>
            source.TryGetFirst(out bool _);

        [return: MaybeNull]
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) =>
            source.TryGetFirst(predicate, out bool _);

        [return: MaybeNull]
        private static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                return partition.TryGetFirst(out found);
            }

            if (source is IList<TSource> list)
            {
                if (list.Count > 0)
                {
                    found = true;
                    return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        found = true;
                        return e.Current;
                    }
                }
            }

            found = false;
            return default!;
        }

        [return: MaybeNull]
        private static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (source is OrderedEnumerable<TSource> ordered)
            {
                return ordered.TryGetFirst(predicate, out found);
            }

            foreach (TSource element in source)
            {
                if (predicate(element))
                {
                    found = true;
                    return element;
                }
            }

            found = false;
            return default!;
        }
    }
}
