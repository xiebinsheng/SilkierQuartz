using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore31.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using SilkierQuartz;
using SilkierQuartz.HostedService;

namespace AspNetCore31
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
            services.AddRazorPages();
            services.AddSilkierQuartz();
            services.AddOptions();
            services.Configure<AppSettings>(Configuration);
            services.Configure<InjectProperty>(options => { options.WriteText = "This is inject string"; });
            services.AddQuartzJob<HelloJob>()
                    .AddQuartzJob<InjectSampleJob>()
                    .AddQuartzJob<HelloJobSingle>()
                    .AddQuartzJob<InjectSampleJobSingle>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<AppSettings> settings, ISchedulerFactory factory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            var scheduler = DemoScheduler.Create().Result;
        
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseSilkierQuartz(new SilkierQuartzOptions() { Scheduler = scheduler, VirtualPathRoot = "/SilkierQuartz" , UseLocalTime =true, DefaultDateFormat="yyyy-MM-dd", DefaultTimeFormat="HH:mm:ss" });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            app.UseQuartzJob<HelloJobSingle>(TriggerBuilder.Create().WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever()))
            .UseQuartzJob<InjectSampleJobSingle>(() =>
            {
                return TriggerBuilder.Create()
                   .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever());
            });

            app.UseQuartzJob<HelloJob>(new List<TriggerBuilder>
                {
                    TriggerBuilder.Create()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever()),
                    TriggerBuilder.Create()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(2).RepeatForever())
                });

            app.UseQuartzJob<InjectSampleJob>(() =>
            {
                var result = new List<TriggerBuilder>();
                result.Add(TriggerBuilder.Create()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever()));
                return result;
            });

        }
    }
}
