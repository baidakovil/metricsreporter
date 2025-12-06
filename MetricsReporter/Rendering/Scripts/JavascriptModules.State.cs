namespace MetricsReporter.Rendering.Scripts;

/// <summary>
/// Defines DOM state management helpers.
/// </summary>
internal static partial class JavascriptModules
{
  internal const string StateManagement = @"
function createStateStore(ctx){
  const tbody = ctx.tbody;
  const rowById = new Map();
  const childrenByParent = new Map();
  const sortCache = new WeakMap();
  const filterCache = new WeakMap();

  const state = {
    rows: [],
    rowById,
    refresh,
    directChildren,
    getDescendants,
    setExpanderState,
    isAncestorExpanded,
    updateVisibility,
    applyDetailLevel,
    getSortSnapshot,
    getFilterInfo,
    updateLeafClasses,
    updateStripedClasses
  };

  refresh();
  return state;

  function refresh(){
    rowById.clear();
    childrenByParent.clear();
    state.rows = Array.from(tbody.querySelectorAll('tr.node-row'));
    state.rows.forEach(function(row){
      ensureRowDefaults(row);
      const id = row.getAttribute('data-id');
      if(id){
        rowById.set(id, row);
      }
      const parentKey = row.getAttribute('data-parent') || '__root__';
      if(!childrenByParent.has(parentKey)){
        childrenByParent.set(parentKey, []);
      }
      childrenByParent.get(parentKey).push(row);
    });
  }

  function isRowHidden(row){
    return row.dataset.hiddenByDetail === 'true'
      || row.dataset.hiddenByFilter === 'true'
      || row.dataset.hiddenByAwareness === 'true'
      || row.dataset.hiddenByState === 'true';
  }

  function ensureRowDefaults(row){
    row.dataset.hiddenByDetail = row.dataset.hiddenByDetail || 'false';
    row.dataset.hiddenByFilter = row.dataset.hiddenByFilter || 'false';
    row.dataset.hiddenByAwareness = row.dataset.hiddenByAwareness || 'false';
    row.dataset.hiddenByState = row.dataset.hiddenByState || 'false';
    row.dataset.expanded = row.dataset.expanded || 'true';
  }

  function directChildren(rowId){
    const key = rowId || '__root__';
    return childrenByParent.get(key) || [];
  }

  function getDescendants(rowId){
    const stack = directChildren(rowId).slice();
    const result = [];
    while(stack.length){
      const current = stack.pop();
      result.push(current);
      const currentId = current.getAttribute('data-id');
      if(currentId){
        directChildren(currentId).forEach(function(child){
          stack.push(child);
        });
      }
    }
    return result;
  }

  function setExpanderState(row, expanded){
    if(!row){
      return;
    }
    if(row.dataset.hasChildren !== 'true'){
      row.dataset.expanded = 'true';
      return;
    }
    row.dataset.expanded = expanded ? 'true' : 'false';
    const expander = row.querySelector('.expander');
    if(expander){
      expander.textContent = expanded ? '-' : '+';
    }
  }

  function isAncestorExpanded(row){
    let parentId = row.getAttribute('data-parent');
    while(parentId){
      const parent = rowById.get(parentId);
      if(!parent){
        break;
      }
      const hidden = parent.dataset.hiddenByDetail === 'true'
        || parent.dataset.hiddenByFilter === 'true'
        || parent.dataset.hiddenByAwareness === 'true'
        || parent.dataset.hiddenByState === 'true';
      if(hidden || parent.dataset.expanded === 'false'){
        return false;
      }
      parentId = parent.getAttribute('data-parent');
    }
    return true;
  }

  function updateVisibility(){
    state.rows.forEach(function(row){
      const hidden = isRowHidden(row);
      if(hidden || !isAncestorExpanded(row)){
        row.style.display = 'none';
      } else {
        row.style.display = '';
      }
    });
    const depth = ctx.currentDetail ? ctx.currentDetail.maxDepth : 2;
    updateLeafClasses(depth);
    updateStripedClasses();
  }

  function applyDetailLevel(maxDepth){
    state.rows.forEach(function(row){
      const level = parseInt(row.getAttribute('data-level') || '0', 10) || 0;
      row.dataset.hiddenByDetail = level > maxDepth ? 'true' : 'false';
    });
    updateVisibility();
  }

  function updateLeafClasses(maxDepth){
    state.rows.forEach(function(row){
      row.classList.remove('leaf-row');
      const expander = row.querySelector('.expander');
      if(expander){
        expander.style.visibility = '';
        expander.style.pointerEvents = '';
      }
    });

    state.rows.forEach(function(row){
      const hasChildren = row.dataset.hasChildren === 'true';
      const level = parseInt(row.getAttribute('data-level') || '0', 10) || 0;
      const role = row.dataset.role || 'member';
      const expander = row.querySelector('.expander');

      if(!hasChildren){
        if((role === 'assembly' || role === 'namespace' || role === 'type') && level < maxDepth){
          return;
        }
        row.classList.add('leaf-row');
        if(expander){
          expander.style.visibility = 'hidden';
          expander.style.pointerEvents = 'none';
        }
        return;
      }

      const rowId = row.getAttribute('data-id');
      const children = rowId ? directChildren(rowId) : [];
      const hasVisibleChild = children.some(function(child){
        return child.dataset.hiddenByDetail !== 'true'
          && child.dataset.hiddenByFilter !== 'true'
          && child.dataset.hiddenByAwareness !== 'true'
          && child.dataset.hiddenByState !== 'true';
      });

      if(!hasVisibleChild || level >= maxDepth){
        row.classList.add('leaf-row');
        if(expander){
          expander.style.visibility = 'hidden';
          expander.style.pointerEvents = 'none';
        }
      }
    });
  }

  function updateStripedClasses(){
    const visibleLeafRows = state.rows.filter(function(row){
      return row.classList.contains('leaf-row') && row.style.display !== 'none';
    });
    visibleLeafRows.forEach(function(row, index){
      row.classList.remove('stripe-odd', 'stripe-even');
      row.classList.add(index % 2 === 0 ? 'stripe-odd' : 'stripe-even');
    });
  }

  function getSortSnapshot(row){
    if(sortCache.has(row)){
      return sortCache.get(row);
    }
    const snapshot = Object.create(null);
    const nameCell = row.querySelector('.symbol .name-text');
    snapshot.symbol = nameCell ? nameCell.textContent.trim() : '';
    const metrics = row.querySelectorAll('.metric');
    metrics.forEach(function(metric){
      const column = metric.dataset.col;
      if(column){
        snapshot[column] = metric.textContent.trim();
      }
    });
    sortCache.set(row, snapshot);
    return snapshot;
  }

  function getFilterInfo(row){
    if(filterCache.has(row)){
      return filterCache.get(row);
    }
    const key = (row.dataset.filterKey || row.dataset.fqn || row.textContent || '').toLowerCase();
    const info = {
      text: key,
      tokens: key.split(/[\s.:\\/]+/).filter(Boolean)
    };
    filterCache.set(row, info);
    return info;
  }
}
";
}


