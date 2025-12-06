## Coverage Reporting

Этот документ описывает, как Metrics Reporter собирает, нормализует и агрегирует данные о покрытии кода на основе AltCover/OpenCover, а также как эти данные попадают в итоговый `MetricsReport.g.json` и HTML‑дашборд.

### Источники и метрики покрытия

- **AltCover/OpenCover XML**  
  - Основной источник покрытия: `AltCoverSequenceCoverage`, `AltCoverBranchCoverage`, `AltCoverCyclomaticComplexity`, `AltCoverNPathComplexity`.  
  - Данные считываются из `CoverageSession/Modules/Module/Classes/Class/Methods/Method` и соответствующих `Summary` узлов в `AltCoverMetricsParser`.
- **Единый словарь метрик**  
  - Все значения попадают в `IDictionary<MetricIdentifier, MetricValue>` на узлах Assembly/Namespace/Type/Member.  
  - Идентификаторы перечислены в `MetricIdentifier` (см. `AltCoverSequenceCoverage`, `AltCoverBranchCoverage`, ...).  
  - Описание единиц измерения задано в `MetricDescriptorCatalog` и попадает в секцию `metadata.metricDescriptors` отчёта.

### Парсинг AltCover XML

#### Структурный проход

- `AltCoverMetricsParser`:
  - Загружает XML (`CoverageSession` → `Modules` → `Module` → `Classes` → `Class` → `Method`).  
  - Для сборок создаёт `CodeElementKind.Assembly`; для классов — `CodeElementKind.Type`; для методов — `CodeElementKind.Member`.  
  - Полные имена типов и методов нормализуются (замена `::` на `.`, `/` на `+`, затем `SymbolNormalizer` приводит методы к формату `Namespace.Type.Method(...)`).  
  - Сопоставление с файлами делается через `FileRef uid` и таблицу `Files` модуля.

#### Извлечение метрик из Summary

- Для **assembly/class Summary** вызывается `PopulateMetrics`:
  - Всегда считывается `sequenceCoverage` в `AltCoverSequenceCoverage`.
  - `branchCoverage` записывается **только если** `numBranchPoints > 0`. При `numBranchPoints = 0` или отсутствии атрибута метрика не создаётся и в отчёт не попадёт.
  - `maxCyclomaticComplexity` и `maxNPathComplexity` мапятся на `AltCoverCyclomaticComplexity` и `AltCoverNPathComplexity`.
- Для **методов** вызывается `PopulateMethodMetrics`:
  - Всегда считывается `sequenceCoverage` в `AltCoverSequenceCoverage`.
  - `branchCoverage` записывается **только если** у метода есть реальный `<BranchPoints>` с хотя бы одним `<BranchPoint>`. Пустой `<BranchPoints />` или его отсутствие трактуются как «ветвлений нет», и `AltCoverBranchCoverage` не создаётся.
  - Сложность/`NPath` аналогично переносятся в метрики AltCover.
- Низкоуровневый помощник `AddMetric`:
  - Парсит числовой атрибут в `decimal`.  
  - При успехе создаёт `MetricValue` с `Value = значение` и временным статусом `ThresholdStatus.NotApplicable`.  
  - Отсутствующие или невалидные атрибуты пропускаются.

Эти правила гарантируют, что:

- Метод или тип **вообще не получает** `AltCoverBranchCoverage`, если AltCover не нашёл ветвлений (0 branch points).  
- Метрики с числом `0`, но при этом с реальными branch points, сохраняются и попадут под пороговую оценку.

### Агрегация и нормализация

#### Структурный merge

- `MetricsAggregationService` через `StructuralElementMerger` объединяет документы AltCover/Roslyn/SARIF в единое дерево:
  - Узлы `Assembly/Namespace/Type/Member` формируются из `ParsedCodeElement`.  
  - При совпадении FQN метрики агрегируются: для суммируемых метрик (например, счётчики нарушений) значения складываются, для остальных берётся значение с ненулевым `Value`.
- Обработка пространств имён и вложенных типов описана отдельно в [`nested-types.md`](./nested-types.md); для покрытия это критично, чтобы AltCover‑типы с нотацией `Outer/Nested` или `Outer+Inner` попадали в те же узлы, что и Roslyn‑типы.

#### Фильтрация членов

