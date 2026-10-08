namespace DesafioTarget;

public class Venda
{
    public required string Vendedor { get; set; }
    public required decimal Valor { get; set; }
}
public class DadosVendas
{
    public required List<Venda> Vendas { get; set; }
}
public class Produto
{
    public required int CodigoProduto { get; set; }
    public required string DescricaoProduto { get; set; }
    public required int Estoque { get; set; }
}
public class DadosEstoque
{
    public required List<Produto> Estoque { get; set; }
}
public record ResultadoJuros(int DiasAtraso, decimal Juros, decimal Total);
public record Movimentacao(long Id, int CodigoProduto, string Tipo, int Quantidade,
    string Descricao, int SaldoFinal, DateTimeOffset Data);
public class EstadoEstoque
{
    public required List<Produto> Produtos { get; set; }
    public required List<Movimentacao> Movimentacoes { get; set; }
}
