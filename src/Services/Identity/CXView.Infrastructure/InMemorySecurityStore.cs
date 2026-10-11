using System.Collections.Concurrent;
using CXView.Application;
using CXView.Domain;
using Microsoft.Extensions.Logging;

namespace CXView.Infrastructure;

public sealed class InMemorySecurityStore(IPasswordHasher passwordHasher) : IUserRepository, IFinePermissionRepository, ISessionRepository, IOtpRepository, ISecurityAdminRepository, IUserSiteRepository, IUserOperationTypeRepository, IUserBusinessUnitRepository, IUserModuleRepository, IUserMenuRepository, IUserSubmenuRepository, ICompanyRepository, IIntegratedLogisticsRepository, ISecurityMasterRepository, ISecurityMasterUserRepository, IUserNavigationAssignmentRepository, IUserAccessRepository, IFormOptionMasterRepository, IUserProfileRepository
{
    private readonly ConcurrentDictionary<Guid, UserAccount> _users = new();
    private readonly ConcurrentDictionary<Guid, AuthSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, OtpChallenge> _otps = new();
    private readonly ConcurrentDictionary<Guid, SecurityProfile> _profiles = new();
    private readonly ConcurrentDictionary<Guid, SecurityPermission> _permissions = new();
    private readonly ConcurrentDictionary<string, Company> _companies = new();
    private readonly ConcurrentDictionary<Guid, UserProfile> _userProfiles = new();

    public void SeedDevelopmentUser()
    {
        if (_users.Count != 0) return;
        var user = new UserAccount(Guid.NewGuid(), "admin", "admin@localhost", passwordHasher.Hash("ChangeMe-123!"), true);
        _users[user.Id] = user;
        foreach (var code in new[] { Permissions.SecurityAdmin, Permissions.UsersRead, Permissions.UsersWrite })
        { var permission = new SecurityPermission(Guid.NewGuid(), code, code); _permissions[permission.Id] = permission; }
    }

