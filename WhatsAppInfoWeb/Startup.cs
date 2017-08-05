using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WhatsAppInfoWeb.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WhatsAppInfoWeb
{
    public class Startup
    {
        IWhatsAppClientWrapper clientWrapper;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IWhatsAppClientWrapper, WhatsAppClientWrapper>();

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime applicationLifetime)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            clientWrapper = app.ApplicationServices.GetService<IWhatsAppClientWrapper>();
            applicationLifetime.ApplicationStopping.Register(OnShutdown);

            app.UseMvc();
        }

        private void OnShutdown()
        {
            clientWrapper.Stop();
        }
    }
}
