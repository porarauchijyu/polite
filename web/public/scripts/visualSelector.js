(function() {
  let hoveredElement = null;

  // ハイライト用のスタイルを注入
  const style = document.createElement('style');
  style.innerHTML = `
    .ps-highlight {
      outline: 2px solid #3b82f6 !important;
      background-color: rgba(59, 130, 246, 0.2) !important;
      cursor: crosshair !important;
    }
  `;
  document.head.appendChild(style);

  // 最適なセレクタを生成する関数
  function getSelector(el) {
    if (el.id) return `#${el.id}`;
    
    let path = [];
    while (el.nodeType === Node.ELEMENT_NODE) {
      let selector = el.nodeName.toLowerCase();
      if (el.className) {
        const classes = Array.from(el.classList)
          .filter(c => !c.includes('ps-highlight'))
          .join('.');
        if (classes) selector += `.${classes}`;
      }
      path.unshift(selector);
      el = el.parentNode;
      if (!el || el.nodeName === 'BODY' || el.nodeName === 'HTML') break;
    }
    return path.join(' > ');
  }

  document.addEventListener('mouseover', (e) => {
    if (hoveredElement) hoveredElement.classList.remove('ps-highlight');
    hoveredElement = e.target;
    hoveredElement.classList.add('ps-highlight');
  }, true);

  document.addEventListener('click', (e) => {
    e.preventDefault();
    e.stopPropagation();
    
    const selector = getSelector(e.target);
    const text = e.target.innerText.trim();
    
    // 親ウィンドウに結果を送信
    window.parent.postMessage({
      type: 'SELECTOR_SELECTED',
      selector: selector,
      text: text
    }, '*');
  }, true);

  console.log('Polite RSS Visual Selector Active');
})();
