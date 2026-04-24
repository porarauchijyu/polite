(function() {
    if (window.__selectorGadgetLoaded) return;
    window.__selectorGadgetLoaded = true;

    // --- UI Structure ---
    const container = document.createElement('div');
    container.id = 'rss-selector-gadget';
    container.style.cssText = `
        position: fixed; top: 10px; right: 10px; z-index: 999999;
        background: #1e293b; color: white; padding: 15px; border-radius: 8px;
        box-shadow: 0 4px 15px rgba(0,0,0,0.5); font-family: sans-serif;
        width: 320px; border: 1px solid #334155;
    `;

    container.innerHTML = `
        <div style="font-weight: bold; margin-bottom: 10px; border-bottom: 1px solid #334155; padding-bottom: 5px;">
            RSS Selector Tool (Premium)
        </div>
        <div id="msg-area" style="font-size: 12px; color: #94a3b8; margin-bottom: 10px;">
            選択したい項目を選んでから、ページ上の要素をクリックしてください。
        </div>
        <div id="match-info" style="font-size: 12px; color: #10b981; margin-bottom: 10px; font-weight: bold; background: rgba(16, 185, 129, 0.1); padding: 5px; border-radius: 4px; display: none;">
            現在のマッチ数: <span id="matched-count">0</span> 件
        </div>
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px;">
            <button id="btn-container" class="gadget-btn" title="記事1つ分を包む共通の枠を選択">ニュースの枠</button>
            <button id="btn-title" class="gadget-btn" title="記事のタイトルを選択">タイトル</button>
            <button id="btn-desc" class="gadget-btn" title="記事の概要（省略可）">概要</button>
            <button id="btn-date" class="gadget-btn" title="投稿日（省略可）">日付</button>
        </div>
        <div style="margin-top: 15px; padding-top: 10px; border-top: 1px solid #334155;">
            <button id="btn-done" style="width: 100%; padding: 10px; background: #0ea5e9; border: none; color: white; border-radius: 4px; cursor: pointer; font-weight: bold; opacity: 0.5;" disabled>設定を保存して終了</button>
        </div>
    `;

    const style = document.createElement('style');
    style.innerHTML = `
        .gadget-btn { background: #334155; border: none; color: white; padding: 8px; border-radius: 4px; cursor: pointer; font-size: 13px; margin-bottom: 2px; }
        .gadget-btn.active { background: #0ea5e9; font-weight: bold; border: 1px solid white; }
        .gadget-btn:hover { background: #475569; }
        .rss-highlight { outline: 2px dashed #0ea5e9 !important; outline-offset: -2px; cursor: crosshair !important; }
        .rss-selected-container { outline: 3px solid #10b981 !important; box-shadow: 0 0 10px #10b981 !important; }
        .rss-selected-title { background: rgba(14, 165, 233, 0.4) !important; outline: 2px solid #0ea5e9 !important; }
        .rss-selected-desc { background: rgba(245, 158, 11, 0.3) !important; }
        .rss-selected-date { background: rgba(139, 92, 246, 0.3) !important; }
    `;

    document.head.appendChild(style);
    document.body.appendChild(container);

    const doneBtn = document.getElementById('btn-done');
    let currentTarget = null; 
    const results = {
        container: null,
        title: null,
        desc: null,
        date: null
    };

    // --- Logic ---
    const updateDoneButton = () => {
        if (results.title || results.container) {
            doneBtn.disabled = false;
            doneBtn.style.opacity = "1";
            doneBtn.style.cursor = "pointer";
            doneBtn.innerText = "設定を保存して終了 ✨";
        }
    };

    const getSelector = (el) => {
        if (!el) return null;
        
        let selector = el.tagName.toLowerCase();
        
        // 繰り返し要素を特定するため、IDよりもクラス（共通性）を優先する
        if (el.classList.length > 0) {
            const classes = Array.from(el.classList).filter(c => !c.startsWith('rss-') && !c.includes('active')).join('.');
            if (classes) {
                selector += '.' + classes;
                return selector; 
            }
        }
        
        if (el.id) return '#' + el.id;

        // 親方向を見てさらに絞る
        if (el.parentNode && el.parentNode.tagName && el.parentNode !== document.body) {
            const p = el.parentNode;
            if (p.classList.length > 0) {
                const pc = Array.from(p.classList)[0];
                return p.tagName.toLowerCase() + '.' + pc + ' ' + selector;
            }
        }

        return selector;
    };

    const updateBtns = () => {
        ['container', 'title', 'desc', 'date'].forEach(k => {
            const btn = document.getElementById('btn-' + k);
            btn.classList.toggle('active', currentTarget === k);
            
            // 選択済みならチェックマークを表示
            const originalText = { container: 'ニュースの枠', title: 'タイトル', desc: '概要', date: '日付' }[k];
            btn.innerText = results[k] ? '✅ ' + originalText : originalText;
            
            if (results[k]) btn.style.border = '1px solid #10b981';
        });
    };


    document.querySelectorAll('.gadget-btn').forEach(btn => {
        btn.onclick = (e) => {
            currentTarget = e.target.id.replace('btn-', '');
            updateBtns();
            document.getElementById('msg-area').innerText = e.target.title;
        };
    });

    document.getElementById('btn-done').onclick = () => {
        console.log("[SelectorGadget] Finishing with results:", results);
        doneBtn.innerText = "Saving...";
        doneBtn.style.background = "#10b981";
        if (window.onConfigDone) {
            window.onConfigDone(JSON.stringify(results)).catch(err => {
                alert("通信エラーが発生しました: " + err);
                doneBtn.innerText = "Retry";
                doneBtn.disabled = false;
            });
        }
    };

    // Hover Highlight
    document.addEventListener('mouseover', (e) => {
        if (!currentTarget || container.contains(e.target)) return;
        e.target.classList.add('rss-highlight');
    });

    document.addEventListener('mouseout', (e) => {
        e.target.classList.remove('rss-highlight');
    });

    // Click Selection & Navigation Block
    document.addEventListener('click', (e) => {
        if (container.contains(e.target)) return;
        
        // 常にリンク移動等を防止
        e.preventDefault();
        e.stopPropagation();

        if (!currentTarget) {
            console.log("[SelectorGadget] Please select a category (Title, Date, etc.) first.");
            return;
        }

        const sel = getSelector(e.target);
        results[currentTarget] = sel;
        
        // マッチ数の表示
        try {
            const count = document.querySelectorAll(sel).length;
            const info = document.getElementById('match-info');
            const countSpan = document.getElementById('matched-count');
            info.style.display = 'block';
            countSpan.innerText = count;
            
            if (count === 1 && (currentTarget === 'container' || currentTarget === 'title')) {
                countSpan.style.color = '#f43f5e'; // 1件のみは警告色
                countSpan.innerText += " (注: 1件のみです。他の箇所を試してください)";
            } else {
                countSpan.style.color = '#10b981';
            }
        } catch(err) { console.error(err); }

        // Visual Feedback
        document.querySelectorAll('.rss-selected-' + currentTarget).forEach(el => el.classList.remove('rss-selected-' + currentTarget));
        e.target.classList.add('rss-selected-' + currentTarget);
        
        updateBtns();
        updateDoneButton();
    }, true);

    function LogToConsole(msg) {
        console.log("[SelectorGadget] " + msg);
    }
})();