- После нормализации FQN `MemberFilter` исключает технические методы:
  - Конструкторы/статические конструкторы, методы компилятора (`<Clone>$`, `MoveNext`, `SetStateMachine`, `DisposeAsync`, лямбды `*b__*` и др.).  
  - Фильтрация происходит в `MetricsAggregationService.MergeMember` уже **после** того, как AltCover‑метрики были прочитаны, поэтому такие методы вообще не появляются в `MetricsReport.g.json`.
- Конкретный набор шаблонов настраивается через MSBuild‑свойство `ExcludedMemberNamesPatterns` и сохраняется в `metadata.excludedMemberNamesPatterns`.

### Обращение с показателями покрытия

#### Применение baseline и порогов

- `MetricsBaselineProcessor` строит финальный набор метрик для каждого узла:
  - Берёт текущие значения и baseline (если есть), считает `delta` через `DeltaCalculator`.  
  - Передаёт `value` и уровень символа (`Assembly/Namespace/Type/Member`) в `ThresholdEvaluator`.  
  - Если статус остаётся `NotApplicable` (нет данных или правило не применимо) — метрика **не попадает** в финальный `metrics` узла.  
  - Иначе создаётся новый `MetricValue` с полями `Value`, `Delta`, `Status`; `breakdown` копируется для SARIF‑метрик.
- JSON‑сериализация использует `JsonSerializerOptions` с `DefaultIgnoreCondition = WhenWritingNull`, поэтому:
  - `value: null` или `delta: null` не появляются в JSON.  
  - Отсутствие ключа метрики трактуется как «нет данных/не применимо».

#### Специальные правила для ветвлений

- **Нет branch points → нет метрики на методе**:  
  - На этапе парсинга мы не создаём `AltCoverBranchCoverage`, когда `numBranchPoints = 0` (для Summary метода) или `<BranchPoints />`/`<BranchPoints>` пуст (для методов).  
  - Такой метод никогда не попадёт под порог `AltCoverBranchCoverage` и не создаст ложные `0%` с Warning/Error.
- **Переопределение применимости на уровне типа**:  
  - После мерджа всех документов выполняется дополнительный reconciliation‑проход по `TypeMetricsNode`.  
  - Если у типа есть `AltCoverBranchCoverage`, но **ни один его член** не содержит `AltCoverBranchCoverage`, метрика у типа удаляется и трактуется как NotApplicable.  
  - Если хотя бы один метод типа имеет branch‑метрику, типовая `AltCoverBranchCoverage` сохраняется и отражает агрегированное ветвление по реальным методам.
- **Iterator/async state machine**:
  - Подробно описано в [`compiler-classes-handling.md`](./compiler-classes-handling.md).  
  - Кратко: покрытие `AltCoverSequenceCoverage` всегда переносится с state machine‑типа на пользовательский метод, чтобы async/iterator‑методы не выглядели «никогда не вызывавшимися».  
  - `AltCoverBranchCoverage` переносится **только если** у пользовательского метода уже есть собственная branch‑метрика из AltCover. Это защищает от ложных `0%` по ветвлениям, которые существуют только в сгенерированном IL.

### Отчёт и HTML‑дашборд

- Итоговый `MetricsReport.g.json` содержит:
  - Для каждого узла: набор метрик, где `value` и `status` уже учли baseline и пороги.  
  - В `metadata.metricDescriptors` — единицы измерения (`percent`, `count`, `score`); UI не дублирует их в каждой ячейке.  
  - В `metadata.thresholdsByLevel` — конфигурация порогов, которая используется и при генерации отчёта, и при рендеринге HTML.
- HTML‑таблица:
  - Читает метрики напрямую из `MetricsReport.g.json`; пустые ячейки соответствуют отсутствию метрики (нет данных/не применимо).  
  - Обозначения `Success/Warning/Error` берутся из `status`; `delta` нужен только для визуализации изменений относительно baseline.  
  - Для методов, где `IncludesIteratorStateMachineCoverage == true`, рядом с именем выводится индикатор `⊃` с tooltip’ом «Includes coverage from compiler-generated iterator state machine».

### Связанные документы

- [`nested-types.md`](./nested-types.md) — как нормализуются вложенные типы и как переносится покрытие между `Outer+Inner` и dot‑FQN.  
- [`compiler-classes-handling.md`](./compiler-classes-handling.md) — подробности обработки compiler‑generated iterator/async классов и переноса их покрытия.
