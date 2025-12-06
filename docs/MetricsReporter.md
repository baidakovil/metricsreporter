## Metrics Reporter Overview

Metrics Reporter — консольное приложение .NET 8, объединяющее метрики покрытия AltCover/OpenCover, кодовые метрики Roslyn и нарушения правил из SARIF в единый файл `MetricsReport.g.json`, после чего генерируется HTML-дашборд.

### Основные артефакты
- `MetricsReport.g.json` — иерархическая модель Solution → Assembly → Namespace → Type → Member с 12 единообразными метриками (используется `metrics-reader` и HTML).
- `MetricsBaseline.g.json` — предыдущий снепшот, служащий источником дельт и пометок `NEW`.
- `MetricsReport.html` — визуализация фактических значений и дельт.
- `SuppressedSymbols.g.json` — плоский список символов, подавленных через `SuppressMessage`, с привязкой к FQN и метрике.

### Источники данных
1. **AltCover/OpenCover**: источники покрытий `AltCoverSequenceCoverage`, `AltCoverBranchCoverage`, `AltCoverCyclomaticComplexity`, `AltCoverNPathComplexity`.  
   Механика парсинга XML, фильтрации технических методов и переноса покрытий между типами/методами описана в `docs/metrics-reporter/coverage-reporting.md`.
   - CLI и MSBuild поддерживают множественные `--altcover` аргументы: достаточно повторить ключ для каждого XML-файла покрытия. Перед мерджем репорт проверяет, что тип/метод встречается максимум в одном AltCover-файле; при пересечении символов выполнение завершается ошибкой парсинга, чтобы исключить некорректное «перетирание» данных.
2. **Roslyn (Microsoft.CodeAnalysis.Metrics)**: `RoslynMaintainabilityIndex`, `RoslynCyclomaticComplexity`, `RoslynClassCoupling`, `RoslynDepthOfInheritance`, `RoslynSourceLines`, `RoslynExecutableLines`. Парсинг разделён на `IRoslynMetricsDocumentLoader` (отвечает за I/O) и `RoslynMetricsDocumentWalker` (разбирает дерево CodeMetricsReport). Walker предоставляет фабрики для сборок, пространств имён, типов и членов, что упростило тестирование и снижает связность основного сервиса.
3. **SARIF (Roslyn анализаторы)**: количество нарушений `SarifCaRuleViolations`, `SarifIdeRuleViolations`. Обработка run-элементов вынесена в `SarifDocumentAggregator`, а построение ParsedCodeElement и breakdown для каждой локации — в `SarifRuleViolationFactory`. Такой подход даёт более предсказуемые единицы повторного использования и облегчает замену источников SARIF.
4. **SuppressMessage-анализ** (опционально): карта подавленных символов, полученная из атрибутов `SuppressMessage` и сопоставленная с Roslyn-метриками (`CA1506 → RoslynClassCoupling`, `CA1502 → RoslynCyclomaticComplexity`, `CA1505 → RoslynMaintainabilityIndex`, `CA1501 → RoslynDepthOfInheritance`). После агрегации дополнительно пробегаемся по дереву метрик и связываем FQN с тем SARIF-идентификатором (`SarifIdeRuleViolations` или `SarifCaRuleViolations`), который уже присутствует на узле, чтобы IDE- и CA-подавления стали частью отчёта. Анализ выполняется только для файлов, расположенных в папках, указанных в `SourceCodeFolders` (например, `["src", "src/Tools", "tests"]`). Имя сборки извлекается из первого сегмента пути после соответствующей папки с исходниками.

