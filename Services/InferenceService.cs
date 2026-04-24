using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace RssGenerator.Services
{
    public class InferenceResult
    {
        public string TitleSelector { get; set; }
        public string LinkSelector { get; set; }
        public string ContainerSelector { get; set; } // [NEW] アイテム一覧の親セレクタ
        public string DescriptionSelector { get; set; } // [NEW] 説明文セレクタ
        public string DateSelector { get; set; } // [NEW] 日付セレクタ
        public double Score { get; set; }
        public string SampleTitle { get; set; }
        public List<string> Samples { get; set; } = new List<string>();
        public string PageTitle { get; set; } // ページの <title> 
    }

    public class InferenceService
    {
        private readonly CrawlerService _crawler;

        public InferenceService(CrawlerService crawler)
        {
            _crawler = crawler;
        }

        public async Task<List<InferenceResult>> InferSelectorsAsync(string url, int waitMs = 3000)
        {
            Console.WriteLine($"[Inference] {url} を多角的に解析しています (wait={waitMs}ms)...");
            
            string script = @"
(() => {
    const links = Array.from(document.querySelectorAll('a'));
    const dateRegex = /[0-9]{4}[.\/\-年][0-9]{1,2}[.\/\-月][0-9]{1,2}日?/;
    
    // セレクタ生成ヘルパー
    const getSimpleSelector = (el) => {
        if (!el || el === document.body) return 'body';
        let sig = el.tagName.toLowerCase();
        if (el.id) return sig + '#' + el.id;
        if (el.className) {
            const classes = Array.from(el.classList).filter(c => !c.includes('active') && !c.includes('hover')).join('.');
            if (classes) sig += '.' + classes;
        }
        return sig;
    };

    const candidates = [];
    const sigCache = new Map();

    links.forEach(link => {
        if (!link.href || link.href.startsWith('#') || link.href.startsWith('javascript:')) return;
        const text = link.innerText.trim();
        if (text.length < 2) return;

        let node = link;
        let depth = 0;
        const potentialBlocks = [];

        while (node && node !== document.body && depth < 6) {
            const sig = getSimpleSelector(node);
            if (!sigCache.has(sig)) {
                try {
                    const instances = document.querySelectorAll(sig);
                    sigCache.set(sig, { count: instances.length, selector: sig });
                } catch(e) {
                    sigCache.set(sig, { count: 0, selector: sig });
                }
            }
            
            const info = sigCache.get(sig);
            if (info.count >= 2 && info.count <= 500) {
                const hasDate = dateRegex.test(node.textContent);
                potentialBlocks.push({
                    selector: sig,
                    count: info.count,
                    hasDate: hasDate,
                    node: node,
                    depth: depth
                });
            }
            node = node.parentNode;
            depth++;
        }

        if (potentialBlocks.length > 0) {
            potentialBlocks.sort((a, b) => {
                if (a.hasDate !== b.hasDate) return b.hasDate ? 1 : -1;
                return a.depth - b.depth; 
            });

            const best = potentialBlocks[0];
            candidates.push({
                link: link,
                text: text,
                blockSelector: best.selector,
                hasDate: best.hasDate,
                blockNode: best.node
            });
        }
    });

    const clusters = {};
    candidates.forEach(c => {
        const key = c.blockSelector;
        if (!clusters[key]) {
            clusters[key] = {
                selector: key,
                nodes: [],
                titles: [],
                blockNodes: [],
                dateMatchCount: 0,
                total: 0
            };
        }
        clusters[key].nodes.push(c.link);
        clusters[key].titles.push(c.text);
        clusters[key].blockNodes.push(c.blockNode);
        clusters[key].total++;
        if (c.hasDate) clusters[key].dateMatchCount++;
    });

    const results = Object.values(clusters).map(c => {
        let score = 0;
        const dateRatio = c.dateMatchCount / c.total;

        if (dateRatio === 0) {
            score = -1000;
        } else {
            score = 600; // Base news block score
            score += (dateRatio * 200);
        }

        const avgLen = c.titles.reduce((a, b) => a + b.length, 0) / c.total;
        score += Math.min(avgLen, 50);

        // コンテナの特定 (共通の親)
        let containerSelector = '';
        if (c.blockNodes.length > 0) {
            const parent = c.blockNodes[0].parentNode;
            if (parent && parent !== document.body) {
                containerSelector = getSimpleSelector(parent);
                // コンテナが重要そうな単語を含んでいるか
                const lowCont = containerSelector.toLowerCase();
                if (lowCont.includes('news') || lowCont.includes('list') || lowCont.includes('topic') || lowCont.includes('main')) {
                    score += 100;
                }
            }
        }

        const lowSel = c.selector.toLowerCase();
        if (lowSel.includes('news')) score += 50;
        if (lowSel.includes('item')) score += 30;

        let finalSelector = c.selector;
        // セレクタが 'a' そのものでないなら、コンテナからの相対として ' a' を付与する場合があるが
        // ExtractorはQuerySelectorAll(linkSelector)をroot(container)に対して行うので、
        // コンテナ内での相対パスにする必要がある。
        // 現在の c.selector はグローバルなタグ.クラスなので、そのまま使える。
        if (!finalSelector.match(/\ba\b/)) {
            finalSelector += ' a';
        }

        return {
            TitleSelector: 'a', // コンテナ内のリンク要素を探す
            LinkSelector: 'a',
            ContainerSelector: c.selector, // これが「記事1件分の枠」になる
            Score: score,
            SampleTitle: c.titles[0],
            Samples: c.titles.slice(0, 3)
        };
    });

    const filteredResults = results.filter(r => r.Score > 0);
    filteredResults.sort((a, b) => b.Score - a.Score);

    const uniqueResults = [];
    const seen = new Set();
    filteredResults.forEach(r => {
        const key = r.ContainerSelector + ' | ' + r.TitleSelector;
        if (!seen.has(key) && uniqueResults.length < 5) {
            seen.add(key);
            uniqueResults.push(r);
        }
    });

    if (uniqueResults.length > 0) {
        uniqueResults.forEach(r => r.PageTitle = document.title);
        return JSON.stringify(uniqueResults);
    }
    return null;
})()
";
            try
            {
                var jsonResult = await _crawler.EvaluateAsync<string>(url, script, waitMs);
                if (string.IsNullOrEmpty(jsonResult)) return new List<InferenceResult>();

                var jArray = JArray.Parse(jsonResult);
                return jArray.ToObject<List<InferenceResult>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Inference] 推論エラー: {ex.Message}");
                return new List<InferenceResult>();
            }
        }
    }
}

