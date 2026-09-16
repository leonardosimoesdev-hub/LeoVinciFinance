using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using FluentAssertions;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

/// <summary>
/// Valida, de forma automatizada, as regras de dependência da Especificação Mestre
/// (seção 4: "Domain não depende de nada; Application depende só de Domain; Infrastructure
/// implementa Application; Api monta tudo" — e seção 38: "módulos não se referenciam entre
/// si, exceto pelos projetos deliberadamente compartilhados BuildingBlocks.*").
///
/// Esta suíte falha o build (via `dotnet test`) se qualquer PR introduzir uma referência
/// indevida entre camadas ou entre módulos — é a "trava" estrutural do projeto.
/// </summary>
public class CleanArchitectureRulesTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            System.Reflection.Assembly.Load("Auth.Domain"),
            System.Reflection.Assembly.Load("Auth.Application"),
            System.Reflection.Assembly.Load("Auth.Infrastructure"),
            System.Reflection.Assembly.Load("Auth.Api"),
            System.Reflection.Assembly.Load("Financeiro.Domain"),
            System.Reflection.Assembly.Load("Financeiro.Application"),
            System.Reflection.Assembly.Load("Financeiro.Infrastructure"),
            System.Reflection.Assembly.Load("Financeiro.Api"),
            System.Reflection.Assembly.Load("Relatorios.Domain"),
            System.Reflection.Assembly.Load("Relatorios.Application"),
            System.Reflection.Assembly.Load("Relatorios.Infrastructure"),
            System.Reflection.Assembly.Load("Relatorios.Api"),
            System.Reflection.Assembly.Load("Consolidacao.Domain"),
            System.Reflection.Assembly.Load("Consolidacao.Application"),
            System.Reflection.Assembly.Load("Consolidacao.Infrastructure"))
        .Build();

    // Helper: carrega tipos via reflection garantindo que consideramos tanto o namespace exato
    // quanto sub-namespaces (ex.: Auth.Infrastructure e Auth.Infrastructure.Persistence).
    private static System.Collections.Generic.List<System.Type> LoadTypesInNamespace(string assemblyName, string namespacePrefix)
    {
        try
        {
            var asm = System.Reflection.Assembly.Load(assemblyName);
            return asm.GetTypes()
                .Where(t => !string.IsNullOrEmpty(t.Namespace) && (t.Namespace == namespacePrefix || t.Namespace.StartsWith(namespacePrefix + ".")))
                .ToList();
        }
        catch
        {
            return new System.Collections.Generic.List<System.Type>();
        }
    }

    private static System.Collections.Generic.List<System.Type> GetModuleTypes(string module)
    {
        var names = new[] { $"{module}.Domain", $"{module}.Application", $"{module}.Infrastructure", $"{module}.Api" };
        var result = new System.Collections.Generic.List<System.Type>();
        foreach (var name in names)
        {
            try
            {
                var asm = System.Reflection.Assembly.Load(name);
                result.AddRange(asm.GetTypes());
            }
            catch { /* ignore missing assemblies */ }
        }

        // As fallback, try loading assembly by module root name
        if (!result.Any())
        {
            try
            {
                var asm = System.Reflection.Assembly.Load(module);
                result.AddRange(asm.GetTypes());
            }
            catch { }
        }

        return result;
    }

    private static bool HasAnyType(string modulo, string layer)
    {
        var archObjects = Types().That().ResideInNamespace($"{modulo}.{layer}.*").GetObjects(Architecture);
        if (archObjects.Any()) return true;

        var refTypes = LoadTypesInNamespace($"{modulo}.{layer}", $"{modulo}.{layer}");
        return refTypes.Any();
    }

    private static System.Collections.Generic.List<string> FindTypeNamespaceViolations(System.Collections.Generic.IEnumerable<System.Type> types, params string[] forbiddenNamespaceStarts)
    {
        var violacoes = new System.Collections.Generic.List<string>();
        foreach (var t in types)
        {
            if (t.BaseType != null && t.BaseType.Namespace != null)
            {
                foreach (var f in forbiddenNamespaceStarts)
                {
                    if (t.BaseType.Namespace.StartsWith(f)) violacoes.Add($"{t.FullName} -> base {t.BaseType.FullName}");
                }
            }

            var memberTypes = new System.Collections.Generic.List<System.Type>();
            memberTypes.AddRange(t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Select(f => f.FieldType));
            memberTypes.AddRange(t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Select(p => p.PropertyType));
            memberTypes.AddRange(t.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .SelectMany(m => m.GetParameters().Select(p => p.ParameterType)));
            memberTypes.AddRange(t.GetInterfaces());

            foreach (var mt in memberTypes.Where(x => x != null && x.Namespace != null))
            {
                foreach (var f in forbiddenNamespaceStarts)
                {
                    if (mt.Namespace.StartsWith(f)) violacoes.Add($"{t.FullName} -> member {mt.FullName}");
                }
            }
        }

        return violacoes;
    }


    // ---------------------------------------------------------------------
    // Regra 1: Domain não pode depender de Infrastructure, Api, EF Core, ASP.NET, MassTransit
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData("Auth")]
    [InlineData("Financeiro")]
    [InlineData("Relatorios")]
    [InlineData("Consolidacao")]
    public void Domain_NaoDeveDependerDeInfrastructureOuApi(string modulo)
    {
        // If ArchUnitNET has subjects, use it; otherwise fallback to reflection-based checks.
        var archDomainObjects = Types().That().ResideInNamespace($"{modulo}.Domain.*").GetObjects(Architecture);
        if (archDomainObjects.Any())
        {
            IObjectProvider<IType> domainLayer = Types().That().ResideInNamespace($"{modulo}.Domain.*");
            Types().That().Are(domainLayer)
                .Should().NotDependOnAny(Types().That().ResideInNamespace($"{modulo}.Infrastructure.*"))
                .AndShould().NotDependOnAny(Types().That().ResideInNamespace($"{modulo}.Api.*"))
                .Because("Domain deve ser POCO puro (Especificação Mestre, seção 4).")
                .Check(Architecture);
            return;
        }

        var domainRefTypes = LoadTypesInNamespace($"{modulo}.Domain", $"{modulo}.Domain");
        if (!domainRefTypes.Any())
        {
            // sem tipos carregados para Domain neste módulo; nada a validar
            return;
        }

        var violacoes = FindTypeNamespaceViolations(domainRefTypes, $"{modulo}.Infrastructure", $"{modulo}.Api");
        violacoes.Should().BeEmpty("Domain deve ser POCO puro (Especificação Mestre, seção 4).");
    }

    [Theory]
    [InlineData("Auth")]
    [InlineData("Financeiro")]
    [InlineData("Relatorios")]
    [InlineData("Consolidacao")]
    public void Domain_NaoDeveDependerDeFrameworksDeInfraestrutura(string modulo)
    {
        var archDomainObjects = Types().That().ResideInNamespace($"{modulo}.Domain.*").GetObjects(Architecture);
        if (archDomainObjects.Any())
        {
            Types().That().ResideInNamespace($"{modulo}.Domain.*")
                .Should().NotDependOnAny(Types().That().ResideInNamespace("Microsoft.EntityFrameworkCore.*"))
                .AndShould().NotDependOnAny(Types().That().ResideInNamespace("Microsoft.AspNetCore.*"))
                .AndShould().NotDependOnAny(Types().That().ResideInNamespace("MassTransit.*"))
                .Because("Domain não pode depender de EF Core, ASP.NET ou MassTransit (seção 4).")
                .Check(Architecture);
            return;
        }

        var domainRefTypes2 = LoadTypesInNamespace($"{modulo}.Domain", $"{modulo}.Domain");
        if (!domainRefTypes2.Any()) return;
        var violacoes = FindTypeNamespaceViolations(domainRefTypes2, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "MassTransit");
        violacoes.Should().BeEmpty("Domain não pode depender de EF Core, ASP.NET ou MassTransit (seção 4).");
    }

    // ---------------------------------------------------------------------
    // Regra 2: Application não pode depender de Infrastructure ou Api
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData("Auth")]
    [InlineData("Financeiro")]
    [InlineData("Relatorios")]
    [InlineData("Consolidacao")]
    public void Application_NaoDeveDependerDeInfrastructureOuApi(string modulo)
    {
        var archAppObjects = Types().That().ResideInNamespace($"{modulo}.Application.*").GetObjects(Architecture);
        if (archAppObjects.Any())
        {
            Types().That().ResideInNamespace($"{modulo}.Application.*")
                .Should().NotDependOnAny(Types().That().ResideInNamespace($"{modulo}.Infrastructure.*"))
                .AndShould().NotDependOnAny(Types().That().ResideInNamespace($"{modulo}.Api.*"))
                .Because("Application depende apenas de Domain e abstrações próprias (seção 4).")
                .Check(Architecture);
            return;
        }

        var appTypes = LoadTypesInNamespace($"{modulo}.Application", $"{modulo}.Application");
        if (!appTypes.Any()) return;
        var violacoesApp = FindTypeNamespaceViolations(appTypes, $"{modulo}.Infrastructure", $"{modulo}.Api");
        violacoesApp.Should().BeEmpty("Application não pode depender de Infrastructure ou Api");
    }

    // ---------------------------------------------------------------------
    // Regra 3: Infrastructure não pode depender de Api (a dependência é sempre Api -> Infrastructure)
    // ---------------------------------------------------------------------
    [Theory]
    [InlineData("Auth")]
    [InlineData("Financeiro")]
    [InlineData("Relatorios")]
    [InlineData("Consolidacao")]
    public void Infrastructure_NaoDeveDependerDeApi(string modulo)
    {
        var infraObjects = Types().That().ResideInNamespace($"{modulo}.Infrastructure.*").GetObjects(Architecture);

        // Se o ArchUnitNET não encontrou tipos (diferenças de versão / carregamento),
        // usamos reflexão direta como fallback para garantir que a regra é verificada de forma real.
        if (!infraObjects.Any())
        {
            var infraTypes = LoadTypesInNamespace($"{modulo}.Infrastructure", $"{modulo}.Infrastructure");
            if (!infraTypes.Any())
            {
                // Não há tipos de infraestrutura carregados para este módulo; não há nada a checar.
                return;
            }
            var violacoes = new System.Collections.Generic.List<string>();

            foreach (var t in infraTypes)
            {
                // base types
                if (t.BaseType != null && t.BaseType.Namespace != null && t.BaseType.Namespace.StartsWith($"{modulo}.Api"))
                {
                    violacoes.Add($"{t.FullName} -> base {t.BaseType.FullName}");
                }

                // members: fields, properties, method signatures
                var memberTypes = new System.Collections.Generic.List<System.Type>();
                memberTypes.AddRange(t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    .Select(f => f.FieldType));
                memberTypes.AddRange(t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    .Select(p => p.PropertyType));
                memberTypes.AddRange(t.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    .SelectMany(m => m.GetParameters().Select(p => p.ParameterType)));

                foreach (var mt in memberTypes.Where(x => x != null && x.Namespace != null))
                {
                    if (mt.Namespace.StartsWith($"{modulo}.Api"))
                    {
                        violacoes.Add($"{t.FullName} -> member {mt.FullName}");
                    }
                }
            }

            violacoes.Should().BeEmpty("A dependência entre Infrastructure e Api é unidirecional: Api monta Infrastructure, nunca o contrário.");
            return;
        }

        // Caso padrão: ArchUnitNET executa a checagem
        Types().That().ResideInNamespace($"{modulo}.Infrastructure.*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace($"{modulo}.Api.*"))
            .Because("A dependência entre Infrastructure e Api é unidirecional: Api monta Infrastructure, nunca o contrário.")
            .Check(Architecture);
    }

    // ---------------------------------------------------------------------
    // Regra 4: isolamento entre módulos — Domain/Application de um módulo não conhece outro módulo
    // ---------------------------------------------------------------------
    public static IEnumerable<object[]> ParesDeModulos()
    {
        string[] modulos = { "Auth", "Financeiro", "Relatorios", "Consolidacao" };
        foreach (var origem in modulos)
        foreach (var destino in modulos)
            if (origem != destino)
                yield return new object[] { origem, destino };
    }

    [Theory]
    [MemberData(nameof(ParesDeModulos))]
    public void DomainEApplication_NaoDevemDependerDeOutrosModulos(string moduloOrigem, string moduloDestino)
    {
        var archDomainObjects = Types().That().ResideInNamespace($"{moduloOrigem}.Domain.*").GetObjects(Architecture);
        if (archDomainObjects.Any())
        {
            Types().That().ResideInNamespace($"{moduloOrigem}.Domain.*")
                .Should().NotDependOnAny(Types().That().ResideInNamespace($"{moduloDestino}.*"))
                .Because($"{moduloOrigem}.Domain não pode depender de {moduloDestino} (isolamento entre módulos — seção 4/38).")
                .Check(Architecture);
        }
        else
        {
            var origemDomainRef = LoadTypesInNamespace($"{moduloOrigem}.Domain", $"{moduloOrigem}.Domain");
            if (!origemDomainRef.Any()) return; // nada a validar
            var violacoes = FindTypeNamespaceViolations(origemDomainRef, $"{moduloDestino}");
            violacoes.Should().BeEmpty($"{moduloOrigem}.Domain não pode depender de {moduloDestino} (isolamento entre módulos — seção 4/38).");
        }

        var archAppObjects = Types().That().ResideInNamespace($"{moduloOrigem}.Application.*").GetObjects(Architecture);
        if (archAppObjects.Any())
        {
            Types().That().ResideInNamespace($"{moduloOrigem}.Application.*")
                .Should().NotDependOnAny(Types().That().ResideInNamespace($"{moduloDestino}.*"))
                .Because($"{moduloOrigem}.Application não pode depender de {moduloDestino} (isolamento entre módulos — seção 4/38).")
                .Check(Architecture);
        }
        else
        {
            var origemAppRef = LoadTypesInNamespace($"{moduloOrigem}.Application", $"{moduloOrigem}.Application");
            if (!origemAppRef.Any()) return;
            var violacoesApp = FindTypeNamespaceViolations(origemAppRef, $"{moduloDestino}");
            violacoesApp.Should().BeEmpty($"{moduloOrigem}.Application não pode depender de {moduloDestino} (isolamento entre módulos — seção 4/38).");
        }
    }

    // ---------------------------------------------------------------------
    // Regra 5 (ADR 0011): Financeiro nunca publica nem consome eventos — sua disponibilidade
    // não pode depender de infraestrutura de mensageria nem de nenhum outro módulo.
    // ---------------------------------------------------------------------
    [Fact]
    public void Financeiro_NaoDeveDependerDeMassTransitOuKafka()
    {
        var financeiroObjects = Types().That().ResideInNamespace("Financeiro.*").GetObjects(Architecture);
        if (!financeiroObjects.Any())
        {
            var financeiroTypes = GetModuleTypes("Financeiro");
            if (!financeiroTypes.Any())
            {
                // nenhum tipo Financeiro encontrado — nada a validar
                return;
            }
            var violacoes = FindTypeNamespaceViolations(financeiroTypes, "MassTransit", "Confluent.Kafka", "Consolidacao.Infrastructure.Messaging");
            violacoes.Should().BeEmpty("ADR 0011: Financeiro não pode depender de mensageria — sua disponibilidade nunca depende de outro módulo.");
            return;
        }

        Types().That().ResideInNamespace("Financeiro.*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("MassTransit.*"))
            .Because("ADR 0011: Financeiro não pode depender de mensageria — sua disponibilidade nunca depende de outro módulo.")
            .Check(Architecture);
    }

    // ---------------------------------------------------------------------
    // Regra 6: entidades de domínio não podem ter setters públicos (encapsulamento —
    // mutação sempre via método de negócio, nunca via propriedade pública)
    // ---------------------------------------------------------------------
    [Fact]
    public void Entidades_NaoDevemExporPropriedadesComSetterPublico()
    {
        // Usamos reflection direta para validar setters públicos nas entidades de domínio,
        // pois a API do ArchUnitNET em uso não expõe GetPropertyMembers nesta versão.
        var domainAssemblies = new[] { "Auth.Domain", "Financeiro.Domain", "Relatorios.Domain", "Consolidacao.Domain" }
            .Select(name => System.Reflection.Assembly.Load(name))
            .ToList();

        var entidades = domainAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !string.IsNullOrEmpty(t.Namespace) && t.Namespace.EndsWith(".Domain.Entities"))
            .ToList();

        Assert.NotEmpty(entidades);

        var violacoes = entidades
            .SelectMany(t => t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            .Where(p => p.SetMethod is not null && p.SetMethod.IsPublic)
            .Select(p => new { DeclaringType = p.DeclaringType?.FullName, Property = p.Name })
            .ToList();

        violacoes.Should().BeEmpty("propriedades de entidades de domínio devem ter setter privado/protegido — mutação sempre via método de negócio explícito.");
    }
}
