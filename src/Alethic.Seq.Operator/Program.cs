using System.Threading.Tasks;

using Alethic.Seq.Operator.Options;

using KubeOps.Operator;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Alethic.Seq.Operator
{

    public static class Program
    {

        public static Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var operatorOptions = builder.Configuration.GetSection("Seq:Operator").Get<OperatorOptions>() ?? new OperatorOptions();
            builder.Services.AddKubernetesOperator(s =>
            {
                s.ParallelReconciliation.MaxParallelReconciliations = operatorOptions.Reconciliation.MaxParallelReconciliations;

                // KubeOps would otherwise attach every registered finalizer on each reconciliation, including the
                // retired identifiers that stay registered only so entities already carrying them can drain. Finalizer
                // attachment is owned by V1alpha1Controller instead; see EntityFinalizers.
                s.AutoAttachFinalizers = false;
            }).RegisterComponents();

            builder.Services.AddMemoryCache();
            builder.Services.Configure<OperatorOptions>(builder.Configuration.GetSection("Seq:Operator"));
            builder.Services.AddScoped<LookupService>();

            var app = builder.Build();
            return app.RunAsync();
        }

    }

}
