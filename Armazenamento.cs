namespace DesafioTarget;

public static class Armazenamento
{
    public static string PastaPadrao => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TargetDesafio");

    public static string Preparar(string pasta, string arquivoInicial, string arquivoAntigo)
    {
        Directory.CreateDirectory(pasta);
        string destino = Path.Combine(pasta, "estado-estoque.json");

        // A primeira abertura importa o histórico da versão anterior sem apagá-lo.
        // Se já houver estado novo, ele sempre tem prioridade.
        if (!File.Exists(destino) && File.Exists(arquivoAntigo))
        {
            _ = new ControleEstoque(arquivoInicial, arquivoAntigo);
            string temporario = destino + ".migracao.tmp";
            File.Copy(arquivoAntigo, temporario, overwrite: true);
            File.Move(temporario, destino);
        }
        return destino;
    }
}
