﻿using System;
using App.Metrics.Extensions.Reporting.InfluxDB;
using App.Metrics.Extensions.Reporting.InfluxDB.Client;
using App.Metrics.Filtering;
using App.Metrics.InfluxDB.Sandbox.JustForTesting;
using App.Metrics.Reporting.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Metrics.InfluxDB.Sandbox
{
    public class Startup
    {
        private static readonly bool HaveAppRunSampleRequests = true;
        private static readonly string InfluxDbDatabase = "AppMetricsSandbox";
        private static readonly Uri InfluxDbUri = new Uri("http://127.0.0.1:8086");
        private static readonly bool RunSamplesWithClientId = true;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath).
                                                     AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).
                                                     AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true).
                                                     AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime lifetime)
        {
            if (RunSamplesWithClientId && HaveAppRunSampleRequests)
            {
                app.Use(
                    (context, func) =>
                    {
                        RandomClientIdForTesting.SetTheFakeClaimsPrincipal(context);
                        return func();
                    });
            }

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            // loggerFactory.AddDebug();

            app.UseMetrics();
            app.UseMetricsReporting(lifetime);

            app.UseMvc();

            if (HaveAppRunSampleRequests)
            {
                SampleRequests.Run(lifetime.ApplicationStopping);
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTestStuff();
            services.AddLogging().AddRouting(options => { options.LowercaseUrls = true; });

            services.AddMvc(options => options.AddMetricsResourceFilter());

            var reportFilter = new DefaultMetricsFilter();
            reportFilter.WithHealthChecks(false);

            services.AddMetrics(Configuration.GetSection("AppMetrics")).
                     AddAsciiHealthSerialization().
                     AddInfluxDBLineProtocolMetricsSerialization().
                     AddInfluxDBLineProtocolMetricsTextSerialization().
                     AddReporting(
                         factory =>
                         {
                             factory.AddInfluxDb(
                                 new InfluxDBReporterSettings
                                 {
                                     InfluxDbSettings = new InfluxDBSettings(InfluxDbDatabase, InfluxDbUri)
                                 },
                                 reportFilter);
                         }).
                     AddHealthChecks(
                         factory =>
                         {
                             factory.RegisterPingHealthCheck("google ping", "google.com", TimeSpan.FromSeconds(10));
                             factory.RegisterHttpGetHealthCheck("github", new Uri("https://github.com/"), TimeSpan.FromSeconds(10));
                         }).
                     AddMetricsMiddleware(Configuration.GetSection("AspNetMetrics"));
        }
    }
}