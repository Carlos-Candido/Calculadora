# Calculadora estatística

Aplicativo desktop em C# com .NET 10 e Avalonia 12.1.4 para calcular intervalos de confiança de média e proporção. A interface e os cálculos são locais: depois de restaurar as dependências e obter a distribuição, não há navegador, servidor web, login, banco de dados ou serviço externo durante o uso.

## Desenvolvimento

Pré-requisitos: SDK do .NET 10 e acesso ao NuGet na primeira restauração (ou pacotes já disponíveis no cache).

```bash
dotnet restore Calculadora.slnx
dotnet run --project src/Calculadora.Desktop/Calculadora.Desktop.csproj
```

Para executar a suíte do Core:

```bash
dotnet test tests/Calculadora.Core.Tests/Calculadora.Core.Tests.csproj
```

A janela usa Avalonia com tema Fluent e acompanha a preferência de tema claro/escuro do sistema. Os controles começam vazios; mudar fórmula, confiança, tipo de desvio ou qualquer entrada remove o resultado anterior. Use vírgula ou ponto como separador decimal, sem agrupamento de milhares. As entradas `n` e `k` aceitam somente inteiros.

## Publicações portáteis

As pastas de saída são locais, completas e ignoradas pelo Git (`artifacts/`):

```bash
dotnet publish src/Calculadora.Desktop/Calculadora.Desktop.csproj -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false -o artifacts/linux-x64
dotnet publish src/Calculadora.Desktop/Calculadora.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false -o artifacts/win-x64
```

Copie e mantenha a pasta inteira de cada RID; não distribua apenas o executável. Para iniciar no Linux, use `./artifacts/linux-x64/Calculadora.Desktop`. No Windows x64, abra `artifacts/win-x64/Calculadora.Desktop.exe`. As publicações incluem o runtime .NET, mas não as bibliotecas gráficas do sistema operacional.

### Linux e XWayland

A publicação Linux usa a plataforma X11 do Avalonia. Em sessão Wayland, é necessário XWayland e um `DISPLAY` acessível. O sistema também precisa das bibliotecas nativas de X11, ICE, SM e Fontconfig (em distribuições Debian/Ubuntu, normalmente `libx11-6`, `libice6`, `libsm6` e `libfontconfig1`). A distribuição não instala dependências de sistema. Nenhuma conexão de rede é necessária para realizar cálculos depois da publicação.

## Métodos, precisão e limites

- Média: `Em = Z × desvio / √n` e `IC = x̄ ± Em`. Com desvio populacional, `n` deve ser positivo; com desvio amostral, este método exige `n > 30`. Não é usado t de Student.
- Proporção: `p = k/n`, `Ep = Z × √(p × (1−p)/n)` e `IC = p ± Ep`. `k` representa as ocorrências do evento de interesse informado pelo usuário; não é convertido para o evento complementar.
- Confiança disponível: 90%, 95% e 99%, usando os valores críticos Z = 1,645; 1,96; e 2,575.
- Resultados são calculados com precisão completa em `double`; não há arredondamento intermediário. A tela apresenta quatro casas decimais e, para proporções, também percentuais com duas casas. Valores não nulos que arredondariam para zero são mostrados em notação científica.
- Na proporção, menos de cinco ocorrências ou não ocorrências gera um aviso sobre a aproximação normal. Limites fora de 0%–100% são preservados e sinalizados, nunca truncados. Esses avisos não mudam o método estatístico.
- Esta entrega não inclui tamanho amostral, histórico, distribuição móvel ou métodos além dos apresentados nos materiais de referência.

Fontes exibidas na janela: *Intervalo de Confiança - Média*, página 5; e *Intervalo de Confiança - Proporção*, página 2.

## Teclado e acessibilidade

A interface usa controles nativos Avalonia com nomes acessíveis, foco visível do tema, erros anunciados, layout de uma coluna e rolagem vertical. `Tab` percorre fórmula, confiança, entradas ativas, ações e resultados na ordem visual; `Enter` ativa Calcular, e o seletor aberto mantém sua navegação normal. Após abrir ou limpar, o foco vai para a primeira entrada numérica ativa. Os campos do modo inativo ficam invisíveis e fora da navegação.

Os blocos de interpretação, valores e passos são selecionáveis para cópia: navegue até o bloco, selecione o texto (por exemplo, `Ctrl+A`) e use `Ctrl+C`. IDs de automação estáveis para ensaios: `FormulaSelector`, `ConfidenceSelector`, `MeanInput`, `SampleSizeMeanInput`, `DeviationKindSelector`, `DeviationInput`, `SuccessesInput`, `SampleSizeProportionInput`, `CalculateButton`, `ClearButton`, `IntervalText`, `ResultDetails` e `StepsText`.

## Estado de verificação das plataformas

O código e as publicações podem ser gerados com os comandos acima. A execução real e a aparência no Windows ainda não foram verificadas; a publicação `win-x64` gerada em Linux não comprova execução em Windows.

Verificação local em Linux: 40 testes do Core aprovados; publicações self-contained `linux-x64` e `win-x64` geradas; janela real exercitada sem rede em namespace isolado. Conferidos média com vírgula/ponto, proporções, avisos, desvio amostral em n=30/31, invalidação, limpeza, validação, Enter/Tab, largura mínima 420×540 e fechamento/reabertura. Temas claro e escuro observados (escuro em ensaio descartável usando a mesma janela). A variável `AVALONIA_SCREEN_SCALE_FACTORS=2` não alterou a escala observada neste backend; escala de 200% permanece não verificada.
