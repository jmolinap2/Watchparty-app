namespace WatchParty.Api.Auditing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableRequestAuditingAttribute : Attribute;
