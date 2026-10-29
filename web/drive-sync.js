/**
 * Google Drive 同期（モバイル Web）。google-config.js の clientId が必要です。
 */
(function () {
  const DRIVE_SCOPE = "https://www.googleapis.com/auth/drive.file";
  const TOKEN_KEY = "keirohi-google-access-token";
  const TOKEN_EXP_KEY = "keirohi-google-access-token-exp";
  const SYNC_FILES = [
    "entry-sets.json",
    "patterns.json",
    "fare-memory.json",
    "settings.json",
  ];

  function getClientId() {
    return window.KEIROHI_GOOGLE?.clientId?.trim() || "";
  }

  function loadToken() {
    const token = sessionStorage.getItem(TOKEN_KEY);
    const exp = Number(sessionStorage.getItem(TOKEN_EXP_KEY) || "0");
    if (!token || Date.now() > exp - 60000) {
      return null;
    }
    return token;
  }

  function saveToken(accessToken, expiresInSec) {
    sessionStorage.setItem(TOKEN_KEY, accessToken);
    sessionStorage.setItem(
      TOKEN_EXP_KEY,
      String(Date.now() + Number(expiresInSec || 3600) * 1000)
    );
  }

  function clearToken() {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(TOKEN_EXP_KEY);
  }

  function waitForGoogleIdentity() {
    return new Promise((resolve, reject) => {
      if (window.google?.accounts?.oauth2) {
        resolve();
        return;
      }
      let tries = 0;
      const t = setInterval(() => {
        tries++;
        if (window.google?.accounts?.oauth2) {
          clearInterval(t);
          resolve();
        } else if (tries > 50) {
          clearInterval(t);
          reject(new Error("Google Identity Services を読み込めませんでした。"));
        }
      }, 100);
    });
  }

  async function requestAccessToken() {
    const clientId = getClientId();
    if (!clientId) {
      throw new Error("google-config.js に Web 用 clientId を設定してください。");
    }
    await waitForGoogleIdentity();
    return new Promise((resolve, reject) => {
      const client = google.accounts.oauth2.initTokenClient({
        client_id: clientId,
        scope: DRIVE_SCOPE,
        callback: (resp) => {
          if (resp.error) {
            reject(new Error(resp.error));
            return;
          }
          saveToken(resp.access_token, resp.expires_in);
          resolve(resp.access_token);
        },
      });
      client.requestAccessToken({ prompt: loadToken() ? "" : "consent" });
    });
  }

  async function getAccessToken() {
    const existing = loadToken();
    if (existing) {
      return existing;
    }
    return requestAccessToken();
  }

  async function driveFetch(path, options = {}) {
    const token = await getAccessToken();
    const res = await fetch(`https://www.googleapis.com/drive/v3${path}`, {
      ...options,
      headers: {
        Authorization: `Bearer ${token}`,
        ...(options.headers || {}),
      },
    });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Drive API ${res.status}: ${text.slice(0, 200)}`);
    }
    return res;
  }

  async function findFolderByName(name) {
    const q = encodeURIComponent(
      `mimeType='application/vnd.google-apps.folder' and name='${name.replace(/'/g, "\\'")}' and trashed=false`
    );
    const res = await driveFetch(`/files?q=${q}&fields=files(id,name)&spaces=drive`);
    const data = await res.json();
    return data.files?.[0] || null;
  }

  async function createFolder(name) {
    const res = await driveFetch("/files", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        name,
        mimeType: "application/vnd.google-apps.folder",
      }),
    });
    return res.json();
  }

  async function findFileInFolder(folderId, fileName) {
    const q = encodeURIComponent(
      `name='${fileName.replace(/'/g, "\\'")}' and '${folderId}' in parents and trashed=false`
    );
    const res = await driveFetch(
      `/files?q=${q}&fields=files(id,name,modifiedTime)&spaces=drive`
    );
    const data = await res.json();
    return data.files?.[0] || null;
  }

  async function downloadFile(fileId) {
    const res = await driveFetch(`/files/${fileId}?alt=media`);
    return res.text();
  }

  async function uploadFile(folderId, fileName, content, existingId) {
    const token = await getAccessToken();
    const metadata = { name: fileName, mimeType: "application/json" };
    if (!existingId) {
      metadata.parents = [folderId];
    }
    const form = new FormData();
    form.append(
      "metadata",
      new Blob([JSON.stringify(metadata)], { type: "application/json" })
    );
    form.append("file", new Blob([content], { type: "application/json" }));

    const url = existingId
      ? `https://www.googleapis.com/upload/drive/v3/files/${existingId}?uploadType=multipart`
      : "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
    const method = existingId ? "PATCH" : "POST";
    const res = await fetch(url, {
      method,
      headers: { Authorization: `Bearer ${token}` },
      body: form,
    });
    if (!res.ok) {
      throw new Error(`アップロード失敗 (${res.status})`);
    }
    return res.json();
  }

  window.KeirohiDriveSync = {
    isConfigured: () => Boolean(getClientId()),
    signIn: () => requestAccessToken().then(() => true),
    signOut: () => {
      clearToken();
      return Promise.resolve();
    },
    /**
     * @param {object} hooks
     * @param {(folderId:string)=>Promise<Record<string,string>>} hooks.buildUploadPayload fileName -> JSON text
     * @param {(files:Record<string,string>)=>void} hooks.applyDownloaded
     */
    async syncFolder(folderName, hooks) {
      let folder = await findFolderByName(folderName);
      if (!folder) {
        folder = await createFolder(folderName);
      }
      const folderId = folder.id;
      const uploads = await hooks.buildUploadPayload(folderId);
      const localRevisionMs = hooks.getLocalRevisionMs ? hooks.getLocalRevisionMs() : 0;
      let downloadedAny = false;
      const names = new Set([...SYNC_FILES, ...Object.keys(uploads)]);

      for (const fileName of names) {
        const remote = await findFileInFolder(folderId, fileName);
        const localText = uploads[fileName];
        const remoteMod = remote?.modifiedTime ? Date.parse(remote.modifiedTime) : 0;

        if (remote && remoteMod > localRevisionMs) {
          const text = await downloadFile(remote.id);
          hooks.applyDownloaded({ [fileName]: text });
          downloadedAny = true;
        } else if (localText) {
          await uploadFile(folderId, fileName, localText, remote?.id);
        }
      }

      return { folderId, downloadedAny };
    },
  };
})();
