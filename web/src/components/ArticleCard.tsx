'use client';

import { useState, useEffect } from 'react';
import styles from './ArticleCard.module.css';

interface ArticleCardProps {
  title: string;
  link: string;
  description: string;
  date: string;
  source: string;
}

export default function ArticleCard({ title, link, description, date, source }: ArticleCardProps) {
  const [isRead, setIsRead] = useState(false);

  useEffect(() => {
    const readArticles = JSON.parse(localStorage.getItem('readArticles') || '[]');
    if (readArticles.includes(link)) {
      setIsRead(true);
    }
  }, [link]);

  const handleLinkClick = () => {
    const readArticles = JSON.parse(localStorage.getItem('readArticles') || '[]');
    if (!readArticles.includes(link)) {
      readArticles.push(link);
      localStorage.setItem('readArticles', JSON.stringify(readArticles));
      setIsRead(true);
    }
  };

  const formattedDate = new Date(date).toLocaleDateString('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });

  return (
    <article className={`${styles.card} ${isRead ? styles.read : ''}`}>
      <div className={styles.header}>
        <span className={styles.source}>{source}</span>
        <time className={styles.date}>{formattedDate}</time>
      </div>
      <h2 className={styles.title}>
        <a href={link} target="_blank" rel="noopener noreferrer" onClick={handleLinkClick}>
          {title}
        </a>
      </h2>
      {description && <p className={styles.description}>{description}</p>}
    </article>
  );
}
