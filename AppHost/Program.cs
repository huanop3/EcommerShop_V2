var builder = DistributedApplication.CreateBuilder(args);
var ProductApi = builder.AddProject<Projects.ProductService>("product-api");
var BlazorApp = builder.AddProject<Projects.BlazorWebApp>("blazor-web-app");
var gateway = builder.AddProject<Projects.GateWayService>("gate-way-app");

builder.AddProject<Projects.MainEcommerceService>("main-ecommerce-api").WithReference(BlazorApp)
    .WithReference(gateway)
.WithReference(ProductApi);


builder.Build().Run();

