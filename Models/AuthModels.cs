namespace GeniusVendas.Api.Models;

public sealed record AuthenticatedUser(long UserId, long CompanyId, long SellerId, string SellerCode, string SellerName, string Username);
public sealed record SessionInfo(string Token, long UserId, long CompanyId, long SellerId, string SellerCode, string SellerName, DateTime ExpiresAtUtc);
