using CXView.Domain;

namespace CXView.Application;

public interface IUserRepository
{
    Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken);
    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IFinePermissionRepository
{
    Task<bool> HasPermissionAsync(Guid userId, string code, CancellationToken cancellationToken);
}

public interface ISessionRepository
{
    Task CreateAsync(AuthSession session, CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task<bool> RenewAsync(Guid sessionId, Guid userId, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    Task RevokeAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}

public interface IOtpRepository
{
    Task SaveAsync(OtpChallenge challenge, CancellationToken cancellationToken);
    Task<OtpChallenge?> FindAsync(Guid challengeId, CancellationToken cancellationToken);
    Task<bool> ConsumeAsync(Guid challengeId, CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string encodedHash, string password);
}

public interface IOtpHasher
{
    string CreateCode();
    string Hash(Guid challengeId, string code);
    bool Verify(Guid challengeId, string code, string hash);
}

public interface ITokenIssuer
{
    string Issue(UserIdentity user, Guid sessionId, DateTimeOffset expiresAt);
}

public interface IOtpDelivery
{
    Task SendAsync(string destination, string code, CancellationToken cancellationToken);
}

public interface IUserSiteRepository
{
    Task<IReadOnlyCollection<UserSite>> GetByUserNameAsync(string userName, CancellationToken cancellationToken);
}

public interface IUserOperationTypeRepository
{
    Task<IReadOnlyCollection<UserOperationType>> GetByUserNameAsync(string userName, CancellationToken cancellationToken);
}

public interface IUserBusinessUnitRepository
{
    Task<IReadOnlyCollection<UserBusinessUnit>> GetByUserNameAsync(string userName, CancellationToken cancellationToken);
}

public interface IUserModuleRepository
{
    Task<IReadOnlyCollection<UserModule>> GetByUserNameAsync(string userName, CancellationToken cancellationToken);
}

public interface IUserMenuRepository
{
    Task<IReadOnlyCollection<UserMenu>> GetByUserNameAndModuleIdAsync(string userName, string moduleId, CancellationToken cancellationToken);
}

public interface IUserSubmenuRepository
{
    Task<IReadOnlyCollection<UserSubmenu>> GetByUserNameAndMenuIdAsync(string userName, string menuId, CancellationToken cancellationToken);
}

public interface ICompanyRepository
{
    Task<IReadOnlyCollection<Company>> SearchAsync(CompanySearchFilter filter, CancellationToken cancellationToken);
    Task<Company> CreateAsync(string companyName, string? ruc, string userName, CancellationToken cancellationToken);
    Task<Company?> UpdateAsync(string companyId, string companyName, string? ruc, string status, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateAsync(string companyId, string userName, CancellationToken cancellationToken);
}

public interface IIntegratedLogisticsRepository
{
    Task<IReadOnlyCollection<IntegratedLogisticsProduct>> SearchProductsAsync(ProductFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<IntegratedLogisticsProduct>> ListActiveProductsAsync(CancellationToken cancellationToken);
    Task<IntegratedLogisticsProduct> CreateProductAsync(CreateProductRequest request, string userName, CancellationToken cancellationToken);
    Task<IntegratedLogisticsProduct?> UpdateProductAsync(string id, UpdateProductRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateProductAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Ubigeo>> ListActiveUbigeosAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<IntegratedLogisticsCampaign>> SearchCampaignsAsync(CampaignFilter filter, CancellationToken cancellationToken);
    Task<IntegratedLogisticsCampaign> CreateCampaignAsync(CreateCampaignRequest request, string userName, CancellationToken cancellationToken);
    Task<IntegratedLogisticsCampaign?> UpdateCampaignAsync(string id, UpdateCampaignRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateCampaignAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FixedCost>> SearchFixedCostsAsync(FixedCostFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ActiveSite>> ListActiveSitesAsync(CancellationToken cancellationToken);
    Task<FixedCost> CreateFixedCostAsync(CreateFixedCostRequest request, string userName, CancellationToken cancellationToken);
    Task<FixedCost?> UpdateFixedCostAsync(string id, UpdateFixedCostRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateFixedCostAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Deposit>> SearchDepositsAsync(DepositFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ActiveDepositType>> ListActiveDepositTypesAsync(CancellationToken cancellationToken);
    Task<Deposit> CreateDepositAsync(CreateDepositRequest request, string userName, CancellationToken cancellationToken);
    Task<Deposit?> UpdateDepositAsync(string id, UpdateDepositRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateDepositAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ContainerDimension>> SearchContainerDimensionsAsync(MasterTableFilter filter, CancellationToken cancellationToken);
    Task<ContainerDimension> CreateContainerDimensionAsync(CreateContainerDimensionRequest request, string userName, CancellationToken cancellationToken);
    Task<ContainerDimension?> UpdateContainerDimensionAsync(string id, UpdateContainerDimensionRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateContainerDimensionAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EntityAddressType>> SearchEntityAddressTypesAsync(MasterTableFilter filter, CancellationToken cancellationToken);
    Task<EntityAddressType> CreateEntityAddressTypeAsync(CreateEntityAddressTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<EntityAddressType?> UpdateEntityAddressTypeAsync(string id, UpdateEntityAddressTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateEntityAddressTypeAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaymentType>> SearchPaymentTypesAsync(MasterTableFilter filter, CancellationToken cancellationToken);
    Task<PaymentType> CreatePaymentTypeAsync(CreatePaymentTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<PaymentType?> UpdatePaymentTypeAsync(string id, UpdatePaymentTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivatePaymentTypeAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ShippingLine>> SearchShippingLinesAsync(ShippingLineFilter filter, CancellationToken cancellationToken);
    Task<ShippingLine> CreateShippingLineAsync(CreateShippingLineRequest request, string userName, CancellationToken cancellationToken);
    Task<ShippingLine?> UpdateShippingLineAsync(string id, UpdateShippingLineRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateShippingLineAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PickupPlace>> SearchPickupPlacesAsync(MasterTableFilter filter, CancellationToken cancellationToken);
    Task<PickupPlace> CreatePickupPlaceAsync(CreatePickupPlaceRequest request, string userName, CancellationToken cancellationToken);
    Task<PickupPlace?> UpdatePickupPlaceAsync(string id, UpdatePickupPlaceRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivatePickupPlaceAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Currency>> SearchCurrenciesAsync(CurrencyFilter filter, CancellationToken cancellationToken);
    Task<Currency> CreateCurrencyAsync(CreateCurrencyRequest request, string userName, CancellationToken cancellationToken);
    Task<Currency?> UpdateCurrencyAsync(string id, UpdateCurrencyRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateCurrencyAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CurrentAccountDocumentType>> SearchCurrentAccountDocumentTypesAsync(CurrentAccountDocumentTypeFilter filter, CancellationToken cancellationToken);
    Task<CurrentAccountDocumentType> CreateCurrentAccountDocumentTypeAsync(CreateCurrentAccountDocumentTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<CurrentAccountDocumentType?> UpdateCurrentAccountDocumentTypeAsync(string id, UpdateCurrentAccountDocumentTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateCurrentAccountDocumentTypeAsync(string id, string userName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PackagingType>> SearchPackagingTypesAsync(MasterTableFilter filter, CancellationToken cancellationToken);
    Task<PackagingType> CreatePackagingTypeAsync(CreatePackagingTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<PackagingType?> UpdatePackagingTypeAsync(string id, UpdatePackagingTypeRequest request, string userName, CancellationToken cancellationToken);
    Task<int> DeactivatePackagingTypeAsync(string id, string userName, CancellationToken cancellationToken);
}

public enum SecurityMasterType { Application, Domain, Area, Role, Module, Menu, Submenu, Product }

public interface ISecurityMasterRepository
{
    Task<IReadOnlyCollection<SecurityMasterRecord>> SearchAsync(SecurityMasterType type, SecurityMasterFilter filter, CancellationToken cancellationToken);
    Task<SecurityMasterRecord> CreateAsync(SecurityMasterType type, string parentId, string name, string? description, string? viewKey, string userName, CancellationToken cancellationToken);
    Task<SecurityMasterRecord?> UpdateAsync(SecurityMasterType type, string id, string parentId, string name, string? description, string? viewKey, string status, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateAsync(SecurityMasterType type, string id, string userName, CancellationToken cancellationToken);
}

public interface ISecurityMasterUserRepository
{
    Task<IReadOnlyCollection<SecurityMasterUser>> SearchAsync(SecurityUserFilter filter, CancellationToken cancellationToken);
    Task<SecurityMasterUser> CreateAsync(CreateSecurityMasterUserRequest request, byte[] passwordHash, string userName, CancellationToken cancellationToken);
    Task<SecurityMasterUser?> UpdateAsync(string id, UpdateSecurityMasterUserRequest request, byte[]? passwordHash, string userName, CancellationToken cancellationToken);
    Task<int> DeactivateAsync(string id, string userName, CancellationToken cancellationToken);
    Task<PasswordResetResult?> ResetPasswordAsync(string id, byte[]? passwordHash, string userName, CancellationToken cancellationToken);
}

public interface IUserNavigationAssignmentRepository
{
    Task<UserModuleAssignment> AssignModuleAsync(string userId, string moduleId, string auditUser, CancellationToken cancellationToken);
    Task<UserMenuAssignment> AssignMenuAsync(string userId, string menuId, string auditUser, CancellationToken cancellationToken);
    Task<UserSubmenuAssignment> AssignSubmenuAsync(string userId, string submenuId, string auditUser, CancellationToken cancellationToken);
}

public interface IUserAccessRepository
{
    Task<IReadOnlyCollection<SecurityMenu>> GetMenusAsync(string moduleId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SecuritySubmenu>> GetSubmenusAsync(string menuId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SecurityFormOption>> GetFormOptionsAsync(string submenuId, CancellationToken cancellationToken);
    Task<UserAccessSnapshot> GetAccessAsync(string userId, CancellationToken cancellationToken);
    Task<UserFormOptionAssignment> AssignFormOptionAsync(string userId, string optionId, string auditUser, CancellationToken cancellationToken);
    Task<bool> RemoveAccessAsync(string userId, string accessId, string auditUser, CancellationToken cancellationToken);
}

public interface IFormOptionMasterRepository
{
    Task<IReadOnlyCollection<FormOptionMaster>> SearchAsync(FormOptionMasterFilter filter, CancellationToken cancellationToken);
    Task<FormOptionMaster> CreateAsync(FormOptionMasterRequest request, string auditUser, CancellationToken cancellationToken);
    Task<FormOptionMaster?> UpdateAsync(string id, FormOptionMasterRequest request, string auditUser, CancellationToken cancellationToken);
    Task<int> DeactivateAsync(string id, string auditUser, CancellationToken cancellationToken);
}

/// <summary>Persistencia de perfiles de permisos TAS017 a TAS022.</summary>
public interface IUserProfileRepository
{
    Task<IReadOnlyCollection<UserProfile>> ListProfilesAsync(Guid? profileId, Guid? applicationId, string? name, string? status, CancellationToken ct);
    Task<UserProfile> CreateProfileAsync(Guid? applicationId, string name, string? description, string actor, CancellationToken ct);
    Task<UserProfile?> UpdateProfileAsync(Guid profileId, Guid? applicationId, string name, string? description, string status, string actor, CancellationToken ct);
    Task<bool> DeactivateProfileAsync(Guid profileId, string actor, CancellationToken ct);
    Task<IReadOnlyCollection<ProfileAccessItem>> ListAccessAsync(ProfileAccessKind kind, Guid profileId, Guid? parentId, CancellationToken ct);
    Task<ProfileAccessItem> AssignAccessAsync(ProfileAccessKind kind, Guid profileId, Guid resourceId, string actor, CancellationToken ct);
    Task<bool> RemoveAccessAsync(Guid accessId, string actor, CancellationToken ct);
    Task<ProfileApplicationResult> ApplyProfileAsync(Guid profileId, Guid userId, string actor, CancellationToken ct);
}

public enum ProfileAccessKind { Modules, Menus, Submenus, SecondLevels, FormOptions }

public interface IClock { DateTimeOffset UtcNow { get; } }

public interface ISecurityAdminRepository
{
    Task<IReadOnlyCollection<UserSummary>> GetUsersAsync(CancellationToken ct);
    Task<Guid> CreateUserAsync(string userName, string email, string passwordHash, CancellationToken ct);
    Task<bool> UpdateUserAsync(Guid id, string email, bool isActive, CancellationToken ct);
    Task<bool> DeactivateUserAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<SecurityProfile>> GetProfilesAsync(CancellationToken ct);
    Task<Guid> CreateProfileAsync(string code, string name, CancellationToken ct);
    Task<bool> UpdateProfileAsync(Guid id, string name, bool isActive, CancellationToken ct);
    Task<IReadOnlyCollection<SecurityPermission>> GetPermissionsAsync(CancellationToken ct);
    Task AssignProfileAsync(Guid userId, Guid profileId, CancellationToken ct);
    Task ReplaceProfilePermissionsAsync(Guid profileId, IReadOnlyCollection<Guid> permissionIds, CancellationToken ct);
}

public sealed record LoginRequest(string UserName, string Password);
public sealed record LoginResult(bool Success, string Message, Guid? ChallengeId = null, string? MaskedDestination = null);
public sealed record VerifyOtpRequest(Guid ChallengeId, string Code);
public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt);
public sealed record CreateUserRequest(string UserName, string Email, string Password);
public sealed record UpdateUserRequest(string Email, bool IsActive);
public sealed record CreateProfileRequest(string Code, string Name);
public sealed record UpdateProfileRequest(string Name, bool IsActive);
public sealed record AssignProfileRequest(Guid ProfileId);
public sealed record ReplacePermissionsRequest(IReadOnlyCollection<Guid> PermissionIds);
public sealed record CreateCompanyRequest(string Name, string? Ruc);
public sealed record UpdateCompanyRequest(string Name, string? Ruc, string Status);
public sealed record CompanySearchFilter(string? Id, string? Name, string? Ruc, string? Status);
public sealed record ProductFilter(string? Id, string? Name, string? Status);
public sealed record CreateProductRequest(string Name, string? Description);
public sealed record UpdateProductRequest(string Name, string? Description, string Status);
public sealed record CampaignFilter(string? Id, string? UbigeoId, string? ProductId, string? Process, string? Status);
public sealed record CreateCampaignRequest(string UbigeoId, string ProductId, DateOnly StartDate, DateOnly EndDate, string Process, string? Description);
public sealed record UpdateCampaignRequest(string UbigeoId, string ProductId, DateOnly StartDate, DateOnly EndDate, string Process, string? Description, string Status);
public sealed record FixedCostFilter(string? Id, string? SiteId, string? Description, string? Status);
public sealed record CreateFixedCostRequest(string SiteId, string Description, decimal Amount);
public sealed record UpdateFixedCostRequest(string SiteId, string Description, decimal Amount, string Status);
public sealed record DepositFilter(string? Id, string? DepositTypeId, string? Name, string? Status);
public sealed record CreateDepositRequest(string DepositTypeId, string Name);
public sealed record UpdateDepositRequest(string DepositTypeId, string Name, string Status);
public sealed record MasterTableFilter(string? Id, string? Name, string? Status);
public sealed record CreateContainerDimensionRequest(string Dimension);
public sealed record UpdateContainerDimensionRequest(string Dimension, string Status);
public sealed record CreateEntityAddressTypeRequest(string Description);
public sealed record UpdateEntityAddressTypeRequest(string Description, string Status);
public sealed record CreatePaymentTypeRequest(string Description);
public sealed record UpdatePaymentTypeRequest(string Description, string Status);
public sealed record ShippingLineFilter(string? Id, string? Description, string? SunatCode, string? Status);
public sealed record CreateShippingLineRequest(string Description, string? SunatCode);
public sealed record UpdateShippingLineRequest(string Description, string? SunatCode, string Status);
public sealed record CreatePickupPlaceRequest(string Description);
public sealed record UpdatePickupPlaceRequest(string Description, string Status);
public sealed record CurrencyFilter(string? Id, string? Description, string? Symbol, string? Status);
public sealed record CreateCurrencyRequest(string Description, string Symbol);
public sealed record UpdateCurrencyRequest(string Description, string Symbol, string Status);
public sealed record CurrentAccountDocumentTypeFilter(string? Id, string? Code, string? Description, string? Status);
public sealed record CreateCurrentAccountDocumentTypeRequest(string Code, string Description);
public sealed record UpdateCurrentAccountDocumentTypeRequest(string Code, string Description, string Status);
public sealed record CreatePackagingTypeRequest(string Description);
public sealed record UpdatePackagingTypeRequest(string Description, string Status);
public sealed record SecurityMasterFilter(string? Id, string? ParentId, string? Name, string? Status);
public sealed record CreateSecurityMasterRequest(string ParentId, string Name, string? Description, string? ViewKey = null);
public sealed record UpdateSecurityMasterRequest(string ParentId, string Name, string? Description, string Status, string? ViewKey = null);
public sealed record SecurityUserFilter(string? Id, string? DomainId, string? RoleId, string? Login, string? Email, string? Status);
public sealed record CreateSecurityMasterUserRequest(string? DomainId, string? RoleId, string Login, string Password, string? FirstNames, string? PaternalLastName, string? MaternalLastName, string? Email, bool ChangePassword, bool MultipleSites, string? DefaultSiteId);
public sealed record UpdateSecurityMasterUserRequest(string? DomainId, string? RoleId, string Login, string? Password, string? FirstNames, string? PaternalLastName, string? MaternalLastName, string? Email, string Status, bool? ChangePassword, bool? MultipleSites, string? DefaultSiteId);
public sealed record ResetSecurityMasterUserPasswordRequest(string? Password);
public sealed record PasswordResetResult(string Id, string Login, string Status, bool ChangePassword, string? UpdatedBy, DateTime? UpdatedAt, int RowsAffected);
public sealed record FormOptionMasterFilter(string? Id, string? SubmenuId, string? SecondLevelId, string? Name, string? Status);
public sealed record FormOptionMasterRequest(string? SubmenuId, string? SecondLevelId, string Code, string Name, string? Description, string? Color, string? Icon, string? Status = null);
public sealed record CreateUserProfileRequest(Guid? ApplicationId, string Name, string? Description);
public sealed record UpdateUserProfileRequest(Guid? ApplicationId, string Name, string? Description, string Status);
