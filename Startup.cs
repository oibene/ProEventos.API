using System.IO;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using ProEventos.Persistence.Contextos;
using ProEventos.Application.Contratos;
using ProEventos.Application;
using ProEventos.Persistence.Contratos;
using ProEventos.Persistence;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;

namespace ProEventos.API
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
            services.AddDbContext<ProEventosContext>(
                //add banco de dados (useSqlite) e puxa os dados tambem
                context => context.UseSqlite(Configuration.GetConnectionString("Default"))
            );
            services.AddControllers() //para de entrar em loop infinito
            .AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling
                                     = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            // dentro do dominio da aplicação existe assemblies, procura quem herda de profile
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //pra nao dar erro de servidor
            services.AddScoped<IEventoService, EventoService>();
            services.AddScoped<ILoteService, LoteService>();
            
            services.AddScoped<IGeralPersist, GeralPersist>();
            services.AddScoped<IEventoPersist, EventoPersist>();
            services.AddScoped<ILotePersist, LotePersist>();

            services.AddCors();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProEventos.API", Version = "v1" });
            });
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProEventos.API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();


            //precisa permitir o cors pra puxar dados do back
            app.UseCors(cors => cors.AllowAnyHeader()
                                    .AllowAnyMethod()
                                    .AllowAnyOrigin());

            //permite a aplicação add arquivos de imagem ou outros arquivos
            app.UseStaticFiles(new StaticFileOptions(){
               FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "resources")),
               RequestPath = new PathString("/resources")
            });
        
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
