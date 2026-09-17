var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSwaggerGen();
builder.Services.AddDataProtection();
builder.Services.AddScoped<checklistWs.Services.DocumentEmailService>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ITenantConnectionReader, checklistWs.Services.Tenant.FirebaseTenantConnectionReader>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ITenantCatalogReader, checklistWs.Services.Tenant.FirebaseTenantConnectionReader>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ITenantDatabaseResolver, checklistWs.Services.Tenant.TenantDatabaseResolver>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ITenantSqlConnectionFactory, checklistWs.Services.Tenant.TenantSqlConnectionFactory>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IDatabaseMetadataReader, checklistWs.Services.Tenant.SqlDatabaseMetadataReader>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IDatabaseIdentityResolver, checklistWs.Services.Tenant.DatabaseIdentityResolver>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IDatabaseGroupingService, checklistWs.Services.Tenant.DatabaseGroupingService>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IProductScopeInventory, checklistWs.Services.Tenant.ProductScopeInventory>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IDatabaseSchemaProbe, checklistWs.Services.Tenant.SqlDatabaseSchemaProbe>();
builder.Services.AddSingleton<checklistWs.Services.Tenant.ISchemaContractProvider, checklistWs.Services.Tenant.ProductosServiciosSchemaContractProvider>();
builder.Services.AddSingleton<checklistWs.Services.Tenant.ISchemaManifestProvider, checklistWs.Services.Tenant.SchemaManifestProvider>();
builder.Services.AddSingleton<checklistWs.Services.Tenant.ISchemaContractSqlGenerator, checklistWs.Services.Tenant.SchemaProvisionSqlGenerator>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaContractPhysicalValidator, checklistWs.Services.Tenant.SchemaContractPhysicalValidator>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaPhysicalSnapshotReader, checklistWs.Services.Tenant.SqlSchemaPhysicalSnapshotReader>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaDriftValidator, checklistWs.Services.Tenant.SchemaDriftValidator>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaProvisionExecutor, checklistWs.Services.Tenant.SchemaProvisionExecutor>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaOperationLock, checklistWs.Services.Tenant.SqlSchemaOperationLock>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaProvisionLock>(sp => (checklistWs.Services.Tenant.SqlSchemaOperationLock)sp.GetRequiredService<checklistWs.Services.Tenant.ISchemaOperationLock>());
builder.Services.AddScoped<checklistWs.Services.Tenant.IProductosServiciosSchemaBootstrapper, checklistWs.Services.Tenant.ProductosServiciosSchemaBootstrapper>();
builder.Services.AddSingleton<checklistWs.Services.Tenant.ISchemaMigrationResolver, checklistWs.Services.Tenant.SchemaMigrationResolver>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaMigrationPackageProvider, checklistWs.Services.Tenant.ProductosServiciosMigrationPackageProvider>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaMigrationSqlExecutor, checklistWs.Services.Tenant.SchemaMigrationSqlExecutor>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaMigrationRunner, checklistWs.Services.Tenant.SchemaMigrationRunner>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IKnownSchemaVersionProvider, checklistWs.Services.Tenant.KnownSchemaVersionProvider>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISchemaVersionRepository, checklistWs.Services.Tenant.SchemaVersionRepository>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IDatabaseVersionEvidenceReader, checklistWs.Services.Tenant.DatabaseVersionEvidenceReader>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IDatabaseStateClassifier, checklistWs.Services.Tenant.DatabaseStateClassifier>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IProductosServiciosHistoricalBaselineAdopter, checklistWs.Services.Tenant.ProductosServiciosHistoricalBaselineAdopter>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IProductosServiciosCompatibilityGate, checklistWs.Services.Tenant.ProductosServiciosCompatibilityGate>();
builder.Services.AddScoped<checklistWs.Services.Tenant.IProductosServiciosAuthorizationService, checklistWs.Services.Tenant.ProductosServiciosAuthorizationService>();
builder.Services.AddScoped<checklistWs.Services.Tenant.ISucursalesScopeRequestContextResolver, checklistWs.Services.Tenant.SucursalesScopeRequestContextResolver>();

builder.Services.AddScoped<checklistWs.Services.Tenant.IProductosServiciosCompanyBootstrapper, checklistWs.Services.Tenant.ProductosServiciosCompanyBootstrapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
app.UseSwaggerUI();
//}

app.Run();
