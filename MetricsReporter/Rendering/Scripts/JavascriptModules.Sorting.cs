namespace MetricsReporter.Rendering.Scripts;

/// <summary>
/// Defines column sorting functionality.
/// </summary>
internal static partial class JavascriptModules
{
  internal const string Sorting = @"
function initSorting(ctx){
  const table = ctx.table;
  const state = ctx.state;
  const numericPattern = /[^0-9.-]/g;
  const collator = typeof Intl !== 'undefined' && Intl.Collator
    ? new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' })
    : null;

  ctx.sortState = { column: null, direction: 'asc' };

  if(!table || !table.tHead){
    return;
  }

  table.tHead.addEventListener('click', function(event){
    const header = event.target.closest('th[data-col]');
    if(!header){
      return;
    }
    const column = header.dataset.col;
    if(!column){
      return;
    }

    if(column !== 'symbol'){
      const hasColumn = state.rows.some(function(row){
        return !!row.querySelector('.metric[data-col=""' + column + '""]');
      });
      if(!hasColumn){
        return;
      }
    }

    const sortState = ctx.sortState;
    const direction = sortState.column === column && sortState.direction === 'asc'
      ? 'desc'
      : 'asc';

    sortState.column = column;
    sortState.direction = direction;
    applySortIndicators(column, direction);
    sortHierarchy(column, direction);
  });

  function applySortIndicators(column, direction){
    const headers = table.tHead.querySelectorAll('th[data-col]');
    headers.forEach(function(header){
      if(header.dataset.col === column){
        header.classList.remove('sort-asc', 'sort-desc');
        header.classList.add(direction === 'asc' ? 'sort-asc' : 'sort-desc');
        header.setAttribute('data-sort-direction', direction);
      } else {
        header.classList.remove('sort-asc', 'sort-desc');
        header.removeAttribute('data-sort-direction');
      }
    });
  }

  function compareRows(a, b, column, direction){
    const dataA = state.getSortSnapshot(a);
    const dataB = state.getSortSnapshot(b);
    const textA = column === 'symbol' ? (dataA.symbol || '') : (dataA[column] || '');
    const textB = column === 'symbol' ? (dataB.symbol || '') : (dataB[column] || '');
    const numericA = parseFloat(textA.replace(numericPattern, ''));
    const numericB = parseFloat(textB.replace(numericPattern, ''));
    const hasNumericA = !isNaN(numericA);
    const hasNumericB = !isNaN(numericB);

    let result;
    if(hasNumericA && hasNumericB){
      result = numericA - numericB;
    } else if(hasNumericA && !hasNumericB){
      result = -1;
    } else if(!hasNumericA && hasNumericB){
      result = 1;
    } else if(collator){
      result = collator.compare(textA, textB);
    } else {
      result = textA.localeCompare(textB);
    }

    if(result === 0){
      const pathA = (a.getAttribute('data-fqn') || '').toLowerCase();
      const pathB = (b.getAttribute('data-fqn') || '').toLowerCase();
      result = pathA.localeCompare(pathB);
    }

    if(direction === 'desc'){
      result = -result;
    }
    return result;
  }

  function sortHierarchy(column, direction){
    function sortChildren(parentId){
      const children = state.directChildren(parentId);
      if(children.length <= 1){
        return;
      }
      const sorted = children.slice().sort(function(a, b){
        return compareRows(a, b, column, direction);
      });

      const parentRow = parentId ? state.rowById.get(parentId) : null;
      if(!parentRow){
        sorted.forEach(function(child){
          const subtree = [child].concat(state.getDescendants(child.getAttribute('data-id')));
          subtree.forEach(function(node){
            ctx.tbody.appendChild(node);
          });
        });
        return;
      }

      let anchor = parentRow;
      sorted.forEach(function(child){
        const subtree = [child].concat(state.getDescendants(child.getAttribute('data-id')));
        subtree.forEach(function(node){
          ctx.tbody.insertBefore(node, anchor.nextSibling);
          anchor = node;
        });
      });
    }

    sortChildren(null);
    state.refresh();
    state.updateVisibility();
    if(ctx.persistPreferences){
      ctx.persistPreferences();
    }
  }
}
";
}


