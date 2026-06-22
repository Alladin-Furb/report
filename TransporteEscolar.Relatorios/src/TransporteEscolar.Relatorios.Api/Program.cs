using TransporteEscolar.Relatorios.Api.Extensions;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.Services;
using TransporteEscolar.Relatorios.Infrastructure.DependencyInjection;
using TransporteEscolar.Relatorios.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddReportHealthChecks();

builder.Services.AddScoped<IRelatorioMensalService, RelatorioMensalService>();
builder.Services.AddScoped<IFrequenciaAlunoService, FrequenciaAlunoService>();
builder.Services.AddScoped<IKmService, KmService>();
builder.Services.AddScoped<IIndicadorOperacionalService, IndicadorOperacionalService>();
builder.Services.AddScoped<ISyncHistoricoService, SyncHistoricoService>();
builder.Services.AddScoped<ISolicitacaoRelatorioService, SolicitacaoRelatorioService>();
builder.Services.AddScoped<IProcessadorRelatorioService, ProcessadorRelatorioService>();
builder.Services.AddScoped<IGeradorRelatorioService, GeradorRelatorioService>();

builder.Services.AddHostedService<PresencaConsumer>();
builder.Services.AddHostedService<AlunoConsumer>();
builder.Services.AddHostedService<RelatorioDispatcher>();
builder.Services.AddHostedService<RelatorioWorker>();

var app = builder.Build();

app.UseGlobalExceptionMiddleware();

await app.ApplyDatabaseMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapReportHealthChecks();

app.Run();
