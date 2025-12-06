namespace MetricsReporter.Rendering.Scripts;

/// <summary>
/// Defines UI actions such as expand/collapse, sliders, row actions, and metadata toggles.
/// </summary>
internal static partial class JavascriptModules
{
  internal const string Actions = @"
function initActions(ctx){
  const refs = ctx.refs;
  const state = ctx.state;
  const table = ctx.table;
  const clipboard = createClipboardHelper(ctx.doc);

  const detailLevels = {
    '1': { maxDepth: 1, label: 'Namespace' },
    '2': { maxDepth: 2, label: 'Type' },
    '3': { maxDepth: 3, label: 'Member' }
  };

  const awarenessLevels = {
    '1': { label: 'All', predicate: function(){ return true; } },
    '2': { label: 'Warning', predicate: function(row){
      if(row.dataset.hasSuppressed === 'true'){ return false; }
      return row.dataset.hasWarning === 'true' || row.dataset.hasError === 'true';
    } },
    '3': { label: 'Error', predicate: function(row){
      if(row.dataset.hasSuppressed === 'true'){ return false; }
      return row.dataset.hasError === 'true';
    } }
  };

  ctx.currentDetailKey = (ctx.savedPreferences && detailLevels[ctx.savedPreferences.detailLevel || '']) ? ctx.savedPreferences.detailLevel : '2';
  ctx.currentDetail = detailLevels[ctx.currentDetailKey];
  ctx.currentAwarenessKey = (ctx.savedPreferences && awarenessLevels[ctx.savedPreferences.awarenessLevel || '']) ? ctx.savedPreferences.awarenessLevel : '1';
  ctx.currentAwareness = awarenessLevels[ctx.currentAwarenessKey];

  refs.detailControl.value = ctx.currentDetailKey;
  refs.detailControl.setAttribute('aria-valuenow', ctx.currentDetailKey);
  refs.detailLabel.textContent = ctx.currentDetail.label;

  refs.awarenessControl.value = ctx.currentAwarenessKey;
  refs.awarenessControl.setAttribute('aria-valuenow', ctx.currentAwarenessKey);
  refs.awarenessLabel.textContent = ctx.currentAwareness.label;

  ctx.persistPreferences = function persistPreferences(){
    if(ctx.isRestoringPreferences){
      return;
    }
    ctx.preferenceStore.write({
      detailLevel: ctx.currentDetailKey,
      awarenessLevel: ctx.currentAwarenessKey,
      filterText: refs.filterInput ? refs.filterInput.value : '',
      filterNew: ctx.stateFilter.onlyNew,
      filterChanges: ctx.stateFilter.onlyChanges,
      filterSuppressed: ctx.stateFilter.onlySuppressed
    });
  };

  ctx.setDetailLevel = setDetailLevel;
  ctx.setAwarenessLevel = setAwarenessLevel;

  setDetailLevel(ctx.currentDetailKey, { suppressPersist: true, expandAll: true });
  setAwarenessLevel(ctx.currentAwarenessKey, { suppressPersist: true });
  ctx.applyStateFilters();
  applyDeltaColors();
  updateStickyHeaderPosition();

  refs.detailControl.addEventListener('input', function(){
    const snapped = snapRangeValue(refs.detailControl, refs.detailControl.value);
    if(refs.detailControl.value !== snapped){
      refs.detailControl.value = snapped;
    }
    setDetailLevel(snapped);
  });
  refs.detailControl.addEventListener('change', function(){
    const snapped = snapRangeValue(refs.detailControl, refs.detailControl.value);
    if(refs.detailControl.value !== snapped){
      refs.detailControl.value = snapped;
    }
    setDetailLevel(snapped);
  });
  refs.detailControl.addEventListener('click', function(event){
    if(event.target === refs.detailControl){
      snapSlider(refs.detailControl, event, setDetailLevel);
    }
  });

  function handleAwarenessInput(){
    const snapped = snapRangeValue(refs.awarenessControl, refs.awarenessControl.value);
    if(refs.awarenessControl.value !== snapped){
      refs.awarenessControl.value = snapped;
    }
    ctx.pendingAwarenessValue = snapped;
    if(!ctx.awarenessDragging){
      commitAwareness();
    }
  }

  function commitAwareness(){
    var value = ctx.pendingAwarenessValue;
    if(typeof value !== 'string'){
      value = snapRangeValue(refs.awarenessControl, refs.awarenessControl.value);
    }
    ctx.pendingAwarenessValue = undefined;
    setAwarenessLevel(value);
  }

  refs.awarenessControl.addEventListener('pointerdown', function(event){
    ctx.awarenessDragging = true;
    ctx.activeAwarenessPointer = event.pointerId;
    if(refs.awarenessControl.setPointerCapture){
      try{
        refs.awarenessControl.setPointerCapture(event.pointerId);
      }catch(_){}
    }
  });

  function releaseAwarenessDrag(event){
    if(ctx.activeAwarenessPointer !== undefined && event.pointerId !== ctx.activeAwarenessPointer){
      return;
    }
    ctx.awarenessDragging = false;
    ctx.activeAwarenessPointer = undefined;
    if(refs.awarenessControl.releasePointerCapture){
      try{
        refs.awarenessControl.releasePointerCapture(event.pointerId);
      }catch(_){}
    }
    commitAwareness();
  }

  refs.awarenessControl.addEventListener('pointerup', releaseAwarenessDrag);
  refs.awarenessControl.addEventListener('pointercancel', releaseAwarenessDrag);

  refs.awarenessControl.addEventListener('input', handleAwarenessInput);
  refs.awarenessControl.addEventListener('change', handleAwarenessInput);

  if(refs.expandButton){
    refs.expandButton.addEventListener('click', function(){
      state.rows.forEach(function(row){
        if(row.dataset.hasChildren === 'true'){
          state.setExpanderState(row, true);
        }
      });
      state.updateVisibility();
    });
  }

  if(refs.collapseButton){
    refs.collapseButton.addEventListener('click', function(){
      state.rows.forEach(function(row){
        if(row.dataset.hasChildren === 'true'){
          state.setExpanderState(row, false);
        }
      });
      state.updateVisibility();
    });
  }

  ctx.doc.addEventListener('click', function(event){
    const actionButton = event.target.closest('.row-action-icon');
    if(actionButton){
      event.preventDefault();
      handleRowAction(actionButton);
      return;
    }

    const expander = event.target.closest('.expander');
    if(expander){
      event.preventDefault();
      const parentId = expander.getAttribute('data-target');
      const row = parentId ? state.rowById.get(parentId) : expander.closest('tr.node-row');
      if(row){
        const shouldExpand = row.dataset.expanded === 'false';
        state.setExpanderState(row, shouldExpand);
        if(!shouldExpand){
          state.getDescendants(row.getAttribute('data-id')).forEach(function(desc){
            state.setExpanderState(desc, false);
          });
        }
        state.updateVisibility();
      }
      return;
    }

    const row = event.target.closest('tr.node-row');
    if(row && row.dataset.hasChildren === 'true' && !row.classList.contains('leaf-row')){
      if(!event.target.closest('button') && !event.target.closest('a') && !event.target.closest('input') && !event.target.closest('textarea') && !event.target.closest('select')){
        const shouldExpand = row.dataset.expanded === 'false';
        state.setExpanderState(row, shouldExpand);
        if(!shouldExpand){
          state.getDescendants(row.getAttribute('data-id')).forEach(function(desc){
            state.setExpanderState(desc, false);
          });
        }
        state.updateVisibility();
      }
    }
  });

  if(refs.metaSummary && refs.metaDetails){
    refs.metaSummary.addEventListener('click', function(){
      const expanded = refs.metaSummary.classList.toggle('expanded');
      if(expanded){
        refs.metaDetails.style.display = 'block';
        requestAnimationFrame(function(){
          refs.metaDetails.classList.add('expanded');
        });
      } else {
        refs.metaDetails.classList.remove('expanded');
        refs.metaDetails.addEventListener('transitionend', function handler(event){
          if(event.propertyName === 'max-height' || event.propertyName === 'opacity'){
            refs.metaDetails.style.display = 'none';
          }
        }, { once: true });
      }
    });
  }

  window.addEventListener('resize', updateStickyHeaderPosition);

  function setDetailLevel(value, options){
    const key = detailLevels[value] ? value : '2';
    ctx.currentDetailKey = key;
    ctx.currentDetail = detailLevels[key];
    refs.detailControl.value = key;
    refs.detailControl.setAttribute('aria-valuenow', key);
    refs.detailLabel.textContent = ctx.currentDetail.label;

    const expandAll = options && (options.expandAll || options.ctrlKey || options.shiftKey);
    if(expandAll){
      state.rows.forEach(function(row){
        if(row.dataset.hasChildren === 'true'){
          state.setExpanderState(row, true);
        }
      });
    }

    state.applyDetailLevel(ctx.currentDetail.maxDepth);
    if(typeof ctx.applyStateFilters === 'function'){
      ctx.applyStateFilters();
    }
    if(!options || options.suppressPersist !== true){
      ctx.persistPreferences && ctx.persistPreferences();
    }
  }

  function setAwarenessLevel(value, options){
    const key = awarenessLevels[value] ? value : '1';
    ctx.currentAwarenessKey = key;
    ctx.currentAwareness = awarenessLevels[key];
    refs.awarenessControl.value = key;
    refs.awarenessControl.setAttribute('aria-valuenow', key);
    refs.awarenessLabel.textContent = ctx.currentAwareness.label;

    const predicate = ctx.currentAwareness.predicate;
    const visibleRows = [];
    state.rows.forEach(function(row){
      const match = predicate(row);
      row.dataset.hiddenByAwareness = match ? 'false' : 'true';
      if(match){
        visibleRows.push(row);
      }
    });
    visibleRows.forEach(function(row){
      let current = row;
      while(current){
        if(current.dataset.hiddenByAwareness !== 'false'){
          current.dataset.hiddenByAwareness = 'false';
        }
        const parentId = current.getAttribute('data-parent');
        current = parentId ? state.rowById.get(parentId) : null;
      }
    });

    state.updateVisibility();
    if(!options || options.suppressPersist !== true){
      ctx.persistPreferences && ctx.persistPreferences();
    }
  }

  function handleRowAction(button){
    const row = button.closest('tr.node-row');
    if(!row){
      return;
    }
    const action = button.dataset.action;
    if(action === 'open'){
      const path = (row.getAttribute('data-source-path') || '').trim();
      const line = parseInt(row.getAttribute('data-source-line') || '', 10);
      if(!path || isNaN(line)){
        return;
      }
      const normalizedPath = path.replace(/\\/g, '/');
      const cursorUrl = 'cursor://file/' + normalizedPath + ':' + line + '#L' + line;
      try{
        window.location.href = encodeURI(cursorUrl);
      }catch(error){
        console.warn('Failed to open file via Cursor protocol:', error);
      }
      return;
    }
    if(action === 'copy'){
      clipboard(row.dataset.fqn || '');
      return;
    }
    if(action === 'filter'){
      if(refs.filterInput){
        refs.filterInput.value = row.dataset.fqn || '';
        ctx.applyFilterText(row.dataset.fqn || '');
        refs.filterInput.focus();
      }
    }
  }

  function applyDeltaColors(){
    if(!ctx.thresholdData){
      return;
    }
    const metricCells = ctx.table.querySelectorAll('.metric[data-metric-id]');
    metricCells.forEach(function(cell){
      const metricId = cell.dataset.metricId;
      const info = metricId ? ctx.thresholdData[metricId] : null;
      if(!info){
        return;
      }
      const higherIsBetter = info.higherIsBetter === true;
      const positiveDeltaNeutral = info.positiveDeltaNeutral === true;
      const deltas = cell.querySelectorAll('.delta-positive, .delta-negative');
      deltas.forEach(function(delta){
        const isPositive = delta.classList.contains('delta-positive');
        delta.classList.remove('delta-positive', 'delta-negative', 'delta-improving', 'delta-degrading', 'delta-neutral');
        if(positiveDeltaNeutral && !higherIsBetter && isPositive){
          delta.classList.add('delta-neutral');
          return;
        }
        const isImproving = higherIsBetter ? isPositive : !isPositive;
        delta.classList.add(isImproving ? 'delta-improving' : 'delta-degrading');
      });
    });
  }

  function updateStickyHeaderPosition(){
    const actionsBar = refs.tableActions;
    const headRows = ctx.table.tHead ? Array.from(ctx.table.tHead.querySelectorAll('tr')) : [];
    if(!actionsBar || !headRows.length){
      return;
    }
    const offset = actionsBar.offsetHeight || 0;
    let cumulative = offset;
    headRows.forEach(function(row){
      row.querySelectorAll('th').forEach(function(th){
        th.style.top = (cumulative - 1) + 'px';
      });
      cumulative += row.offsetHeight || 0;
    });
  }

  ctx.updateStickyHeaderPosition = updateStickyHeaderPosition;
  ctx.applyDeltaColors = applyDeltaColors;
}
";
}