### HTML Dashboard Highlights
- **Фильтрация сборок**: при агрегации исключаются сборки, перечисленные в `ExcludedAssemblyNames`; в HTML они не отображаются.
- **Раскрытие узлов**: строки уровней Solution/Assembly/Namespace/Type кликабельны по всей ячейке и по кнопке `+/-`, что ускоряет навигацию.
- **Панель действий**. Все интерактивные элементы — поле фильтра, чекбоксы `new/changes/suppressed`, слайдеры `Detailing` и `Awareness`, кнопки `Expand all` / `Collapse all` — управляются модульным JavaScript-движком:
  - `Detailing (Namespace → Type → Member)` переключает максимальную глубину отображения, используя кэшированное дерево DOM и атрибуты `data-hiddenByDetail`. Узлы не пересоздаются.
  - `Awareness (All → Warning → Error)` показывает строки с предупреждениями/ошибками и автоматически оставляет видимыми их предков. Слайдер снапится к ближайшему значению, события дебаунсятся, чтобы устранить «дрожание».
  - Все пользовательские предпочтения (детализация, awareness, фильтр, чекбоксы) сохраняются в `localStorage` per-path и восстанавливаются при открытии отчёта.
- **Статистика под спойлером**: в блоке `meta-details` отображаются подсчитанные количества символов (total/no metric/clear/warning/error) и проценты с дельтами относительно baseline.

### Nested Type Semantics
- Вложенные типы нормализуются к dot-FQN и привязываются к реальным namespace; AltCover‑нотация `Outer/Nested` и `Outer+Inner` не просачивается в итоговый DTO.  
- Перенос покрытия между `Outer+Inner` и dot‑типами, а также взаимодействие с iterator state machine описаны в `docs/metrics-reporter/nested-types.md` и `docs/metrics-reporter/compiler-classes-handling.md`.

### JSON-структура (сокращённо)
```json
{
  "metadata": {
    "generatedAtUtc": "2025-11-25T21:33:10Z",
    "paths": {
      "metricsDirectory": "build/Metrics",
      "baseline": "build/MetricsTemp/MetricsBaseline.g.json",
      "report": "build/Metrics/Report/MetricsReport.g.json",
      "html": "build/Metrics/Report/MetricsReport.html",
      "thresholds": "build/MetricsRules/MetricsReporterThresholds.json"
    },
    "thresholdsByLevel": {
      "RoslynClassCoupling": {
        "Type": { "warning": 40, "error": 70, "higherIsBetter": false }
      }
    },
    "metricDescriptors": {
      "AltCoverSequenceCoverage": { "unit": "percent" },
      "RoslynMaintainabilityIndex": { "unit": "score" },
      "SarifCaRuleViolations": { "unit": "count" }
    },
    "excludedMemberNamesPatterns": "*b__*,<Clone>$,ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync"
  },
  "solution": {
    "name": "sample-plugin",
    "assemblies": [
      {
        "name": "Sample.Loader",
        "kind": "Assembly",
        "fullyQualifiedName": "Sample.Loader",
        "metrics": {
          "AltCoverSequenceCoverage": { "value": 48.59, "delta": -1.41, "status": "Warning" },
          "RoslynMaintainabilityIndex": { "value": 81, "delta": 0, "status": "Success" },
          "SarifCaRuleViolations": { "value": 12, "delta": 2, "status": "Warning" }
        },
        "namespaces": [
          {
            "name": "Sample.Loader.Infrastructure",
            "kind": "Namespace",
            "metrics": {},
            "types": [
              {
                "name": "CommandValidationService",
                "kind": "Type",
                "fullyQualifiedName": "Sample.Loader.Infrastructure.CommandValidationService",
                "source": {
                  "path": "src/Sample.Loader/Infrastructure/CommandValidationService.cs",
                  "startLine": 12,
                  "endLine": 200
                },
                "isNew": false,
                "metrics": {
                  "RoslynCyclomaticComplexity": { "value": 48, "delta": -5, "status": "Warning" },
                  "SarifCaRuleViolations": { "value": 3, "delta": 1, "status": "Error" }
                },
                "members": [
                  {
                    "name": "ValidateAsync",
                    "kind": "Member",
                    "fullyQualifiedName": "Sample.Loader.Infrastructure.CommandValidationService.ValidateAsync(System.String)",
                    "isNew": true,
                    "metrics": {
                      "AltCoverSequenceCoverage": { "value": 72.1, "status": "Warning" },
                      "RoslynMaintainabilityIndex": { "value": 42, "status": "Error" }
                    }
                  }
                ]
              }
            ]
          }
        ]
      }
    ]
  }
}
```