    public Task<UserAccount?> FindByUserNameAsync(string value, CancellationToken ct)
    {
        var user = _users.Values.FirstOrDefault(x =>
            x.UserName.Equals(value, StringComparison.OrdinalIgnoreCase) || x.Id.ToString().Equals(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }
    public Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(_users.GetValueOrDefault(userId));
    public Task<bool> HasPermissionAsync(Guid userId, string code, CancellationToken ct) => Task.FromResult(_users.ContainsKey(userId));
    public Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<string>>([Permissions.SecurityAdmin, Permissions.UsersRead, Permissions.UsersWrite]);
    public Task CreateAsync(AuthSession session, CancellationToken ct) { _sessions[session.Id] = session; return Task.CompletedTask; }
    public Task<bool> IsActiveAsync(Guid id, Guid userId, CancellationToken ct) => Task.FromResult(_sessions.TryGetValue(id, out var s) && s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow);
    public Task<bool> RenewAsync(Guid id, Guid userId, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken ct)
    {
        if (!_sessions.TryGetValue(id, out var session) || session.UserId != userId || session.IsRevoked || session.ExpiresAt <= now) return Task.FromResult(false);
        _sessions[id] = session with { ExpiresAt = expiresAt }; return Task.FromResult(true);
    }
    public Task RevokeAsync(Guid id, Guid userId, CancellationToken ct) { if (_sessions.TryGetValue(id, out var s) && s.UserId == userId) _sessions[id] = s with { IsRevoked = true }; return Task.CompletedTask; }
    public Task SaveAsync(OtpChallenge otp, CancellationToken ct) { _otps[otp.Id] = otp; return Task.CompletedTask; }
    public Task<OtpChallenge?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(_otps.GetValueOrDefault(id));
    public Task<bool> ConsumeAsync(Guid id, CancellationToken ct)
    {
        while (_otps.TryGetValue(id, out var current))
        {
            if (current.IsConsumed) return Task.FromResult(false);
            if (_otps.TryUpdate(id, current with { IsConsumed = true }, current)) return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
    public Task<IReadOnlyCollection<UserSite>> GetByUserNameAsync(string userName, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<UserSite>>(Array.Empty<UserSite>());
    Task<IReadOnlyCollection<UserOperationType>> IUserOperationTypeRepository.GetByUserNameAsync(string userName, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<UserOperationType>>(Array.Empty<UserOperationType>());
    Task<IReadOnlyCollection<UserBusinessUnit>> IUserBusinessUnitRepository.GetByUserNameAsync(string userName, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<UserBusinessUnit>>(Array.Empty<UserBusinessUnit>());
    Task<IReadOnlyCollection<UserModule>> IUserModuleRepository.GetByUserNameAsync(string userName, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<UserModule>>(Array.Empty<UserModule>());
    Task<IReadOnlyCollection<UserMenu>> IUserMenuRepository.GetByUserNameAndModuleIdAsync(string userName, string moduleId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<UserMenu>>(Array.Empty<UserMenu>());
    Task<IReadOnlyCollection<UserSubmenu>> IUserSubmenuRepository.GetByUserNameAndMenuIdAsync(string userName, string menuId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<UserSubmenu>>(Array.Empty<UserSubmenu>());
    public Task<IReadOnlyCollection<Company>> SearchAsync(CompanySearchFilter filter, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<Company>>(_companies.Values.Where(company => company.IsRecordActive
            && (filter.Id is null || company.Id == filter.Id)
            && (filter.Name is null || company.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase))
            && (filter.Ruc is null || company.Ruc == filter.Ruc)
            && (filter.Status is null || company.Status == filter.Status)).OrderBy(company => company.Name).ToArray());
    public Task<Company> CreateAsync(string companyName, string? ruc, string userName, CancellationToken ct)
    {
        var company = new Company(Guid.NewGuid().ToString(), companyName, ruc, "A", true, userName, DateTime.UtcNow, null, null);
        _companies[company.Id] = company;
        return Task.FromResult(company);
    }
    public Task<Company?> UpdateAsync(string companyId, string companyName, string? ruc, string status, string userName, CancellationToken ct)
    {
        if (!_companies.TryGetValue(companyId, out var company) || !company.IsRecordActive) return Task.FromResult<Company?>(null);
        var updated = company with { Name = companyName, Ruc = ruc, Status = status, UpdatedBy = userName, UpdatedAt = DateTime.UtcNow };
        _companies[companyId] = updated;
        return Task.FromResult<Company?>(updated);
    }
    public Task<int> DeactivateAsync(string companyId, string userName, CancellationToken ct)
    {
        if (!_companies.TryGetValue(companyId, out var company) || !company.IsRecordActive) return Task.FromResult(0);
        _companies[companyId] = company with { Status = "I", IsRecordActive = false, UpdatedBy = userName, UpdatedAt = DateTime.UtcNow };
        return Task.FromResult(1);
    }
    public Task<IReadOnlyCollection<UserProfile>> ListProfilesAsync(Guid? profileId, Guid? applicationId, string? name, string? status, CancellationToken ct) =>
        Task.FromResult<IReadOnlyCollection<UserProfile>>(_userProfiles.Values.Where(x => (!profileId.HasValue || x.Id == profileId) && (!applicationId.HasValue || x.ApplicationId == applicationId) && (string.IsNullOrWhiteSpace(name) || x.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(status) || x.Status == status)).ToArray());
    public Task<UserProfile> CreateProfileAsync(Guid? applicationId, string name, string? description, string actor, CancellationToken ct)
    { var profile = new UserProfile(Guid.NewGuid(), applicationId, name, description, "A", true); _userProfiles[profile.Id] = profile; return Task.FromResult(profile); }
    public Task<UserProfile?> UpdateProfileAsync(Guid profileId, Guid? applicationId, string name, string? description, string status, string actor, CancellationToken ct)
    { if (!_userProfiles.TryGetValue(profileId, out var profile)) return Task.FromResult<UserProfile?>(null); var updated = profile with { ApplicationId = applicationId, Name = name, Description = description, Status = status }; _userProfiles[profileId] = updated; return Task.FromResult<UserProfile?>(updated); }
    public Task<bool> DeactivateProfileAsync(Guid profileId, string actor, CancellationToken ct)
    { if (!_userProfiles.TryGetValue(profileId, out var profile)) return Task.FromResult(false); _userProfiles[profileId] = profile with { Status = "I", IsRecordActive = false }; return Task.FromResult(true); }
    public Task<IReadOnlyCollection<ProfileAccessItem>> ListAccessAsync(ProfileAccessKind kind, Guid profileId, Guid? parentId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ProfileAccessItem>>([]);
    public Task<ProfileAccessItem> AssignAccessAsync(ProfileAccessKind kind, Guid profileId, Guid resourceId, string actor, CancellationToken ct) => Task.FromResult(new ProfileAccessItem(resourceId, null, null, string.Empty, null, true, Guid.NewGuid()));
    public Task<bool> RemoveAccessAsync(Guid accessId, string actor, CancellationToken ct) => Task.FromResult(true);
    public Task<ProfileApplicationResult> ApplyProfileAsync(Guid profileId, Guid userId, string actor, CancellationToken ct) => Task.FromResult(new ProfileApplicationResult(profileId, userId, 0, 0, 0, 0, 0));
    Task<IReadOnlyCollection<SecurityMasterRecord>> ISecurityMasterRepository.SearchAsync(SecurityMasterType type, SecurityMasterFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SecurityMasterRecord>>(Array.Empty<SecurityMasterRecord>());
    Task<SecurityMasterRecord> ISecurityMasterRepository.CreateAsync(SecurityMasterType type, string parentId, string name, string? description, string? viewKey, string userName, CancellationToken ct) => Task.FromResult(new SecurityMasterRecord(Guid.NewGuid().ToString(), parentId, name, description, "A", true, userName, DateTime.UtcNow, null, null, viewKey));
    Task<SecurityMasterRecord?> ISecurityMasterRepository.UpdateAsync(SecurityMasterType type, string id, string parentId, string name, string? description, string? viewKey, string status, string userName, CancellationToken ct) => Task.FromResult<SecurityMasterRecord?>(new SecurityMasterRecord(id, parentId, name, description, status, true, null, null, userName, DateTime.UtcNow, viewKey));
    Task<int> ISecurityMasterRepository.DeactivateAsync(SecurityMasterType type, string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<IntegratedLogisticsProduct>> IIntegratedLogisticsRepository.SearchProductsAsync(ProductFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<IntegratedLogisticsProduct>>([]);
    Task<IReadOnlyCollection<IntegratedLogisticsProduct>> IIntegratedLogisticsRepository.ListActiveProductsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<IntegratedLogisticsProduct>>([]);
    Task<IntegratedLogisticsProduct> IIntegratedLogisticsRepository.CreateProductAsync(CreateProductRequest request, string userName, CancellationToken ct) => Task.FromResult(new IntegratedLogisticsProduct(Guid.NewGuid().ToString(), request.Name, request.Description, "A", true, userName, DateTime.UtcNow, null, null));
    Task<IntegratedLogisticsProduct?> IIntegratedLogisticsRepository.UpdateProductAsync(string id, UpdateProductRequest request, string userName, CancellationToken ct) => Task.FromResult<IntegratedLogisticsProduct?>(new IntegratedLogisticsProduct(id, request.Name, request.Description, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateProductAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<Ubigeo>> IIntegratedLogisticsRepository.ListActiveUbigeosAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Ubigeo>>([]);
    Task<IReadOnlyCollection<IntegratedLogisticsCampaign>> IIntegratedLogisticsRepository.SearchCampaignsAsync(CampaignFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<IntegratedLogisticsCampaign>>([]);
    Task<IntegratedLogisticsCampaign> IIntegratedLogisticsRepository.CreateCampaignAsync(CreateCampaignRequest request, string userName, CancellationToken ct) => Task.FromResult(new IntegratedLogisticsCampaign(Guid.NewGuid().ToString(), request.UbigeoId, null, null, null, null, request.ProductId, null, null, request.StartDate, request.EndDate, request.Process, request.Description, "A", true, userName, DateTime.UtcNow, null, null));
    Task<IntegratedLogisticsCampaign?> IIntegratedLogisticsRepository.UpdateCampaignAsync(string id, UpdateCampaignRequest request, string userName, CancellationToken ct) => Task.FromResult<IntegratedLogisticsCampaign?>(new IntegratedLogisticsCampaign(id, request.UbigeoId, null, null, null, null, request.ProductId, null, null, request.StartDate, request.EndDate, request.Process, request.Description, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateCampaignAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<FixedCost>> IIntegratedLogisticsRepository.SearchFixedCostsAsync(FixedCostFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<FixedCost>>([]);
    Task<IReadOnlyCollection<ActiveSite>> IIntegratedLogisticsRepository.ListActiveSitesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ActiveSite>>([]);
    Task<FixedCost> IIntegratedLogisticsRepository.CreateFixedCostAsync(CreateFixedCostRequest request, string userName, CancellationToken ct) => Task.FromResult(new FixedCost(Guid.NewGuid().ToString(), request.SiteId, null, request.Description, request.Amount, "A", true, userName, DateTime.UtcNow, null, null));
    Task<FixedCost?> IIntegratedLogisticsRepository.UpdateFixedCostAsync(string id, UpdateFixedCostRequest request, string userName, CancellationToken ct) => Task.FromResult<FixedCost?>(new FixedCost(id, request.SiteId, null, request.Description, request.Amount, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateFixedCostAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<Deposit>> IIntegratedLogisticsRepository.SearchDepositsAsync(DepositFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Deposit>>([]);
    Task<IReadOnlyCollection<ActiveDepositType>> IIntegratedLogisticsRepository.ListActiveDepositTypesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ActiveDepositType>>([]);
    Task<Deposit> IIntegratedLogisticsRepository.CreateDepositAsync(CreateDepositRequest request, string userName, CancellationToken ct) => Task.FromResult(new Deposit(Guid.NewGuid().ToString(), request.DepositTypeId, null, request.Name, "A", true, userName, DateTime.UtcNow, null, null));
    Task<Deposit?> IIntegratedLogisticsRepository.UpdateDepositAsync(string id, UpdateDepositRequest request, string userName, CancellationToken ct) => Task.FromResult<Deposit?>(new Deposit(id, request.DepositTypeId, null, request.Name, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateDepositAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<ContainerDimension>> IIntegratedLogisticsRepository.SearchContainerDimensionsAsync(MasterTableFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ContainerDimension>>([]);
    Task<ContainerDimension> IIntegratedLogisticsRepository.CreateContainerDimensionAsync(CreateContainerDimensionRequest request, string userName, CancellationToken ct) => Task.FromResult(new ContainerDimension(Guid.NewGuid().ToString(), request.Dimension, "A", true, userName, DateTime.UtcNow, null, null));
    Task<ContainerDimension?> IIntegratedLogisticsRepository.UpdateContainerDimensionAsync(string id, UpdateContainerDimensionRequest request, string userName, CancellationToken ct) => Task.FromResult<ContainerDimension?>(new ContainerDimension(id, request.Dimension, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateContainerDimensionAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<EntityAddressType>> IIntegratedLogisticsRepository.SearchEntityAddressTypesAsync(MasterTableFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<EntityAddressType>>([]);
    Task<EntityAddressType> IIntegratedLogisticsRepository.CreateEntityAddressTypeAsync(CreateEntityAddressTypeRequest request, string userName, CancellationToken ct) => Task.FromResult(new EntityAddressType(Guid.NewGuid().ToString(), request.Description, "A", true, userName, DateTime.UtcNow, null, null));
    Task<EntityAddressType?> IIntegratedLogisticsRepository.UpdateEntityAddressTypeAsync(string id, UpdateEntityAddressTypeRequest request, string userName, CancellationToken ct) => Task.FromResult<EntityAddressType?>(new EntityAddressType(id, request.Description, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateEntityAddressTypeAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<PaymentType>> IIntegratedLogisticsRepository.SearchPaymentTypesAsync(MasterTableFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<PaymentType>>([]);
    Task<PaymentType> IIntegratedLogisticsRepository.CreatePaymentTypeAsync(CreatePaymentTypeRequest request, string userName, CancellationToken ct) => Task.FromResult(new PaymentType(Guid.NewGuid().ToString(), request.Description, "A", true, userName, DateTime.UtcNow, null, null));
    Task<PaymentType?> IIntegratedLogisticsRepository.UpdatePaymentTypeAsync(string id, UpdatePaymentTypeRequest request, string userName, CancellationToken ct) => Task.FromResult<PaymentType?>(new PaymentType(id, request.Description, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivatePaymentTypeAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<ShippingLine>> IIntegratedLogisticsRepository.SearchShippingLinesAsync(ShippingLineFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<ShippingLine>>([]);
    Task<ShippingLine> IIntegratedLogisticsRepository.CreateShippingLineAsync(CreateShippingLineRequest request, string userName, CancellationToken ct) => Task.FromResult(new ShippingLine(Guid.NewGuid().ToString(), request.Description, request.SunatCode, "A", true, userName, DateTime.UtcNow, null, null));
    Task<ShippingLine?> IIntegratedLogisticsRepository.UpdateShippingLineAsync(string id, UpdateShippingLineRequest request, string userName, CancellationToken ct) => Task.FromResult<ShippingLine?>(new ShippingLine(id, request.Description, request.SunatCode, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateShippingLineAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<PickupPlace>> IIntegratedLogisticsRepository.SearchPickupPlacesAsync(MasterTableFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<PickupPlace>>([]);
    Task<PickupPlace> IIntegratedLogisticsRepository.CreatePickupPlaceAsync(CreatePickupPlaceRequest request, string userName, CancellationToken ct) => Task.FromResult(new PickupPlace(Guid.NewGuid().ToString(), request.Description, "A", true, userName, DateTime.UtcNow, null, null));
    Task<PickupPlace?> IIntegratedLogisticsRepository.UpdatePickupPlaceAsync(string id, UpdatePickupPlaceRequest request, string userName, CancellationToken ct) => Task.FromResult<PickupPlace?>(new PickupPlace(id, request.Description, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivatePickupPlaceAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<Currency>> IIntegratedLogisticsRepository.SearchCurrenciesAsync(CurrencyFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Currency>>([]);
    Task<Currency> IIntegratedLogisticsRepository.CreateCurrencyAsync(CreateCurrencyRequest request, string userName, CancellationToken ct) => Task.FromResult(new Currency(Guid.NewGuid().ToString(), request.Description, request.Symbol, "A", true, userName, DateTime.UtcNow, null, null));
    Task<Currency?> IIntegratedLogisticsRepository.UpdateCurrencyAsync(string id, UpdateCurrencyRequest request, string userName, CancellationToken ct) => Task.FromResult<Currency?>(new Currency(id, request.Description, request.Symbol, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateCurrencyAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<CurrentAccountDocumentType>> IIntegratedLogisticsRepository.SearchCurrentAccountDocumentTypesAsync(CurrentAccountDocumentTypeFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<CurrentAccountDocumentType>>([]);
    Task<CurrentAccountDocumentType> IIntegratedLogisticsRepository.CreateCurrentAccountDocumentTypeAsync(CreateCurrentAccountDocumentTypeRequest request, string userName, CancellationToken ct) => Task.FromResult(new CurrentAccountDocumentType(Guid.NewGuid().ToString(), request.Code, request.Description, "A", true, userName, DateTime.UtcNow, null, null));
    Task<CurrentAccountDocumentType?> IIntegratedLogisticsRepository.UpdateCurrentAccountDocumentTypeAsync(string id, UpdateCurrentAccountDocumentTypeRequest request, string userName, CancellationToken ct) => Task.FromResult<CurrentAccountDocumentType?>(new CurrentAccountDocumentType(id, request.Code, request.Description, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivateCurrentAccountDocumentTypeAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<PackagingType>> IIntegratedLogisticsRepository.SearchPackagingTypesAsync(MasterTableFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<PackagingType>>([]);
    Task<PackagingType> IIntegratedLogisticsRepository.CreatePackagingTypeAsync(CreatePackagingTypeRequest request, string userName, CancellationToken ct) => Task.FromResult(new PackagingType(Guid.NewGuid().ToString(), request.Description, "A", true, userName, DateTime.UtcNow, null, null));
    Task<PackagingType?> IIntegratedLogisticsRepository.UpdatePackagingTypeAsync(string id, UpdatePackagingTypeRequest request, string userName, CancellationToken ct) => Task.FromResult<PackagingType?>(new PackagingType(id, request.Description, request.Status, true, null, null, userName, DateTime.UtcNow));
    Task<int> IIntegratedLogisticsRepository.DeactivatePackagingTypeAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<IReadOnlyCollection<SecurityMasterUser>> ISecurityMasterUserRepository.SearchAsync(SecurityUserFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SecurityMasterUser>>(Array.Empty<SecurityMasterUser>());
    Task<SecurityMasterUser> ISecurityMasterUserRepository.CreateAsync(CreateSecurityMasterUserRequest request, byte[] passwordHash, string userName, CancellationToken ct) => Task.FromResult(new SecurityMasterUser(Guid.NewGuid().ToString(), request.DomainId, request.RoleId, request.Login, request.FirstNames, request.PaternalLastName, request.MaternalLastName, request.Email, "A", request.ChangePassword, request.MultipleSites, request.DefaultSiteId, true, userName, DateTime.UtcNow, null, null));
    Task<SecurityMasterUser?> ISecurityMasterUserRepository.UpdateAsync(string id, UpdateSecurityMasterUserRequest request, byte[]? passwordHash, string userName, CancellationToken ct) => Task.FromResult<SecurityMasterUser?>(new SecurityMasterUser(id, request.DomainId, request.RoleId, request.Login, request.FirstNames, request.PaternalLastName, request.MaternalLastName, request.Email, request.Status, request.ChangePassword ?? false, request.MultipleSites ?? false, request.DefaultSiteId, true, null, null, userName, DateTime.UtcNow));
    Task<int> ISecurityMasterUserRepository.DeactivateAsync(string id, string userName, CancellationToken ct) => Task.FromResult(1);
    Task<PasswordResetResult?> ISecurityMasterUserRepository.ResetPasswordAsync(string id, byte[]? passwordHash, string userName, CancellationToken ct) => Task.FromResult<PasswordResetResult?>(null);
    Task<UserModuleAssignment> IUserNavigationAssignmentRepository.AssignModuleAsync(string userId, string moduleId, string auditUser, CancellationToken ct) => Task.FromResult(new UserModuleAssignment(Guid.NewGuid().ToString(), moduleId, userId, "A", true, auditUser, DateTime.UtcNow, null, null));
    Task<UserMenuAssignment> IUserNavigationAssignmentRepository.AssignMenuAsync(string userId, string menuId, string auditUser, CancellationToken ct) => Task.FromResult(new UserMenuAssignment(Guid.NewGuid().ToString(), menuId, userId, "A", true, auditUser, DateTime.UtcNow, null, null));
    Task<UserSubmenuAssignment> IUserNavigationAssignmentRepository.AssignSubmenuAsync(string userId, string submenuId, string auditUser, CancellationToken ct) => Task.FromResult(new UserSubmenuAssignment(Guid.NewGuid().ToString(), submenuId, userId, "A", true, auditUser, DateTime.UtcNow, null, null));
    Task<IReadOnlyCollection<SecurityMenu>> IUserAccessRepository.GetMenusAsync(string moduleId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SecurityMenu>>(Array.Empty<SecurityMenu>());
    Task<IReadOnlyCollection<SecuritySubmenu>> IUserAccessRepository.GetSubmenusAsync(string menuId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SecuritySubmenu>>(Array.Empty<SecuritySubmenu>());
    Task<IReadOnlyCollection<SecurityFormOption>> IUserAccessRepository.GetFormOptionsAsync(string submenuId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SecurityFormOption>>(Array.Empty<SecurityFormOption>());
    Task<UserAccessSnapshot> IUserAccessRepository.GetAccessAsync(string userId, CancellationToken ct) => Task.FromResult(new UserAccessSnapshot(Array.Empty<UserAccessItem>(), Array.Empty<UserAccessItem>(), Array.Empty<UserAccessItem>(), Array.Empty<UserAccessItem>()));
    Task<UserFormOptionAssignment> IUserAccessRepository.AssignFormOptionAsync(string userId, string optionId, string auditUser, CancellationToken ct) => Task.FromResult(new UserFormOptionAssignment(Guid.NewGuid().ToString(), optionId, userId, "A", true, auditUser, DateTime.UtcNow, null, null));
    Task<bool> IUserAccessRepository.RemoveAccessAsync(string userId, string accessId, string auditUser, CancellationToken ct) => Task.FromResult(true);
    Task<IReadOnlyCollection<FormOptionMaster>> IFormOptionMasterRepository.SearchAsync(FormOptionMasterFilter filter, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<FormOptionMaster>>(Array.Empty<FormOptionMaster>());
    Task<FormOptionMaster> IFormOptionMasterRepository.CreateAsync(FormOptionMasterRequest request, string auditUser, CancellationToken ct) => Task.FromResult(new FormOptionMaster(Guid.NewGuid().ToString(), request.SubmenuId, request.SecondLevelId, request.Code, request.Name, request.Description, request.Color, request.Icon, "A", null, null, null, null));
    Task<FormOptionMaster?> IFormOptionMasterRepository.UpdateAsync(string id, FormOptionMasterRequest request, string auditUser, CancellationToken ct) => Task.FromResult<FormOptionMaster?>(new FormOptionMaster(id, request.SubmenuId, request.SecondLevelId, request.Code, request.Name, request.Description, request.Color, request.Icon, request.Status ?? "A", null, null, null, null));
    Task<int> IFormOptionMasterRepository.DeactivateAsync(string id, string auditUser, CancellationToken ct) => Task.FromResult(1);
    public Task<IReadOnlyCollection<UserSummary>> GetUsersAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<UserSummary>>(_users.Values.Select(x => new UserSummary(x.Id,x.UserName,x.Email,x.IsActive)).ToArray());
    public Task<Guid> CreateUserAsync(string userName,string email,string passwordHash,CancellationToken ct) { var id=Guid.NewGuid(); _users[id]=new(id,userName,email,passwordHash,true); return Task.FromResult(id); }
    public Task<bool> UpdateUserAsync(Guid id,string email,bool isActive,CancellationToken ct) { if(!_users.TryGetValue(id,out var u)) return Task.FromResult(false); _users[id]=u with { Email=email,IsActive=isActive }; return Task.FromResult(true); }
    public Task<bool> DeactivateUserAsync(Guid id,CancellationToken ct) { if(!_users.TryGetValue(id,out var u)) return Task.FromResult(false); _users[id]=u with { IsActive=false }; return Task.FromResult(true); }
    public Task<IReadOnlyCollection<SecurityProfile>> GetProfilesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SecurityProfile>>(_profiles.Values.ToArray());
    public Task<Guid> CreateProfileAsync(string code,string name,CancellationToken ct) { var id=Guid.NewGuid(); _profiles[id]=new(id,code,name,true); return Task.FromResult(id); }
    public Task<bool> UpdateProfileAsync(Guid id,string name,bool isActive,CancellationToken ct) { if(!_profiles.TryGetValue(id,out var p)) return Task.FromResult(false); _profiles[id]=p with { Name=name,IsActive=isActive }; return Task.FromResult(true); }
    Task<IReadOnlyCollection<SecurityPermission>> ISecurityAdminRepository.GetPermissionsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SecurityPermission>>(_permissions.Values.ToArray());
    public Task AssignProfileAsync(Guid userId,Guid profileId,CancellationToken ct) => Task.CompletedTask;
    public Task ReplaceProfilePermissionsAsync(Guid profileId,IReadOnlyCollection<Guid> permissionIds,CancellationToken ct) => Task.CompletedTask;
}

public sealed class LoggingOtpDelivery(Microsoft.Extensions.Logging.ILogger<LoggingOtpDelivery> logger) : IOtpDelivery
{
    public Task SendAsync(string destination, string code, CancellationToken ct)
    {
        logger.LogInformation("OTP generado para {Destination}. Configure un proveedor de correo para entregarlo.", destination);
        return Task.CompletedTask;
    }
}
