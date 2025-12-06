## Compiler‑Generated Classes Handling

Этот документ описывает, как Metrics Reporter обрабатывает компилятор‑сгенерированные классы (iterator/async state machines) и как их покрытие переносится на пользовательские методы.

### Какие классы считаются state machine

- C# компилятор создаёт вложенные типы вида `<Method>d__N`:
  - Полный FQN в AltCover: `Namespace.Type+<Method>d__N`.  
  - В AltCover XML такие типы появляются как отдельные `Class` с собственными `Summary` и `Methods` (обычно `MoveNext` и `SetStateMachine`).
- Эти типы содержат большую часть IL‑логики async/iterator‑методов:
  - AltCover привязывает к ним sequence/branch coverage и сложность.  
  - Сам пользовательский метод (`MethodName(...)`) часто имеет пустые `SequencePoints` и `BranchPoints`, но в отчёте по AltCover числится с `sequenceCoverage=0` и `branchCoverage=0`.

### Обнаружение iterator state machine типов

- Класс `IteratorCoverageReconciler` работает на уже агрегированном дереве типов (`AggregationWorkspaceState.Types`):
  - `TryExtractIteratorInfo` анализирует FQN:
    - Ищет последний `'+'` и берёт правую часть как nested‑тип.  
    - Проверяет, что nested‑тип начинается с `'<'`, содержит `'>'` и суффикс `"d__" + число`.  
    - Левая часть до `'+'` считается FQN внешнего типа, содержимое между `<` и `>` — именем исходного метода.
  - Если разбор успешен, получаем:
    - `outerTypeFqn`: например, `MetricsReporter.Processing.Parsers.SarifMetricsParser`.  
    - `methodName`: например, `ParseAsync`.
- Затем:
  - Ищется внешний тип в словаре `types`.  
  - Внутри него `FindMethodOnType` просматривает `MemberMetricsNode` и через `SymbolNormalizer.ExtractMethodName` сравнивает имена методов.

### Алгоритм переноса покрытия

Когда найдено соответствие между state machine и пользовательским методом, применяется следующая логика:

1. **Оценка наличия покрытия**  
   - `HasNonZeroAltCoverCoverage` проверяет, есть ли у типа/метода ненулевая `AltCoverSequenceCoverage` или `AltCoverBranchCoverage`.  
   - Варианты:
     - Оба имеют покрытие → ничего не переносим, чтобы не перезаписать реальные данные.  
     - Оба не имеют покрытия → iterator‑тип удаляется как шум, метод не меняется.  
     - Покрытие есть только у iterator‑типа → переносим.

2. **Перенос метрик** (`TransferIteratorCoverage`):
   - **Всегда** переносится `AltCoverSequenceCoverage`.  
   - `AltCoverCyclomaticComplexity` и `AltCoverNPathComplexity` также копируются, если заданы.  
   - `IncludesIteratorStateMachineCoverage` на методе устанавливается в `true`, чтобы UI мог показать индикатор.

3. **Специальное правило для branch coverage** (текущее поведение):
   - Branch‑покрытие iterator‑типа не всегда является полезной характеристикой пользовательского метода:  
     оно включает ветвления внутри сгенерированного state machine, которые в исходном коде не видны.
   - Поэтому:
     - `AltCoverBranchCoverage` переносится **только если** у пользовательского метода уже есть собственная branch‑метрика (AltCover создал `AltCoverBranchCoverage` для самого метода по его `BranchPoints`).  
     - Если у метода **нет** `AltCoverBranchCoverage` (типичный случай async/iterator методов вроде `ParseAsync`), мы:
       - Переносим только sequence/complexity метрики.  
       - Оставляем метод **без ветвлений** в агрегированном отчёте; HTML показывает пустую ячейку вместо `0%`, устраняя ложные красные зоны.

4. **Удаление iterator‑типа**  
   - После успешного переноса вызывается делегат `removeIteratorType`, который убирает state machine тип из словаря типов и из иерархии отчёта.  
   - Это предотвращает дублирование строк в HTML и концентрирует внимание на пользовательских методах.

### Взаимодействие с Plus‑нотацией и nested‑типами

- AltCover описывает вложенные типы как `Outer/Nested`; парсер приводит их к `Outer+Nested`.  
- Общие правила по нормализации вложенных типов и переносу покрытия между `Outer+Inner` и dot‑FQN (`Outer.Inner`) описаны в [`nested-types.md`](./nested-types.md).  
- Для iterator state machine:
  - `IteratorCoverageReconciler` работает **поверх** уже нормализованных FQN.  
  - После того как coverage переносится и iterator‑тип удаляется, в дереве остаётся только dot‑тип с агрегированным покрытием.

### HTML‑рендеринг и индикаторы

- `MemberMetricsNode.IncludesIteratorStateMachineCoverage`:
  - Устанавливается в `true`, когда метод получил метрики от iterator‑типа.  
  - Не зависит от того, переносилось ли branch‑покрытие — важно только наличие факта агрегации.
- HTML‑генератор (`HtmlTableGenerator`/`NodeRenderer`):
  - При `IncludesIteratorStateMachineCoverage == true` добавляет к имени метода значок `⊃` и tooltip  
    `Includes coverage from compiler-generated iterator state machine`.  
  - Это нейтральная метка: она не меняет цветов ячеек, а лишь поясняет происхождение чисел.

### Тестовое покрытие

- Основные сценарии проверяются в `MetricsAggregationServiceTests`:
  - `BuildReport_IteratorTypeCoverage_IsTransferredToMethodAndTypeIsHidden` — последовательность и ветвления переносятся, iterator‑тип скрывается.  
  - `BuildReport_IteratorType_NoMatchingMethod_KeepsTypeUnchanged` — когда нет соответствующего метода, state machine остаётся в иерархии.  
  - `BuildReport_IteratorType_MethodAlreadyHasCoverage_DoesNotOverrideOrHideType` — не перезаписываем реальные метрики метода.  
  - `BuildReport_IteratorTypeCoverage_DoesNotTransferBranchCoverage_WhenMethodHasNoBranchMetric` — **новый сценарий**: когда у метода нет своей branch‑метрики, переносится только sequence/complexity, а `AltCoverBranchCoverage` не создаётся.

### Связанные документы

- [`coverage-reporting.md`](./coverage-reporting.md) — общий конвейер парсинга и агрегации покрытия, включая правила появления/отсутствия branch‑метрик.  
- [`nested-types.md`](./nested-types.md) — нормализация вложенных типов и перенос покрытия между `Outer+Inner` и dot‑типа.
