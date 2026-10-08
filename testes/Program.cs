using DesafioTarget;
using System.Text.Json;

int passou = 0, falhou = 0;
string dados = Path.Combine(AppContext.BaseDirectory, "dados");

void Teste(string nome, Action acao)
{
    try { acao(); passou++; Console.WriteLine($"OK: {nome}"); }
    catch (Exception erro) { falhou++; Console.WriteLine($"FALHOU: {nome}: {erro.Message}"); }
}
void Igual<T>(T esperado, T atual)
{
    if (!EqualityComparer<T>.Default.Equals(esperado, atual))
        throw new Exception($"Esperado: {esperado}. Obtido: {atual}.");
}
void Rejeita<T>(Action acao) where T : Exception
{
    try { acao(); } catch (T) { return; }
    throw new Exception($"Deveria lançar {typeof(T).Name}.");
}
void ComEstoque(Action<ControleEstoque, string> acao)
{
    string pasta = Path.Combine(Path.GetTempPath(), "target-testes-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(pasta);
    try
    {
        acao(new ControleEstoque(Path.Combine(dados, "estoque.json"), Path.Combine(pasta, "estado.json")), pasta);
    }
    finally
    {
        // Pasta exclusiva desta verificação; não contém dados do usuário.
        foreach (string arquivo in Directory.EnumerateFiles(pasta)) File.Delete(arquivo);
        Directory.Delete(pasta);
    }
}
foreach (var (valor, esperado) in new[] { (0m, 0m), (99.99m, 0m), (100m, 1m), (499.99m, 5m), (500m, 25m), (1200.50m, 60.03m) })
    Teste($"Comissão de {valor}", () => Igual(esperado, Calculos.CalcularComissao(valor)));
Teste("Comissão negativa rejeitada", () => Rejeita<ArgumentException>(() => Calculos.CalcularComissao(-1)));
Teste("Mais de duas casas rejeitado", () => Rejeita<ArgumentException>(() => Calculos.CalcularComissao(100.001m)));
Teste("Faixa aplicada por venda", () => Igual(6m, Calculos.CalcularComissoes([
    new Venda { Vendedor = "A", Valor = 300 }, new Venda { Vendedor = "A", Valor = 300 }])["A"]));
Teste("Vendedor vazio rejeitado", () => Rejeita<InvalidDataException>(() => Calculos.CalcularComissoes([
    new Venda { Vendedor = " ", Valor = 100 }])));
Teste("Lista vazia", () => Igual(0, Calculos.CalcularComissoes([]).Count));
Teste("Totais do enunciado", () =>
{
    var vendas = Arquivos.Ler<DadosVendas>(Path.Combine(dados, "vendas.json")).Vendas;
    Igual(36, vendas.Count);
    var totais = Calculos.CalcularComissoes(vendas);
    Igual(4, totais.Count);
    Igual(495.69m, totais["João Silva"]); Igual(465.96m, totais["Maria Souza"]);
    Igual(379.38m, totais["Carlos Oliveira"]); Igual(404.99m, totais["Ana Lima"]);
});
var hoje = new DateOnly(2026, 10, 8);
foreach (int dias in new[] { -2, 0, 1, 2, 10 })
    Teste($"Juros: {dias} dias", () =>
    {
        var resultado = Calculos.CalcularJuros(100, hoje.AddDays(-dias), hoje);
        Igual(Math.Max(0, dias), resultado.DiasAtraso);
        Igual(2.5m * Math.Max(0, dias), resultado.Juros);
        Igual(100m + 2.5m * Math.Max(0, dias), resultado.Total);
    });
Teste("Juros em ano bissexto", () => Igual(2, Calculos.CalcularJuros(100,
    new DateOnly(2024, 2, 28), new DateOnly(2024, 3, 1)).DiasAtraso));
Teste("Juros na virada do ano", () => Igual(2, Calculos.CalcularJuros(100,
    new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 2)).DiasAtraso));