### Семантика ключевых полей
- `kind` — тип узла (`Solution`, `Assembly`, `Namespace`, `Type`, `Member`), требуется для HTML-дрила.
- `fullyQualifiedName` — FQN в формате `Namespace.Type.Member(args)`, пусто для Solution/Namespace когда неприменимо.
- `metrics` — словарь по метрике (строковый идентификатор `MetricIdentifier`) и объекту значения:
  - `value` (`number?`) — фактическое значение, `null`, если данных нет.
  - `delta` (`number?`, опционально) — отклонение от baseline. Поле отсутствует, если дельта равна нулю или baseline не найден; потребители трактуют отсутствие как `0`.
  - `status` — результат сравнения с порогом (значения: `Success`, `Warning`, `Error`). Метрики без данных не сериализуются, поэтому вариант `NotApplicable` больше не попадает в JSON.
- `isNew` — пометка новых элементов, для HTML добавляется префикс `NEW`; дельты не отображаются.
- `source` — сведения о файле/диапазоне строк, используются для сопоставления SARIF и подсказок в отчёте.

Если ключ отсутствует в `metrics`, значит метрика неприменима к данному символу или недоступна в исходных данных. Метрики с фактическими значениями, но без настроенных порогов, по умолчанию получают статус `Success`, чтобы значение сохранилось в отчёте без визуального подсвечивания.

Краткая семантика ключевых полей и общих правил сериализации приведена выше; подробная логика построения метрик (baseline, пороги, правила для отсутствующих значений и branch‑coverage) вынесена в `docs/metrics-reporter/coverage-reporting.md`.

### JSON Schema
- Файл `src/MetricsReporter/Model/metrics-report.schema.json` описывает структуру `MetricsReport.g.json` (metadata, пороги, дерево Solution → Member). Любые изменения схемы синхронизируются с этим файлом.
- DTO сериализуются через `System.Text.Json` (camelCase).

### Автоматическая генерация через MSBuild
- В `build/Targets/code-metrics.targets` добавлен таргет `GenerateMetricsDashboard`, который срабатывает после сборки проекта `MetricsReporter.Tests`.
- Таргет:
  - Строит набор проектов и тестов, чтобы гарантировать актуальные метрики (через `MSBuild` по списку зависимостей).
  - Формирует аргументы (AltCover, все Roslyn XML, SARIF, baseline, пороги) с правильными разделителями.
  - Вызывает `MetricsReporter.exe` из `src/MetricsReporter/bin/<Configuration>/net8.0`.
  - Создает каталог отчётов (`$(MetricsDir)\Report`) и записывает JSON/HTML + лог.
- Благодаря этому, после стандартного `dotnet build --no-incremental` в `build/Metrics/Report` автоматически появляются `MetricsReport.g.json` и `MetricsReport.html`.
- **Настройка анализа suppressed-символов**: В `build/Props/code-metrics.props` можно настроить:
  - `AnalyzeSuppressedSymbols` — включить/выключить анализ (по умолчанию `true`).
  - `SourceCodeFolders` — список папок с исходниками через запятую (по умолчанию `"src,src/Tools,tests"`). Пути указываются относительно корня решения (`SolutionDir`). Анализатор сканирует только файлы в этих папках и определяет имя сборки по первому сегменту пути после соответствующей папки.

### Автоматическое управление Baseline

Система автоматического управления baseline создает `MetricsBaseline.g.json` из предыдущего отчета перед генерацией нового отчета, обеспечивая автоматический расчет дельт между запусками.

#### Настройка

В `build/Props/code-metrics.props` установите:
```xml
<ReplaceMetricsBaseline>true</ReplaceMetricsBaseline>
```

#### Логика работы

1. **Создание baseline из предыдущего отчета (если baseline не существует)**: 
   - Если `ReplaceMetricsBaseline=true`, путь к baseline задан, но baseline не существует, система проверяет наличие предыдущего `MetricsReport.g.json`.
   - Если предыдущий отчет существует, он копируется в `MetricsBaseline.g.json` **ДО генерации нового отчета**.
   - Это позволяет новому отчету сразу генерироваться с дельтами, рассчитанными относительно предыдущего отчета.

