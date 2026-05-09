open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open WebSharper.AspNetCore
open PortfolioPilot

[<EntryPoint>]
let main args =

    let builder = WebApplication.CreateBuilder(args)
    
    // Register WebSharper services and cookie authentication
    builder.Services.AddWebSharper()
        .AddAuthentication("WebSharper")
        .AddCookie("WebSharper", fun options -> ())
    |> ignore

    let app = builder.Build()

    // Enable production error handling and HSTS
    if not (app.Environment.IsDevelopment()) then
        app.UseExceptionHandler("/Error")
            .UseHsts()
        |> ignore
    
    app.UseHttpsRedirection()

#if DEBUG        
        // Redirect scripts to the Vite development server in DEBUG mode
        .UseWebSharperScriptRedirect(startVite = true)
#endif

        .UseDefaultFiles()
        .UseStaticFiles()

        // Enable if server-side RPC calls are required
        //.UseWebSharperRemoting()

    |> ignore 
       
    app.Run()

    0