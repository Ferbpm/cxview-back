using CXView.Domain;

namespace CXView.Application;

public sealed class CompanyService(ICompanyRepository repository)
{
    public Task<IReadOnlyCollection<Company>> SearchAsync(CompanySearchFilter filter, CancellationToken ct) =>
        repository.SearchAsync(new CompanySearchFilter(
            NormalizeOptional(filter.Id, 36, "El identificador de empresa no es válido."),
            NormalizeOptional(filter.Name, 100, "El nombre de empresa no puede superar 100 caracteres."),
            NormalizeOptional(filter.Ruc, 11, "El RUC no puede superar 11 caracteres."),
            NormalizeStatus(filter.Status, optional: true)), ct);

    public Task<Company> CreateAsync(CreateCompanyRequest request, string userName, CancellationToken ct) =>
        repository.CreateAsync(RequiredName(request.Name), NormalizeOptional(request.Ruc, 11, "El RUC no puede superar 11 caracteres."), RequiredUserName(userName), ct);

    public Task<Company?> UpdateAsync(string companyId, UpdateCompanyRequest request, string userName, CancellationToken ct) =>
        repository.UpdateAsync(RequiredId(companyId), RequiredName(request.Name), NormalizeOptional(request.Ruc, 11, "El RUC no puede superar 11 caracteres."), NormalizeStatus(request.Status, optional: false)!, RequiredUserName(userName), ct);

    public Task<int> DeactivateAsync(string companyId, string userName, CancellationToken ct) =>
        repository.DeactivateAsync(RequiredId(companyId), RequiredUserName(userName), ct);

    private static string RequiredName(string value) => NormalizeOptional(value, 100, "El nombre de empresa no puede superar 100 caracteres.")
        ?? throw new ArgumentException("El nombre de la empresa es obligatorio.", nameof(value));

    private static string RequiredId(string value)
    {
        var normalized = NormalizeOptional(value, 36, "El identificador de empresa no es válido.");
        if (normalized is null || !Guid.TryParse(normalized, out _))
            throw new ArgumentException("El identificador de empresa no es válido.", nameof(value));
        return normalized;
    }

    private static string RequiredUserName(string value) => NormalizeOptional(value, 20, "El nombre de usuario no puede superar 20 caracteres.")
        ?? throw new ArgumentException("No se encontró el usuario autenticado.", nameof(value));

    private static string? NormalizeStatus(string? value, bool optional)
    {
        var normalized = NormalizeOptional(value, 1, "El estado debe ser A o I.");
        if (normalized is null && optional) return null;
        if (normalized is not "A" and not "I") throw new ArgumentException("El estado debe ser A o I.", nameof(value));
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string invalidMessage)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maximumLength) throw new ArgumentException(invalidMessage, nameof(value));
        return normalized;
    }
}
