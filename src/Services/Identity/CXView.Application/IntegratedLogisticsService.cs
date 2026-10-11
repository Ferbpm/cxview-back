using CXView.Domain;

namespace CXView.Application;

public sealed class IntegratedLogisticsService(IIntegratedLogisticsRepository repository)
{
    public Task<IReadOnlyCollection<IntegratedLogisticsProduct>> SearchProductsAsync(ProductFilter filter, CancellationToken ct) =>
        repository.SearchProductsAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Name, 100), SecurityMasterService.Status(filter.Status, true)), ct);

    public Task<IReadOnlyCollection<IntegratedLogisticsProduct>> ListActiveProductsAsync(CancellationToken ct) => repository.ListActiveProductsAsync(ct);

    public Task<IntegratedLogisticsProduct> CreateProductAsync(CreateProductRequest request, string actor, CancellationToken ct) =>
        repository.CreateProductAsync(request with
        {
            Name = SecurityMasterService.Required(request.Name, 100, "El nombre del producto es obligatorio y no puede superar 100 caracteres."),
            Description = SecurityMasterService.Optional(request.Description, 100)
        }, SecurityMasterService.User(actor), ct);

    public Task<IntegratedLogisticsProduct?> UpdateProductAsync(string id, UpdateProductRequest request, string actor, CancellationToken ct) =>
        repository.UpdateProductAsync(SecurityMasterService.RequiredId(id), request with
        {
            Name = SecurityMasterService.Required(request.Name, 100, "El nombre del producto es obligatorio y no puede superar 100 caracteres."),
            Description = SecurityMasterService.Optional(request.Description, 100),
            Status = SecurityMasterService.Status(request.Status, false)!
        }, SecurityMasterService.User(actor), ct);

    public Task<int> DeactivateProductAsync(string id, string actor, CancellationToken ct) => repository.DeactivateProductAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);

    public Task<IReadOnlyCollection<Ubigeo>> ListActiveUbigeosAsync(CancellationToken ct) => repository.ListActiveUbigeosAsync(ct);

    public Task<IReadOnlyCollection<IntegratedLogisticsCampaign>> SearchCampaignsAsync(CampaignFilter filter, CancellationToken ct) =>
        repository.SearchCampaignsAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.OptionalId(filter.UbigeoId), SecurityMasterService.OptionalId(filter.ProductId), SecurityMasterService.Optional(filter.Process, 50), SecurityMasterService.Status(filter.Status, true)), ct);

    public Task<IntegratedLogisticsCampaign> CreateCampaignAsync(CreateCampaignRequest request, string actor, CancellationToken ct) => repository.CreateCampaignAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<IntegratedLogisticsCampaign?> UpdateCampaignAsync(string id, UpdateCampaignRequest request, string actor, CancellationToken ct) => repository.UpdateCampaignAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateCampaignAsync(string id, string actor, CancellationToken ct) => repository.DeactivateCampaignAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);

    public Task<IReadOnlyCollection<FixedCost>> SearchFixedCostsAsync(FixedCostFilter filter, CancellationToken ct) =>
        repository.SearchFixedCostsAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.OptionalId(filter.SiteId), SecurityMasterService.Optional(filter.Description, 50), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<IReadOnlyCollection<ActiveSite>> ListActiveSitesAsync(CancellationToken ct) => repository.ListActiveSitesAsync(ct);
    public Task<FixedCost> CreateFixedCostAsync(CreateFixedCostRequest request, string actor, CancellationToken ct) => repository.CreateFixedCostAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<FixedCost?> UpdateFixedCostAsync(string id, UpdateFixedCostRequest request, string actor, CancellationToken ct) => repository.UpdateFixedCostAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateFixedCostAsync(string id, string actor, CancellationToken ct) => repository.DeactivateFixedCostAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<Deposit>> SearchDepositsAsync(DepositFilter filter, CancellationToken ct) =>
        repository.SearchDepositsAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.OptionalId(filter.DepositTypeId), SecurityMasterService.Optional(filter.Name, 50), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<IReadOnlyCollection<ActiveDepositType>> ListActiveDepositTypesAsync(CancellationToken ct) => repository.ListActiveDepositTypesAsync(ct);
    public Task<Deposit> CreateDepositAsync(CreateDepositRequest request, string actor, CancellationToken ct) => repository.CreateDepositAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<Deposit?> UpdateDepositAsync(string id, UpdateDepositRequest request, string actor, CancellationToken ct) => repository.UpdateDepositAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateDepositAsync(string id, string actor, CancellationToken ct) => repository.DeactivateDepositAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<ContainerDimension>> SearchContainerDimensionsAsync(MasterTableFilter filter, CancellationToken ct) => repository.SearchContainerDimensionsAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Name, 2), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<ContainerDimension> CreateContainerDimensionAsync(CreateContainerDimensionRequest request, string actor, CancellationToken ct) => repository.CreateContainerDimensionAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<ContainerDimension?> UpdateContainerDimensionAsync(string id, UpdateContainerDimensionRequest request, string actor, CancellationToken ct) => repository.UpdateContainerDimensionAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateContainerDimensionAsync(string id, string actor, CancellationToken ct) => repository.DeactivateContainerDimensionAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<EntityAddressType>> SearchEntityAddressTypesAsync(MasterTableFilter filter, CancellationToken ct) => repository.SearchEntityAddressTypesAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Name, 50), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<EntityAddressType> CreateEntityAddressTypeAsync(CreateEntityAddressTypeRequest request, string actor, CancellationToken ct) => repository.CreateEntityAddressTypeAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<EntityAddressType?> UpdateEntityAddressTypeAsync(string id, UpdateEntityAddressTypeRequest request, string actor, CancellationToken ct) => repository.UpdateEntityAddressTypeAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateEntityAddressTypeAsync(string id, string actor, CancellationToken ct) => repository.DeactivateEntityAddressTypeAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<PaymentType>> SearchPaymentTypesAsync(MasterTableFilter filter, CancellationToken ct) => repository.SearchPaymentTypesAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Name, 50), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<PaymentType> CreatePaymentTypeAsync(CreatePaymentTypeRequest request, string actor, CancellationToken ct) => repository.CreatePaymentTypeAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<PaymentType?> UpdatePaymentTypeAsync(string id, UpdatePaymentTypeRequest request, string actor, CancellationToken ct) => repository.UpdatePaymentTypeAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivatePaymentTypeAsync(string id, string actor, CancellationToken ct) => repository.DeactivatePaymentTypeAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<ShippingLine>> SearchShippingLinesAsync(ShippingLineFilter filter, CancellationToken ct) => repository.SearchShippingLinesAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Description, 50), SecurityMasterService.Optional(filter.SunatCode, 10), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<ShippingLine> CreateShippingLineAsync(CreateShippingLineRequest request, string actor, CancellationToken ct) => repository.CreateShippingLineAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<ShippingLine?> UpdateShippingLineAsync(string id, UpdateShippingLineRequest request, string actor, CancellationToken ct) => repository.UpdateShippingLineAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateShippingLineAsync(string id, string actor, CancellationToken ct) => repository.DeactivateShippingLineAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<PickupPlace>> SearchPickupPlacesAsync(MasterTableFilter filter, CancellationToken ct) => repository.SearchPickupPlacesAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Name, 50), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<PickupPlace> CreatePickupPlaceAsync(CreatePickupPlaceRequest request, string actor, CancellationToken ct) => repository.CreatePickupPlaceAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<PickupPlace?> UpdatePickupPlaceAsync(string id, UpdatePickupPlaceRequest request, string actor, CancellationToken ct) => repository.UpdatePickupPlaceAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivatePickupPlaceAsync(string id, string actor, CancellationToken ct) => repository.DeactivatePickupPlaceAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<Currency>> SearchCurrenciesAsync(CurrencyFilter filter, CancellationToken ct) => repository.SearchCurrenciesAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Description, 50), SecurityMasterService.Optional(filter.Symbol, 3), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<Currency> CreateCurrencyAsync(CreateCurrencyRequest request, string actor, CancellationToken ct) => repository.CreateCurrencyAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<Currency?> UpdateCurrencyAsync(string id, UpdateCurrencyRequest request, string actor, CancellationToken ct) => repository.UpdateCurrencyAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateCurrencyAsync(string id, string actor, CancellationToken ct) => repository.DeactivateCurrencyAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<CurrentAccountDocumentType>> SearchCurrentAccountDocumentTypesAsync(CurrentAccountDocumentTypeFilter filter, CancellationToken ct) => repository.SearchCurrentAccountDocumentTypesAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Code, 5), SecurityMasterService.Optional(filter.Description, 20), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<CurrentAccountDocumentType> CreateCurrentAccountDocumentTypeAsync(CreateCurrentAccountDocumentTypeRequest request, string actor, CancellationToken ct) => repository.CreateCurrentAccountDocumentTypeAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<CurrentAccountDocumentType?> UpdateCurrentAccountDocumentTypeAsync(string id, UpdateCurrentAccountDocumentTypeRequest request, string actor, CancellationToken ct) => repository.UpdateCurrentAccountDocumentTypeAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivateCurrentAccountDocumentTypeAsync(string id, string actor, CancellationToken ct) => repository.DeactivateCurrentAccountDocumentTypeAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);
    public Task<IReadOnlyCollection<PackagingType>> SearchPackagingTypesAsync(MasterTableFilter filter, CancellationToken ct) => repository.SearchPackagingTypesAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.Optional(filter.Name, 20), SecurityMasterService.Status(filter.Status, true)), ct);
    public Task<PackagingType> CreatePackagingTypeAsync(CreatePackagingTypeRequest request, string actor, CancellationToken ct) => repository.CreatePackagingTypeAsync(Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<PackagingType?> UpdatePackagingTypeAsync(string id, UpdatePackagingTypeRequest request, string actor, CancellationToken ct) => repository.UpdatePackagingTypeAsync(SecurityMasterService.RequiredId(id), Normalize(request), SecurityMasterService.User(actor), ct);
    public Task<int> DeactivatePackagingTypeAsync(string id, string actor, CancellationToken ct) => repository.DeactivatePackagingTypeAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(actor), ct);

    private static CreateCampaignRequest Normalize(CreateCampaignRequest r)
    {
        if (r.EndDate < r.StartDate) throw new ArgumentException("La fecha de fin no puede ser menor que la fecha de inicio.");
        return r with { UbigeoId = SecurityMasterService.RequiredId(r.UbigeoId), ProductId = SecurityMasterService.RequiredId(r.ProductId), Process = SecurityMasterService.Required(r.Process, 50, "El proceso de la campaña es obligatorio y no puede superar 50 caracteres."), Description = SecurityMasterService.Optional(r.Description, 100) };
    }

    private static UpdateCampaignRequest Normalize(UpdateCampaignRequest r)
    {
        var normalized = Normalize(new CreateCampaignRequest(r.UbigeoId, r.ProductId, r.StartDate, r.EndDate, r.Process, r.Description));
        return r with { UbigeoId = normalized.UbigeoId, ProductId = normalized.ProductId, Process = normalized.Process, Description = normalized.Description, Status = SecurityMasterService.Status(r.Status, false)! };
    }

    private static CreateFixedCostRequest Normalize(CreateFixedCostRequest r)
    {
        if (r.Amount < 0 || r.Amount > 99999999.99m) throw new ArgumentException("El importe del costo fijo debe estar entre 0 y 99999999.99.");
        return r with { SiteId = SecurityMasterService.RequiredId(r.SiteId), Description = SecurityMasterService.Required(r.Description, 50, "La descripción del costo fijo es obligatoria y no puede superar 50 caracteres.") };
    }
    private static UpdateFixedCostRequest Normalize(UpdateFixedCostRequest r)
    {
        var normalized = Normalize(new CreateFixedCostRequest(r.SiteId, r.Description, r.Amount));
        return r with { SiteId = normalized.SiteId, Description = normalized.Description, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreateDepositRequest Normalize(CreateDepositRequest r) => r with { DepositTypeId = SecurityMasterService.RequiredId(r.DepositTypeId), Name = SecurityMasterService.Required(r.Name, 50, "El nombre del depósito es obligatorio y no puede superar 50 caracteres.") };
    private static UpdateDepositRequest Normalize(UpdateDepositRequest r)
    {
        var normalized = Normalize(new CreateDepositRequest(r.DepositTypeId, r.Name));
        return r with { DepositTypeId = normalized.DepositTypeId, Name = normalized.Name, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreateContainerDimensionRequest Normalize(CreateContainerDimensionRequest r) => r with { Dimension = SecurityMasterService.Required(r.Dimension, 2, "La dimensión del contenedor es obligatoria y no puede superar 2 caracteres.") };
    private static UpdateContainerDimensionRequest Normalize(UpdateContainerDimensionRequest r)
    {
        var normalized = Normalize(new CreateContainerDimensionRequest(r.Dimension));
        return r with { Dimension = normalized.Dimension, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreateEntityAddressTypeRequest Normalize(CreateEntityAddressTypeRequest r) => r with { Description = SecurityMasterService.Required(r.Description, 50, "La descripción del tipo de dirección es obligatoria y no puede superar 50 caracteres.") };
    private static UpdateEntityAddressTypeRequest Normalize(UpdateEntityAddressTypeRequest r)
    {
        var normalized = Normalize(new CreateEntityAddressTypeRequest(r.Description));
        return r with { Description = normalized.Description, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreatePaymentTypeRequest Normalize(CreatePaymentTypeRequest r) => r with { Description = SecurityMasterService.Required(r.Description, 50, "La descripción del tipo de pago es obligatoria y no puede superar 50 caracteres.") };
    private static UpdatePaymentTypeRequest Normalize(UpdatePaymentTypeRequest r)
    {
        var normalized = Normalize(new CreatePaymentTypeRequest(r.Description));
        return r with { Description = normalized.Description, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreateShippingLineRequest Normalize(CreateShippingLineRequest r) => r with { Description = SecurityMasterService.Required(r.Description, 50, "La descripción de la línea naviera es obligatoria y no puede superar 50 caracteres."), SunatCode = SecurityMasterService.Optional(r.SunatCode, 10) };
    private static UpdateShippingLineRequest Normalize(UpdateShippingLineRequest r)
    {
        var normalized = Normalize(new CreateShippingLineRequest(r.Description, r.SunatCode));
        return r with { Description = normalized.Description, SunatCode = normalized.SunatCode, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreatePickupPlaceRequest Normalize(CreatePickupPlaceRequest r) => r with { Description = SecurityMasterService.Required(r.Description, 50, "La descripción del lugar de recojo es obligatoria y no puede superar 50 caracteres.") };
    private static UpdatePickupPlaceRequest Normalize(UpdatePickupPlaceRequest r)
    {
        var normalized = Normalize(new CreatePickupPlaceRequest(r.Description));
        return r with { Description = normalized.Description, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreateCurrencyRequest Normalize(CreateCurrencyRequest r) => r with { Description = SecurityMasterService.Required(r.Description, 50, "La descripción de la moneda es obligatoria y no puede superar 50 caracteres."), Symbol = SecurityMasterService.Required(r.Symbol, 3, "El símbolo de la moneda es obligatorio y no puede superar 3 caracteres.") };
    private static UpdateCurrencyRequest Normalize(UpdateCurrencyRequest r)
    {
        var normalized = Normalize(new CreateCurrencyRequest(r.Description, r.Symbol));
        return r with { Description = normalized.Description, Symbol = normalized.Symbol, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreateCurrentAccountDocumentTypeRequest Normalize(CreateCurrentAccountDocumentTypeRequest r) => r with { Code = SecurityMasterService.Required(r.Code, 5, "El código del tipo de documento es obligatorio y no puede superar 5 caracteres."), Description = SecurityMasterService.Required(r.Description, 20, "La descripción del tipo de documento es obligatoria y no puede superar 20 caracteres.") };
    private static UpdateCurrentAccountDocumentTypeRequest Normalize(UpdateCurrentAccountDocumentTypeRequest r)
    {
        var normalized = Normalize(new CreateCurrentAccountDocumentTypeRequest(r.Code, r.Description));
        return r with { Code = normalized.Code, Description = normalized.Description, Status = SecurityMasterService.Status(r.Status, false)! };
    }
    private static CreatePackagingTypeRequest Normalize(CreatePackagingTypeRequest r) => r with { Description = SecurityMasterService.Required(r.Description, 20, "La descripción del tipo de embalaje es obligatoria y no puede superar 20 caracteres.") };
    private static UpdatePackagingTypeRequest Normalize(UpdatePackagingTypeRequest r)
    {
        var normalized = Normalize(new CreatePackagingTypeRequest(r.Description));
        return r with { Description = normalized.Description, Status = SecurityMasterService.Status(r.Status, false)! };
    }
}
