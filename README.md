# Desafio Target Sistemas

Aplicação de console em C# e .NET 10 com três exercícios: comissões, estoque e juros.

## Executar

Com o .NET SDK 10 instalado, abra o terminal na pasta do projeto:

```powershell
dotnet run
```

- **1 — Comissões:** lê `dados/vendas.json` e mostra o total por vendedor.
- **2 — Estoque:** consulta produtos, registra entradas e saídas e exibe o histórico.
- **3 — Juros:** calcula o atraso a partir do valor e do vencimento informados.
- **0 — Sair.**

Digite valores com vírgula decimal, sem separador de milhar (`1200,50`), e datas no formato `dd/MM/aaaa`.

## Regras adotadas

**Comissões:** vendas abaixo de R$ 100 não geram comissão; de R$ 100 até menos de R$ 500 geram 1%; a partir de R$ 500 geram 5%. Cada comissão é arredondada para centavos antes da soma por vendedor, com meio centavo para cima.

**Estoque:** cada movimentação tem número sequencial, tipo, quantidade, descrição e saldo final. Quantidades devem ser inteiras positivas e saídas não podem superar o saldo. Saldo e histórico ficam em `%LOCALAPPDATA%\TargetDesafio\estado-estoque.json`, fora da pasta de compilação. Limpar `bin` ou `obj` não apaga esses dados. Na primeira abertura, se houver um histórico antigo junto do executável e ainda não existir o novo, ele é copiado automaticamente, preservando o original. Se já existir estado no novo local, ele tem prioridade. Um erro de movimentação mantém o usuário no menu de estoque para tentar novamente. O programa permite uma instância por vez na sessão local.

**Juros:** juros simples de 2,5% por dia corrido de atraso, arredondados para centavos. Vencimento hoje ou no futuro não gera juros. Usa a data atual do computador. Juros simples e arredondamento por venda são premissas, pois o enunciado não especifica esses detalhes.

## Arquivos

- `Program.cs`: menu, entrada de dados e resultados.
- `Modelos.cs`: estruturas de vendas, produtos e movimentações.
- `Regras.cs`: cálculos, leitura de JSON e controle de estoque.
- `Armazenamento.cs`: localização dos dados salvos e importação do histórico antigo.
- `dados/`: arquivos originais de vendas e estoque.
- `testes/`: projeto separado de verificações automatizadas, sem dependências externas.


## Verificações automatizadas

Na pasta principal, execute:

```powershell
dotnet run --project testes/DesafioTarget.Testes.csproj
```

O executor informa cada caso e termina com código de saída 1 se houver falhas. É um programa de verificações próprio, executado com `dotnet run`, e não um projeto xUnit/MSTest executado com `dotnet test`. A escolha mantém o desafio sem pacotes externos.

São verificados limites de comissão, totais do enunciado, juros com datas fixas, entradas e saídas, rejeições sem alteração de saldo, persistência, falha de gravação e migração de histórico. Cada cenário de estoque usa uma pasta temporária exclusiva, sem alterar o estoque usado pelo programa.

Para demonstrações isoladas, é possível definir a variável de ambiente `TARGET_DESAFIO_DADOS` apontando para outra pasta. Sem essa variável, usa-se o local padrão descrito acima. Os números das movimentações são únicos dentro de cada histórico preservado. A aplicação é local, para uma instância por vez na sessão; não implementa um serviço multiusuário.