2. **Генерация нового отчета**: Создается новый `MetricsReport.g.json` с текущими метриками на основе baseline (если он существует или был создан на шаге 1).

3. **Архивация и замена baseline**: После генерации нового отчета, если `ReplaceMetricsBaseline=true`:
   - Если старый baseline существует, он архивируется в директорию хранения (`MetricsReportStoragePath`, по умолчанию `C:\Users\<username>\AppData\Local\RCA\Metrics`) с добавлением timestamp к имени файла (формат: `MetricsBaseline-YYYYMMDD-HHMMSS.g.json`).
   - Новый `MetricsReport.g.json` копируется в `MetricsBaseline.g.json`, подготавливая baseline для следующего цикла генерации.

4. **Завершение**: После замены baseline процесс завершается. При следующем запуске baseline будет создан из этого отчета (шаг 1), а текущий baseline будет заархивирован (шаг 3).

#### Важные детали

- **Порядок операций**: Baseline создается из предыдущего отчета **ДО** генерации нового отчета, чтобы новый отчет сразу строился на основе предыдущего.
- **Без сравнений**: Система не сравнивает содержимое файлов. Если предыдущий отчет существует и baseline отсутствует, он становится baseline.
- **Автоматическое создание**: При каждом запуске, если baseline не существует, он автоматически создается из предыдущего отчета. Это означает, что baseline всегда актуален и соответствует последнему сгенерированному отчету.
- **Условие создания baseline из предыдущего отчета**: 
  - `ReplaceMetricsBaseline=true`
  - Путь к baseline задан (не null и не пустой)
  - Baseline не существует
  - Предыдущий `MetricsReport.g.json` существует
- **Путь к baseline**: Должен быть задан через параметр `--baseline` или MSBuild property `MetricsBaselineJson`. MSBuild target автоматически передает путь к baseline в `MetricsReporter` когда `ReplaceMetricsBaseline=true`, даже если файл еще не существует.

#### Пример использования

```bash
# Включить автоматическое управление baseline
dotnet msbuild rca-plugin.sln /t:Build /p:ReplaceMetricsBaseline=true
```

### Дополнительно
- **Автоматическое управление baseline**: При включенной опции `ReplaceMetricsBaseline=true` baseline автоматически создается из предыдущего отчета перед генерацией нового отчета. Подробности см. в разделе "Автоматическое управление Baseline" выше.
- Приложение логирует шаги в `$(MetricsDir)\Report\MetricsReporter.log` и возвращает коды: 0 — OK, 1 — parsing error, 2 — IO error, 3 — validation error.
- Пороговые значения хранятся в `build/MetricsRules/MetricsReporterThresholds.json`; путь до файла конфигурируется через `build/Props/paths.props` (свойство `MetricsThresholdsPath`) и передается агрегатору.

