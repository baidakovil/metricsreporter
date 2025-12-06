# MetricsReporter – inheritance notes for AI agents

## Что это за проект
- Библиотека (`MetricsReporter`) и dotnet tool (`MetricsReporter.Tool`, команда `metricsreporter`) для агрегации метрик (Roslyn, SARIF, AltCover) и генерации `MetricsReport.g.json` + `MetricsReport.html`.
- Основной потребитель — внешний плагин, но пакет вынесен в отдельный репозиторий и распространяется через локальный NuGet‑фид.

## Структура
- `src/MetricsReporter` — библиотека, PackageId `MetricsReporter`.
- `src/MetricsReporter.Tool` — CLI (PackAsTool), PackageId `MetricsReporter.Tool`, команда `metricsreporter`.
- `tests/MetricsReporter.Tests` — NUnit/FluentAssertions/NSubstitute.
- `docs/` — пользовательские и внутренние документы (developer-notes, metrics-updater).

## Локальный фид / пути
- Локальный фид: `C:\Users\baidakov\.nuget\local-feed`.
- Tool manifest у потребителя (`sample-plugin`): `.config/dotnet-tools.json`.
- Бинарники tool ставятся в кеши:
  - `%USERPROFILE%\.nuget\packages\metricsreporter.tool\<version>\…`
  - `%USERPROFILE%\.dotnet\toolspkgs` и `.dotnet\tools\.store\metricsreporter.tool\<version>\…`

## Как генерировать метрики (пример для consumer-проекта)
```pwsh
dotnet msbuild sample-plugin.sln /t:Build `
  /p:GenerateMetricsDashboard=true `
  /p:RoslynMetricsEnabled=true `
  /p:SarifMetricsEnabled=true `
  /p:MetricsTargetsAnchorProject=Sample.Core.Tests `
  /p:AltCoverEnabled=true `
  /p:MrCoverageEnabled=false
```
Результат: `build/Metrics/Report/MetricsReport.html`, `MetricsReport.g.json`, `build/MetricsTemp/SuppressedSymbols.g.json`.

## Как читать метрики
- Coupling: `dotnet tool run metricsreporter metrics-reader readany --namespace <ns> --metric Coupling --symbol-kind Any`
- Coverage: `dotnet tool run metricsreporter metrics-reader readany --namespace <ns> --metric AltCoverBranchCoverage --group-by type`
- SARIF: `dotnet tool run metricsreporter metrics-reader readsarif --namespace <ns> [--ruleid <CAxxxx|IDExxxx>]`
- Проверка символа: `dotnet tool run metricsreporter metrics-reader test --symbol <FQN> --metric <Metric>`

## MetricsUpdater (внутри MetricsReporter)
- Делает два вызова `dotnet msbuild`:
  1) `CollectCoverage` на runtime‑проекте (если AltCoverEnabled=true).
  2) `GenerateMetricsDashboard` на «якорном» тестовом проекте.
- Якорь выбирается так:
  - Env vars: `METRICS_TARGETS_ANCHOR_PROJECT` или `METRICS_REPORTER_ANCHOR_PROJECT` (`ProjectName` или `ProjectName.csproj`).
  - Далее fallback: `MetricsReporter.Tests.csproj`.
- Ошибка, если ни один вариант не найден.

## Обновление версии и публикация в локальный фид
1) В `C:\Users\baidakov\metricsreporter`:
   - Поднять версию (минимум patch) в `Directory.Build.props`.
   - `dotnet pack src/MetricsReporter/MetricsReporter.csproj -c Release -o C:\Users\baidakov\.nuget\local-feed`
   - `dotnet pack src/MetricsReporter.Tool/MetricsReporter.Tool.csproj -c Release -o C:\Users\baidakov\.nuget\local-feed`
2) В consumer-проекте:
   - `dotnet tool update --tool-manifest .config\dotnet-tools.json --add-source C:\Users\baidakov\.nuget\local-feed MetricsReporter.Tool --version <новая>`
   - (опционально) `dotnet add package MetricsReporter --version <…> --source C:\Users\baidakov\.nuget\local-feed`
   - `dotnet tool restore`, `dotnet restore`, `dotnet build`.

**Почему лучше всегда поднимать версию:** NuGet кеширует пакеты по номеру; без bump придётся вручную чистить `%USERPROFILE%\.nuget\packages\metricsreporter*\<version>` и удалять старые `.nupkg` из фида, плюс `--no-cache` — это легко сломать.

## Работа с исходниками без упаковки
- `dotnet run --project src/MetricsReporter.Tool/MetricsReporter.Tool.csproj -- metrics-reader ...`
- `dotnet tool run ...` использует установленный пакет из кеша, не сборку из текущих исходников.

## Известные предупреждения/риски
- Публичные XML-комментарии не везде дописаны (есть предупреждения CS1591/CS1734/CS1573).
- Зависимость на `Spectre.Console.Cli 1.0.0-alpha.0.7` помечается NU5104 (pre-release). Решение: либо поднять версию пакета на prerelease, либо обновить Spectre на стабильный билд.
- AltCover тесно встроен в пайплайн; для других репозиториев могут потребоваться другие таргеты/сидиректории.

## Идеи для будущего (чистый/SOLID вариант)
- Вынести оркестрацию в интерфейс `IMetricsUpdateOrchestrator`, инжектировать `IProcessRunner`, явные опции вместо env-vars, логирование через `Microsoft.Extensions.Logging`, детализированные результаты/ошибки и repo-agnostic профили по умолчанию.