Teste("Arredondamento dos juros", () => Igual(0.03m, Calculos.CalcularJuros(1, hoje.AddDays(-1), hoje).Juros));
Teste("Juros de valor negativo", () => Rejeita<ArgumentException>(() => Calculos.CalcularJuros(-1, hoje, hoje)));
Teste("Estoque inicial", () => ComEstoque((estoque, _) =>
{
    Igual(5, estoque.Produtos.Count);
    Igual(150, estoque.Produtos.First(p => p.CodigoProduto == 101).Estoque);
}));
Teste("Entrada, saída e persistência", () => ComEstoque((estoque, pasta) =>
{
    Igual(160, estoque.Movimentar(101, "entrada", 10, "Compra").SaldoFinal);
    Igual(155, estoque.Movimentar(101, "saida", 5, "Venda").SaldoFinal);
    var reaberto = new ControleEstoque(Path.Combine(dados, "estoque.json"), Path.Combine(pasta, "estado.json"));
    Igual(155, reaberto.Produtos.First(p => p.CodigoProduto == 101).Estoque);
    Igual(2, reaberto.Historico.Count);
    Igual(3L, reaberto.Movimentar(101, "entrada", 1, "Devolução").Id);
}));
Teste("Saída pode zerar saldo", () => ComEstoque((estoque, _) => Igual(0, estoque.Movimentar(101, "saida", 150, "Venda").SaldoFinal)));
foreach (var (nome, codigo, tipo, quantidade, descricao) in new[] {
    ("Saldo insuficiente", 101, "saida", 999, "Venda"),
    ("Produto inexistente", 999, "entrada", 1, "Compra"),
    ("Quantidade zero", 101, "entrada", 0, "Compra"),
    ("Quantidade negativa", 101, "entrada", -1, "Compra"),
    ("Descrição vazia", 101, "entrada", 1, " "),
    ("Tipo inválido", 101, "outro", 1, "Compra") })
    Teste(nome + " preserva estado", () => ComEstoque((estoque, pasta) =>
    {
        estoque.Movimentar(101, "entrada", 1, "Compra inicial");
        string antes = File.ReadAllText(Path.Combine(pasta, "estado.json"));
        Rejeita<ArgumentException>(() => estoque.Movimentar(codigo, tipo, quantidade, descricao));
        Igual(antes, File.ReadAllText(Path.Combine(pasta, "estado.json")));
        Igual(151, estoque.Produtos.First(p => p.CodigoProduto == 101).Estoque);
        Igual(1, estoque.Historico.Count);
    }));
Teste("Overflow de saldo", () => ComEstoque((estoque, _) =>
    Rejeita<OverflowException>(() => estoque.Movimentar(101, "entrada", int.MaxValue, "Compra"))));
Teste("Falha ao salvar preserva memória", () => ComEstoque((_, pasta) =>
{
    var estoque = new ControleEstoque(Path.Combine(dados, "estoque.json"), Path.Combine(pasta, "ausente", "estado.json"));
    Rejeita<IOException>(() => estoque.Movimentar(101, "entrada", 1, "Compra"));
    Igual(150, estoque.Produtos.First(p => p.CodigoProduto == 101).Estoque);
    Igual(0, estoque.Historico.Count);
}));
Teste("Migração preserva saldo, histórico e arquivo antigo", () => ComEstoque((estoque, pasta) =>
{
    estoque.Movimentar(101, "entrada", 10, "Compra");
    string antigo = Path.Combine(pasta, "estado.json");
    string antes = File.ReadAllText(antigo);
    string novo = Armazenamento.Preparar(pasta, Path.Combine(dados, "estoque.json"), antigo);
    Igual(antes, File.ReadAllText(antigo));
    var migrado = new ControleEstoque(Path.Combine(dados, "estoque.json"), novo);
    Igual(160, migrado.Produtos.First(p => p.CodigoProduto == 101).Estoque);
    Igual(2L, migrado.Movimentar(101, "saida", 1, "Venda").Id);
    // A próxima abertura não pode substituir o estado novo pela cópia antiga.
    Armazenamento.Preparar(pasta, Path.Combine(dados, "estoque.json"), antigo);
    Igual(159, new ControleEstoque(Path.Combine(dados, "estoque.json"), novo).Produtos.First(p => p.CodigoProduto == 101).Estoque);
}));
Teste("Migração rejeita arquivo corrompido", () => ComEstoque((_, pasta) =>
{
    string antigo = Path.Combine(pasta, "antigo.json");
    File.WriteAllText(antigo, "{");
    Rejeita<JsonException>(() => Armazenamento.Preparar(pasta, Path.Combine(dados, "estoque.json"), antigo));
    Igual(false, File.Exists(Path.Combine(pasta, "estado-estoque.json")));
    Igual("{", File.ReadAllText(antigo));
}));
Teste("Primeiro uso sem histórico", () => ComEstoque((_, pasta) =>
{
    string caminho = Armazenamento.Preparar(pasta, Path.Combine(dados, "estoque.json"), Path.Combine(pasta, "ausente.json"));
    var estoque = new ControleEstoque(Path.Combine(dados, "estoque.json"), caminho);
    Igual(0, estoque.Historico.Count);
    Igual(1L, estoque.Movimentar(101, "entrada", 1, "Compra").Id);
}));
Console.WriteLine($"\n{passou} testes passaram; {falhou} falharam.");
Environment.ExitCode = falhou == 0 ? 0 : 1;
