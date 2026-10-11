using System.Data;
using System.Data.Common;
using CXView.Application;
using CXView.Domain;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace CXView.Infrastructure;

public interface IDbConnectionFactory { DbConnection Create(); }

public sealed class ConfiguredDbConnectionFactory(IOptions<DatabaseOptions> options) : IDbConnectionFactory
{
    private readonly DatabaseOptions _options = options.Value;
    public DbConnection Create() => _options.Provider.ToLowerInvariant() switch
    {
        "sqlserver" => new SqlConnection(_options.ConnectionString),
        "mysql" => new MySqlConnection(_options.ConnectionString),
        _ => throw new InvalidOperationException("Database:Provider debe ser SqlServer o MySql.")
    };
}

public sealed class DapperSecurityStore(IDbConnectionFactory connections) : IUserRepository, IFinePermissionRepository, ISessionRepository, IOtpRepository, ISecurityAdminRepository, IUserSiteRepository, IUserOperationTypeRepository, IUserBusinessUnitRepository, IUserModuleRepository, IUserMenuRepository, IUserSubmenuRepository, ICompanyRepository, IIntegratedLogisticsRepository, ISecurityMasterRepository, ISecurityMasterUserRepository, IUserNavigationAssignmentRepository, IUserAccessRepository, IFormOptionMasterRepository, IUserProfileRepository
{
    private static CommandDefinition Sp(string name, object? args, CancellationToken ct) => new(name, args, commandType: CommandType.StoredProcedure, cancellationToken: ct);

