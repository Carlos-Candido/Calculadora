# Calculadora estatística

Aplicativo desktop em C# com .NET 10 e Avalonia 12.1.4 para calcular intervalos de confiança e tamanhos mínimos de amostra para média e proporção. A interface e os cálculos são locais: depois de restaurar as dependências e obter a distribuição, não há navegador, servidor web, login, banco de dados ou serviço externo durante o uso.

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

A janela usa Avalonia com tema Fluent e acompanha a preferência de tema claro/escuro do sistema. O layout compacto tem visor grande alinhado à direita, seletor de fórmula em largura total, campos em duas colunas e um teclado numérico real de 4×4; teclas arredondadas e o botão azul `=` aproveitam a aparência de calculadora desktop, sem operadores, memória ou funções científicas simuladas. Os controles começam vazios. Durante a edição, o visor indica `ENTRADA` e mostra o campo numérico ativo; após um cálculo válido, indica o modo do resultado e o nível escolhido. O estado vazio usa um traço, nunca um zero fictício. Mudar fórmula, confiança, tipo de desvio ou qualquer entrada remove o resultado anterior. Use vírgula ou ponto como separador decimal, sem agrupamento de milhares. As entradas `n` e `k` aceitam somente inteiros. O teclado insere dígitos na posição do cursor ou substitui a seleção; `⌫` apaga a seleção ou o caractere anterior, `CE` limpa apenas o campo ativo, `±` e `,` só ficam disponíveis em entradas decimais e `C` limpa a calculadora mantendo a fórmula selecionada.

## Publicações portáteis

As pastas de saída são locais, completas e ignoradas pelo Git (`artifacts/`). Para publicar as duas plataformas e registrar o aplicativo no menu Linux, execute a partir da raiz:

```bash
bash scripts/package-desktop.sh
```

O script não recebe argumentos e não usa `sudo`; exige o SDK do .NET 10 (`dotnet`), `zip` e `desktop-file-validate`. Ele gera `artifacts/linux-x64/`, `artifacts/win-x64/` e `artifacts/Calculadora-win-x64.zip`, e atualiza a entrada `calculadora.desktop` em `${XDG_DATA_HOME:-$HOME/.local/share}/applications/` (se `XDG_DATA_HOME` não for absoluto, usa o diretório padrão). Para iniciar no Linux, abra **Calculadora** no menu de aplicativos ou execute `./artifacts/linux-x64/Calculadora.Desktop`.

No Windows x64, extraia o ZIP completo e mantenha a pasta `win-x64/` inteira; não distribua apenas o executável. Abra `Calculadora.Desktop.exe` diretamente ou execute `Criar atalho.cmd` dentro da pasta extraída. O comando cria ou atualiza `Calculadora.lnk` na Área de Trabalho, apontando para o executável na pasta extraída — não para o conteúdo do ZIP — e usando essa pasta como diretório de trabalho. Nenhum instalador ou privilégio de administrador é necessário.

As publicações incluem o runtime .NET, mas não as bibliotecas gráficas do sistema operacional.

### Linux e XWayland

A publicação Linux usa a plataforma X11 do Avalonia. Em sessão Wayland, é necessário XWayland e um `DISPLAY` acessível. O sistema também precisa das bibliotecas nativas de X11, ICE, SM e Fontconfig (em distribuições Debian/Ubuntu, normalmente `libx11-6`, `libice6`, `libsm6` e `libfontconfig1`). A distribuição não instala dependências de sistema. Nenhuma conexão de rede é necessária para realizar cálculos depois da publicação.

## Métodos, precisão e limites

