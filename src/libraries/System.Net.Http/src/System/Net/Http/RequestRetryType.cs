// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    /// <summary>
    /// Used with <see cref="HttpRequestException"/> to indicate if a request is safe to retry.
    /// </summary>
    internal enum RequestRetryType
    {
        NoRetry,
        RetryOnSameOrNextProxy,
        RetryOnNextProxy
    }
}
