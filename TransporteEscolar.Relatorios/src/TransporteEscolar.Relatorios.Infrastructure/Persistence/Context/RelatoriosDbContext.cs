using Microsoft.EntityFrameworkCore;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

public class RelatoriosDbContext : DbContext
{
    public RelatoriosDbContext(DbContextOptions<RelatoriosDbContext> options)
        : base(options)
    {
    }

    public DbSet<AlunoSnapshot> Alunos => Set<AlunoSnapshot>();
    public DbSet<PresencaHistorica> PresencasHistoricas => Set<PresencaHistorica>();
    public DbSet<RotaHistorica> RotasHistoricas => Set<RotaHistorica>();
    public DbSet<SolicitacaoRelatorio> SolicitacoesRelatorio => Set<SolicitacaoRelatorio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlunoSnapshot>(entity =>
        {
            entity.ToTable("alunos_snapshot");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.ExternalId)
                .IsUnique()
                .HasFilter("\"ExternalId\" <> 0");
        });

        modelBuilder.Entity<PresencaHistorica>(entity =>
        {
            entity.ToTable("presencas_historicas");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Data).IsRequired();
            entity.Property(x => x.EnderecoUtilizado).HasMaxLength(500);

            entity.HasOne(x => x.Aluno)
                .WithMany()
                .HasForeignKey(x => x.AlunoId);

            entity.HasIndex(x => new { x.AlunoId, x.Data }).IsUnique();
        });

        modelBuilder.Entity<RotaHistorica>(entity =>
        {
            entity.ToTable("rotas_historicas");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Data).IsRequired();
            entity.Property(x => x.DistanciaKm).HasPrecision(10, 2);
        });

        modelBuilder.Entity<SolicitacaoRelatorio>(entity =>
        {
            entity.ToTable("solicitacoes_relatorio");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Tipo)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(x => x.ResultadoJson).HasColumnType("jsonb");
            entity.Property(x => x.Erro).HasMaxLength(2000);
            entity.Property(x => x.PapelSolicitante).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CriadoEm);
            entity.HasIndex(x => new { x.ProfileIdSolicitante, x.CriadoEm });
        });
    }
}
