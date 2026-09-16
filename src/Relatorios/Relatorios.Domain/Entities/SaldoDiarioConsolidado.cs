using BuildingBlocks.Common.Domain;
using Relatorios.Domain.Exceptions;

namespace Relatorios.Domain.Entities;

/// <summary>
/// Saldo diário consolidado de uma conta (Especificação Mestre, seção 19).
/// Constraint única (IdConta, Data) garantida também no banco (índice único) — o Domain
/// apenas valida os dados intrínsecos; a unicidade de fato (evitar corrida entre réplicas)
/// é responsabilidade do índice único + tratamento de exceção de violação em Infrastructure
/// (ver docs/adr/0007-idempotencia-consolidacao.md).
/// </summary>
public class SaldoDiarioConsolidado : Entity
{
    public Guid IdConta { get; private set; }

    public DateOnly Data { get; private set; }

    /// <summary>numeric(18,2) no banco, decimal no Domain (seção 13).</summary>
    public decimal Saldo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    private SaldoDiarioConsolidado() { }

    private SaldoDiarioConsolidado(Guid id, Guid idConta, DateOnly data, decimal saldo, DateTimeOffset criadoEm) : base(id)
    {
        IdConta = idConta;
        Data = data;
        Saldo = saldo;
        CriadoEm = criadoEm;
    }

    public static SaldoDiarioConsolidado Criar(Guid idConta, DateOnly data, decimal saldo)
    {
        if (idConta == Guid.Empty)
            throw new RelatoriosDomainException("IdConta é obrigatório.");

        if (saldo == 0)
            throw new RelatoriosDomainException("O saldo consolidado não pode ser zero.");

        return new SaldoDiarioConsolidado(Guid.NewGuid(), idConta, data, saldo, DateTimeOffset.UtcNow);
    }
}
