## Metrics Reader CLI

`metrics-reader` — часть `MetricsReporter.exe`, построенная на `Spectre.Console.Cli`. Команды читают уже сгенерированный `MetricsReport.g.json`, поэтому перед запуском либо выполните `dotnet build --no-incremental`, либо передайте `--no-update`, если метрики уже актуальны. Команды регистрируются в `MetricsReaderCommandConfigurator` и работают поверх `MetricsReaderEngine`, который оперирует готовой моделью репорта.

**Обновление метрик и сбор покрытия**: При запуске команд без флага `--no-update`, `metrics-reader` автоматически:
1. Запускает таргет `GenerateMetricsDashboard` для обновления метрик
2. Запускает таргет `CollectCoverage` для сбора покрытия (только если `AltCoverEnabled=true` в `code-metrics.props`)

Сбор покрытия контролируется свойством `AltCoverEnabled` в `build/Props/code-metrics.props` — если оно установлено в `false`, сбор покрытия автоматически пропускается.

### Общие параметры

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `--report <PATH>` | `build/Metrics/Report/MetricsReport.g.json` | Источник данных. |
| `--thresholds-file <PATH>` | Пороги из отчёта | Временная замена `ReportMetadata.thresholdsByLevel`; подхватывается `IMetricsThresholdProvider`. |
| `--include-suppressed` | `false` | Показывает символы, отмеченные `SuppressMessage` (см. `SuppressedSymbolsService`). |
| `--no-update` | `false` | Пропускает таргет `GenerateMetricsDashboard` и сбор покрытия (CollectCoverage). При использовании этого флага метрики и покрытие не обновляются, используется существующий `MetricsReport.g.json`. |
| `--group-by <metric/namespace/type/method/ruleId>` | `None` для `readany`, `ruleId` для `readsarif` | Определяет структуру вывода (см. ниже). |

### Формат сгруппированного ответа

- Включается автоматически при использовании `--group-by`.
- Верхний уровень:  
  ```json
  {
    "metric": "<input metric or Any>",
    "namespace": "<filter>",
    "symbolKind": "Any|Type|Member",
    "includeSuppressed": true,
    "groupBy": "type",
    "violationsGroupsCount": 25,
    "violationsGroups": [ … ],
    "ruleId": "CA1506" // только для readsarif, если был фильтр
  }
  ```
- Каждый элемент `violationsGroups` имеет ключ, соответствующий измерению (`metric`, `namespace`, `type`, `method`, `ruleId`), поле `violationsCount` и массив `violations`.  
- Если флаг `--all` не задан, `violationsGroups` содержит только первую группу по текущему порядку сортировки, но `violationsGroupsCount` показывает реальное число групп, найденных в отчёте.

### Команда `readany`

```
metrics-reader readany --namespace <NamespacePrefix> --metric <MetricAlias>
                       [--symbol-kind <Any|Type|Member>] [--all] [--group-by ...] [общие параметры]
```

- Параметры валидации реализованы в `NamespaceMetricSettings`. `MetricIdentifierResolver` принимает полные идентификаторы и алиасы (`Complexity`, `Maintainability`, `Coupling`, `Coverage`, `Inheritance`, …).
- Выполнение (`ReadAnyCommandExecutor`):
  1. `SymbolQueryService` строит `SymbolFilter` и запрашивает `MetricsReaderEngine.GetProblematicSymbols`.
  2. `SymbolSnapshotOrderer` сортирует результаты (типы перед членами, затем `Error`/`Warning`, далее по величине превышения порога и FQN).
  3. `ReadAnyCommandResultHandler`:
     - Без `--group-by`: выводит массив (`--all`) или топ-1 элемент.
     - С `--group-by`: использует `SymbolMetadataParser` для извлечения namespace/type/method и формирует групповой DTO (`GroupedViolationsResponseDto`).
- Удаление suppressed-символов контролируется `--include-suppressed`; фильтрация происходит на уровне `SymbolQueryService`.
- Пример (группировка по типам):
  ```powershell
  metrics-reader readany --namespace Sample.Loader.Services `
                         --metric Complexity `
                         --group-by type `
                         --all
  ```
  Возвращает объект, где каждая группа содержит `type`, `violationsCount` и массив `violations` с исходными полями (`symbolFqn`, `symbolType`, `value`, `threshold`, `delta`, `filePath`, `status`, `isSuppressed`).

### Команда `readsarif`

```
metrics-reader readsarif --namespace <NamespacePrefix>
                         [--metric <SarifCaRuleViolations|SarifIdeRuleViolations|Any>]
                         [--symbol-kind <Any|Type|Member>]
                         [--all] [--ruleid <CAxxxx|IDExxxx>] [--group-by ...]
                         [общие параметры]
