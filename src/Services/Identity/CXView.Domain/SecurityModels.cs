namespace CXView.Domain;

public sealed record UserAccount(Guid Id, string UserName, string Email, string PasswordHash, bool IsActive);
public sealed record UserIdentity(Guid Id, string UserName, string Email, IReadOnlyCollection<string> Permissions);
public sealed record AuthSession(Guid Id, Guid UserId, DateTimeOffset ExpiresAt, bool IsRevoked);
public sealed record OtpChallenge(Guid Id, Guid UserId, string OtpHash, DateTimeOffset ExpiresAt, bool IsConsumed);
public sealed record UserSummary(Guid Id, string UserName, string Email, bool IsActive);
public sealed record SecurityProfile(Guid Id, string Code, string Name, bool IsActive);
public sealed record SecurityPermission(Guid Id, string Code, string Name);
public sealed record UserSite(Guid SiteId, string Name, string? Abbreviation, string? Address, string? Phone, bool IsDefault);
public sealed record UserOperationType(string OperationTypeId, string Name);
public sealed record UserBusinessUnit(string BusinessUnitId, string Name, string? Description, string SalesRegionId);
public sealed record UserModule(string ModuleId, string ApplicationId, string ApplicationName, string Name, string? Description);
public sealed record UserMenu(string MenuId, string Name, string? Description);
public sealed record UserSubmenu(string SubmenuId, string Name, string? Description, string? ViewKey);
public sealed record Company(string Id, string Name, string? Ruc, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record IntegratedLogisticsProduct(string Id, string Name, string? Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record IntegratedLogisticsCampaign(string Id, string UbigeoId, string? UbigeoCode, string? District, string? Province, string? Department, string ProductId, string? ProductName, string? ProductDescription, DateOnly StartDate, DateOnly EndDate, string Process, string? Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record FixedCost(string Id, string SiteId, string? SiteName, string Description, decimal Amount, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record Deposit(string Id, string DepositTypeId, string? DepositTypeDescription, string Name, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record ActiveSite(string Id, string Name, string? Abbreviation);
public sealed record ActiveDepositType(string Id, string Description);
public sealed record ContainerDimension(string Id, string Dimension, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record EntityAddressType(string Id, string Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record PaymentType(string Id, string Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record ShippingLine(string Id, string Description, string? SunatCode, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record PickupPlace(string Id, string Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record Currency(string Id, string Description, string Symbol, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record CurrentAccountDocumentType(string Id, string Code, string Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record PackagingType(string Id, string Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record Ubigeo(string Id, string? Code, string? District, string? Province, string? Department, string? Description);
public sealed record SecurityMasterRecord(string Id, string? ParentId, string Name, string? Description, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt, string? ViewKey = null);
public sealed record SecurityMasterUser(string Id, string? DomainId, string? RoleId, string Login, string? FirstNames, string? PaternalLastName, string? MaternalLastName, string? Email, string Status, bool ChangePassword, bool MultipleSites, string? DefaultSiteId, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record UserModuleAssignment(string Id, string ModuleId, string UserId, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record UserMenuAssignment(string Id, string MenuId, string UserId, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record UserSubmenuAssignment(string Id, string SubmenuId, string UserId, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record SecurityMenu(string Id, string ModuleId, string Name, string? Description, string Status, bool IsRecordActive);
public sealed record SecuritySubmenu(string Id, string MenuId, string Name, string? Description, string Status, bool IsRecordActive);
public sealed record SecurityFormOption(string Id, string? SubmenuId, string? SecondLevelSubmenuId, string Code, string? Description, string Status, bool IsRecordActive);
public sealed record UserFormOptionAssignment(string Id, string OptionId, string UserId, string Status, bool IsRecordActive, string? CreatedBy, DateTime? CreatedAt, string? UpdatedBy, DateTime? UpdatedAt);
public sealed record UserAccessItem(string? AccessId, string Id, string ResourceId, string? ParentId, string Name, string? Code, string? Description, bool CatalogActive, bool Assigned, bool AssignedByProfile);
public sealed record UserAccessSnapshot(IReadOnlyCollection<UserAccessItem> Modules, IReadOnlyCollection<UserAccessItem> Menus, IReadOnlyCollection<UserAccessItem> Submenus, IReadOnlyCollection<UserAccessItem> FormOptions);
public sealed record FormOptionMaster(string Id, string? SubmenuId, string? SecondLevelId, string Code, string Name, string? Description, string? Color, string? Icon, string Status, string? SubmenuName, string? MenuName, string? ModuleName, string? ApplicationName);
public sealed record UserProfile(Guid Id, Guid? ApplicationId, string Name, string? Description, string Status, bool IsRecordActive);
public sealed record ProfileAccessItem(Guid ResourceId, Guid? ParentId, string? Code, string Name, string? Description, bool Assigned, Guid? AccessId);
public sealed record ProfileApplicationResult(Guid ProfileId, Guid UserId, int Modules, int Menus, int Submenus, int SecondLevels, int Options);

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string SecurityAdmin = "security.admin";
}
