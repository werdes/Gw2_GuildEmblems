using Gw2_GuildEmblem_Cdn.Core.Custom.Middleware;
using Gw2_GuildEmblem_Cdn.Core.Utility;
using Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Gw2_GuildEmblem_Cdn.Core
{
    public class Startup
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public const string AllowSpecificOriginsName = "Default";
        public IConfiguration Configuration { get; }


        public Startup(IConfiguration configuration)
        {
            _log.Info("start");
            Configuration = configuration;
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddResponseCaching();
            services.AddCors(options =>
            {
                options.AddPolicy(AllowSpecificOriginsName,
                builder =>
                {
                    builder.AllowAnyOrigin()
                                        .AllowAnyHeader()
                                        .AllowAnyMethod();
                });
            });
            services.AddSingleton(Configuration);
            services.AddSingleton<IApiUtility, ApiUtility>();
            services.AddSingleton<IEmblemCacheUtility, EmblemCacheUtility>();
            services.AddSingleton<IStatisticsUtility, StatisticsUtility>();
            services.AddHttpContextAccessor();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseCors(AllowSpecificOriginsName);
            app.UseIgnoreClientResponseCache();
            app.UseResponseStatisticsMiddleware();
            app.UseResponseCaching();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

    }
}