### HTML Dashboard UI и производительность
- **Модульный JavaScript (ES5-compatible)**. Сгенерированный `HtmlScriptGenerator` теперь собирает скрипт из независимых модулей (`Utilities`, `Tooltips`, `StateManagement`, `Filtering`, `Sorting`, `Actions`, `Hotkeys`, `Bootstrap`). Каждый модуль отвечает за строго определённую область, что упрощает сопровождение и тестирование.
- **StateCache**. При инициализации строится кэш `rowId → element` и `parentId → children`. Все операции (фильтрация, сортировка, раскрытие) работают с этим кэшем и только в конце обновляют DOM батчами.
- **Без повторных обходов DOM**. Метки `data-hiddenBy*` (`detail`, `filter`, `awareness`, `state`) используются для комбинирования фильтров. Изменение любого фильтра не пересоздаёт строки — метод `updateVisibility` всего лишь переключает `display` и пересчитывает полосатость видимых листовых строк.
- **Debounce/Throttle**. Ввод фильтра обрабатывается спустя 200 мс без повторной обработки, awareness-slider — через короткий таймер (120 мс) и отдельное событие `pointerup`, что предотвращает визуальные скачки.
- **Tooltips**. Все виды подсказок (threshold, suppression, symbol, simple) используют один `tooltipHost`. Позиционирование происходит только при `scroll/resize`, таймеры едины, что устраняет множественные `setTimeout`.
- **Копирование и переходы**. Кнопки `Open`, `Copy`, `Filter` используют современный Clipboard API с фоллбеком и `cursor://` протокол (поддерживает Cursor IDE). При ошибках выводится `console.warn`, чтобы не блокировать UI.
- **Leaf-обработка**. Узлы `Namespace/Type` без видимых дочерних элементов автоматически считаются `leaf-row`: скрываем `+/-`, показываем `symbol-indicator` (`∅`), но стилевое оформление остаётся структурным — пользователь видит, что это уровень Namespace/Type, даже когда Awareness скрывает детей.
- **Tooltips → performance**. При наведении на метрики/символы используется делегирование событий на `tbody`, подсказки строятся на лету из кэша JSON и переиспользуют один DOM-элемент. Это устраняет десятки слушателей на каждую строку.
- **Persisted layout**. Sticky-header сдвигается на высоту панели действий, поэтому скролл остаётся плавным даже при большом количестве колонок. Высота вычисляется один раз и обновляется только по событию `resize`.
### HTML Dashboard UI
- Панель действий содержит кнопки `Expand all` / `Collapse all` и компактный слайдер **Detailing**. Диапазон `[Namespace → Type → Member]` управляет максимальной глубиной видимых строк — дерево не перестраивается, а существующие строки Solution → Member повторно используются и скрываются через `data-hiddenByDetail`, поэтому дублирования метрик или символов в DOM нет.
- Слайдер снапится к ближайшему уровню при клике по треку и использует кэшированную иерархию строк в JavaScript. Это обеспечивает быстрые переключения без повторных обходов DOM.
- Узлы `Namespace` и `Type` без дочерних элементов отображаются с тем же форматированием, что и структурные узлы с детьми, но вместо кнопки раскрытия показывают серый символ `∅`, иллюстрирующий отсутствие дочерних элементов.
- Скрипт `HtmlScriptGenerator` кэширует сопоставление `parent → children`, вычисляет полосатость видимых листовых строк и состояние экспандеров в одном проходе, придерживаясь принципов SOLID: отдельные функции отвечают за детализацию, визуальное оформление и манипуляцию состояниями.
- **MetricsReporter backend**:
  - _Конвейер MSBuild_. Таргет `GenerateMetricsDashboard` автоматически собирает все проекты, запускает тесты и генерирует метрики в рамках `dotnet build --no-incremental`. AltCover, Roslyn Metrics и SARIF значения собираются параллельно, а `MetricsReporter.exe` лишь агрегирует уже готовые артефакты.
  - _Baseline management_. Включён режим автоматической ротации baseline (`ReplaceMetricsBaseline=true`): перед запуском создаётся baseline из предыдущего отчёта, после генерации свежий отчёт сохраняется и копируется в baseline. Это исключает ручные шаги и гарантирует валидные дельты.
  - _SuppressMessage analyzer_. Обходит только каталоги из `SourceCodeFolders`, строит индекс FQN → атрибуты и сопоставляет с Roslyn метриками: дорогие операции ограничены подмножеством файлов и выполняются один раз за прогон.
  - _Парсеры метрик_. Каждый источник метрик обслуживают небольшие компоненты: Roslyn использует `IRoslynMetricsDocumentLoader` + `RoslynMetricsDocumentWalker`, AltCover — `CreateClassNode` и `AltCoverMethodNodeFactory`, SARIF — `SarifDocumentAggregator` + `SarifRuleViolationFactory`. Это снижает связанность orchestration-кода (`xxxMetricsParser`) и позволяет подменять зависимости (например, моки загрузчиков) без переписывания остального пайплайна.
  - _Архитектура сервисов с интерфейсами и DTO_. Все основные сервисы (`MetricsReportPipeline`, `BaselineLifecycleService`, `SuppressedSymbolsService`, `BaselineManager`) реализуют интерфейсы (`IMetricsReportPipeline`, `IBaselineLifecycleService`, `ISuppressedSymbolsService`, `IBaselineManager`), что обеспечивает низкую связанность и улучшает тестируемость. Параметры методов группируются в DTO классы (`ReportGenerationContext`, `PipelineExecutionContext`, `ParsedDocumentsResult`, `BaselineCreationParameters`, `BaselineReplacementParameters`) для снижения coupling метрик и улучшения читаемости кода. Логирование абстрагировано через `ILogger` интерфейс.
  - _Память и сериализация_. Все DTO сериализуются `System.Text.Json` с включённым `Pooling`, JSON собирается без промежуточных аллокаций (используем `Utf8JsonWriter`). Это позволяет генерировать отчёт (>50k узлов) за единицы секунд с минимальной нагрузкой на GC.
  - _Модульная архитектура HTML-генерации_. Код генерации HTML-таблицы (`MetricsReporter.Rendering`) рефакторен для снижения coupling и улучшения поддерживаемости. Основной класс `HtmlTableGenerator` делегирует ответственность специализированным классам-помощникам: `SuppressionIndexBuilder`, `DescendantCountIndexBuilder`, `RowStateCalculator`, `RowAttributeBuilder`, `SuppressionAttributeBuilder`, `BreakdownAttributeBuilder`, `SymbolTooltipBuilder`, `MetricCellRenderer`, `MetricCellAttributeBuilder`, `TableStructureBuilder`, `TableRendererInitializer`, `TableContentBuilder`, `NodeChildrenRenderer`. Каждый класс отвечает за строго определённую область, что упрощает тестирование и сопровождение кода.

