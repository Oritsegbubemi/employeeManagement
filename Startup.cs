using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EmployeeManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using EmployeeManagement.Security;

namespace EmployeeManagement
{
    public class Startup
    {
        private IConfiguration _config;
        public Startup(IConfiguration config)
        {
            _config = config;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //connects to the database. connection string comes from appsettings.json file
            services.AddDbContextPool<AppDbContext>(options => options.UseSqlServer(_config.GetConnectionString("EmployeeDBConnection")));

            //adds authentication services to our application
            services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                //alters the default password validation
                options.Password.RequiredLength = 10;
                options.Password.RequiredUniqueChars = 3;
                options.SignIn.RequireConfirmedEmail = true;

                options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";
            }).AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<CustomEmailConfirmationTokenProvider<ApplicationUser>>("CustomEmailConfirmation");

            //reset the expiration time of tokens to 5 hrs...tokens for email confirmation and reset password
            services.Configure<DataProtectionTokenProviderOptions>(
                                options => options.TokenLifespan = TimeSpan.FromHours(5));

            //set the liftspan of email confirmation token to 3 days
            services.Configure<CustomEmailConfirmationTokenProviderOptions>(
                                o => o.TokenLifespan = TimeSpan.FromDays(3));
            //adds all the services we need to use MVC in .net app, calls addMvcCore under the hood
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;

                //adds authorization to all our app routes - users must be authenticated to access all routes in the application except those decorated with allowAnonymous annotation
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.Filters.Add(new AuthorizeFilter(policy));

            }).AddXmlSerializerFormatters();

            //adds google adn facebook auth to the app
            //services.AddAuthentication().AddGoogle(options => {
            //    options.ClientId = _config.GetSection("Google")["ClientId"];
            //    options.ClientSecret = _config.GetSection("Google")["ClientSecret"];
            //}).AddFacebook(options => {
            //    options.AppId = _config.GetSection("Facebook")["AppId"];
            //    options.AppSecret = _config.GetSection("Facebook")["AppSecret"];
            //});

            //services.AddMvcCore(options => options.EnableEndpointRouting = false);
            //registers a singleton. This is how we register interfaces to concrete implementations

            //implementation for in-memory database 
            //services.AddSingleton<IEmployeeRepository, MockEmployeeRepository>();

            services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();

            //adds custom authorization handlers to our application
            services.AddSingleton<IAuthorizationHandler, CanEditOnlyOtherAdminRolesAndClaimsHandler>();
            services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();

            //adds authorization service to the app - used for claim based access control
            services.AddAuthorization(options =>
            {
                options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role", "true"));
                //chaining them is and adn condition
                //options.AddPolicy("EditRolePolicy", policy => policy.RequireClaim("Edit Role", "true").RequireRole("Admin"));


                //using func for authorization we can customize how we want resources to be accessed by users
                //options.AddPolicy("EditRolePolicy", policy => policy.RequireAssertion(context => 
                //    context.User.IsInRole("Admin") &&
                //    context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
                //    context.User.IsInRole("Super Admin")
                //));

                //using a private func for authorization access
                //options.AddPolicy("EditRolePolicy", policy => policy.RequireAssertion(context => AuthorizeAccess(context)));

                //only super admins or admins with edit role can access any route affected by this policy
                options.AddPolicy("EditRolePolicy", policy => policy.AddRequirements(new ManageAdminRolesAndClaimsRequirements()));
                //role is a claim type also, so we can register roles here and use them

                //to prevent calling other handlers when a particular handler fails, override this property - default is true
                //options.InvokeHandlersAfterFailure = false;

                options.AddPolicy("AdminRolePolicy", policy => policy.RequireRole("Admin"));
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/administration/accessdenied");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                //add configuration options for developer exception middleware
                DeveloperExceptionPageOptions developerExceptionPageOptions = new DeveloperExceptionPageOptions
                {
                    SourceCodeLineCount = 2
                };

                app.UseDeveloperExceptionPage(developerExceptionPageOptions);
            }
            else
            {

                app.UseExceptionHandler("/Error");
                //use custom path for errors
                //this redirects to the specified route when an error occurs and passes the error code as the parameter to the controller action
                //app.UseStatusCodePagesWithRedirects("/Error/{0}");

                //this reexecutes the request pipeline without issuing a redirect response to the browser hence the status code remains at 400 not 302
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            }


            //set the default filename to use when handling request, by default it is either(index.html,index.htm, default.html, default.htm)
            //used when using UseDefaultFiles middleware
            //UseDefaultFiles middleware must be used together with UseStaticFiles. It configures the file to be used by the later and so it must be called before UseStaticFiles
            // DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
            // defaultFilesOptions.DefaultFileNames.Clear();
            // defaultFilesOptions.DefaultFileNames.Add("foo.html");

            // app.UseDefaultFiles(defaultFilesOptions);

            //enables the serving of static files in the wwroot directory
            app.UseStaticFiles();
            //add MVC functionnality with default routes enabled
            //app.UseMvcWithDefaultRoute();

            //adds authentication middleware to our application
            app.UseAuthentication();

            //adds MVC functionality 
            app.UseMvc(routes => {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            //app.UseMvc();


            //UseFileServer combines the functionality of UseDefaultFiles and UseStaticFiles
            // FileServerOptions fileServerOptions = new FileServerOptions();
            // fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();
            // fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add(_config["DefaultFile"]);
            //app.UseFileServer(fileServerOptions);


            //this middleware is always called if the incoming request is not handled before getting here, can be used to return an error page if the request is not understood by any of the registered middleware above
            // app.Run(async context =>
            // {
            //     await context.Response.WriteAsync(env.EnvironmentName);
            //     //adds logging in app
            //     logger.LogInformation("final");
            // });

            /*            app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", async context =>
                            {
                                await context.Response.WriteAsync(System.Diagnostics.Process.GetCurrentProcess().ProcessName + _config["MyKey"]);
                            });
                        });*/
        }

        private bool AuthorizeAccess(AuthorizationHandlerContext context)
        {
            return context.User.IsInRole("Admin") &&
                   context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
                   context.User.IsInRole("Super Admin");
        }
    }
}
