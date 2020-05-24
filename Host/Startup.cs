using System;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Host.Authentication;
using Host.Filters;
using Host.Options;
using Host.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Host
{
    public sealed class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly JwtOptions _jwtOptions;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment,
            IOptions<JwtOptions> jwtOptions)
        {
            _configuration = configuration;
            _environment = environment;
            _jwtOptions = jwtOptions.Value;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.Configure<JwtOptions>(_configuration.GetSection("Jwt"));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = _jwtOptions.Issuer,
                        ValidAudience = _jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtOptions.SecretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["auth"];
                            return Task.CompletedTask;
                        }
                    };
                    options.Validate();
                });
            
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Roles.Admin, Roles.AdminPolicy());
                options.AddPolicy(Roles.ReadOnly, Roles.ReadOnlyPolicy());
            });

            if (_environment.IsDevelopment())
                services.AddSingleton<IAuthService, LocalAuthService>();
            else
                services.AddScoped<IAuthService, DatabaseBasedAuthService>();

            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(_configuration.GetConnectionString("AuthConnectionString")));
            
            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(_configuration.GetConnectionString("HangfireConnectionString"));
            });
            services.AddHangfireServer();
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();

            app.UseStatusCodePages(context =>
            {
                var response = context.HttpContext.Response;
                if (response.StatusCode == 401)
                {
                    response.Redirect("/auth");
                }

                return Task.CompletedTask;
            });
            
            app.UseHangfireDashboard(options: new DashboardOptions
            {
                Authorization = new[]
                {
                    new HangfireAuthorizationFilter()
                },
                IsReadOnlyFunc = context => context.GetHttpContext().User.IsInRole(Roles.ReadOnly)
            });
            
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}