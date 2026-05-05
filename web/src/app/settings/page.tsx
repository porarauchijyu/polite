'use client';

import { useState, useEffect } from 'react';
import { triggerScraperWorkflow } from '@/lib/github';

export default function SettingsPage() {
  const [token, setToken] = useState('');
  const [status, setStatus] = useState('');

  useEffect(() => {
    setToken(localStorage.getItem('gh_pat') || '');
  }, []);

  const handleSave = () => {
    localStorage.setItem('gh_pat', token);
    setStatus('Token saved successfully!');
    setTimeout(() => setStatus(''), 3000);
  };

  const handleManualRun = async () => {
    try {
      setStatus('Triggering scraper...');
      await triggerScraperWorkflow();
      setStatus('Scraper triggered! Check GitHub Actions tab.');
    } catch (error: any) {
      setStatus(`Error: ${error.message}`);
    }
    setTimeout(() => setStatus(''), 5000);
  };

  return (
    <div style={{ maxWidth: '600px' }}>
      <section style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 800, marginBottom: '0.5rem' }}>
          Settings
        </h1>
        <p style={{ color: 'var(--text-secondary)' }}>
          Configure your GitHub integration for management features.
        </p>
      </section>

      <div style={{ backgroundColor: 'var(--card-bg)', border: '1px solid var(--border-color)', borderRadius: '12px', padding: '2rem' }}>
        <div style={{ marginBottom: '2rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.9rem' }}>
            GitHub Personal Access Token (classic)
          </label>
          <input
            type="password"
            value={token}
            onChange={(e) => setToken(e.target.value)}
            placeholder="ghp_xxxxxxxxxxxx"
            style={{ 
              width: '100%', 
              backgroundColor: 'var(--bg-color)', 
              border: '1px solid var(--border-color)', 
              color: 'var(--text-primary)', 
              padding: '0.75rem', 
              borderRadius: '8px',
              marginBottom: '1rem'
            }}
          />
          <p style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginBottom: '1.5rem' }}>
            Required for: Saving site configurations and manual scraping. 
            Permissions needed: <code>repo</code>, <code>workflow</code>.
          </p>
          <button 
            onClick={handleSave}
            style={{ backgroundColor: 'var(--accent-color)', color: 'white', border: 'none', padding: '0.75rem 1.5rem', borderRadius: '8px', fontWeight: 600, cursor: 'pointer' }}
          >
            Save Token
          </button>
        </div>

        <hr style={{ border: 'none', borderTop: '1px solid var(--border-color)', margin: '2rem 0' }} />

        <div>
          <h3 style={{ marginBottom: '1rem' }}>Manual Actions</h3>
          <button 
            onClick={handleManualRun}
            style={{ backgroundColor: 'transparent', color: 'var(--text-primary)', border: '1px solid var(--border-color)', padding: '0.75rem 1.5rem', borderRadius: '8px', fontWeight: 600, cursor: 'pointer' }}
          >
            Run Scraper Now
          </button>
        </div>

        {status && (
          <div style={{ marginTop: '1.5rem', padding: '1rem', borderRadius: '8px', backgroundColor: 'rgba(59, 130, 246, 0.1)', color: 'var(--accent-color)', fontSize: '0.9rem' }}>
            {status}
          </div>
        )}
      </div>
    </div>
  );
}
