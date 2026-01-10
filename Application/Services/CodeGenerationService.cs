using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Application.Services;

public interface ICodeGenerationService
{
    Task<string> GenerateCodeAsync<TEntity>(
        IQueryable<TEntity> query,
        string prefix,
        Expression<Func<TEntity, string>> codeSelector,
        CancellationToken cancellationToken = default) where TEntity : class;
}

public class CodeGenerationService : ICodeGenerationService
{
    private readonly IRepository<Company, int> _companyRepository;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private const string CACHE_KEY = "CompanyCodeGenerationType";
    private const int CACHE_DURATION_MINUTES = 30;

    public CodeGenerationService(IRepository<Company, int> companyRepository, IMemoryCache cache)
    {
        _companyRepository = companyRepository;
        _cache = cache;
    }

    public async Task<string> GenerateCodeAsync<TEntity>(
        IQueryable<TEntity> query,
        string prefix,
        Expression<Func<TEntity, string>> codeSelector,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var codeGenerationType = await GetCodeGenerationTypeAsync(cancellationToken);

        if (codeGenerationType == ECodeGeneration.Auto)
        {
            return GenerateAutoCode(prefix);
        }

        return await GenerateDailyCountCodeAsync(query, prefix, codeSelector, cancellationToken);
    }

    private async Task<ECodeGeneration> GetCodeGenerationTypeAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(CACHE_KEY, out ECodeGeneration cachedType))
        {
            return cachedType;
        }

        // Query database and cache the result
        var company = await _companyRepository.Query()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => c.CodeGeneration)
            .FirstOrDefaultAsync(cancellationToken);

        var codeGenType = company;

        _cache.Set(CACHE_KEY, codeGenType, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

        return codeGenType;
    }

    private string GenerateAutoCode(string prefix)
    {
        var datePart = DateTime.UtcNow.ToString("yyMMdd");
        var uniquePart = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"{prefix}-{datePart}-{uniquePart}";
    }

    private async Task<string> GenerateDailyCountCodeAsync<TEntity>(
        IQueryable<TEntity> query,
        string prefix,
        Expression<Func<TEntity, string>> codeSelector,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        // Use semaphore to prevent race conditions
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var datePart = DateTime.UtcNow.ToString("yyMMdd");
            var searchPattern = $"{prefix}-{datePart}-";

            // Optimized query with pattern matching - using Expression for EF Core translation
            var lastCode = await query
                .AsNoTracking()
                .Select(codeSelector)
                .Where(code => code.StartsWith(searchPattern))
                .OrderByDescending(code => code)
                .FirstOrDefaultAsync(cancellationToken);

            int nextSequence = 1;

            if (!string.IsNullOrEmpty(lastCode))
            {
                // Use regex for safer parsing
                var match = Regex.Match(lastCode, @"-(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            return $"{prefix}-{datePart}-{nextSequence:D3}";
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // Method to clear cache when company settings change
    public void ClearCache()
    {
        _cache.Remove(CACHE_KEY);
    }
}