- Média: `Em = Z × desvio / √n` e `IC = x̄ ± Em`. Com desvio populacional, `n` deve ser positivo; com desvio amostral, este método exige `n > 30`. Não é usado t de Student.
- Proporção: `p = k/n`, `Ep = Z × √(p × (1−p)/n)` e `IC = p ± Ep`. `k` representa as ocorrências do evento de interesse informado pelo usuário; não é convertido para o evento complementar.
- Confiança disponível: 90%, 95% e 99%, usando os valores críticos Z = 1,645; 1,96; e 2,575.
- Resultados são calculados com precisão completa em `double`; não há arredondamento intermediário. A tela apresenta quatro casas decimais e, para proporções, também percentuais com duas casas. Valores não nulos que arredondariam para zero são mostrados em notação científica.
- Na proporção, menos de cinco ocorrências ou não ocorrências gera um aviso sobre a aproximação normal. Limites fora de 0%–100% são preservados e sinalizados, nunca truncados. Esses avisos não mudam o método estatístico.
- Tamanho mínimo da amostra para média: `n = (Z × σ / E)²`; para proporção: `n = Z² × p × (1−p) / E²`. O resultado arredonda para cima e tem mínimo de uma observação.
- Na proporção de planejamento, p é opcional: em branco, usa 50% como estimativa conservadora. Digite proporções em porcentagem e margens em pontos percentuais (sem o símbolo `%`).
- Fonte do planejamento: *Tamanho da Amostra — AMS*, páginas 3–7 e 9.

Fontes exibidas na janela: *Intervalo de Confiança - Média*, página 5; e *Intervalo de Confiança - Proporção*, página 2.

## Teclado e acessibilidade

A interface usa controles nativos Avalonia com nomes acessíveis, foco visível do tema, erros anunciados e rolagem vertical. `Tab` percorre fórmula, confiança, campos ativos, teclas da esquerda para a direita e de cima para baixo e, após o cálculo, os detalhes selecionáveis. `Enter` atua sobre o controle focado: em um campo numérico aciona o cálculo padrão (`=`), enquanto em uma tecla numérica ativa essa tecla; o seletor aberto mantém sua navegação normal. O foco inicial, após limpar ou ao trocar de fórmula vai para o primeiro campo numérico ativo. Os campos do modo inativo ficam invisíveis e fora da navegação. Ao mover o foco para uma tecla, o campo de edição ativo e sua seleção permanecem como alvo do teclado.

IDs de automação estáveis para ensaios: `FormulaSelector`, `ConfidenceSelector`, `MeanInput`, `SampleSizeMeanInput`, `DeviationKindSelector`, `DeviationInput`, `SuccessesInput`, `SampleSizeProportionInput`, `PlanningDeviationInput`, `MeanMarginInput`, `ExpectedProportionInput`, `ProportionMarginInput`, `Key7Button` (e `Key0Button`–`Key9Button`), `BackspaceButton`, `ClearEntryButton`, `SignButton`, `DecimalButton`, `ClearButton` (`C`), `CalculateButton` (`=`), `DisplayModeText`, `DisplayContextText`, `DisplayText` (visor), `IntervalText`, `ResultDetails` e `StepsText`.

## Estado de verificação das plataformas

O código e as publicações podem ser gerados com os comandos acima. A execução real e a aparência no Windows ainda não foram verificadas; a publicação `win-x64` gerada em Linux não comprova execução em Windows.

Verificação do teclado em Linux: 13 cenários de interação aprovados em ensaio descartável usando a janela real, cobrindo inserção no cursor, seleção normal/reversa, retorno do foco de edição, exclusão, separador decimal, sinal com espaços externos ou `+`, restrições de inteiros, `CE`, `C`, `=`, invalidação e troca de fórmula. O ensaio foi executado com `dotnet run --project /tmp/calculadora-keypad-smoke/KeypadSmoke.csproj`; não é uma dependência distribuída. A janela também foi observada nos temas claro/escuro, em 560×860 e 420×540, e o exemplo da média foi calculado com cliques nas teclas. As publicações self-contained Linux e Windows foram geradas. Os 40 testes do Core foram aprovados na entrega anterior; as fórmulas não foram alteradas neste redesign. A variável `AVALONIA_SCREEN_SCALE_FACTORS=2` não alterou a escala observada no ensaio anterior; escala de 200% permanece não verificada.
