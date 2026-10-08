using System.Globalization;
using System.Text;
using System.Text.Json;
using DesafioTarget;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
var cultura = CultureInfo.GetCultureInfo("pt-BR");

// Evita duas instâncias sobrescrevendo o mesmo estoque.
using var trava = new Mutex(false, "TargetDesafioEstoqueLocal");
bool adquiriu;
try { adquiriu = trava.WaitOne(0); }
catch (AbandonedMutexException) { adquiriu = true; }
if (!adquiriu)
{
    Console.WriteLine("O programa já está aberto. Use uma instância por vez.");
    return;
}
try
{
    while (true)
    {
        Console.WriteLine("\nDESAFIO TARGET SISTEMAS");
        Console.WriteLine("1 - Comissões por vendedor");
        Console.WriteLine("2 - Estoque e movimentações");
        Console.WriteLine("3 - Juros por atraso");
        Console.WriteLine("0 - Sair");
        string opcao = Ler("Escolha: ");
        if (opcao == "0") break;
        try
        {
            switch (opcao)
            {
                case "1": MostrarComissoes(); break;
                case "2": MenuEstoque(); break;
                case "3": MostrarJuros(); break;
                default: Console.WriteLine("Opção inválida."); break;
            }
        }
        catch (EndOfStreamException) { throw; }
        catch (Exception erro) when (erro is ArgumentException or JsonException or
            IOException or InvalidDataException or UnauthorizedAccessException or OverflowException)
        {
            Console.WriteLine($"Não foi possível concluir: {erro.Message}");
        }
    }
}
catch (EndOfStreamException) { Console.WriteLine("\nEntrada encerrada."); }
finally { trava.ReleaseMutex(); }

string Ler(string mensagem)
{
    Console.Write(mensagem);
    return Console.ReadLine()?.Trim() ?? throw new EndOfStreamException();
}
int LerInteiro(string mensagem)
{
    while (true)
    {
        if (int.TryParse(Ler(mensagem), out int valor) && valor > 0) return valor;
        Console.WriteLine("Digite um número inteiro maior que zero.");
    }
}
string Dinheiro(decimal valor) => valor.ToString("C2", cultura);
string ArquivoDados(string nome) => Path.Combine(AppContext.BaseDirectory, "dados", nome);

void MostrarComissoes()
{
    var dados = Arquivos.Ler<DadosVendas>(ArquivoDados("vendas.json"));
    if (dados.Vendas is null) throw new InvalidDataException("A lista de vendas não foi informada.");
    var totais = Calculos.CalcularComissoes(dados.Vendas);
    Console.WriteLine($"\nVendas carregadas: {dados.Vendas.Count}");
    foreach (var vendedor in totais)
        Console.WriteLine($"{vendedor.Key}: {Dinheiro(vendedor.Value)}");
}
void MostrarJuros()
{
    decimal valor;
    while (true)
    {
        string entrada = Ler("Valor original (exemplo: 100,00; sem ponto de milhar): ");
        if (decimal.TryParse(entrada, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                cultura, out valor) && valor >= 0 && valor == Calculos.Arredondar(valor)) break;
        Console.WriteLine("Digite um valor não negativo, com até duas casas decimais e vírgula decimal.");
    }
    DateOnly vencimento;
    while (!DateOnly.TryParseExact(Ler("Vencimento (dd/MM/aaaa): "), "dd/MM/yyyy", cultura,
        DateTimeStyles.None, out vencimento))
        Console.WriteLine("Data inválida. Exemplo de formato: 08/10/2026.");
    DateOnly hoje = DateOnly.FromDateTime(DateTime.Today);
    var resultado = Calculos.CalcularJuros(valor, vencimento, hoje);
    Console.WriteLine($"Data de referência: {hoje:dd/MM/yyyy}");
    Console.WriteLine($"Dias de atraso: {resultado.DiasAtraso}");
    Console.WriteLine($"Juros simples (2,5% ao dia): {Dinheiro(resultado.Juros)}");
    Console.WriteLine($"Total: {Dinheiro(resultado.Total)}");
}
void MenuEstoque()
{
    string pasta = Environment.GetEnvironmentVariable("TARGET_DESAFIO_DADOS")
        ?? Armazenamento.PastaPadrao;
    string caminhoEstado = Armazenamento.Preparar(pasta, ArquivoDados("estoque.json"),
        Path.Combine(AppContext.BaseDirectory, "estado-estoque.json"));
    var estoque = new ControleEstoque(ArquivoDados("estoque.json"), caminhoEstado);
    while (true)
    {
        Console.WriteLine("\n1 - Consultar produtos | 2 - Movimentar | 3 - Histórico | 0 - Voltar");
        string opcao = Ler("Escolha: ");
        if (opcao == "0") return;
        try
        {
            switch (opcao)
            {
                case "1":
                    foreach (var produto in estoque.Produtos)
                        Console.WriteLine($"{produto.CodigoProduto} | {produto.DescricaoProduto} | Saldo: {produto.Estoque}");
                    break;
                case "2":
                    int codigo = LerInteiro("Código do produto: ");
                    string tipo = Ler("Tipo (E = entrada, S = saída): ").ToUpperInvariant();
                    if (tipo != "E" && tipo != "S")
                    {
                        Console.WriteLine("Tipo inválido. Use E ou S.");
                        break;
          
            }
                int quantidade = LerInteiro("Quantidade: ");
                string descricao = Ler("Descrição da movimentação: ");
                var movimento = estoque.Movimentar(codigo, tipo == "E" ? "entrada" : "saida", quantidade, descricao);
                Console.WriteLine($"Movimentação #{movimento.Id} registrada. Estoque final: {movimento.SaldoFinal}.");
                break;
            case "3":
                if (estoque.Historico.Count == 0) Console.WriteLine("Nenhuma movimentação registrada.");
                foreach (var item in estoque.Historico)
                    Console.WriteLine($"#{item.Id} | {item.Data:dd/MM/yyyy HH:mm:ss} | Produto {item.CodigoProduto} | " +
                        $"{item.Tipo} | Quantidade: {item.Quantidade} | {item.Descricao} | Saldo final: {item.SaldoFinal}");
                break;
            default: Console.WriteLine("Opção inválida."); break;
          }
        }
        catch (EndOfStreamException) { throw; }
        catch (Exception erro) when (erro is ArgumentException or JsonException or
            IOException or InvalidDataException or UnauthorizedAccessException or OverflowException)
        {
            Console.WriteLine($"Não foi possível concluir: {erro.Message}");
        }
    }
}


