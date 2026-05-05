import fs from 'fs-extra';
import path from 'path';

interface Target {
  Id: number;
  Name: string;
  Url: string;
}

async function getTargets(): Promise<Target[]> {
  const filePath = path.join(process.cwd(), '../targets.json');
  try {
    if (await fs.pathExists(filePath)) {
      return await fs.readJson(filePath);
    }
  } catch (error) {
    console.error('Error loading targets:', error);
  }
  return [];
}

export default async function SitesPage() {
  const targets = await getTargets();

  return (
    <div>
      <section style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 800, marginBottom: '0.5rem' }}>
          Registered Sites
        </h1>
        <p style={{ color: 'var(--text-secondary)' }}>
          Manage your RSS sources and monitoring status.
        </p>
      </section>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '1rem' }}>
        {targets.map((target) => (
          <div key={target.Id} style={{ 
            backgroundColor: 'var(--card-bg)', 
            border: '1px solid var(--border-color)', 
            borderRadius: '12px', 
            padding: '1.5rem' 
          }}>
            <h3 style={{ marginBottom: '0.5rem' }}>{target.Name || 'Unnamed Site'}</h3>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '1rem', wordBreak: 'break-all' }}>
              {target.Url}
            </p>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <a 
                href={target.Url} 
                target="_blank" 
                rel="noopener noreferrer"
                style={{ fontSize: '0.8rem', color: 'var(--accent-color)', border: '1px solid var(--accent-color)', padding: '0.25rem 0.75rem', borderRadius: '4px' }}
              >
                Visit Site
              </a>
              <a 
                href={`/feeds/${(target.Name || 'unknown').replace(/[^a-z0-9]/gi, '_').toLowerCase()}.xml`} 
                style={{ fontSize: '0.8rem', color: 'var(--text-primary)', backgroundColor: 'var(--border-color)', padding: '0.25rem 0.75rem', borderRadius: '4px' }}
              >
                RSS Feed
              </a>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
