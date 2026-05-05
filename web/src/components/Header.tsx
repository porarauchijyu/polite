import Link from 'next/link';
import styles from './Header.module.css';

export default function Header() {
  return (
    <header className={styles.header}>
      <div className={styles.container}>
        <Link href="/" className={styles.logo}>
          Polite RSS
        </Link>
        <nav className={styles.nav}>
          <Link href="/" className={styles.navLink}>Home</Link>
          <Link href="/sites" className={styles.navLink}>Sites</Link>
          <Link href="/settings" className={styles.navLink}>Settings</Link>
        </nav>
      </div>
    </header>
  );
}
