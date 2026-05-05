'use client';

import { useState, useEffect } from 'react';
import SiteModal from '@/components/SiteModal';

interface Target {
  Id: number;
  Name: string;
  Url: string;
  ContainerSelector?: string;
  TitleSelector?: string;
}

export default function SitesPage() {
  const [targets, setTargets] = useState<Target[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSite, setEditingSite] = useState<Target | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchTargets();
  }, []);

  const fetchTargets = async () => {
    try {
      // basePath (/polite) を考慮したパス
      const res = await fetch('/polite/data/targets.json');
      if (res.ok) {
        const data = await res.json();
        setTargets(data);
      }
    } catch (error) {
      console.error('Error fetching targets:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleEdit = (site: Target) => {
    setEditingSite(site);
    setIsModalOpen(true);
  };

  const handleAdd = () => {
    setEditingSite(null);
    setIsModalOpen(true);
  };

  return (
    <div>
      <section style={{ marginBottom: '2rem', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
        <div>
          <h1 style={{ fontSize: '2rem', fontWeight: 800, marginBottom: '0.5rem' }}>
            Registered Sites
          </h1>
          <p style={{ color: 'var(--text-secondary)' }}>
            Manage your RSS sources and monitoring status.
          </p>
        </div>
        <button 
          onClick={handleAdd}
          style={{ 
            backgroundColor: 'var(--accent-color)', 
            color: 'white', 
            border: 'none', 
            padding: '0.75rem 1.5rem', 
            borderRadius: '8px', 
            fontWeight: 600,
            cursor: 'pointer'
          }}
        >
          + Add New Site
        </button>
      </section>

      {isLoading ? (
        <p>Loading sites...</p>
      ) : (
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
                <button 
                  onClick={() => handleEdit(target)}
                  style={{ 
                    flex: 1,
                    fontSize: '0.8rem', 
                    color: 'var(--text-primary)', 
                    border: '1px solid var(--border-color)', 
                    padding: '0.5rem', 
                    borderRadius: '4px',
                    backgroundColor: 'transparent',
                    cursor: 'pointer'
                  }}
                >
                  Edit Settings
                </button>
                <a 
                  href={target.Url} 
                  target="_blank" 
                  rel="noopener noreferrer"
                  style={{ fontSize: '0.8rem', color: 'var(--accent-color)', border: '1px solid var(--accent-color)', padding: '0.5rem 1rem', borderRadius: '4px' }}
                >
                  Visit
                </a>
              </div>
            </div>
          ))}
        </div>
      )}

      <SiteModal 
        isOpen={isModalOpen} 
        onClose={() => setIsModalOpen(false)} 
        site={editingSite} 
      />
    </div>
  );
}
