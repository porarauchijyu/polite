import styles from './ArticleCard.module.css';

interface ArticleCardProps {
  title: string;
  link: string;
  description: string;
  date: string;
  source: string;
}

export default function ArticleCard({ title, link, description, date, source }: ArticleCardProps) {
  const formattedDate = new Date(date).toLocaleDateString('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });

  return (
    <article className={styles.card}>
      <div className={styles.header}>
        <span className={styles.source}>{source}</span>
        <time className={styles.date}>{formattedDate}</time>
      </div>
      <h2 className={styles.title}>
        <a href={link} target="_blank" rel="noopener noreferrer">
          {title}
        </a>
      </h2>
      {description && <p className={styles.description}>{description}</p>}
    </article>
  );
}
