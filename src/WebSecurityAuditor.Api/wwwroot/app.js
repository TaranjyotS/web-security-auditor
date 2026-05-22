const form = document.querySelector('#auditForm');
const runBtn = document.querySelector('#runBtn');
const results = document.querySelector('#results');
const summary = document.querySelector('#summary');
const portsTable = document.querySelector('#portsTable');
const recommendations = document.querySelector('#recommendations');
const historyTable = document.querySelector('#historyTable');
const downloadLink = document.querySelector('#downloadLink');
const refreshBtn = document.querySelector('#refreshBtn');

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  clearUiError();
  runBtn.disabled = true;
  runBtn.textContent = 'Running...';

  try {
    const payload = {
      target: document.querySelector('#target').value.trim(),
      startPort: Number(document.querySelector('#startPort').value),
      endPort: Number(document.querySelector('#endPort').value),
      timeoutMs: Number(document.querySelector('#timeoutMs').value),
      authorized: document.querySelector('#authorized').checked
    };

    const response = await fetch('/api/audits', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
      body: JSON.stringify(payload)
    });

    const data = await readResponse(response);
    if (!response.ok) {
      throw new Error(data?.error || data?.title || `Audit failed with HTTP ${response.status}`);
    }

    renderReport(data);

    // History loading should not hide a successful audit result.
    try {
      await loadHistory();
    } catch (historyError) {
      showUiError(`Audit completed, but history refresh failed: ${historyError.message}`);
    }
  } catch (error) {
    showUiError(error.message);
  } finally {
    runBtn.disabled = false;
    runBtn.textContent = 'Run Audit';
  }
});

refreshBtn.addEventListener('click', async () => {
  clearUiError();
  try {
    await loadHistory();
  } catch (error) {
    showUiError(error.message);
  }
});

async function readResponse(response) {
  const text = await response.text();
  if (!text) {
    return null;
  }

  const contentType = response.headers.get('content-type') || '';
  if (!contentType.toLowerCase().includes('application/json')) {
    throw new Error(`Expected JSON but received ${contentType || 'an empty/unknown response'}.`);
  }

  try {
    return JSON.parse(text);
  } catch {
    throw new Error('The server returned invalid JSON. Try refreshing the page and running the audit again.');
  }
}

function renderReport(report) {
  results.classList.remove('hidden');
  downloadLink.href = `/api/reports/${report.id}/download`;
  downloadLink.download = `audit-${report.target}-${report.id}.json`;

  summary.innerHTML = `
    <div class="metric"><span>Target</span><strong>${escapeHtml(report.target)}</strong></div>
    <div class="metric"><span>Status</span><strong>${escapeHtml(report.status)}</strong></div>
    <div class="metric"><span>HTTP</span><strong>${report.http?.statusCode ?? 'N/A'}</strong></div>
    <div class="metric"><span>Open Ports</span><strong>${report.ports?.length ?? 0}</strong></div>`;

  portsTable.innerHTML = report.ports?.length
    ? report.ports.map(port => `<tr><td>${port.port}</td><td>${escapeHtml(port.serviceHint || 'Unknown')}</td></tr>`).join('')
    : '<tr><td colspan="2">No open ports found in selected range.</td></tr>';

  recommendations.innerHTML = report.recommendations?.length
    ? report.recommendations.map(item => `<li>${escapeHtml(item)}</li>`).join('')
    : '<li>No recommendations generated.</li>';
}

async function loadHistory() {
  const response = await fetch('/api/audits', { headers: { 'Accept': 'application/json' } });
  const audits = await readResponse(response);

  if (!response.ok) {
    throw new Error(audits?.error || audits?.title || `History failed with HTTP ${response.status}`);
  }

  const rows = Array.isArray(audits) ? audits : [];
  historyTable.innerHTML = rows.length
    ? rows.map(audit => `<tr><td>${escapeHtml(audit.target)}</td><td>${escapeHtml(audit.status)}</td><td>${(audit.openPorts || []).join(', ') || 'None'}</td><td>${new Date(audit.createdAtUtc).toLocaleString()}</td></tr>`).join('')
    : '<tr><td colspan="4">No audits yet.</td></tr>';
}

function showUiError(message) {
  let errorBox = document.querySelector('#uiError');
  if (!errorBox) {
    errorBox = document.createElement('div');
    errorBox.id = 'uiError';
    errorBox.className = 'error-box';
    form.insertAdjacentElement('afterend', errorBox);
  }
  errorBox.textContent = message;
  errorBox.classList.remove('hidden');
}

function clearUiError() {
  const errorBox = document.querySelector('#uiError');
  if (errorBox) {
    errorBox.textContent = '';
    errorBox.classList.add('hidden');
  }
}

function escapeHtml(value) {
  return String(value).replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '\'': '&#39;', '"': '&quot;' }[char]));
}

loadHistory().catch(error => showUiError(`History failed to load: ${error.message}`));
