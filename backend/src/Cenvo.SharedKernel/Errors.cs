namespace Cenvo.SharedKernel;

/// <summary>Business rule violation — the API returns 400 with a Vietnamese message.</summary>
public class BusinessException(string message) : Exception(message);

/// <summary>Not found (or not in the current branch) — the API returns 404.</summary>
public class NotFoundException(string what, object? id) : Exception($"Không tìm thấy {what} '{id}'.");
