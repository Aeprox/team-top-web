using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamTopFtpWeb.Services;
using ZNetCS.AspNetCore.Authentication.Basic;
using ZNetCS.AspNetCore.Authentication.Basic.Events;

namespace TeamTopFtpWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

                services
                    .AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
                    .AddBasicAuthentication(
                        options =>
                        {
                            options.Realm = "No guts no glory now pow no story";
                            options.Events = new BasicAuthenticationEvents
                            {
                                OnValidatePrincipal = context =>
                                {
                                    if ((context.UserName == Configuration.GetValue<string>("WebUsername")) 
                                        && (context.Password == Configuration.GetValue<string>("WebPassword")))
                                    {
                                        var claims = new List<Claim>
                                        {
                                            new Claim(ClaimTypes.Name, context.UserName, context.Options.ClaimsIssuer)
                                        };

                                        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                                        context.Principal = principal;
                                    }
                                    else
                                    {
                                        // optional with following default.
                                        // context.AuthenticationFailMessage = "Authentication failed."; 
                                    }

                                    return Task.CompletedTask;
                                }
                            };
                        });
     

            services.AddRazorPages().AddRazorPagesOptions(options =>
            {
                options.Conventions.AddPageRoute("/index", "{*url}");
            });

            services.AddLogging();

            services.AddTransient(typeof(IAzureService), typeof(AzureService));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                
            });
        }
    }
}