### Запуск из командной строки
Инструмент можно запустить вручную:

```bash
dotnet run --project src/MetricsReporter/MetricsReporter.csproj -- \
  --solution-name "sample-plugin" \
  --metrics-dir "build/Metrics" \
  --altcover "build/Metrics/AltCover/coverage-runtime.xml" \
  --altcover "build/Metrics/AltCover/coverage-metrics-reporter.xml" \
  --roslyn "build/Metrics/Roslyn/Sample.Loader.xml" \
  --sarif "build/Metrics/Sarif/Sample.Loader.sarif" \
  --baseline "build/MetricsTemp/MetricsBaseline.g.json" \
  --output-json "build/Metrics/Report/MetricsReport.g.json" \
  --output-html "build/Metrics/Report/MetricsReport.html" \
  --thresholds "{'AltCoverSequenceCoverage':{'warning':75,'error':60,'higherIsBetter':true}}"
```

Параметры `--roslyn` и `--sarif` допускают множественные значения; `--thresholds` принимает JSON-строку (символ `'` автоматически заменяется на `"`).

Информация о CLI и MSBuild обновляется по мере развития инструмента.

## Metrics Reader Helper

CLI для чтения готового отчёта (`metrics-reader`) описан в отдельном документе [`docs/metrics-reporter/metrics-reader.md`](./metrics-reporter/metrics-reader.md). Там перечислены все параметры, форматы ответов (включая новые группировки `--group-by`) и примеры вызовов `readany`, `readsarif`, `test`. Ниже приведён только базовый пример запуска.

```powershell
dotnet run --project src/MetricsReporter/MetricsReporter.csproj -- `
  metrics-reader readany --namespace Sample.Loader --metric Complexity --all
```

```powershell
.\src\MetricsReporter\bin\Debug\net8.0\MetricsReporter.exe `
  metrics-reader readsarif --namespace Sample.Loader.Infrastructure --metric SarifCaRuleViolations
```

## Symbol Normalization

Metrics Reporter объединяет метрики из разных источников (AltCover, Roslyn, SARIF), которые описывают одни и те же символы (классы, методы) в разных форматах. Для корректного объединения метрик символы нормализуются к единому формату.

### Проблема несовместимости форматов

Разные инструменты используют разные форматы для описания символов:

- **AltCover**: `System.Void Sample.Loader.LoaderApp::OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)`
  - Использует полностью квалифицированные имена типов параметров
  - Использует C++-стиль разделителя `::` для пространств имен
  - Всегда включает return type в начале

