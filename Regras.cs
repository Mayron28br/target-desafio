using System.Text.Json;

namespace DesafioTarget;

public static class Arquivos
{
    public static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    public static T Ler<T>(string caminho) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(caminho), Opcoes)
        ?? throw new InvalidDataException("O arquivo JSON está vazio ou contém null.");
}

public static class Calculos
{
    public static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static void ValidarDinheiro(decimal valor)
    {
        if (valor < 0 || valor != Arredondar(valor))
            throw new ArgumentException("Informe um valor não negativo com até duas casas decimais.");
    }
    public static decimal CalcularComissao(decimal valorVenda)
    {
        ValidarDinheiro(valorVenda);
        if (valorVenda < 100m) return 0m;
        decimal taxa = valorVenda < 500m ? 0.01m : 0.05m;
        // Cada venda é arredondada antes de entrar no total do vendedor.
        return Arredondar(valorVenda * taxa);
    }
    public static Dictionary<string, decimal> CalcularComissoes(List<Venda> vendas)
    {
        var totais = new Dictionary<string, decimal>();
        foreach (Venda venda in vendas)
        {
            if (venda is null || string.IsNullOrWhiteSpace(venda.Vendedor))
                throw new InvalidDataException("Toda venda precisa de um vendedor.");
            string nome = venda.Vendedor.Trim();
            totais.TryGetValue(nome, out decimal acumulado);
            totais[nome] = acumulado + CalcularComissao(venda.Valor);
        }
        return totais;
    }
    public static ResultadoJuros CalcularJuros(decimal valor, DateOnly vencimento, DateOnly hoje)
    {
        ValidarDinheiro(valor);
        int dias = Math.Max(0, hoje.DayNumber - vencimento.DayNumber);
        decimal juros = Arredondar(valor * 0.025m * dias);
        return new ResultadoJuros(dias, juros, valor + juros);
    }
}

public class ControleEstoque
{
    private EstadoEstoque estado;
    private readonly string caminhoEstado;
    public IReadOnlyList<Produto> Produtos => estado.Produtos.Select(p => new Produto
    {
        CodigoProduto = p.CodigoProduto, DescricaoProduto = p.DescricaoProduto, Estoque = p.Estoque
    }).ToList();
    public IReadOnlyList<Movimentacao> Historico => estado.Movimentacoes.AsReadOnly();

    public ControleEstoque(string caminhoInicial, string caminhoEstado)
    {
        this.caminhoEstado = caminhoEstado;
        estado = File.Exists(caminhoEstado)
            ? Arquivos.Ler<EstadoEstoque>(caminhoEstado)
            : new EstadoEstoque
            {
                Produtos = Arquivos.Ler<DadosEstoque>(caminhoInicial).Estoque,
                Movimentacoes = []
            };
        ValidarEstado();
    }
    private void ValidarEstado()
    {
        if (estado.Produtos is null || estado.Movimentacoes is null ||
            estado.Produtos.Any(p => p is null || p.CodigoProduto <= 0 ||
                p.Estoque < 0 || string.IsNullOrWhiteSpace(p.DescricaoProduto)) ||
            estado.Produtos.Select(p => p.CodigoProduto).Distinct().Count() != estado.Produtos.Count)
            throw new InvalidDataException("O arquivo de estoque contém produtos inválidos ou duplicados.");
        if (estado.Movimentacoes.Any(m => m is null || m.Id <= 0 || m.Quantidade <= 0 ||
                m.SaldoFinal < 0 || string.IsNullOrWhiteSpace(m.Descricao) ||
                (m.Tipo != "entrada" && m.Tipo != "saida") ||
                !estado.Produtos.Any(p => p.CodigoProduto == m.CodigoProduto)) ||
            estado.Movimentacoes.Select(m => m.Id).Distinct().Count() != estado.Movimentacoes.Count)
            throw new InvalidDataException("O histórico contém movimentações inválidas ou duplicadas.");
    }
    public Movimentacao Movimentar(int codigo, string tipo, int quantidade, string descricao)
    {
        if (tipo != "entrada" && tipo != "saida")
            throw new ArgumentException("Tipo deve ser entrada ou saida.");
        if (quantidade <= 0) throw new ArgumentException("A quantidade deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("A descrição é obrigatória.");
        Produto produto = estado.Produtos.FirstOrDefault(p => p.CodigoProduto == codigo)
            ?? throw new ArgumentException("Produto não encontrado.");
        if (tipo == "saida" && quantidade > produto.Estoque)
            throw new ArgumentException($"Saldo insuficiente. Disponível: {produto.Estoque}.");
        int saldo = checked(produto.Estoque + (tipo == "entrada" ? quantidade : -quantidade));
        long id = checked(estado.Movimentacoes.Select(m => m.Id).DefaultIfEmpty(0).Max() + 1);
        var movimento = new Movimentacao(id, codigo, tipo, quantidade, descricao.Trim(), saldo, DateTimeOffset.Now);
        var novoEstado = new EstadoEstoque
        {
            Produtos = estado.Produtos.Select(p => new Produto
            {
                CodigoProduto = p.CodigoProduto, DescricaoProduto = p.DescricaoProduto,
                Estoque = p.CodigoProduto == codigo ? saldo : p.Estoque
            }).ToList(),
            Movimentacoes = [.. estado.Movimentacoes, movimento]
        };
        // Saldo e histórico são salvos juntos; a memória só muda depois da gravação.
        string temporario = caminhoEstado + ".tmp";
        File.WriteAllText(temporario, JsonSerializer.Serialize(novoEstado, Arquivos.Opcoes));
        File.Move(temporario, caminhoEstado, overwrite: true);
        estado = novoEstado;
        return movimento;
    }
}
