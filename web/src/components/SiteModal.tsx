'use client';

import { useState, useEffect, useRef } from 'react';
import styles from './SiteModal.module.css';
import { saveTargetsToGithub } from '@/lib/github';

interface SiteModalProps {
  isOpen: boolean;
  onClose: () => void;
  site?: any;
}

export default function SiteModal({ isOpen, onClose, site }: SiteModalProps) {
  const [url, setUrl] = useState('');
  const [name, setName] = useState('');
  const [containerSelector, setContainerSelector] = useState('');
  const [titleSelector, setTitleSelector] = useState('');
  const [isVisualMode, setIsVisualMode] = useState(false);
  const [selectionTarget, setSelectionTarget] = useState<'container' | 'title'>('container');
  const [isSaving, setIsSaving] = useState(false);
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    if (site) {
      setUrl(site.Url || '');
      setName(site.Name || '');
      setContainerSelector(site.ContainerSelector || '');
      setTitleSelector(site.TitleSelector || '');
    } else {
      setUrl('');
      setName('');
      setContainerSelector('');
      setTitleSelector('');
    }
  }, [site, isOpen]);

  const handleSave = async () => {
    setIsSaving(true);
    try {
      // 現在の全サイトデータを再取得
      const res = await fetch('/polite/data/targets.json');
      let currentTargets = [];
      if (res.ok) {
        currentTargets = await res.json();
      } else {
        // ローカル開発用フォールバック
        const resLocal = await fetch('/data/targets.json');
        if (resLocal.ok) currentTargets = await resLocal.json();
      }

      const newSite = {
        Id: site ? site.Id : Date.now(),
        Name: name,
        Url: url,
        ContainerSelector: containerSelector,
        TitleSelector: titleSelector,
        LinkSelector: titleSelector,
        DescriptionSelector: "",
        DateSelector: ""
      };

      let updatedTargets;
      if (site) {
        updatedTargets = currentTargets.map((t: any) => t.Id === site.Id ? newSite : t);
      } else {
        updatedTargets = [...currentTargets, newSite];
      }

      await saveTargetsToGithub(updatedTargets);
      alert('Settings saved to GitHub! Deploy started.');
      onClose();
      window.location.reload();
    } catch (error: any) {
      alert(`Save Error: ${error.message}`);
    } finally {
      setIsSaving(false);
    }
  };

  useEffect(() => {
    const handleMessage = (event: MessageEvent) => {
      if (event.data.type === 'SELECTOR_SELECTED') {
        if (selectionTarget === 'container') {
          setContainerSelector(event.data.selector);
        } else {
          setTitleSelector(event.data.selector);
        }
        setIsVisualMode(false);
      }
    };
    window.addEventListener('message', handleMessage);
    return () => window.removeEventListener('message', handleMessage);
  }, [selectionTarget]);

  if (!isOpen) return null;

  return (
    <div className={styles.overlay}>
      <div className={`${styles.modal} ${isVisualMode ? styles.visualExpanded : ''}`}>
        <div className={styles.header}>
          <h2>{site ? 'Edit Site' : 'Add New Site'}</h2>
          <button className={styles.closeBtn} onClick={onClose}>&times;</button>
        </div>

        <div className={styles.content}>
          <div className={styles.formSide}>
            <div className={styles.field}>
              <label>Target URL</label>
              <input type="text" value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://..." />
            </div>
            <div className={styles.field}>
              <label>Site Name</label>
              <input type="text" value={name} onChange={(e) => setName(e.target.value)} placeholder="My News Site" />
            </div>
            <div className={styles.field}>
              <label>Container Selector</label>
              <div className={styles.inputGroup}>
                <input type="text" value={containerSelector} onChange={(e) => setContainerSelector(e.target.value)} />
                <button onClick={() => { setIsVisualMode(true); setSelectionTarget('container'); }}>Pick</button>
              </div>
            </div>
            <div className={styles.field}>
              <label>Title Selector</label>
              <div className={styles.inputGroup}>
                <input type="text" value={titleSelector} onChange={(e) => setTitleSelector(e.target.value)} />
                <button onClick={() => { setIsVisualMode(true); setSelectionTarget('title'); }}>Pick</button>
              </div>
            </div>
            <button 
              className={styles.saveBtn} 
              onClick={handleSave} 
              disabled={isSaving}
            >
              {isSaving ? 'Saving to GitHub...' : 'Save Configuration'}
            </button>
          </div>

          {isVisualMode && (
            <div className={styles.visualSide}>
              <div className={styles.visualHeader}>
                <span>Selecting: <strong>{selectionTarget}</strong></span>
                <button onClick={() => setIsVisualMode(false)}>Cancel</button>
              </div>
              <iframe
                ref={iframeRef}
                src={`/polite/api/proxy?url=${encodeURIComponent(url)}`}
                className={styles.iframe}
                onLoad={() => {
                  setTimeout(() => {
                    const script = document.createElement('script');
                    script.src = '/polite/scripts/visualSelector.js';
                    iframeRef.current?.contentWindow?.document.body.appendChild(script);
                  }, 1500);
                }}
              />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
