namespace MetricsReporter.Rendering.Scripts;

/// <summary>
/// Defines text and state filtering functionality.
/// </summary>
internal static partial class JavascriptModules
{
  internal const string Filtering = @"
function initFiltering(ctx){
  const filterInput = ctx.refs.filterInput;
  const filterClear = ctx.refs.filterClear;
  const newFilterControl = ctx.refs.newFilter;
  const changesFilterControl = ctx.refs.changesFilter;
  const suppressedFilterControl = ctx.refs.suppressedFilter;
  const state = ctx.state;
  const filterState = ctx.stateFilter;

  let currentFilter = '';
  let debounceHandle = null;
  let filterSequence = 0;

  ctx.applyFilterText = applyFilterText;
  ctx.applyStateFilters = applyStateFilters;

  if(filterInput && ctx.savedPreferences && typeof ctx.savedPreferences.filterText === 'string'){
    filterInput.value = ctx.savedPreferences.filterText;
    currentFilter = ctx.savedPreferences.filterText.toLowerCase();
  }

  if(filterInput){
    filterInput.addEventListener('input', function(event){
      scheduleFilter(event.target.value || '');
      updateFilterClear();
    });
    filterInput.addEventListener('keydown', function(event){
      if(event.key === 'Enter'){
        event.preventDefault();
        applyFilterText(event.target.value || '');
        filterInput.blur();
      } else if(event.key === 'Escape'){
        filterInput.value = '';
        applyFilterText('');
      }
    });
  }

  if(filterClear){
    filterClear.addEventListener('click', function(){
      if(filterInput){
        filterInput.value = '';
      }
      applyFilterText('');
      if(filterInput){
        filterInput.focus();
      }
    });
  }

  if(newFilterControl){
    newFilterControl.checked = filterState.onlyNew;
    newFilterControl.addEventListener('change', function(){
      filterState.onlyNew = newFilterControl.checked;
    applyStateFilters();
    ctx.persistPreferences && ctx.persistPreferences();
    });
  }

  if(changesFilterControl){
    changesFilterControl.checked = filterState.onlyChanges;
    changesFilterControl.addEventListener('change', function(){
      filterState.onlyChanges = changesFilterControl.checked;
    applyStateFilters();
    ctx.persistPreferences && ctx.persistPreferences();
    });
  }

  if(suppressedFilterControl){
    suppressedFilterControl.checked = filterState.onlySuppressed;
    suppressedFilterControl.addEventListener('change', function(){
      filterState.onlySuppressed = suppressedFilterControl.checked;
    applyStateFilters();
    ctx.persistPreferences && ctx.persistPreferences();
    });
  }

  updateFilterClear();

  function scheduleFilter(value){
    const token = ++filterSequence;
    if(debounceHandle){
      clearTimeout(debounceHandle);
    }
    debounceHandle = window.setTimeout(function(){
      if(token !== filterSequence){
        return;
      }
      applyFilterText(value);
    }, 200);
  }

  function updateFilterClear(){
    if(!filterClear){
      return;
    }
    filterClear.style.display = currentFilter.length > 0 ? '' : 'none';
  }

  function applyFilterText(value, persist){
    currentFilter = (value || '').trim().toLowerCase();
    if(!currentFilter){
      state.rows.forEach(function(row){
        row.dataset.hiddenByFilter = 'false';
      });
      state.updateVisibility();
      if(persist !== false && ctx.persistPreferences){
        ctx.persistPreferences();
      }
      updateFilterClear();
      return;
    }

    const ancestors = new Set();
    state.rows.forEach(function(row){
      const info = state.getFilterInfo(row);
      const matches = info.text.includes(currentFilter);
      row.dataset.hiddenByFilter = matches ? 'false' : 'true';
      if(matches){
        let parentId = row.getAttribute('data-parent');
        while(parentId){
          const parent = state.rowById.get(parentId);
          if(!parent){
            break;
          }
          ancestors.add(parent);
          parentId = parent.getAttribute('data-parent');
        }
      }
    });

    ancestors.forEach(function(row){
      row.dataset.hiddenByFilter = 'false';
    });

    state.updateVisibility();
    if(persist !== false && ctx.persistPreferences){
      ctx.persistPreferences();
    }
    updateFilterClear();
  }

  function applyStateFilters(){
    const requireNew = filterState.onlyNew;
    const requireChanges = filterState.onlyChanges;
    const requireSuppressed = filterState.onlySuppressed;
    const rows = state.rows;

    if(!requireNew && !requireChanges && !requireSuppressed){
      rows.forEach(function(row){
        row.dataset.hiddenByState = 'false';
      });
      state.updateVisibility();
      return;
    }

    rows.forEach(function(row){
      row.dataset.hiddenByState = 'true';
    });

    rows.forEach(function(row){
      const matchesNew = requireNew && row.dataset.isNew === 'true';
      const matchesChanges = requireChanges && row.dataset.hasDelta === 'true';
      const matchesSuppressed = requireSuppressed && row.dataset.hasSuppressed === 'true';
      if(matchesNew || matchesChanges || matchesSuppressed){
        let current = row;
        while(current){
          current.dataset.hiddenByState = 'false';
          const parentId = current.getAttribute('data-parent');
          current = parentId ? state.rowById.get(parentId) : null;
        }
      }
    });

    state.updateVisibility();
  }
}
";
}