```

- `SarifMetricSettings` разрешает либо `SarifCaRuleViolations`, либо `SarifIdeRuleViolations`, либо `Any` (оба сразу). По умолчанию `GroupBy` = `ruleId`. Параметр `ruleId` фильтрует уже агрегированные группы.
- Выполнение (`ReadSarifCommandExecutor`):
  1. `SarifGroupAggregator` строит `SarifViolationGroupBuilder` на основе `MetricValue.Breakdown`, дополняя `SarifSymbolContribution` для каждого символа (учитываются suppression-флаги).
  2. `SarifGroupSorter` сортирует по количеству нарушений (DESC), затем по `ruleId` (case-insensitive).
  3. `SarifGroupFilter` применяет `ruleId` фильтр.
  4. `ReadSarifCommandResultHandler` формирует DTO:
     - `groupBy ruleId` (режим по умолчанию): каждая группа содержит `ruleId`, `shortDescription`, `violationsCount`, `violations` (символ, сообщение, URI, диапазон строк). Поле `violationsCount` заменяет прежнее `count`.
     - `groupBy metric|namespace|type|method`: builder-ы агрегируют взносы `SarifSymbolContribution`. Во вложенных группах сохраняются списки `violations` (первая группа ссылается сразу на все конкретные записи).
- Пример (агрегация по namespace):
  ```powershell
  metrics-reader readsarif --namespace Sample.Loader `
                           --group-by namespace `
                           --all `
                           --include-suppressed
  ```
  Ответ содержит `violationsGroups` с ключом `namespace`, `violationsCount` (объединённые CA/IDE метрики) и массивом `violations`.
- Ошибки:
  - Неверная метрика (`readsarif --metric Complexity`) → `SarifInvalidMetricDto`.
  - Отсутствие совпадений (с учётом `ruleId`/namespace/symbolKind) → `SarifNoViolationsFoundDto` с пояснением.

### Команда `test`

```
metrics-reader test --symbol <FullyQualifiedName> --metric <MetricAlias>
                    [--include-suppressed] [--report ...] [--thresholds-file ...] [--no-update]
```

- Принимает точный FQN с постфиксом `(...)` для методов (см. `SymbolNormalizer`).  
- Возвращает `{ "isOk": bool, "details": SymbolMetricDto, "message": "..." }`. `isOk` сравнивает `MetricValue.Status` с порогами (используются актуальные или переопределённые thresholds).
- Удобно для пайплайнов, где нужно быстро проверить один символ после рефакторинга.

### Групповые примеры

#### readany + `--group-by type`

```json
{
  "metric": "RoslynCyclomaticComplexity",
  "namespace": "Sample.Loader.Services",
  "symbolKind": "Any",
  "includeSuppressed": false,
  "groupBy": "type",
  "violationsGroupsCount": 2,
  "violationsGroups": [
    {
      "type": "Sample.Loader.Services.CommandValidationService",
      "violationsCount": 3,
      "violations": [
        {
          "symbolFqn": "Sample.Loader.Services.CommandValidationService.ValidateAsync(System.String)",
          "status": "Error",
          "value": 48,
          "threshold": 35,
          "thresholdKind": "Error",
          "delta": 6,
          "filePath": "src/Sample.Loader/Infrastructure/CommandValidationService.cs",
          "isSuppressed": false
        }
      ]
    }
  ]
}
```

#### readsarif + `--group-by namespace`

```json
{
  "metric": "Any",
  "namespace": "Sample.Loader",
  "symbolKind": "Any",
  "includeSuppressed": true,
  "groupBy": "namespace",
  "violationsGroupsCount": 2,
  "violationsGroups": [
    {
      "namespace": "Sample.Loader.Core",
      "violationsCount": 5,
      "violations": [
        {
          "symbol": "Sample.Loader.Core.TypeA.Process(...)",
          "message": "Avoid excessive complexity (CA1502)",
          "uri": "file:///src/Sample.Loader.Core/TypeA.cs",
          "startLine": 40,
          "endLine": 55
        }
      ]
    }
  ]
}
```

### Реализационные детали

- **Сервисная композиция**:
  - `ReadAnyCommand` → `SymbolQueryService`, `SymbolSnapshotOrderer`, `ReadAnyCommandResultHandler`.
  - `ReadSarifCommand` → `SarifGroupAggregator`, `SarifGroupSorter`, `SarifGroupFilter`, `ReadSarifCommandResultHandler`.
  - `ReadTestCommand` (не изменялся) использует `MetricsReaderEngine.TryGetSymbol`.
- **Парсинг символов**: `SymbolMetadataParser` приводит FQN к namespace/type/method. Это обеспечивает консистентные ключи группировки независимо от источника метрик.
- **DTO слой**:
  - `SymbolMetricDto` — прежний формат строковых результатов.
  - `GroupedViolationsResponseDto<TGroup>` и `GroupedViolationsGroupDto<TViolation>` — общий каркас для обоих команд.
  - `SarifViolationDetailDto` — адаптация `SarifViolationRecord` для JSON (camelCase, null-safe).
- **Тесты**:
  - `ReadAnyCommandTests` проверяют сортировку, фильтрацию suppressed символов, новые сценарии `--group-by`.
  - `ReadSarifCommandTests` покрывают сводки по `ruleId`, комбинированные метрики, `--ruleid` фильтр, а также новые группировки (`metric`, `namespace`).
  - `SarifViolationOrdererTests` обновлены на обязательный параметр `metric` в `SarifViolationGroupBuilder`.

Используйте этот файл как единственный источник правды о CLI; `docs/Metrics-Reporter.md` содержит лишь краткое упоминание и ссылку.
