// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class UrlMembershipCondition : ISecurityEncodable, IMembershipCondition, ISecurityPolicyEncodable
    {
        public UrlMembershipCondition(string url) { }
        public string Url { get; set; }
        public bool Check(Evidence evidence) { return false; }
        public IMembershipCondition Copy() { return default(IMembershipCondition); }
        public override bool Equals(object obj) => base.Equals(obj);
        public void FromXml(SecurityElement e) { }
        public void FromXml(SecurityElement e, PolicyLevel level) { }
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
        public SecurityElement ToXml() { return default(SecurityElement); }
        public SecurityElement ToXml(PolicyLevel level) { return default(SecurityElement); }
    }
}