    public async Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<AuthenticableUserRow>(
            Sp("SP_TAS006_OBTENER_AUTENTICABLE", new { usuario = userName }, ct));
        if (row is null || !Guid.TryParse(row.IDUSER, out var id)) return null;
        return new UserAccount(id, row.USUARIO, row.EMAIL ?? string.Empty, row.PASSWORD_HASH, true);
    }
    public async Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<UserAccountRow>(Sp("usp_security_user_get_by_id",new{p_user_id=userId},ct));
        return row is null || !Guid.TryParse(row.Id, out var id) ? null : new UserAccount(id,row.UserName,row.Email ?? string.Empty,row.PasswordHash,row.IsActive);
    }
    public async Task<bool> HasPermissionAsync(Guid userId, string code, CancellationToken ct) { await using var db=connections.Create(); return await db.QuerySingleAsync<int>(Sp("usp_security_permission_has_code",new{p_user_id=userId.ToString(),p_code=code},ct))==1; }
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId,CancellationToken ct) { await using var db=connections.Create(); return (await db.QueryAsync<string>(Sp("usp_security_permission_codes_by_user",new{p_user_id=userId},ct))).AsList(); }
    public async Task CreateAsync(AuthSession session,CancellationToken ct) { await using var db=connections.Create(); await db.ExecuteAsync(Sp("usp_security_session_create",new{p_id=session.Id,p_user_id=session.UserId,p_expires_at=session.ExpiresAt,p_created_at=DateTimeOffset.UtcNow},ct)); }
    public async Task<bool> IsActiveAsync(Guid sessionId,Guid userId,CancellationToken ct) { await using var db=connections.Create(); return await db.QuerySingleAsync<int>(Sp("usp_security_session_is_active",new{p_session_id=sessionId,p_user_id=userId,p_now=DateTimeOffset.UtcNow},ct))==1; }
    public async Task<bool> RenewAsync(Guid sessionId,Guid userId,DateTimeOffset now,DateTimeOffset expiresAt,CancellationToken ct) { await using var db=connections.Create(); return await db.QuerySingleAsync<int>(Sp("usp_security_session_renew",new{p_session_id=sessionId,p_user_id=userId,p_now=now,p_expires_at=expiresAt},ct))==1; }
    public async Task RevokeAsync(Guid sessionId,Guid userId,CancellationToken ct) { await using var db=connections.Create(); await db.ExecuteAsync(Sp("usp_security_session_revoke",new{p_session_id=sessionId,p_user_id=userId},ct)); }
    public async Task SaveAsync(OtpChallenge challenge,CancellationToken ct) { await using var db=connections.Create(); await db.ExecuteAsync(Sp("usp_security_otp_create",new{p_id=challenge.Id,p_user_id=challenge.UserId,p_otp_hash=challenge.OtpHash,p_expires_at=challenge.ExpiresAt,p_created_at=DateTimeOffset.UtcNow},ct)); }
    public async Task<OtpChallenge?> FindAsync(Guid challengeId,CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<OtpChallengeRow>(Sp("usp_security_otp_get",new{p_challenge_id=challengeId},ct));
        if (row is null || !Guid.TryParse(row.Id,out var id) || !Guid.TryParse(row.UserId,out var userId)) return null;
        var expiresAt = new DateTimeOffset(DateTime.SpecifyKind(row.ExpiresAt,DateTimeKind.Utc));
        return new OtpChallenge(id,userId,row.OtpHash,expiresAt,row.IsConsumed);
    }
    public async Task<bool> ConsumeAsync(Guid challengeId,CancellationToken ct) { await using var db=connections.Create(); return await db.QuerySingleAsync<int>(Sp("usp_security_otp_consume",new{p_challenge_id=challengeId,p_now=DateTimeOffset.UtcNow},ct))==1; }
    public async Task<IReadOnlyCollection<UserSite>> GetByUserNameAsync(string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<UserSiteRow>(
            Sp("SP_TAS024_LISTAR_SEDES_USUARIO", new { usuario = userName }, ct));
        var sites = new List<UserSite>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.IDSEDE, out var siteId))
                throw new DataException("SP_TAS024_LISTAR_SEDES_USUARIO devolvió un IDSEDE inválido.");
            sites.Add(new UserSite(siteId, row.NOMSEDE, row.NOMABRV, row.DIRSEDE, row.TELFSEDE, row.ESPREDETERMINADA));
        }
        return sites;
    }
    async Task<IReadOnlyCollection<UserOperationType>> IUserOperationTypeRepository.GetByUserNameAsync(string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<UserOperationTypeRow>(
            Sp("SP_TAM070_LISTAR_TIPOS_OPERACION_USUARIO", new { usuario = userName }, ct));
        return rows.Select(row => new UserOperationType(row.IDTIPOPE, row.TIPOOPEP)).ToArray();
    }
    async Task<IReadOnlyCollection<UserBusinessUnit>> IUserBusinessUnitRepository.GetByUserNameAsync(string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<UserBusinessUnitRow>(
            Sp("SP_TAM022_LISTAR_UNIDADES_NEGOCIO_USUARIO", new { usuario = userName }, ct));
        return rows.Select(row => new UserBusinessUnit(row.IDUNDIV, row.NOMUNDIV, row.DSCUNDIV, row.IDREGVTA)).ToArray();
    }
    async Task<IReadOnlyCollection<UserModule>> IUserModuleRepository.GetByUserNameAsync(string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<UserModuleRow>(
            Sp("SP_TAS008_LISTAR_MODULOS_USUARIO", new { usuario = userName }, ct));
        return rows.Select(row => new UserModule(row.IDMOD, row.IDAPP, row.NOMAPP, row.NOMMOD, row.DESCMOD)).ToArray();
    }
    async Task<IReadOnlyCollection<UserMenu>> IUserMenuRepository.GetByUserNameAndModuleIdAsync(string userName, string moduleId, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<UserMenuRow>(
            Sp("SP_TAS009_LISTAR_MENUS_USUARIO_MODULO", new { usuario = userName, modulo = moduleId }, ct));
        return rows.Select(row => new UserMenu(row.IDMENU, row.NOMMENU, row.DESCMENU)).ToArray();
    }
    async Task<IReadOnlyCollection<UserSubmenu>> IUserSubmenuRepository.GetByUserNameAndMenuIdAsync(string userName, string menuId, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<UserSubmenuRow>(
            Sp("SP_TAS011_LISTAR_SUBMENUS_USUARIO_MENU", new { usuario = userName, menu = menuId }, ct));
        return rows.Select(row => new UserSubmenu(row.IDSUBMENU, row.NOMSUBMNU, row.DESCSUBMNU, row.VIEWKEY)).ToArray();
    }
    public async Task<IReadOnlyCollection<Company>> SearchAsync(CompanySearchFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<CompanyRow>(Sp("SP_TAS001_CONSULTAR_EMPRESAS", new { idempresa = filter.Id, empresa = filter.Name, ruc = filter.Ruc, estado = filter.Status }, ct));
        return rows.Select(ToCompany).ToArray();
    }
    public async Task<Company> CreateAsync(string companyName, string? ruc, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return ToCompany(await db.QuerySingleAsync<CompanyRow>(Sp("SP_TAS001_CREAR_EMPRESA", new { empresa = companyName, ruc, usuario = userName }, ct)));
    }
    public async Task<Company?> UpdateAsync(string companyId, string companyName, string? ruc, string status, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<CompanyRow>(Sp("SP_TAS001_ACTUALIZAR_EMPRESA", new { idempresa = companyId, empresa = companyName, ruc, estado = status, usuario = userName }, ct));
        return row is null ? null : ToCompany(row);
    }
    public async Task<int> DeactivateAsync(string companyId, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAS001_DAR_BAJA_EMPRESA", new { idempresa = companyId, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<SecurityMasterRecord>> SearchAsync(SecurityMasterType type, SecurityMasterFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<SecurityMasterRow>(Sp(SearchProcedure(type), SearchArgs(type, filter), ct));
        return rows.Select(row => ToMaster(type, row)).ToArray();
    }
    public async Task<SecurityMasterRecord> CreateAsync(SecurityMasterType type, string parentId, string name, string? description, string? viewKey, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<SecurityMasterRow>(Sp(MasterProcedure(type, "CREAR"), CreateArgs(type, parentId, name, description, viewKey, userName), ct));
        return ToMaster(type, row);
    }
    public async Task<SecurityMasterRecord?> UpdateAsync(SecurityMasterType type, string id, string parentId, string name, string? description, string? viewKey, string status, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<SecurityMasterRow>(Sp(MasterProcedure(type, "ACTUALIZAR"), UpdateArgs(type, id, parentId, name, description, viewKey, status, userName), ct));
        return row is null ? null : ToMaster(type, row);
    }
    public async Task<int> DeactivateAsync(SecurityMasterType type, string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp(MasterProcedure(type, "DAR_BAJA"), DeactivateArgs(type, id, userName), ct));
    }

    public async Task<IReadOnlyCollection<IntegratedLogisticsProduct>> SearchProductsAsync(ProductFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<IntegratedLogisticsProductRow>(Sp("SP_TAM001_CONSULTAR_PRODUCTOS", new { idpro = filter.Id, producto = filter.Name, estado = filter.Status }, ct));
        return rows.Select(ToIntegratedLogisticsProduct).ToArray();
    }

    public async Task<IReadOnlyCollection<IntegratedLogisticsProduct>> ListActiveProductsAsync(CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<IntegratedLogisticsProductRow>(Sp("SP_TAM001_LISTAR_PRODUCTOS_ACTIVOS", null, ct));
        // El catálogo activo solo devuelve IDPRO, NOMPRO y DESPRO; el SP ya filtra
        // SACRG = 1 y ESTPRO = 'A', por lo que se completa esa información estable.
        return rows.Select(row => new IntegratedLogisticsProduct(row.IDPRO, row.NOMPRO, row.DESPRO, "A", true, null, null, null, null)).ToArray();
    }

    public async Task<IntegratedLogisticsProduct> CreateProductAsync(CreateProductRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<IntegratedLogisticsProductRow>(Sp("SP_TAM001_CREAR_PRODUCTO", new { producto = request.Name, descripcion = request.Description, usuario = userName }, ct));
        return ToIntegratedLogisticsProduct(row);
    }

    public async Task<IntegratedLogisticsProduct?> UpdateProductAsync(string id, UpdateProductRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<IntegratedLogisticsProductRow>(Sp("SP_TAM001_ACTUALIZAR_PRODUCTO", new { idpro = id, producto = request.Name, descripcion = request.Description, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToIntegratedLogisticsProduct(row);
    }

    public async Task<int> DeactivateProductAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM001_DAR_BAJA_PRODUCTO", new { idpro = id, usuario = userName }, ct));
    }

    public async Task<IReadOnlyCollection<Ubigeo>> ListActiveUbigeosAsync(CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<UbigeoRow>(Sp("SP_TAM034_LISTAR_UBIGEOS_ACTIVOS", null, ct));
        return rows.Select(row => new Ubigeo(row.IDUBIG, row.CODUBIG, row.DSTUBIG, row.PROVUBIG, row.DPROUBIG, row.DESCRIPCIONUBIGEO)).ToArray();
    }

    public async Task<IReadOnlyCollection<IntegratedLogisticsCampaign>> SearchCampaignsAsync(CampaignFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<IntegratedLogisticsCampaignRow>(Sp("SP_TAM002_CONSULTAR_CAMPANAS", new { idcam = filter.Id, idubigeo = filter.UbigeoId, idpro = filter.ProductId, proceso = filter.Process, estado = filter.Status }, ct));
        return rows.Select(ToIntegratedLogisticsCampaign).ToArray();
    }

    public async Task<IntegratedLogisticsCampaign> CreateCampaignAsync(CreateCampaignRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<IntegratedLogisticsCampaignRow>(Sp("SP_TAM002_CREAR_CAMPANA", new { idubigeo = request.UbigeoId, idpro = request.ProductId, fechaInicio = request.StartDate, fechaFin = request.EndDate, proceso = request.Process, descripcion = request.Description, usuario = userName }, ct));
        return ToIntegratedLogisticsCampaign(row);
    }

    public async Task<IntegratedLogisticsCampaign?> UpdateCampaignAsync(string id, UpdateCampaignRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<IntegratedLogisticsCampaignRow>(Sp("SP_TAM002_ACTUALIZAR_CAMPANA", new { idcam = id, idubigeo = request.UbigeoId, idpro = request.ProductId, fechaInicio = request.StartDate, fechaFin = request.EndDate, proceso = request.Process, descripcion = request.Description, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToIntegratedLogisticsCampaign(row);
    }

    public async Task<int> DeactivateCampaignAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM002_DAR_BAJA_CAMPANA", new { idcam = id, usuario = userName }, ct));
    }

    public async Task<IReadOnlyCollection<FixedCost>> SearchFixedCostsAsync(FixedCostFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<FixedCostRow>(Sp("SP_TAM003_CONSULTAR_COSTOS_FIJOS", new { idcosfij = filter.Id, idsede = filter.SiteId, descripcion = filter.Description, estado = filter.Status }, ct));
        return rows.Select(ToFixedCost).ToArray();
    }

    public async Task<IReadOnlyCollection<ActiveSite>> ListActiveSitesAsync(CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<ActiveSiteRow>(Sp("SP_TAM003_LISTAR_SEDES_ACTIVAS", null, ct));
        return rows.Select(row => new ActiveSite(row.IDSEDE, row.NOMSEDE, row.NOMABRV)).ToArray();
    }

    public async Task<FixedCost> CreateFixedCostAsync(CreateFixedCostRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<FixedCostRow>(Sp("SP_TAM003_CREAR_COSTO_FIJO", new { idsede = request.SiteId, descripcion = request.Description, importe = request.Amount, usuario = userName }, ct));
        return ToFixedCost(row);
    }

    public async Task<FixedCost?> UpdateFixedCostAsync(string id, UpdateFixedCostRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<FixedCostRow>(Sp("SP_TAM003_ACTUALIZAR_COSTO_FIJO", new { idcosfij = id, idsede = request.SiteId, descripcion = request.Description, importe = request.Amount, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToFixedCost(row);
    }

    public async Task<int> DeactivateFixedCostAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM003_DAR_BAJA_COSTO_FIJO", new { idcosfij = id, usuario = userName }, ct));
    }

    public async Task<IReadOnlyCollection<Deposit>> SearchDepositsAsync(DepositFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<DepositRow>(Sp("SP_TAM004_CONSULTAR_DEPOSITOS", new { iddepo = filter.Id, idtdepo = filter.DepositTypeId, nombre = filter.Name, estado = filter.Status }, ct));
        return rows.Select(ToDeposit).ToArray();
    }

    public async Task<IReadOnlyCollection<ActiveDepositType>> ListActiveDepositTypesAsync(CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<ActiveDepositTypeRow>(Sp("SP_TAM004_LISTAR_TIPOS_DEPOSITO_ACTIVOS", null, ct));
        return rows.Select(row => new ActiveDepositType(row.IDTDEPO, row.DESCTDEPO)).ToArray();
    }

    public async Task<Deposit> CreateDepositAsync(CreateDepositRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<DepositRow>(Sp("SP_TAM004_CREAR_DEPOSITO", new { idtdepo = request.DepositTypeId, nombre = request.Name, usuario = userName }, ct));
        return ToDeposit(row);
    }

    public async Task<Deposit?> UpdateDepositAsync(string id, UpdateDepositRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<DepositRow>(Sp("SP_TAM004_ACTUALIZAR_DEPOSITO", new { iddepo = id, idtdepo = request.DepositTypeId, nombre = request.Name, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToDeposit(row);
    }

    public async Task<int> DeactivateDepositAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM004_DAR_BAJA_DEPOSITO", new { iddepo = id, usuario = userName }, ct));
    }

    public async Task<IReadOnlyCollection<ContainerDimension>> SearchContainerDimensionsAsync(MasterTableFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<ContainerDimensionRow>(Sp("SP_TAM006_CONSULTAR_DIMENSIONES_CONTENEDOR", new { iddimcon = filter.Id, dimension = filter.Name, estado = filter.Status }, ct));
        return rows.Select(ToContainerDimension).ToArray();
    }
    public async Task<ContainerDimension> CreateContainerDimensionAsync(CreateContainerDimensionRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<ContainerDimensionRow>(Sp("SP_TAM006_CREAR_DIMENSION_CONTENEDOR", new { dimension = request.Dimension, usuario = userName }, ct));
        return ToContainerDimension(row);
    }
    public async Task<ContainerDimension?> UpdateContainerDimensionAsync(string id, UpdateContainerDimensionRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<ContainerDimensionRow>(Sp("SP_TAM006_ACTUALIZAR_DIMENSION_CONTENEDOR", new { iddimcon = id, dimension = request.Dimension, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToContainerDimension(row);
    }
    public async Task<int> DeactivateContainerDimensionAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM006_DAR_BAJA_DIMENSION_CONTENEDOR", new { iddimcon = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<EntityAddressType>> SearchEntityAddressTypesAsync(MasterTableFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<EntityAddressTypeRow>(Sp("SP_TAM007_CONSULTAR_TIPOS_DIRECCION", new { idtipdir = filter.Id, descripcion = filter.Name, estado = filter.Status }, ct));
        return rows.Select(ToEntityAddressType).ToArray();
    }
    public async Task<EntityAddressType> CreateEntityAddressTypeAsync(CreateEntityAddressTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<EntityAddressTypeRow>(Sp("SP_TAM007_CREAR_TIPO_DIRECCION", new { descripcion = request.Description, usuario = userName }, ct));
        return ToEntityAddressType(row);
    }
    public async Task<EntityAddressType?> UpdateEntityAddressTypeAsync(string id, UpdateEntityAddressTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<EntityAddressTypeRow>(Sp("SP_TAM007_ACTUALIZAR_TIPO_DIRECCION", new { idtipdir = id, descripcion = request.Description, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToEntityAddressType(row);
    }
    public async Task<int> DeactivateEntityAddressTypeAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM007_DAR_BAJA_TIPO_DIRECCION", new { idtipdir = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<PaymentType>> SearchPaymentTypesAsync(MasterTableFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<PaymentTypeRow>(Sp("SP_TAM008_CONSULTAR_TIPOS_PAGO", new { idtpag = filter.Id, descripcion = filter.Name, estado = filter.Status }, ct));
        return rows.Select(ToPaymentType).ToArray();
    }
    public async Task<PaymentType> CreatePaymentTypeAsync(CreatePaymentTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<PaymentTypeRow>(Sp("SP_TAM008_CREAR_TIPO_PAGO", new { descripcion = request.Description, usuario = userName }, ct));
        return ToPaymentType(row);
    }
    public async Task<PaymentType?> UpdatePaymentTypeAsync(string id, UpdatePaymentTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<PaymentTypeRow>(Sp("SP_TAM008_ACTUALIZAR_TIPO_PAGO", new { idtpag = id, descripcion = request.Description, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToPaymentType(row);
    }
    public async Task<int> DeactivatePaymentTypeAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM008_DAR_BAJA_TIPO_PAGO", new { idtpag = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<ShippingLine>> SearchShippingLinesAsync(ShippingLineFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<ShippingLineRow>(Sp("SP_TAM010_CONSULTAR_LINEAS_NAVIERAS", new { idlinnav = filter.Id, descripcion = filter.Description, codsunat = filter.SunatCode, estado = filter.Status }, ct));
        return rows.Select(ToShippingLine).ToArray();
    }
    public async Task<ShippingLine> CreateShippingLineAsync(CreateShippingLineRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<ShippingLineRow>(Sp("SP_TAM010_CREAR_LINEA_NAVIERA", new { descripcion = request.Description, codsunat = request.SunatCode, usuario = userName }, ct));
        return ToShippingLine(row);
    }
    public async Task<ShippingLine?> UpdateShippingLineAsync(string id, UpdateShippingLineRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<ShippingLineRow>(Sp("SP_TAM010_ACTUALIZAR_LINEA_NAVIERA", new { idlinnav = id, descripcion = request.Description, codsunat = request.SunatCode, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToShippingLine(row);
    }
    public async Task<int> DeactivateShippingLineAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM010_DAR_BAJA_LINEA_NAVIERA", new { idlinnav = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<PickupPlace>> SearchPickupPlacesAsync(MasterTableFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<PickupPlaceRow>(Sp("SP_TAM011_CONSULTAR_LUGARES_RECOJO", new { idlugrec = filter.Id, descripcion = filter.Name, estado = filter.Status }, ct));
        return rows.Select(ToPickupPlace).ToArray();
    }
    public async Task<PickupPlace> CreatePickupPlaceAsync(CreatePickupPlaceRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<PickupPlaceRow>(Sp("SP_TAM011_CREAR_LUGAR_RECOJO", new { descripcion = request.Description, usuario = userName }, ct));
        return ToPickupPlace(row);
    }
    public async Task<PickupPlace?> UpdatePickupPlaceAsync(string id, UpdatePickupPlaceRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<PickupPlaceRow>(Sp("SP_TAM011_ACTUALIZAR_LUGAR_RECOJO", new { idlugrec = id, descripcion = request.Description, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToPickupPlace(row);
    }
    public async Task<int> DeactivatePickupPlaceAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM011_DAR_BAJA_LUGAR_RECOJO", new { idlugrec = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<Currency>> SearchCurrenciesAsync(CurrencyFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<CurrencyRow>(Sp("SP_TAM013_CONSULTAR_MONEDAS", new { idmon = filter.Id, descripcion = filter.Description, simbolo = filter.Symbol, estado = filter.Status }, ct));
        return rows.Select(ToCurrency).ToArray();
    }
    public async Task<Currency> CreateCurrencyAsync(CreateCurrencyRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<CurrencyRow>(Sp("SP_TAM013_CREAR_MONEDA", new { descripcion = request.Description, simbolo = request.Symbol, usuario = userName }, ct));
        return ToCurrency(row);
    }
    public async Task<Currency?> UpdateCurrencyAsync(string id, UpdateCurrencyRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<CurrencyRow>(Sp("SP_TAM013_ACTUALIZAR_MONEDA", new { idmon = id, descripcion = request.Description, simbolo = request.Symbol, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToCurrency(row);
    }
    public async Task<int> DeactivateCurrencyAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM013_DAR_BAJA_MONEDA", new { idmon = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<CurrentAccountDocumentType>> SearchCurrentAccountDocumentTypesAsync(CurrentAccountDocumentTypeFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<CurrentAccountDocumentTypeRow>(Sp("SP_TAM014_CONSULTAR_TIPO_DOCUMENTO_CTA_CTE", new { idtipctacte = filter.Id, codigo = filter.Code, descripcion = filter.Description, estado = filter.Status }, ct));
        return rows.Select(ToCurrentAccountDocumentType).ToArray();
    }
    public async Task<CurrentAccountDocumentType> CreateCurrentAccountDocumentTypeAsync(CreateCurrentAccountDocumentTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<CurrentAccountDocumentTypeRow>(Sp("SP_TAM014_CREAR_TIPO_DOCUMENTO_CTA_CTE", new { codigo = request.Code, descripcion = request.Description, usuario = userName }, ct));
        return ToCurrentAccountDocumentType(row);
    }
    public async Task<CurrentAccountDocumentType?> UpdateCurrentAccountDocumentTypeAsync(string id, UpdateCurrentAccountDocumentTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<CurrentAccountDocumentTypeRow>(Sp("SP_TAM014_ACTUALIZAR_TIPO_DOCUMENTO_CTA_CTE", new { idtipctacte = id, codigo = request.Code, descripcion = request.Description, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToCurrentAccountDocumentType(row);
    }
    public async Task<int> DeactivateCurrentAccountDocumentTypeAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM014_DAR_BAJA_TIPO_DOCUMENTO_CTA_CTE", new { idtipctacte = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<PackagingType>> SearchPackagingTypesAsync(MasterTableFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<PackagingTypeRow>(Sp("SP_TAM015_CONSULTAR_TIPOS_EMBALAJE", new { idtemblj = filter.Id, descripcion = filter.Name, estado = filter.Status }, ct));
        return rows.Select(ToPackagingType).ToArray();
    }
    public async Task<PackagingType> CreatePackagingTypeAsync(CreatePackagingTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<PackagingTypeRow>(Sp("SP_TAM015_CREAR_TIPO_EMBALAJE", new { descripcion = request.Description, usuario = userName }, ct));
        return ToPackagingType(row);
    }
    public async Task<PackagingType?> UpdatePackagingTypeAsync(string id, UpdatePackagingTypeRequest request, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<PackagingTypeRow>(Sp("SP_TAM015_ACTUALIZAR_TIPO_EMBALAJE", new { idtemblj = id, descripcion = request.Description, estado = request.Status, usuario = userName }, ct));
        return row is null ? null : ToPackagingType(row);
    }
    public async Task<int> DeactivatePackagingTypeAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAM015_DAR_BAJA_TIPO_EMBALAJE", new { idtemblj = id, usuario = userName }, ct));
    }
    public async Task<IReadOnlyCollection<SecurityMasterUser>> SearchAsync(SecurityUserFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<SecurityMasterUserRow>(Sp("SP_TAS006_CONSULTAR_USUARIOS", new { iduser=filter.Id, iddominio=filter.DomainId, idrol=filter.RoleId, login=filter.Login, email=filter.Email, estado=filter.Status }, ct));
        return rows.Select(ToMasterUser).ToArray();
    }
    public async Task<SecurityMasterUser> CreateAsync(CreateSecurityMasterUserRequest request, byte[] passwordHash, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<SecurityMasterUserRow>(Sp("SP_TAS006_CREAR_USUARIO", new { iddominio=request.DomainId, idrol=request.RoleId, login=request.Login, clave=passwordHash, nombres=request.FirstNames, apepat=request.PaternalLastName, apemat=request.MaternalLastName, email=request.Email, cambiaclave=request.ChangePassword, multiplesedes=request.MultipleSites, sededefecto=request.DefaultSiteId, usuario=userName }, ct));
        return ToMasterUser(row);
    }
    public async Task<SecurityMasterUser?> UpdateAsync(string id, UpdateSecurityMasterUserRequest request, byte[]? passwordHash, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<SecurityMasterUserRow>(Sp("SP_TAS006_ACTUALIZAR_USUARIO", new { iduser=id, iddominio=request.DomainId, idrol=request.RoleId, login=request.Login, clave=passwordHash, nombres=request.FirstNames, apepat=request.PaternalLastName, apemat=request.MaternalLastName, email=request.Email, estado=request.Status, cambiaclave=request.ChangePassword, multiplesedes=request.MultipleSites, sededefecto=request.DefaultSiteId, usuario=userName }, ct));
        return row is null ? null : ToMasterUser(row);
    }
    async Task<int> ISecurityMasterUserRepository.DeactivateAsync(string id, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAS006_DAR_BAJA_USUARIO", new { iduser=id, usuario=userName }, ct));
    }
    public async Task<PasswordResetResult?> ResetPasswordAsync(string id, byte[]? passwordHash, string userName, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<PasswordResetRow>(Sp("SP_TAS006_REINICIAR_CLAVE", new { iduser=id, clave=passwordHash, usuario=userName }, ct));
        return row is null ? null : new PasswordResetResult(row.IDUSER, row.USUARIO, row.ESTUSER, row.FLGACTPASS, row.CULUSA, row.FCHHRAACT, row.FILASAFECTADAS);
    }
    public async Task<UserModuleAssignment> AssignModuleAsync(string userId, string moduleId, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<UserModuleAssignmentRow>(Sp("SP_TAS008_ASIGNAR_MODULO_USUARIO", new { iduser=userId, idmodulo=moduleId, usuario=auditUser }, ct));
        return new UserModuleAssignment(row.IDMODUSU, row.IDMOD, row.IDUSER, row.ESTMODUSU, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    }
    public async Task<UserMenuAssignment> AssignMenuAsync(string userId, string menuId, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<UserMenuAssignmentRow>(Sp("SP_TAS010_ASIGNAR_MENU_USUARIO", new { iduser=userId, idmenu=menuId, usuario=auditUser }, ct));
        return new UserMenuAssignment(row.IDMENUUSU, row.IDMENU, row.IDUSER, row.ESTMENUUSU, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    }
    public async Task<UserSubmenuAssignment> AssignSubmenuAsync(string userId, string submenuId, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<UserSubmenuAssignmentRow>(Sp("SP_TAS012_ASIGNAR_SUBMENU_USUARIO", new { iduser=userId, idsubmenu=submenuId, usuario=auditUser }, ct));
        return new UserSubmenuAssignment(row.IDSMUSER, row.IDSUBMENU, row.IDUSER, row.ESTSMUSER, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    }
    public async Task<IReadOnlyCollection<SecurityMenu>> GetMenusAsync(string moduleId, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<SecurityMenuRow>(Sp("SP_TAS009_LISTAR_MENUS_POR_MODULO", new { idmodulo = moduleId }, ct));
        return rows.Select(row => new SecurityMenu(row.IDMENU, row.IDMOD, row.NOMMENU, row.DESCMENU, row.ESTMENU, row.SACRG)).ToArray();
    }
    public async Task<IReadOnlyCollection<SecuritySubmenu>> GetSubmenusAsync(string menuId, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<SecuritySubmenuRow>(Sp("SP_TAS011_LISTAR_SUBMENUS_POR_MENU", new { idmenu = menuId }, ct));
        return rows.Select(row => new SecuritySubmenu(row.IDSUBMENU, row.IDMENU, row.NOMSUBMNU, row.DESCSUBMNU, row.ESTSUBMNU, row.SACRG)).ToArray();
    }
    public async Task<IReadOnlyCollection<SecurityFormOption>> GetFormOptionsAsync(string submenuId, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<SecurityFormOptionRow>(Sp("SP_TAS015_LISTAR_OPCIONES_POR_SUBMENU", new { idsubmenu = submenuId }, ct));
        return rows.Select(row => new SecurityFormOption(row.IDOPCFOMR, row.IDSUBMENU, row.IDSMSN, row.NOMOPC, row.DESCOPC, row.ESTOPC, row.SACRG)).ToArray();
    }
    public async Task<UserAccessSnapshot> GetAccessAsync(string userId, CancellationToken ct)
    {
        await using var db = connections.Create();
        using var rows = await db.QueryMultipleAsync(Sp("SP_TAS_USER_ACCESS_GET", new { iduser = userId }, ct));
        var modules = (await rows.ReadAsync<UserAccessRow>()).Select(ToAccessItem).ToArray();
        var menus = (await rows.ReadAsync<UserAccessRow>()).Select(ToAccessItem).ToArray();
        var submenus = (await rows.ReadAsync<UserAccessRow>()).Select(ToAccessItem).ToArray();
        var formOptions = (await rows.ReadAsync<UserAccessRow>()).Select(ToAccessItem).ToArray();
        return new UserAccessSnapshot(modules, menus, submenus, formOptions);
    }
    public async Task<UserFormOptionAssignment> AssignFormOptionAsync(string userId, string optionId, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleAsync<UserFormOptionAssignmentRow>(Sp("SP_TAS016_ASIGNAR_OPCION_USUARIO", new { iduser = userId, idopcion = optionId, usuario = auditUser }, ct));
        return new UserFormOptionAssignment(row.IDOPCFORMUSU, row.IDOPCFOMR, row.IDUSER, row.ESTOPC, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    }
    public async Task<bool> RemoveAccessAsync(string userId, string accessId, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAS_USER_ACCESS_REMOVE", new { iduser = userId, idaccess = accessId, usuario = auditUser }, ct)) == 1;
    }
    public async Task<IReadOnlyCollection<FormOptionMaster>> SearchAsync(FormOptionMasterFilter filter, CancellationToken ct)
    {
        await using var db = connections.Create();
        var rows = await db.QueryAsync<FormOptionMasterRow>(Sp("SP_TAS015_CONSULTAR_OPCIONES", new { idopcfomr=filter.Id, idsubmenu=filter.SubmenuId, idsmsn=filter.SecondLevelId, nomopc=filter.Name, estado=filter.Status }, ct));
        return rows.Select(ToFormOptionMaster).ToArray();
    }
    public async Task<FormOptionMaster> CreateAsync(FormOptionMasterRequest request, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        return ToFormOptionMaster(await db.QuerySingleAsync<FormOptionMasterRow>(Sp("SP_TAS015_CREAR_OPCION", new { idsubmenu=request.SubmenuId, idsmsn=request.SecondLevelId, nomopc=request.Code, descopc=request.Description ?? request.Name, color=request.Color, icono=request.Icon, usuario=auditUser }, ct)));
    }
    public async Task<FormOptionMaster?> UpdateAsync(string id, FormOptionMasterRequest request, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        var row = await db.QuerySingleOrDefaultAsync<FormOptionMasterRow>(Sp("SP_TAS015_ACTUALIZAR_OPCION", new { idopcfomr=id, idsubmenu=request.SubmenuId, idsmsn=request.SecondLevelId, nomopc=request.Code, descopc=request.Description ?? request.Name, color=request.Color, icono=request.Icon, estado=request.Status, usuario=auditUser }, ct));
        return row is null ? null : ToFormOptionMaster(row);
    }
    async Task<int> IFormOptionMasterRepository.DeactivateAsync(string id, string auditUser, CancellationToken ct)
    {
        await using var db = connections.Create();
        return await db.QuerySingleAsync<int>(Sp("SP_TAS015_DAR_BAJA_OPCION", new { idopcfomr=id, usuario=auditUser }, ct));
    }
    public async Task<IReadOnlyCollection<UserSummary>> GetUsersAsync(CancellationToken ct) { await using var db=connections.Create(); return (await db.QueryAsync<UserSummary>(Sp("usp_security_users_list",null,ct))).AsList(); }
    public async Task<Guid> CreateUserAsync(string userName,string email,string passwordHash,CancellationToken ct) { var id=Guid.NewGuid(); await using var db=connections.Create(); await db.ExecuteAsync(Sp("usp_security_user_create",new{p_id=id,p_user_name=userName,p_email=email,p_password_hash=passwordHash,p_created_at=DateTimeOffset.UtcNow},ct)); return id; }
    public async Task<bool> UpdateUserAsync(Guid id,string email,bool isActive,CancellationToken ct) { await using var db=connections.Create(); return await db.QuerySingleAsync<int>(Sp("usp_security_user_update",new{p_id=id,p_email=email,p_is_active=isActive,p_updated_at=DateTimeOffset.UtcNow},ct))==1; }
    public async Task<bool> DeactivateUserAsync(Guid id,CancellationToken ct) { await using var db=connections.Create(); return await db.QuerySingleAsync<int>(Sp("usp_security_user_deactivate",new{p_id=id,p_updated_at=DateTimeOffset.UtcNow},ct))==1; }
    public async Task<IReadOnlyCollection<SecurityProfile>> GetProfilesAsync(CancellationToken ct) { await using var db=connections.Create(); return (await db.QueryAsync<SecurityProfile>(Sp("usp_security_profiles_list",null,ct))).AsList(); }
    public async Task<Guid> CreateProfileAsync(string code,string name,CancellationToken ct) { var id=Guid.NewGuid(); await using var db=connections.Create(); await db.ExecuteAsync(Sp("usp_security_profile_create",new{p_id=id,p_code=code,p_name=name,p_created_at=DateTimeOffset.UtcNow},ct)); return id; }
    public async Task<bool> UpdateProfileAsync(Guid id,string name,bool isActive,CancellationToken ct) { await using var db=connections.Create(); return await db.QuerySingleAsync<int>(Sp("usp_security_profile_update",new{p_id=id,p_name=name,p_is_active=isActive,p_updated_at=DateTimeOffset.UtcNow},ct))==1; }
    public async Task<IReadOnlyCollection<SecurityPermission>> GetPermissionsAsync(CancellationToken ct) { await using var db=connections.Create(); return (await db.QueryAsync<SecurityPermission>(Sp("usp_security_permissions_list",null,ct))).AsList(); }
    public async Task AssignProfileAsync(Guid userId,Guid profileId,CancellationToken ct) { await using var db=connections.Create(); await db.ExecuteAsync(Sp("usp_security_user_profile_assign",new{p_user_id=userId,p_profile_id=profileId},ct)); }
    public async Task ReplaceProfilePermissionsAsync(Guid profileId,IReadOnlyCollection<Guid> permissionIds,CancellationToken ct) { await using var db=connections.Create(); await db.ExecuteAsync(Sp("usp_security_profile_permissions_replace",new{p_profile_id=profileId,p_permission_ids_json=System.Text.Json.JsonSerializer.Serialize(permissionIds)},ct)); }

    public async Task<IReadOnlyCollection<UserProfile>> ListProfilesAsync(Guid? profileId, Guid? applicationId, string? name, string? status, CancellationToken ct)
    { await using var db=connections.Create(); var rows=await db.QueryAsync<ProfileRow>(Sp("SP_TAS017_CONSULTAR_PERFILES",new { idperfil=profileId, idapp=applicationId, nombre=name, estado=status },ct)); return rows.Select(MapProfile).ToArray(); }
    public async Task<UserProfile> CreateProfileAsync(Guid? applicationId,string name,string? description,string actor,CancellationToken ct)
    { await using var db=connections.Create(); var row=await db.QuerySingleAsync<ProfileRow>(Sp("SP_TAS017_CREAR_PERFIL",new { idapp=applicationId, nombre=name, descripcion=description, usuario=actor },ct)); return MapProfile(row); }
    public async Task<UserProfile?> UpdateProfileAsync(Guid profileId,Guid? applicationId,string name,string? description,string status,string actor,CancellationToken ct)
    { await using var db=connections.Create(); var row=await db.QuerySingleOrDefaultAsync<ProfileRow>(Sp("SP_TAS017_ACTUALIZAR_PERFIL",new { idperfil=profileId, idapp=applicationId, nombre=name, descripcion=description, estado=status, usuario=actor },ct)); return row is null?null:MapProfile(row); }
    public async Task<bool> DeactivateProfileAsync(Guid profileId,string actor,CancellationToken ct)
    { await using var db=connections.Create(); return (await db.ExecuteAsync(Sp("SP_TAS017_DAR_BAJA_PERFIL",new { idperfil=profileId, usuario=actor },ct))) >= 0; }
    public async Task<IReadOnlyCollection<ProfileAccessItem>> ListAccessAsync(ProfileAccessKind kind,Guid profileId,Guid? parentId,CancellationToken ct)
    { await using var db=connections.Create(); var rows=await db.QueryAsync<ProfileAccessRow>(Sp(ListProfileProcedure(kind),ListProfileArgs(kind,profileId,parentId),ct)); return rows.Select(MapProfileAccess).ToArray(); }
    public async Task<ProfileAccessItem> AssignAccessAsync(ProfileAccessKind kind,Guid profileId,Guid resourceId,string actor,CancellationToken ct)
    { await using var db=connections.Create(); var rows=await db.QueryAsync<ProfileAccessRow>(Sp(AssignProfileProcedure(kind),AssignProfileArgs(kind,profileId,resourceId,actor),ct)); return rows.Select(MapProfileAccess).First(x=>x.ResourceId==resourceId); }
    public async Task<bool> RemoveAccessAsync(Guid accessId,string actor,CancellationToken ct)
    { await using var db=connections.Create(); return (await db.ExecuteAsync(Sp("SP_TAS_PERFIL_RETIRAR_ASIGNACION",new { accessid=accessId, usuario=actor },ct))) >= 0; }
    public async Task<ProfileApplicationResult> ApplyProfileAsync(Guid profileId,Guid userId,string actor,CancellationToken ct)
    { await using var db=connections.Create(); var row=await db.QuerySingleAsync<ProfileApplicationRow>(Sp("SP_TAS017_APLICAR_PERFIL_USUARIO",new { idperfil=profileId,iduser=userId,usuario=actor },ct)); return new(Guid.Parse(row.PROFILEID),Guid.Parse(row.USERID),row.MODULES,row.MENUS,row.SUBMENUS,row.SECONDLEVELS,row.OPTIONS); }

    private sealed class AuthenticableUserRow
    {
        public string IDUSER { get; init; } = string.Empty;
        public string USUARIO { get; init; } = string.Empty;
        public string PASSWORD_HASH { get; init; } = string.Empty;
        public string? EMAIL { get; init; }
    }

    private sealed class UserAccountRow
    {
        public string Id { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string PasswordHash { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    private sealed class OtpChallengeRow
    {
        public string Id { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public string OtpHash { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public bool IsConsumed { get; init; }
    }

    private sealed class UserSiteRow
    {
        public string IDSEDE { get; init; } = string.Empty;
        public string NOMSEDE { get; init; } = string.Empty;
        public string? NOMABRV { get; init; }
        public string? DIRSEDE { get; init; }
        public string? TELFSEDE { get; init; }
        public bool ESPREDETERMINADA { get; init; }
    }

    private sealed class UserOperationTypeRow
    {
        public string IDTIPOPE { get; init; } = string.Empty;
        public string TIPOOPEP { get; init; } = string.Empty;
    }

    private sealed class UserBusinessUnitRow
    {
        public string IDUNDIV { get; init; } = string.Empty;
        public string NOMUNDIV { get; init; } = string.Empty;
        public string? DSCUNDIV { get; init; }
        public string IDREGVTA { get; init; } = string.Empty;
    }

    private sealed class UserModuleRow
    {
        public string IDMOD { get; init; } = string.Empty;
        public string IDAPP { get; init; } = string.Empty;
        public string NOMAPP { get; init; } = string.Empty;
        public string NOMMOD { get; init; } = string.Empty;
        public string? DESCMOD { get; init; }
    }

    private sealed class UserMenuRow
    {
        public string IDMENU { get; init; } = string.Empty;
        public string NOMMENU { get; init; } = string.Empty;
        public string? DESCMENU { get; init; }
    }

    private sealed class UserSubmenuRow
    {
        public string IDSUBMENU { get; init; } = string.Empty;
        public string NOMSUBMNU { get; init; } = string.Empty;
        public string? DESCSUBMNU { get; init; }
        public string? VIEWKEY { get; init; }
    }

    private sealed class CompanyRow
    {
        public string IDEMPRESA { get; init; } = string.Empty;
        public string NOMEMPRESA { get; init; } = string.Empty;
        public string? NRORUC { get; init; }
        public string ESTEMP { get; init; } = string.Empty;
        public bool SACRG { get; init; }
        public string? CUSCRT { get; init; }
        public DateTime? FCHHRACRT { get; init; }
        public string? CULUSA { get; init; }
        public DateTime? FCHHRAACT { get; init; }
    }

    private static Company ToCompany(CompanyRow row) => new(row.IDEMPRESA, row.NOMEMPRESA, row.NRORUC, row.ESTEMP, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);

    private sealed class IntegratedLogisticsProductRow
    {
        public string IDPRO { get; init; } = string.Empty;
        public string NOMPRO { get; init; } = string.Empty;
        public string? DESPRO { get; init; }
        public string ESTPRO { get; init; } = string.Empty;
        public bool SACRG { get; init; }
        public string? CUSCRT { get; init; }
        public DateTime? FCHHRACRT { get; init; }
        public string? CULUSA { get; init; }
        public DateTime? FCHHRAACT { get; init; }
    }

    private sealed class UbigeoRow
    {
        public string IDUBIG { get; init; } = string.Empty;
        public string? CODUBIG { get; init; }
        public string? DSTUBIG { get; init; }
        public string? PROVUBIG { get; init; }
        public string? DPROUBIG { get; init; }
        public string? DESCRIPCIONUBIGEO { get; init; }
    }

    private sealed class IntegratedLogisticsCampaignRow
    {
        public string IDCAM { get; init; } = string.Empty;
        public string IDUBIG { get; init; } = string.Empty;
        public string? CODUBIG { get; init; }
        public string? DSTUBIG { get; init; }
        public string? PROVUBIG { get; init; }
        public string? DPROUBIG { get; init; }
        public string IDPRO { get; init; } = string.Empty;
        public string? NOMPRO { get; init; }
        public string? DESPRO { get; init; }
        public DateTime INICAM { get; init; }
        public DateTime FINCAM { get; init; }
        public string PROCAM { get; init; } = string.Empty;
        public string? DESCAM { get; init; }
        public string ESTCAM { get; init; } = string.Empty;
        public bool SACRG { get; init; }
        public string? CUSCRT { get; init; }
        public DateTime? FCHHRACRT { get; init; }
        public string? CULUSA { get; init; }
        public DateTime? FCHHRAACT { get; init; }
    }

    private static IntegratedLogisticsProduct ToIntegratedLogisticsProduct(IntegratedLogisticsProductRow row) => new(row.IDPRO, row.NOMPRO, row.DESPRO, row.ESTPRO, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private static IntegratedLogisticsCampaign ToIntegratedLogisticsCampaign(IntegratedLogisticsCampaignRow row) => new(row.IDCAM, row.IDUBIG, row.CODUBIG, row.DSTUBIG, row.PROVUBIG, row.DPROUBIG, row.IDPRO, row.NOMPRO, row.DESPRO, DateOnly.FromDateTime(row.INICAM), DateOnly.FromDateTime(row.FINCAM), row.PROCAM, row.DESCAM, row.ESTCAM, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);

    private sealed class FixedCostRow
    {
        public string IDCOSFIJ { get; init; } = string.Empty;
        public string IDSEDE { get; init; } = string.Empty;
        public string? NOMSEDE { get; init; }
        public string DESCCOSFIJ { get; init; } = string.Empty;
        public decimal IMPCOSFIJ { get; init; }
        public string ESTCOSFIJ { get; init; } = string.Empty;
        public bool SACRG { get; init; }
        public string? CUSCRT { get; init; }
        public DateTime? FCHHRACRT { get; init; }
        public string? CULUSA { get; init; }
        public DateTime? FCHHRAACT { get; init; }
    }
    private sealed class ActiveSiteRow { public string IDSEDE { get; init; } = string.Empty; public string NOMSEDE { get; init; } = string.Empty; public string? NOMABRV { get; init; } }
    private sealed class DepositRow
    {
        public string IDDEPO { get; init; } = string.Empty;
        public string IDTDEPO { get; init; } = string.Empty;
        public string? DESCTDEPO { get; init; }
        public string NOMDEPO { get; init; } = string.Empty;
        public string ESTDEPO { get; init; } = string.Empty;
        public bool SACRG { get; init; }
        public string? CUSCRT { get; init; }
        public DateTime? FCHHRACRT { get; init; }
        public string? CULUSA { get; init; }
        public DateTime? FCHHRAACT { get; init; }
    }
    private sealed class ActiveDepositTypeRow { public string IDTDEPO { get; init; } = string.Empty; public string DESCTDEPO { get; init; } = string.Empty; }
    private static FixedCost ToFixedCost(FixedCostRow row) => new(row.IDCOSFIJ, row.IDSEDE, row.NOMSEDE, row.DESCCOSFIJ, row.IMPCOSFIJ, row.ESTCOSFIJ, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private static Deposit ToDeposit(DepositRow row) => new(row.IDDEPO, row.IDTDEPO, row.DESCTDEPO, row.NOMDEPO, row.ESTDEPO, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private sealed class ContainerDimensionRow { public string IDDIMCON { get; init; } = string.Empty; public string TAMCON { get; init; } = string.Empty; public string ESTCON { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class EntityAddressTypeRow { public string IDTIPDIR { get; init; } = string.Empty; public string DSCTIPDIR { get; init; } = string.Empty; public string ESTTIPDIR { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private static ContainerDimension ToContainerDimension(ContainerDimensionRow row) => new(row.IDDIMCON, row.TAMCON, row.ESTCON, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private static EntityAddressType ToEntityAddressType(EntityAddressTypeRow row) => new(row.IDTIPDIR, row.DSCTIPDIR, row.ESTTIPDIR, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private sealed class PaymentTypeRow { public string IDTPAG { get; init; } = string.Empty; public string DSCTPAG { get; init; } = string.Empty; public string ESTTPAG { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class ShippingLineRow { public string IDLINNAV { get; init; } = string.Empty; public string DESLINNAV { get; init; } = string.Empty; public string? CODSUNAT { get; init; } public string ESTLINNAV { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private static PaymentType ToPaymentType(PaymentTypeRow row) => new(row.IDTPAG, row.DSCTPAG, row.ESTTPAG, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private static ShippingLine ToShippingLine(ShippingLineRow row) => new(row.IDLINNAV, row.DESLINNAV, row.CODSUNAT, row.ESTLINNAV, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private sealed class PickupPlaceRow { public string IDLUGREC { get; init; } = string.Empty; public string DSCLUGREC { get; init; } = string.Empty; public string ESTLUGREC { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private static PickupPlace ToPickupPlace(PickupPlaceRow row) => new(row.IDLUGREC, row.DSCLUGREC, row.ESTLUGREC, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private sealed class CurrencyRow { public string IDMON { get; init; } = string.Empty; public string DESMON { get; init; } = string.Empty; public string SIMMON { get; init; } = string.Empty; public string ESTMON { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private static Currency ToCurrency(CurrencyRow row) => new(row.IDMON, row.DESMON, row.SIMMON, row.ESTMON, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private sealed class CurrentAccountDocumentTypeRow { public string IDTIPCTACTE { get; init; } = string.Empty; public string CODCTACTE { get; init; } = string.Empty; public string DSCCTACTE { get; init; } = string.Empty; public string ESTCTACTE { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class PackagingTypeRow { public string IDTEMBLJ { get; init; } = string.Empty; public string DSCTEMBLJ { get; init; } = string.Empty; public string ESTTEMBLJ { get; init; } = string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private static CurrentAccountDocumentType ToCurrentAccountDocumentType(CurrentAccountDocumentTypeRow row) => new(row.IDTIPCTACTE, row.CODCTACTE, row.DSCCTACTE, row.ESTCTACTE, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);
    private static PackagingType ToPackagingType(PackagingTypeRow row) => new(row.IDTEMBLJ, row.DSCTEMBLJ, row.ESTTEMBLJ, row.SACRG, row.CUSCRT, row.FCHHRACRT, row.CULUSA, row.FCHHRAACT);

    private sealed class SecurityMasterRow { 
        public string? IDAPP { get; init; } 
        public string? IDDOM { get; init; } 
        public string? IDAREA { get; init; } 
        public string? IDROLEMP { get; init; } 
        public string? IDMOD { get; init; } 
        public string? IDMENU { get; init; } 
        public string? IDSUBMENU { get; init; } 
        public string? IDPRO { get; init; } 
        public string? IDEMPRESA { get; init; } 
        public string? NOMAPP { get; init; } 
        public string? DESCAPP { get; init; } 
        public string? ESTAPP { get; init; } 
        public string? NOMDOM { get; init; } 
        public string? DESCDOM { get; init; } 
        public string? ESTDOM { get; init; } 
        public string? NOMAREA { get; init; } 
        public string? DESCAREA { get; init; } 
        public string? ESTAREA { get; init; } 
        public string? NOMROL { get; init; } 
        public string? DESCROL { get; init; } 
        public string? ESTROL { get; init; } 
        public string? NOMMOD { get; init; } 
        public string? DESCMOD { get; init; } 
        public string? ESTMOD { get; init; } 
        public string? NOMMENU { get; init; } 
        public string? DESCMENU { get; init; } 
        public string? ESTMENU { get; init; } 
        public string? NOMSUBMNU { get; init; } 
        public string? DESCSUBMNU { get; init; } 
        public string? VIEWKEY { get; init; }
        public string? ESTSUBMNU { get; init; } 
        public string? NOMPRO { get; init; } 
        public string? DESCPRO { get; init; } 
        public string? ESTPRO { get; init; } 
        public bool SACRG { get; init; } 
        public string? CUSCRT { get; init; } 
        public DateTime? FCHHRACRT { get; init; } 
        public string? CULUSA { get; init; } 
        public DateTime? FCHHRAACT { get; init; } 
    }
    private sealed class SecurityMasterUserRow { public string IDUSER { get; init; }=string.Empty; public string? IDDOMINIO { get; init; } public string? IDROLEMP { get; init; } public string USUARIO { get; init; }=string.Empty; public string? NOMBRES { get; init; } public string? APEPAT { get; init; } public string? APEMAT { get; init; } public string? EMAIL { get; init; } public string ESTUSER { get; init; }=string.Empty; public bool FLGACTPASS { get; init; } public bool FLGMLTSEDE { get; init; } public string? IDSEDEDEFAULT { get; init; } public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class PasswordResetRow { public string IDUSER { get; init; }=string.Empty; public string USUARIO { get; init; }=string.Empty; public string ESTUSER { get; init; }=string.Empty; public bool FLGACTPASS { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } public int FILASAFECTADAS { get; init; } }
    private sealed class UserModuleAssignmentRow { public string IDMODUSU { get; init; }=string.Empty; public string IDMOD { get; init; }=string.Empty; public string IDUSER { get; init; }=string.Empty; public string ESTMODUSU { get; init; }=string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class UserMenuAssignmentRow { public string IDMENUUSU { get; init; }=string.Empty; public string IDMENU { get; init; }=string.Empty; public string IDUSER { get; init; }=string.Empty; public string ESTMENUUSU { get; init; }=string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class UserSubmenuAssignmentRow { public string IDSMUSER { get; init; }=string.Empty; public string IDSUBMENU { get; init; }=string.Empty; public string IDUSER { get; init; }=string.Empty; public string ESTSMUSER { get; init; }=string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class SecurityMenuRow { public string IDMENU { get; init; }=string.Empty; public string IDMOD { get; init; }=string.Empty; public string NOMMENU { get; init; }=string.Empty; public string? DESCMENU { get; init; } public string ESTMENU { get; init; }=string.Empty; public bool SACRG { get; init; } }
    private sealed class SecuritySubmenuRow { public string IDSUBMENU { get; init; }=string.Empty; public string IDMENU { get; init; }=string.Empty; public string NOMSUBMNU { get; init; }=string.Empty; public string? DESCSUBMNU { get; init; } public string? VIEWKEY { get; init; } public string ESTSUBMNU { get; init; }=string.Empty; public bool SACRG { get; init; } }
    private sealed class SecurityFormOptionRow { public string IDOPCFOMR { get; init; }=string.Empty; public string? IDSUBMENU { get; init; } public string? IDSMSN { get; init; } public string NOMOPC { get; init; }=string.Empty; public string? DESCOPC { get; init; } public string ESTOPC { get; init; }=string.Empty; public bool SACRG { get; init; } }
    private sealed class UserFormOptionAssignmentRow { public string IDOPCFORMUSU { get; init; }=string.Empty; public string IDOPCFOMR { get; init; }=string.Empty; public string IDUSER { get; init; }=string.Empty; public string ESTOPC { get; init; }=string.Empty; public bool SACRG { get; init; } public string? CUSCRT { get; init; } public DateTime? FCHHRACRT { get; init; } public string? CULUSA { get; init; } public DateTime? FCHHRAACT { get; init; } }
    private sealed class UserAccessRow { public string? ACCESSID { get; init; } public string RESOURCEID { get; init; }=string.Empty; public string? PARENTID { get; init; } public string NAME { get; init; }=string.Empty; public string? CODE { get; init; } public string? DESCRIPTION { get; init; } public bool CATALOGACTIVE { get; init; } public bool ASSIGNED { get; init; } public bool ASSIGNEDBYPROFILE { get; init; } }
    private sealed class FormOptionMasterRow { public string IDOPCFOMR { get; init; }=string.Empty; public string? IDSUBMENU { get; init; } public string? IDSMSN { get; init; } public string NOMOPC { get; init; }=string.Empty; public string? DESCOPC { get; init; } public string? COLOR { get; init; } public string? ICONO { get; init; } public string ESTOPC { get; init; }=string.Empty; public string? SUBMENU_NAME { get; init; } public string? MENU_NAME { get; init; } public string? MODULE_NAME { get; init; } public string? APPLICATION_NAME { get; init; } }
    private sealed class ProfileRow { public string ID {get;init;}=""; public string? IDAPP {get;init;} public string NAME {get;init;}=""; public string? DESCRIPTION {get;init;} public string STATUS {get;init;}="A"; public bool ISRECORDACTIVE {get;init;} }
    private sealed class ProfileAccessRow { public string RESOURCEID {get;init;}=""; public string? IDMOD {get;init;} public string? IDMENU {get;init;} public string? IDSUBMENU {get;init;} public string? IDSMSN {get;init;} public string? CODE {get;init;} public string NAME {get;init;}=""; public string? DESCRIPTION {get;init;} public bool ASSIGNED {get;init;} public string? ACCESSID {get;init;} }
    private sealed class ProfileApplicationRow { public string PROFILEID {get;init;}=""; public string USERID {get;init;}=""; public int MODULES {get;init;} public int MENUS {get;init;} public int SUBMENUS {get;init;} public int SECONDLEVELS {get;init;} public int OPTIONS {get;init;} }
    private static UserAccessItem ToAccessItem(UserAccessRow row) => new(row.ACCESSID, row.RESOURCEID, row.RESOURCEID, row.PARENTID, row.NAME, row.CODE, row.DESCRIPTION, row.CATALOGACTIVE, row.ASSIGNED, row.ASSIGNEDBYPROFILE);
    private static FormOptionMaster ToFormOptionMaster(FormOptionMasterRow row) => new(row.IDOPCFOMR,row.IDSUBMENU,row.IDSMSN,row.NOMOPC,row.DESCOPC ?? row.NOMOPC,row.DESCOPC,row.COLOR,row.ICONO,row.ESTOPC,row.SUBMENU_NAME,row.MENU_NAME,row.MODULE_NAME,row.APPLICATION_NAME);
    private static string ListProfileProcedure(ProfileAccessKind kind) => kind switch { ProfileAccessKind.Modules=>"SP_TAS018_LISTAR_MODULOS_PERFIL",ProfileAccessKind.Menus=>"SP_TAS019_LISTAR_MENUS_PERFIL",ProfileAccessKind.Submenus=>"SP_TAS020_LISTAR_SUBMENUS_PERFIL",ProfileAccessKind.SecondLevels=>"SP_TAS021_LISTAR_SEGUNDO_NIVEL_PERFIL",_=>"SP_TAS022_LISTAR_OPCIONES_PERFIL" };
    private static string AssignProfileProcedure(ProfileAccessKind kind) => kind switch { ProfileAccessKind.Modules=>"SP_TAS018_ASIGNAR_MODULO_PERFIL",ProfileAccessKind.Menus=>"SP_TAS019_ASIGNAR_MENU_PERFIL",ProfileAccessKind.Submenus=>"SP_TAS020_ASIGNAR_SUBMENU_PERFIL",ProfileAccessKind.SecondLevels=>"SP_TAS021_ASIGNAR_SEGUNDO_NIVEL_PERFIL",_=>"SP_TAS022_ASIGNAR_OPCION_PERFIL" };
    private static object ListProfileArgs(ProfileAccessKind kind,Guid id,Guid? parent) => kind switch { ProfileAccessKind.Modules=>new { idperfil=id },ProfileAccessKind.Menus=>new { idperfil=id,idmod=parent },ProfileAccessKind.Submenus=>new { idperfil=id,idmenu=parent },ProfileAccessKind.SecondLevels=>new { idperfil=id,idsubmenu=parent },_=>new { idperfil=id,idsubmenu=parent,idsmsn=(Guid?)null } };
    private static object AssignProfileArgs(ProfileAccessKind kind,Guid profile,Guid resource,string actor) => kind switch { ProfileAccessKind.Modules=>new { idperfil=profile,idmod=resource,usuario=actor },ProfileAccessKind.Menus=>new { idperfil=profile,idmenu=resource,usuario=actor },ProfileAccessKind.Submenus=>new { idperfil=profile,idsubmenu=resource,usuario=actor },ProfileAccessKind.SecondLevels=>new { idperfil=profile,idsmsn=resource,usuario=actor },_=>new { idperfil=profile,idopcfomr=resource,usuario=actor } };
    private static UserProfile MapProfile(ProfileRow row) => new(Guid.Parse(row.ID),Guid.TryParse(row.IDAPP,out var app)?app:null,row.NAME,row.DESCRIPTION,row.STATUS,row.ISRECORDACTIVE);
    private static ProfileAccessItem MapProfileAccess(ProfileAccessRow row) => new(Guid.Parse(row.RESOURCEID),Guid.TryParse(row.IDMOD ?? row.IDMENU ?? row.IDSUBMENU ?? row.IDSMSN,out var parent)?parent:null,row.CODE,string.IsNullOrWhiteSpace(row.NAME) ? row.CODE ?? string.Empty : row.NAME,row.DESCRIPTION,row.ASSIGNED,Guid.TryParse(row.ACCESSID,out var access)?access:null);
    private static int MasterNumber(SecurityMasterType type) => type switch { SecurityMasterType.Application=>2, SecurityMasterType.Domain=>3, SecurityMasterType.Area=>4, SecurityMasterType.Role=>5, SecurityMasterType.Module=>7, SecurityMasterType.Menu=>9, SecurityMasterType.Submenu=>11, SecurityMasterType.Product=>13, _=>throw new ArgumentOutOfRangeException(nameof(type)) };
    private static string MasterProcedure(SecurityMasterType type, string operation) => $"SP_TAS{MasterNumber(type):000}_{operation}_{MasterName(type)}";
    private static string MasterName(SecurityMasterType type) => type switch { SecurityMasterType.Application=>"APLICACION", SecurityMasterType.Domain=>"DOMINIO", SecurityMasterType.Area=>"AREA", SecurityMasterType.Role=>"ROL", SecurityMasterType.Module=>"MODULO", SecurityMasterType.Menu=>"MENU", SecurityMasterType.Submenu=>"SUBMENU", SecurityMasterType.Product=>"PRODUCTO", _=>throw new ArgumentOutOfRangeException(nameof(type)) };
    private static string SearchProcedure(SecurityMasterType type) => type switch { SecurityMasterType.Application=>"SP_TAS002_CONSULTAR_APLICACIONES", SecurityMasterType.Domain=>"SP_TAS003_CONSULTAR_DOMINIOS", SecurityMasterType.Area=>"SP_TAS004_CONSULTAR_AREAS", SecurityMasterType.Role=>"SP_TAS005_CONSULTAR_ROLES", SecurityMasterType.Module=>"SP_TAS007_CONSULTAR_MODULOS", SecurityMasterType.Menu=>"SP_TAS009_CONSULTAR_MENUS", SecurityMasterType.Submenu=>"SP_TAS011_CONSULTAR_SUBMENUS", SecurityMasterType.Product=>"SP_TAS013_CONSULTAR_PRODUCTOS", _=>throw new ArgumentOutOfRangeException(nameof(type)) };
    private static object SearchArgs(SecurityMasterType type,SecurityMasterFilter f) => type switch { SecurityMasterType.Application=>new {idapp=f.Id,idempresa=f.ParentId,aplicacion=f.Name,estado=f.Status}, SecurityMasterType.Domain=>new {iddominio=f.Id,idempresa=f.ParentId,dominio=f.Name,estado=f.Status}, SecurityMasterType.Area=>new {idarea=f.Id,idempresa=f.ParentId,area=f.Name,estado=f.Status}, SecurityMasterType.Role=>new {idrol=f.Id,idarea=f.ParentId,rol=f.Name,estado=f.Status}, SecurityMasterType.Module=>new {idmodulo=f.Id,idapp=f.ParentId,modulo=f.Name,estado=f.Status}, SecurityMasterType.Menu=>new {idmenu=f.Id,idmodulo=f.ParentId,menu=f.Name,estado=f.Status}, SecurityMasterType.Submenu=>new {idsubmenu=f.Id,idmenu=f.ParentId,submenu=f.Name,estado=f.Status}, _=>throw new ArgumentOutOfRangeException(nameof(type))};
    private static object CreateArgs(SecurityMasterType type,string parent,string name,string? description,string? viewKey,string user) => type switch { SecurityMasterType.Application=>new {idempresa=parent,aplicacion=name,descripcion=description,usuario=user}, SecurityMasterType.Domain=>new {idempresa=parent,dominio=name,descripcion=description,usuario=user}, SecurityMasterType.Area=>new {idempresa=parent,area=name,descripcion=description,usuario=user}, SecurityMasterType.Role=>new {idarea=parent,rol=name,descripcion=description,usuario=user}, SecurityMasterType.Module=>new {idapp=parent,modulo=name,descripcion=description,usuario=user}, SecurityMasterType.Menu=>new {idmodulo=parent,menu=name,descripcion=description,usuario=user}, SecurityMasterType.Submenu=>new {idmenu=parent,submenu=name,descripcion=description,viewkey=viewKey,usuario=user}, SecurityMasterType.Product=>new {idempresa=parent,nompro=name,despro=description,estado="A",usuario=user,sacrg=0}, _=>throw new ArgumentOutOfRangeException(nameof(type))};
    private static object UpdateArgs(SecurityMasterType type,string id,string parent,string name,string? description,string? viewKey,string status,string user) => type switch { SecurityMasterType.Application=>new {idapp=id,idempresa=parent,aplicacion=name,descripcion=description,estado=status,usuario=user}, SecurityMasterType.Domain=>new {iddominio=id,idempresa=parent,dominio=name,descripcion=description,estado=status,usuario=user}, SecurityMasterType.Area=>new {idarea=id,idempresa=parent,area=name,descripcion=description,estado=status,usuario=user}, SecurityMasterType.Role=>new {idrol=id,idarea=parent,rol=name,descripcion=description,estado=status,usuario=user}, SecurityMasterType.Module=>new {idmodulo=id,idapp=parent,modulo=name,descripcion=description,estado=status,usuario=user}, SecurityMasterType.Menu=>new {idmenu=id,idmodulo=parent,menu=name,descripcion=description,estado=status,usuario=user}, SecurityMasterType.Submenu=>new {idsubmenu=id,idmenu=parent,submenu=name,descripcion=description,viewkey=viewKey,estado=status,usuario=user}, SecurityMasterType.Product=>new {idpro=id,idempresa=parent,nompro=name,despro=description,estado=status,usuario=user,sacrg=0}, _=>throw new ArgumentOutOfRangeException(nameof(type))};
    private static object DeactivateArgs(SecurityMasterType type,string id,string user) => type switch { SecurityMasterType.Application=>new {idapp=id,usuario=user}, SecurityMasterType.Domain=>new {iddominio=id,usuario=user}, SecurityMasterType.Area=>new {idarea=id,usuario=user}, SecurityMasterType.Role=>new {idrol=id,usuario=user}, SecurityMasterType.Module=>new {idmodulo=id,usuario=user}, SecurityMasterType.Menu=>new {idmenu=id,usuario=user}, SecurityMasterType.Submenu=>new {idsubmenu=id,usuario=user}, SecurityMasterType.Product=>new {idpro=id,usuario=user}, _=>throw new ArgumentOutOfRangeException(nameof(type))};
    private static SecurityMasterRecord ToMaster(SecurityMasterType t, SecurityMasterRow r) => t switch { SecurityMasterType.Application=>new(r.IDAPP!,r.IDEMPRESA,r.NOMAPP!,r.DESCAPP,r.ESTAPP!,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT), SecurityMasterType.Domain=>new(r.IDDOM!,r.IDEMPRESA,r.NOMDOM!,r.DESCDOM,r.ESTDOM!,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT), SecurityMasterType.Area=>ToArea(r), SecurityMasterType.Role=>new(r.IDROLEMP!,r.IDAREA,r.NOMROL!,r.DESCROL,r.ESTROL!,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT), SecurityMasterType.Module=>new(r.IDMOD!,r.IDAPP,r.NOMMOD!,r.DESCMOD,r.ESTMOD!,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT), SecurityMasterType.Menu=>new(r.IDMENU!,r.IDMOD,r.NOMMENU!,r.DESCMENU,r.ESTMENU!,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT), SecurityMasterType.Submenu=>new(r.IDSUBMENU!,r.IDMENU,r.NOMSUBMNU!,r.DESCSUBMNU,r.ESTSUBMNU!,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT,r.VIEWKEY), SecurityMasterType.Product=>new(r.IDPRO!,r.IDEMPRESA,r.NOMPRO!,r.DESCPRO,r.ESTPRO!,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT), _=>throw new ArgumentOutOfRangeException(nameof(t))};
    private static SecurityMasterRecord ToArea(SecurityMasterRow row) => new(
        row.IDAREA ?? throw new DataException("SP_TAS004_CONSULTAR_AREAS devolvió un IDAREA inválido."),
        row.IDEMPRESA,
        row.NOMAREA ?? string.Empty,
        row.DESCAREA,
        row.ESTAREA ?? string.Empty,
        row.SACRG,
        row.CUSCRT,
        row.FCHHRACRT,
        row.CULUSA,
        row.FCHHRAACT);
    private static SecurityMasterUser ToMasterUser(SecurityMasterUserRow r) => new(r.IDUSER,r.IDDOMINIO,r.IDROLEMP,r.USUARIO,r.NOMBRES,r.APEPAT,r.APEMAT,r.EMAIL,r.ESTUSER,r.FLGACTPASS,r.FLGMLTSEDE,r.IDSEDEDEFAULT,r.SACRG,r.CUSCRT,r.FCHHRACRT,r.CULUSA,r.FCHHRAACT);
}