- **Roslyn**: `void OnApplicationIdling(object? sender, IdlingEventArgs e)`
  - Использует короткие имена типов (без namespace)
  - Использует nullable annotations (`?`)
  - Может не включать полный путь к типу в имени метода

Без нормализации один и тот же метод из разных источников рассматривается как два разных символа, что приводит к дублированию записей в отчете.

### Нормализованный формат

Все символы нормализуются к единому формату:

- **Типы**: `Namespace.Type` (generic параметры удаляются, например `List<string>` → `List`)
- **Методы**: `Namespace.Type.Method(...)` (параметры заменяются на `...`, generic параметры метода удаляются)
- **Конструкторы**: `Namespace.Type..ctor(...)` (статический конструктор: `.cctor(...)`)
- **Операторы**: `Namespace.Type.op_Equality(...)`, `Namespace.Type.op_Inequality(...)`
- **Специальные методы**: `Namespace.Type.<Clone>$(...)`, `Namespace.Type.ToString(...)`

### Процесс нормализации

#### 1. Извлечение имени метода (`ExtractMethodName`)

Извлекает только имя метода без параметров, return type и generic параметров:

- Удаляет return type (например, `void Method(...)` → `Method`)
- Удаляет generic параметры метода (например, `Method<T>(...)` → `Method`)
- Извлекает имя после последней точки (например, `Namespace.Type.Method` → `Method`)
- Сохраняет ведущую точку для конструкторов (`.ctor`, `.cctor`)
- Сохраняет специальные символы в имени (например, `<Clone>$`)

**Примеры:**
- `System.String Sample.Logging.Contracts.LogEntryDto::ToString()` → `ToString`
- `System.Void Sample.UI.Services.ServiceResolver::.ctor(...)` → `.ctor`
- `Sample.Logging.Contracts.LogEntryDto Sample.Logging.Contracts.LogEntryDto::<Clone>$()` → `<Clone>$`
- `TInterface IServiceResolver.Resolve<TInterface>()` → `Resolve`

#### 2. Нормализация сигнатуры метода (`NormalizeMethodSignature`)

Заменяет параметры на placeholder `(...)`:

- Находит открывающую скобку параметров `(`
- Находит соответствующую закрывающую скобку `)`, обрабатывая вложенные скобки в generic типах
- Заменяет все содержимое между скобками на `...`

**Примеры:**
- `Method(System.Object, System.String)` → `Method(...)`
- `Method(object? sender, string name)` → `Method(...)`
- `Method(System.Collections.Generic.List<System.String>)` → `Method(...)`

#### 3. Нормализация FQN метода (`NormalizeFullyQualifiedMethodName`)

Применяет нормализацию сигнатуры и удаляет generic параметры метода:

- Удаляет generic параметры метода (например, `Process<T>` → `Process`)
- Отличает generic параметры от части имени метода (например, `<Clone>$` не является generic параметром)
- Применяет нормализацию сигнатуры для замены параметров

**Примеры:**
- `IServiceRegistrar.Register<TInterface>(TInterface implementation)` → `IServiceRegistrar.Register(...)`
- `UiPipeLogger.Log<TState>(LogLevel logLevel, ...)` → `UiPipeLogger.Log(...)`
- `LogEntryDto.<Clone>$()` → `LogEntryDto.<Clone>$(...)`

#### 4. Нормализация имени типа (`NormalizeTypeName`)

Удаляет generic параметры из имени типа:

- Находит первую открывающую угловую скобку `<`
- Удаляет все до конца типа (включая вложенные generic параметры)

**Примеры:**
- `List<string>` → `List`
- `Dictionary<string, int>` → `Dictionary`
- `List<Dictionary<string, int>>` → `List`

### Обработка edge cases

#### Generic параметры

Методы с generic параметрами нормализуются одинаково независимо от источника:
- AltCover: `Register(TInterface)` → `Register(...)`
- Roslyn: `Register<TInterface>(TInterface implementation)` → `Register(...)`

#### Конструкторы

