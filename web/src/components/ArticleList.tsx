'use client';

import { useState, useMemo } from 'react';
import ArticleCard from './ArticleCard';
import styles from './ArticleList.module.css';

interface Article {
  title: string;
  link: string;
  description: string;
  date: string;
  source: string;
}

interface ArticleListProps {
  initialArticles: Article[];
}

export default function ArticleList({ initialArticles }: ArticleListProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedSource, setSelectedSource] = useState('All');

  const sources = useMemo(() => {
    const s = new Set(initialArticles.map(a => a.source));
    return ['All', ...Array.from(s)];
  }, [initialArticles]);

  const filteredArticles = useMemo(() => {
    return initialArticles.filter(article => {
      const matchesSearch = article.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          article.source.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesSource = selectedSource === 'All' || article.source === selectedSource;
      return matchesSearch && matchesSource;
    });
  }, [initialArticles, searchQuery, selectedSource]);

  return (
    <div>
      <div className={styles.controls}>
        <input
          type="text"
          placeholder="Search articles..."
          className={styles.searchInput}
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
        <select
          className={styles.sourceSelect}
          value={selectedSource}
          onChange={(e) => setSelectedSource(e.target.value)}
        >
          {sources.map(source => (
            <option key={source} value={source}>{source}</option>
          ))}
        </select>
      </div>

      <div className={styles.list}>
        {filteredArticles.length > 0 ? (
          filteredArticles.map((article, index) => (
            <ArticleCard
              key={`${article.link}-${index}`}
              {...article}
            />
          ))
        ) : (
          <p className={styles.empty}>No articles match your criteria.</p>
        )}
      </div>
    </div>
  );
}
