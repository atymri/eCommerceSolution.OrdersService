using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

// for api gateway i used ocelot package: Ocelot(24.1.0)
// configuring ocelot is not that hard it just needs an ocelot.json file to map the Upstream's to Downstream's
// you have to add the file to configuration with this reserved name: "Ocelot.json"

// if you want to all fault tolerance policies you have to add Polly alongside with ocelot, for this matter:
//      first you have to install this package: Ocelot.Provider.Polly
//      then you have to add it as a service alongside with ocelot:
//          builder.Services.AddOcelot().AddPolly();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Ocelot.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOcelot()
    .AddPolly();

var app = builder.Build();

await app.UseOcelot();

app.MapGet("/", () => "Hello World! GATEWAY is on!");

app.Run();
