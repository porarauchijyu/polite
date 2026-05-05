export async function saveTargetsToGithub(newTargets: any[]) {
  const token = localStorage.getItem('gh_pat');
  const repo = 'porarauchijyu/polite';
  const path = 'targets.json';

  if (!token) {
    throw new Error('GitHub Personal Access Token is not set. Please set it in Settings.');
  }

  // 1. 現在のファイルの SHA を取得
  const res = await fetch(`https://api.github.com/repos/${repo}/contents/${path}`, {
    headers: { Authorization: `token ${token}` }
  });
  
  if (!res.ok) throw new Error('Failed to fetch file info from GitHub');
  const fileData = await res.json();
  const sha = fileData.sha;

  // 2. 更新をコミット
  const updateRes = await fetch(`https://api.github.com/repos/${repo}/contents/${path}`, {
    method: 'PUT',
    headers: {
      Authorization: `token ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      message: 'chore: update targets.json via web dashboard',
      content: btoa(unescape(encodeURIComponent(JSON.stringify(newTargets, null, 2)))),
      sha: sha
    }),
  });

  if (!updateRes.ok) {
    const errorData = await updateRes.json();
    throw new Error(errorData.message || 'Failed to update file on GitHub');
  }

  return await updateRes.json();
}

export async function triggerScraperWorkflow() {
  const token = localStorage.getItem('gh_pat');
  const repo = 'porarauchijyu/polite';
  const workflowId = 'deploy.yml';

  if (!token) throw new Error('Token required');

  const res = await fetch(`https://api.github.com/repos/${repo}/actions/workflows/${workflowId}/dispatches`, {
    method: 'POST',
    headers: {
      Authorization: `token ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ ref: 'main' }),
  });

  if (!res.ok) throw new Error('Failed to trigger workflow');
  return true;
}
