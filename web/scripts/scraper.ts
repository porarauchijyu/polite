import { chromium } from 'playwright';
import type { Page } from 'playwright';
import fs from 'fs-extra';
import path from 'path';
import RSS from 'rss';
import { format } from 'date-fns';
import { JSDOM } from 'jsdom';

const TARGETS_PATH = path.join(process.cwd(), '../targets.json');
const OUTPUT_DATA_PATH = path.join(process.cwd(), 'public/data/articles.json');
const FEEDS_DIR = path.join(process.cwd(), 'public/feeds');

interface Target {
  Name: string;
  Url: string;
  ContainerSelector?: string;
  TitleSelector?: string;
  LinkSelector?: string;
  DescriptionSelector?: string;
  DateSelector?: string;
}

interface Article {
  title: string;
  link: string;
  description: string;
  date: string;
  source: string;
}

async function ensureDirs() {
  await fs.ensureDir(path.dirname(OUTPUT_DATA_PATH));
  await fs.ensureDir(FEEDS_DIR);
}

function normalizeUrl(baseUrl: string, relativeUrl: string): string {
  try {
    return new URL(relativeUrl, baseUrl).toString();
  } catch {
    return relativeUrl;
  }
}

function cleanText(text: string): string {
  return text?.replace(/[\r\n\t]+/g, ' ').trim() || '';
}

async function inferSelectors(page: Page) {
  // 既存の InferenceService.cs 内のスクリプトを移植/再利用
  const script = `
    (() => {
      const links = Array.from(document.querySelectorAll('a'));
      const dateRegex = /[0-9]{4}[.\\/\\-年][0-9]{1,2}[.\\/\\-月][0-9]{1,2}日?/;
      
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

      const clusters = {};
      links.forEach(link => {
          const text = link.innerText.trim();
          if (text.length < 5 || !link.href || link.href.startsWith('#')) return;

          let node = link;
          let depth = 0;
          while (node && node !== document.body && depth < 5) {
              const sig = getSimpleSelector(node);
              if (!clusters[sig]) clusters[sig] = { selector: sig, count: 0, texts: [], nodes: [], hasDate: false };
              clusters[sig].count++;
              clusters[sig].texts.push(text);
              clusters[sig].nodes.push(node);
              if (dateRegex.test(node.textContent || '')) clusters[sig].hasDate = true;
              node = node.parentNode;
              depth++;
          }
      });

      return Object.values(clusters)
          .filter((c) => c.count >= 3 && c.count <= 100)
          .map((c) => ({
              ContainerSelector: c.selector,
              TitleSelector: 'a',
              LinkSelector: 'a',
              Score: (c.hasDate ? 100 : 0) + Math.min(c.count, 20)
          }))
          .sort((a, b) => b.Score - a.Score)[0];
    })()
  `;
  return await page.evaluate(script);
}

async function scrape() {
  console.log('Starting scraper...');
  await ensureDirs();

  if (!await fs.pathExists(TARGETS_PATH)) {
    console.error('targets.json not found');
    return;
  }

  const targets: Target[] = await fs.readJson(TARGETS_PATH);
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36'
  });

  const allArticles: Article[] = [];
  const junkWords = ['戻る', 'トップ', '閉じる', 'メニュー', 'menu', '一覧', 'pagetop', 'ホーム'];

  for (const target of targets) {
    const siteName = target.Name || new URL(target.Url).hostname;
    console.log(`Scraping: ${siteName}`);
    const page = await context.newPage();
    try {
      await page.goto(target.Url, { waitUntil: 'networkidle', timeout: 45000 });
      
      let containerSelector = target.ContainerSelector;
      let titleSelector = target.TitleSelector || 'a';
      let linkSelector = target.LinkSelector || 'a';

      if (!containerSelector) {
        console.log(`  Inferring selectors for ${siteName}...`);
        const inferred = await inferSelectors(page) as any;
        if (inferred) {
          containerSelector = inferred.ContainerSelector;
          titleSelector = inferred.TitleSelector;
          linkSelector = inferred.LinkSelector;
          console.log(`  Inferred: ${containerSelector}`);
        }
      }

      if (containerSelector) {
        const containers = await page.$$(containerSelector);
        console.log(`  Found ${containers.length} containers for ${siteName}`);
        for (const container of containers) {
          let titleEl = await container.$(titleSelector);
          let linkEl = await container.$(linkSelector);
          
          const tagName = await page.evaluate(el => el.tagName.toLowerCase(), container);
          if (tagName === 'a') {
            if (!linkEl) linkEl = container;
            if (!titleEl) titleEl = container;
          }

          if (titleEl && linkEl) {
            const title = cleanText(await titleEl.innerText());
            const href = await linkEl.getAttribute('href');
            const link = href ? normalizeUrl(target.Url, href) : '';

            // フィルタリング
            const isJunk = junkWords.some(word => title.toLowerCase().includes(word));
            const isTooShort = title.length < 5;

            if (title && link && !isJunk && !isTooShort) {
              if (!allArticles.some(a => a.link === link)) {
                allArticles.push({
                  title,
                  link,
                  description: '',
                  date: new Date().toISOString(),
                  source: siteName
                });
              }
            }
          }
        }
      }

      // RSS Feed 出力
      const feed = new RSS({
        title: siteName,
        feed_url: `${target.Url}/rss.xml`,
        site_url: target.Url,
      });

      allArticles.filter(a => a.source === siteName).forEach(a => {
        feed.item({
          title: a.title,
          url: a.link,
          description: a.description,
          date: a.date
        });
      });

      const safeName = siteName.replace(/[^a-z0-9]/gi, '_').toLowerCase();
      await fs.writeFile(path.join(FEEDS_DIR, `${safeName}.xml`), feed.xml({ indent: true }));

    } catch (error) {
      console.error(`  Error scraping ${target.Name}:`, error);
    } finally {
      await page.close();
    }
  }

  await browser.close();
  
  // 最新順にソートして保存
  allArticles.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  await fs.writeJson(OUTPUT_DATA_PATH, allArticles, { spaces: 2 });
  
  console.log(`Scraping completed. Total articles: ${allArticles.length}`);
}

scrape().catch(console.error);
