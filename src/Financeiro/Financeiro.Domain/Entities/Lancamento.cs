using BuildingBlocks.Common.Domain;
using Financeiro.Domain.Exceptions;

namespace Financeiro.Domain.Entities;

/// <summary>
/// Lançamento de fluxo de caixa (débito ou crédito). Regra intrínseca central do domínio
/// (Especificação Mestre, seções 17 e 22): valor é decimal, nunca zero, pode ser positivo
/// (crédito) ou negativo (débito).
/// </summary>
public class Lancamento : Entity
{
    public Guid IdConta { get; private set; }

    /// <summary>Valor monetário — decimal no Domain, numeric(18,2) no banco (seção 13).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Data de competência do lançamento (não a data de gravação em si).</summary>
    public DateOnly Data { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    private Lancamento() { }

    private Lancamento(Guid id, Guid idConta, decimal valor, DateOnly data, DateTimeOffset criadoEm) : base(id)
    {
        IdConta = idConta;
        Valor = valor;
        Data = data;
        CriadoEm = criadoEm;
    }

    /// <summary>
    /// Cria um novo lançamento para a data informada (ou "hoje" quando omitida).
    /// </summary>
    public static Lancamento Criar(Guid idConta, decimal valor, DateOnly? data = null)
    {
        if (idConta == Guid.Empty)
            throw new FinanceiroDomainException("IdConta é obrigatório.");

        if (valor == 0)
            throw new FinanceiroDomainException("O valor do lançamento não pode ser zero.");

        var dataLancamento = data ?? DateOnly.FromDateTime(DateTime.UtcNow);

        return new Lancamento(Guid.NewGuid(), idConta, valor, dataLancamento, DateTimeOffset.UtcNow);
    }

    public bool EhCredito => Valor > 0;

    public bool EhDebito => Valor < 0;
}
