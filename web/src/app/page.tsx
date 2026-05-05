import fs from 'fs-extra';
import path from 'path';
import ArticleList from '@/components/ArticleList';

interface Article {
  title: string;
  link: string;
  description: string;
  date: string;
  source: string;
}

async function getArticles(): Promise<Article[]> {
  const filePath = path.join(process.cwd(), 'public/data/articles.json');
  try {
    if (await fs.pathExists(filePath)) {
      return await fs.readJson(filePath);
    }
  } catch (error) {
    console.error('Error loading articles:', error);
  }
  return [];
}

export default async function Home() {
  const articles = await getArticles();

  return (
    <div>
      <section style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 800, marginBottom: '0.5rem' }}>
          Latest Updates
        </h1>
        <p style={{ color: 'var(--text-secondary)' }}>
          Unified feed from all your subscribed sources.
        </p>
      </section>

      <ArticleList initialArticles={articles} />
    </div>
  );
}
