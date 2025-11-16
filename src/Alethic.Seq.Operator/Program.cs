using System.Threading.Tasks;

using Alethic.Seq.Operator.Options;

using KubeOps.Operator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Alethic.Seq.Operator
{

    public static class Program
    {

        public static Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddKubernetesOperator().RegisterComponents();
            builder.Services.AddMemoryCache();
            builder.Services.Configure<OperatorOptions>(builder.Configuration.GetSection("Seq:Operator"));
            builder.Services.AddScoped<LookupService>();

            var app = builder.Build();
            return app.RunAsync();
        }

    }

}
