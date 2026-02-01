using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StoneHammer;
using StoneHammer.Systems;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CityBridge>();
builder.Services.AddScoped<AssetManager>();
builder.Services.AddScoped<CombatService>();
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<SaveService>();
builder.Services.AddScoped<ShopService>();

await builder.Build().RunAsync();