Конструкторы идентифицируются по паттерну `TypeName.TypeName(...)`. Имя метода для конструкторов извлекается как имя типа:
- AltCover: `System.Void ServiceResolver::.ctor(...)` → `ServiceResolver..ctor(...)`
- Roslyn: `ServiceResolver.ServiceResolver(...)` → `ServiceResolver.ServiceResolver(...)`

#### Операторы и специальные методы

Операторы и методы компилятора сохраняют специальные имена:
- `op_Equality`, `op_Inequality` → `Namespace.Type.op_Equality(...)`
- `<Clone>$` (для record типов) → `Namespace.Type.<Clone>$(...)`

#### Сложные возвращаемые типы

Методы с tuple или generic возвращаемыми типами обрабатываются корректно:
- `Task<string> ExecuteAsync(...)` → `ExecuteAsync(...)`
- `Task<(bool, string?)> LoadRuntimeAsync(...)` → `LoadRuntimeAsync(...)`

#### Nested типы

Вложенные типы используют разделитель `+`: AltCover `Outer/Nested` → `Outer+Nested`

### Результат нормализации

После нормализации методы из разных источников с одинаковой сигнатурой объединяются в одну запись в отчете:

**До нормализации:**
- AltCover: `Sample.Loader.LoaderApp.OnApplicationIdling(System.Object, Autodesk.Revit.UI.Events.IdlingEventArgs)`
- Roslyn: `Sample.Loader.LoaderApp.OnApplicationIdling(object? sender, IdlingEventArgs e)`
- Результат: Две отдельные записи в отчете

**После нормализации:**
- AltCover: `Sample.Loader.LoaderApp.OnApplicationIdling(...)`
- Roslyn: `Sample.Loader.LoaderApp.OnApplicationIdling(...)`
- Результат: Одна запись с метриками из обоих источников

### Реализация

Нормализация реализована в классе `SymbolNormalizer` (`src/MetricsReporter/Processing/SymbolNormalizer.cs`):

- `NormalizeMethodSignature(string?)` — нормализует сигнатуру метода
- `ExtractMethodName(string?)` — извлекает имя метода
- `NormalizeFullyQualifiedMethodName(string?)` — нормализует FQN метода
- `NormalizeTypeName(string?)` — нормализует имя типа

Парсеры (`AltCoverMetricsParser`, `RoslynMetricsParser`) используют `SymbolNormalizer` для нормализации символов перед агрегацией.

Все edge cases покрыты unit-тестами в `tests/MetricsReporter.Tests/Processing/SymbolNormalizerTests.cs` с использованием реальных примеров из метрик проекта.

## Member Filtering

Metrics Reporter автоматически исключает методы конструктора и компилятора из отчётов, так как они не представляют интереса для анализа качества кода.

### Конфигурация и значения по умолчанию

- Свойство MSBuild `ExcludedMemberNamesPatterns` (см. `build/Props/code-metrics.props`) задаёт шаблоны для исключения. Формат — список через запятую/точку с запятой, поддерживаются `*` и `?`. По умолчанию: `*b__*,<Clone>$,ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync`.
- CLI `MetricsReporter.exe` получает этот список через аргумент `--excluded-members` (см. `build/Targets/code-metrics.targets`).
- Фактический набор сохраняется в `ReportMetadata.excludedMemberNamesPatterns` и отображается в шапке HTML, чтобы было видно, какие шаблоны использовались.

### Механизм работы

Фильтрация происходит в `MetricsAggregationService.MergeMember()` после нормализации FQN. Конструкторы AltCover (`Namespace.Type..ctor(...)`) и Roslyn (`Namespace.Type.Type(...)`) определяются автоматически, остальные методы сравниваются с шаблонами `MemberFilter`. Исключённые методы не попадают в `MetricsReport.g.json`/HTML.

### Реализация

`MemberFilter` (`src/MetricsReporter/Processing/MemberFilter.cs`): `ShouldExcludeMethod(string?)` — проверка по имени, `ShouldExcludeMethodByFqn(string?)` — проверка по FQN. Тесты можно найти в `MemberFilterTests.cs` и `MetricsAggregationServiceTests.cs`.
