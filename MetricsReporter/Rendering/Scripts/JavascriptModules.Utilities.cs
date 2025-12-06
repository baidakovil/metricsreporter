namespace MetricsReporter.Rendering.Scripts;

/// <summary>
/// Defines shared helper functions reused by multiple script modules.
/// </summary>
internal static partial class JavascriptModules
{
  internal const string Utilities = @"
const RCA_TOOLTIP_DELAY = 420;

function readThresholdData(doc){
  const holder = doc.getElementById('threshold-data');
  if(!holder){
    return null;
  }
  const raw = holder.textContent || holder.innerText || '';
  if(!raw.trim()){
    return null;
  }
  try{
    const parsed = JSON.parse(raw);
    return parsed && Object.keys(parsed).length > 0 ? parsed : null;
  }catch(_){
    return null;
  }
}

function readRuleDescriptionsData(doc){
  const holder = doc.getElementById('rule-descriptions-data');
  if(!holder){
    return null;
  }
  const raw = holder.textContent || holder.innerText || '';
  if(!raw.trim()){
    return null;
  }
  try{
    const parsed = JSON.parse(raw);
    return parsed && Object.keys(parsed).length > 0 ? parsed : null;
  }catch(_){
    return null;
  }
}

function createPreferenceStore(locationKey){
  const key = 'rcaMetricsReport.preferences:' + (locationKey || 'report');
  return {
    read(){
      try{
        if(typeof window === 'undefined' || typeof window.localStorage === 'undefined'){
          return null;
        }
        const serialized = window.localStorage.getItem(key);
        return serialized ? JSON.parse(serialized) : null;
      }catch(_){
        return null;
      }
    },
    write(value){
      if(!value){
        return;
      }
      try{
        if(typeof window === 'undefined' || typeof window.localStorage === 'undefined'){
          return;
        }
        window.localStorage.setItem(key, JSON.stringify(value));
      }catch(_){
        /* silent */
      }
    }
  };
}

function escapeHtml(value){
  if(value === null || value === undefined){
    return '';
  }
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/""/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function createClipboardHelper(doc){
  return function copy(value){
    if(!value){
      return;
    }
    try{
      if(navigator && navigator.clipboard && navigator.clipboard.writeText){
        navigator.clipboard.writeText(value);
        return;
      }
    }catch(_){
      /* ignore */
    }
    const textarea = doc.createElement('textarea');
    textarea.value = value;
    textarea.setAttribute('readonly', '');
    textarea.style.position = 'absolute';
    textarea.style.left = '-9999px';
    doc.body.appendChild(textarea);
    textarea.select();
    try{
      document.execCommand('copy');
    }catch(_){
      /* ignore */
    }
    doc.body.removeChild(textarea);
  };
}

function createTooltipHost(doc){
  const element = doc.createElement('div');
  element.className = 'metric-tooltip';
  element.style.display = 'none';
  doc.body.appendChild(element);

  let timerId = null;
  let anchor = null;
  let currentBuilder = null;

  function cancel(){
    if(timerId){
      clearTimeout(timerId);
      timerId = null;
    }
  }

  function position(target, preferBelow){
    if(!target || element.style.display === 'none'){
      return;
    }
    const rect = target.getBoundingClientRect();
    const tooltipRect = element.getBoundingClientRect();
    const isButton = target.classList && target.classList.contains('row-action-icon');
    const extraOffset = isButton ? 24 : 8;

    let top = window.scrollY + rect.bottom + extraOffset;
    let left = window.scrollX + rect.left + (rect.width - tooltipRect.width) / 2;
    const minLeft = window.scrollX + 8;
    const maxLeft = window.scrollX + window.innerWidth - tooltipRect.width - 8;
    left = Math.min(Math.max(left, minLeft), Math.max(minLeft, maxLeft));

    if(!preferBelow && top + tooltipRect.height > window.scrollY + window.innerHeight - 8){
      top = window.scrollY + rect.top - tooltipRect.height - 8;
    }
    if(top < window.scrollY + 8){
      top = window.scrollY + 8;
    }
    element.style.left = left + 'px';
    element.style.top = top + 'px';
  }

  function show(target, builder, preferBelow){
    cancel();
    if(!builder){
      return;
    }
    const html = builder();
    if(!html){
      return;
    }
    currentBuilder = builder;
    anchor = target;
    element.innerHTML = html;
    element.style.display = 'block';
    position(target, preferBelow);
  }

  function hide(){
    cancel();
    anchor = null;
    currentBuilder = null;
    element.style.display = 'none';
  }

  function schedule(target, builder, delay, preferBelow){
    cancel();
    timerId = window.setTimeout(function(){
      show(target, builder, preferBelow);
    }, typeof delay === 'number' ? delay : RCA_TOOLTIP_DELAY);
  }

  window.addEventListener('scroll', function(){
    if(anchor){
      position(anchor);
    }
  }, { passive: true });

  window.addEventListener('resize', function(){
    if(anchor){
      position(anchor);
    }
  });

  return {
    schedule,
    show,
    hide,
    element,
    get anchor(){ return anchor; },
    get builder(){ return currentBuilder; }
  };
}

function normalizeHotkeyKey(rawKey){
  if(!rawKey){
    return '';
  }
  const key = rawKey.toLowerCase();
  const map = {
    'т': 'n',
    'с': 'c',
    'а': 'f',
    'ч': 'x',
    'ф': 'a',
    'я': 'z',
    'у': 'e',
    'к': 'r',
    'в': 'd',
    'ы': 's',
    'й': 'q'
  };
  return map[key] || key;
}

function snapRangeValue(control, raw){
  if(!control){
    return raw;
  }
  const min = parseInt(control.min || '1', 10) || 1;
  const max = parseInt(control.max || '3', 10) || 3;
  let numeric = parseFloat(raw);
  if(Number.isNaN(numeric)){
    numeric = min;
  }
  const snapped = Math.min(max, Math.max(min, Math.round(numeric)));
  return String(snapped);
}

function snapSlider(control, event, setter){
  if(!control || !setter){
    return;
  }
  if(event.clientX === 0 && event.clientY === 0){
    return;
  }
  const rect = control.getBoundingClientRect();
  const width = rect.width;
  if(width <= 0){
    return;
  }
  const min = parseInt(control.min || '1', 10) || 1;
  const max = parseInt(control.max || '3', 10) || 3;
  const ratio = Math.min(Math.max((event.clientX - rect.left) / width, 0), 1);
  const numeric = ratio * (max - min) + min;
  const snapped = snapRangeValue(control, numeric);
  control.value = snapped;
  setter(snapped);
}
";
}


