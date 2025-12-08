namespace MetricsReporter.Rendering.Scripts;

/// <summary>
/// Defines the bootstrapper that wires up all modules.
/// </summary>
internal static partial class JavascriptModules
{
  internal const string Bootstrap = @"
(function(){
  'use strict';

  const doc = document;
  const table = doc.getElementById('metrics-table');
  if(!table){
    return;
  }
  const tbody = table.tBodies.length ? table.tBodies[0] : null;
  if(!tbody){
    return;
  }

  const refs = {
    tableActions: doc.querySelector('.table-actions'),
    filterInput: doc.getElementById('filter-input'),
    filterClear: doc.getElementById('filter-clear'),
    newFilter: doc.getElementById('filter-new'),
    changesFilter: doc.getElementById('filter-changes'),
    suppressedFilter: doc.getElementById('filter-suppressed'),
    detailControl: doc.getElementById('detail-level'),
    detailLabel: doc.getElementById('detail-label'),
    awarenessControl: doc.getElementById('awareness-level'),
    awarenessLabel: doc.getElementById('awareness-label'),
    expandButton: doc.getElementById('expand-all'),
    collapseButton: doc.getElementById('collapse-all'),
    metaSummary: doc.querySelector('.meta-summary'),
    metaDetails: doc.querySelector('.meta-details')
  };

  // Ensure mandatory refs exist
  if(!refs.detailControl || !refs.detailLabel || !refs.awarenessControl || !refs.awarenessLabel){
    return;
  }

  const preferenceStore = createPreferenceStore(window.location && window.location.pathname);
  const savedPreferences = preferenceStore.read() || null;

  const ctx = {
    doc,
    table,
    tbody,
    refs,
    thresholdData: readThresholdData(doc),
    ruleDescriptionsData: readRuleDescriptionsData(doc),
    metricAliasesData: readMetricAliasesData(doc),
    preferenceStore,
    savedPreferences,
    isRestoringPreferences: !!savedPreferences,
    stateFilter: {
      onlyNew: savedPreferences ? savedPreferences.filterNew === true : false,
      onlyChanges: savedPreferences ? savedPreferences.filterChanges === true : false,
      onlySuppressed: savedPreferences ? savedPreferences.filterSuppressed === true : false
    }
  };

  ctx.state = createStateStore(ctx);

  initFiltering(ctx);
  initTooltips(ctx);
  initSorting(ctx);
  initActions(ctx);
  initHotkeys(ctx);

  if(refs.filterInput){
    ctx.applyFilterText(refs.filterInput.value || '', false);
  } else {
    ctx.applyStateFilters();
  }

  ctx.isRestoringPreferences = false;
})();
";
}


