using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Dominio;
using Dominio.Entidades.Identidad;
using Infraestructura;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistencia;

namespace Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Name = configuration.GetValue<string>("AppSettings:Name");
            DefaultCulture = new CultureInfo(configuration.GetValue<string>("CultureInfo:DefaultCulture"));
            CurrencyCulture = new CultureInfo(configuration.GetValue<string>("CultureInfo:CurrencyCulture"));
            DefaultCulture.DateTimeFormat.ShortDatePattern = Configuration.GetValue<string>("CultureInfo:DateFormatInfo:ShortDatePattern");
            DefaultCulture.DateTimeFormat.DateSeparator = Configuration.GetValue<string>("CultureInfo:DateFormatInfo:DateSeparator");
            if (Configuration.GetSection("CultureInfo:NumberFormatInfo") != null)
            {
                NumberFormatInfo = new NumberFormatInfo()
                {
                    CurrencyDecimalSeparator = Configuration.GetValue<string>("CultureInfo:NumberFormatInfo:CurrencyDecimalSeparator"),
                    NumberDecimalSeparator = Configuration.GetValue<string>("CultureInfo:NumberFormatInfo:NumberDecimalSeparator"),
                    PercentDecimalSeparator = Configuration.GetValue<string>("CultureInfo:NumberFormatInfo:PercentDecimalSeparator"),
                    CurrencyGroupSeparator = Configuration.GetValue<string>("CultureInfo:NumberFormatInfo:CurrencyGroupSeparator"),
                    NumberGroupSeparator = Configuration.GetValue<string>("CultureInfo:NumberFormatInfo:NumberGroupSeparator"),
                    PercentGroupSeparator = Configuration.GetValue<string>("CultureInfo:NumberFormatInfo:PercentGroupSeparator")
                };
                DefaultCulture.NumberFormat = NumberFormatInfo;
                CurrencyCulture.NumberFormat = NumberFormatInfo;
            }
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            System.Threading.Thread.CurrentThread.CurrentCulture = DefaultCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = DefaultCulture;
        }

        public static string Name { get; private set; }
        internal IConfiguration Configuration { get; private set; }
        public NumberFormatInfo NumberFormatInfo { get; private set; }
        public CultureInfo DefaultCulture { get; private set; }
        public CultureInfo CurrencyCulture { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization();
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(DefaultCulture);
                options.SupportedCultures = new[] { DefaultCulture };
                options.SupportedUICultures = new[] { DefaultCulture };
                options.DefaultRequestCulture.Culture.NumberFormat = NumberFormatInfo;
                options.DefaultRequestCulture.UICulture.NumberFormat = NumberFormatInfo;
            });

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(24);
                options.IOTimeout = TimeSpan.FromHours(24);
#if DEBUG
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.IOTimeout = TimeSpan.FromMinutes(60);
#endif
                options.Cookie.IsEssential = true;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDefaultIdentity<Usuario>()
               .AddRoles<Rol>()
               .AddEntityFrameworkStores<ContextoPrincipal>()
               .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
#if DEBUG
                options.ExpireTimeSpan = TimeSpan.FromHours(3);
#endif
                options.LoginPath = "/Identidad/Cuenta/Inicio";
                options.LogoutPath = "/Identidad/Cuenta/Inicio";
                options.AccessDeniedPath = "/Identidad/Cuenta/AccesoDenegado";
                options.Cookie = new CookieBuilder
                {
                    IsEssential = true // required for auth to work without explicit user consent; adjust to suit your privacy policy
                };
            });

            services
                .AddDominio()
                .AddPersistencia(Configuration)
                .AddInfraestructura();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.Administracion), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.Administracion));
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.Consultas), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.Consultas));
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.HistorialGeneral), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.HistorialGeneral));
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.HistorialEmpresa), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.HistorialEmpresa));
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.HistorialUsuario), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.HistorialUsuario));
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.ReporteResumenConsultas), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.ReporteResumenConsultas));
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.ReporteResumenCliente), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.ReporteResumenCliente));
                options.AddPolicy(nameof(Dominio.Tipos.Pantallas.AdministracionUsuariosCooprogreso), builder => builder.RequireClaim(System.Security.Claims.CustomClaimTypes.Screen, Dominio.Tipos.Pantallas.AdministracionUsuariosCooprogreso));
            });

            services.AddHttpContextAccessor();

            services.AddRazorPages();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 3;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = false;
            });

            services.AddApplicationInsightsTelemetry();

            services.Configure<GzipCompressionProviderOptions>(config =>
            {
                config.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            });

#if DEBUG
            services.AddControllersWithViews()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.MaxDepth = 4;
                })
                .AddRazorRuntimeCompilation();
#else
            services.AddControllersWithViews()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.MaxDepth = 4;
                });

#endif

            services.AddMemoryCache();

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Default/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (Configuration.GetValue<bool>("AppSettings:UseHttpsRedirection"))
            {
                app.UseHttpsRedirection();
            }

            app.UseRequestLocalization(option =>
            {
                option.DefaultRequestCulture = new RequestCulture(DefaultCulture);
                option.SupportedCultures = new[] { DefaultCulture };
                option.SupportedUICultures = new[] { DefaultCulture };
            });

            app.UseStaticFiles();

            app.UseResponseCompression();

            app.UseRouting();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
            }

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "areas",
                    "{area:exists}/{controller=Default}/{action=Inicio}/{id?}");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Default}/{action=Inicio}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
