using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using JsonUI_blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();

// https://lldev.thespacedevs.com/2.3.0/launches/18b49918-d2e0-4899-be7a-6c216952b8f3/
// npx @tailwindcss/cli -i ./styles/input.css -o ./wwwroot/css/tailwind.css --watch