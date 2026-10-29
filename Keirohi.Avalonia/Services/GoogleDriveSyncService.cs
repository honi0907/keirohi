using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Util.Store;
using Keirohi_App.Helpers;
using Keirohi_App.Services;

namespace Keirohi.Avalonia.Services;

internal sealed class GoogleDriveSyncService
{
    private const string ApplicationName = "Keirohi";

    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];

    public static string ClientSecretsPath =>
        Path.Combine(DataPersistenceStatusHelper.DataDirectory, "google-oauth-client.json");

    public static string TokenStorePath =>
        Path.Combine(DataPersistenceStatusHelper.DataDirectory, "google-oauth-tokens");

    public static bool IsClientConfigured => System.IO.File.Exists(ClientSecretsPath);

    public static async Task<(bool Ok, string Message)> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (!IsClientConfigured)
        {
            return (false, $"OAuth クライアント JSON がありません。\n{ClientSecretsPath}\nに Google Cloud のデスクトップ用クライアントを配置してください（docs/google-drive-setup.md 参照）。");
        }

        try
        {
            await CreateDriveServiceAsync(cancellationToken);
            var prefs = DriveSyncPreferencesStore.Load();
            prefs.Connected = true;
            DriveSyncPreferencesStore.Save(prefs);
            return (true, "Google アカウントでログインしました。");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static Task SignOutAsync()
    {
        if (Directory.Exists(TokenStorePath))
        {
            Directory.Delete(TokenStorePath, true);
        }

        var prefs = DriveSyncPreferencesStore.Load();
        prefs.Connected = false;
        prefs.FolderId = "";
        prefs.AccountEmail = null;
        DriveSyncPreferencesStore.Save(prefs);
        return Task.CompletedTask;
    }

    public static async Task<(bool Ok, string Message, bool Reloaded)> SyncAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsClientConfigured)
        {
            return (false, "OAuth クライアント JSON が未設定です。", false);
        }

        var prefs = DriveSyncPreferencesStore.Load();
        if (!prefs.Connected)
        {
            return (false, "先に Google でログインしてください。", false);
        }

        if (string.IsNullOrWhiteSpace(prefs.FolderName))
        {
            return (false, "同期フォルダ名を設定してください。", false);
        }

        LocalDataSyncBackupService.CreatePreSyncBackup();

        try
        {
            var service = await CreateDriveServiceAsync(cancellationToken);
            var folderId = await ResolveFolderIdAsync(service, prefs, cancellationToken);
            prefs.FolderId = folderId;
            DriveSyncPreferencesStore.Save(prefs);

            var dataDir = DataPersistenceStatusHelper.DataDirectory;
            var reloaded = false;

            foreach (var fileName in CloudSyncConstants.DataFileNames)
            {
                var localPath = Path.Combine(dataDir, fileName);
                var localExists = System.IO.File.Exists(localPath);
                var localMod = localExists ? System.IO.File.GetLastWriteTimeUtc(localPath) : DateTime.MinValue;

                var remote = await FindFileInFolderAsync(service, folderId, fileName, cancellationToken);
                var remoteMod = remote?.ModifiedTimeDateTimeOffset?.UtcDateTime ?? DateTime.MinValue;

                if (remote is not null && remoteMod > localMod)
                {
                    await DownloadAsync(service, remote.Id!, localPath, cancellationToken);
                    reloaded = true;
                }
                else if (localExists)
                {
                    await UploadAsync(service, folderId, fileName, localPath, remote?.Id, cancellationToken);
                }
            }

            prefs = DriveSyncPreferencesStore.Load();
            prefs.LastSyncedAt = DateTimeOffset.Now;
            DriveSyncPreferencesStore.Save(prefs);

            var message = reloaded
                ? "Drive と同期しました。Drive 側が新しいファイルを取り込みました。"
                : "Drive と同期しました。ローカルの内容をアップロードしました。";
            return (true, message, reloaded);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, false);
        }
    }

    private static async Task<DriveService> CreateDriveServiceAsync(CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(ClientSecretsPath, FileMode.Open, FileAccess.Read);
        var secrets = GoogleClientSecrets.FromStream(stream).Secrets;
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            "user",
            cancellationToken,
            new FileDataStore(TokenStorePath, true));

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });
    }

    private static async Task<string> ResolveFolderIdAsync(
        DriveService service,
        DriveSyncPreferences prefs,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(prefs.FolderId))
        {
            try
            {
                var existing = await service.Files.Get(prefs.FolderId).ExecuteAsync(cancellationToken);
                if (existing is not null && existing.Trashed != true)
                {
                    return prefs.FolderId;
                }
            }
            catch
            {
                // fall through to search/create
            }
        }

        var escapedName = prefs.FolderName.Replace("'", "\\'", StringComparison.Ordinal);
        var query =
            $"mimeType='application/vnd.google-apps.folder' and name='{escapedName}' and trashed=false";
        var listRequest = service.Files.List();
        listRequest.Q = query;
        listRequest.Fields = "files(id,name)";
        listRequest.Spaces = "drive";
        var list = await listRequest.ExecuteAsync(cancellationToken);

        var found = list.Files?.FirstOrDefault();
        if (found?.Id is { Length: > 0 } id)
        {
            return id;
        }

        var metadata = new DriveFile
        {
            Name = prefs.FolderName,
            MimeType = "application/vnd.google-apps.folder",
        };
        var created = await service.Files.Create(metadata).ExecuteAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(created.Id))
        {
            throw new InvalidOperationException("Drive 上に同期フォルダを作成できませんでした。");
        }

        return created.Id;
    }

    private static async Task<DriveFile?> FindFileInFolderAsync(
        DriveService service,
        string folderId,
        string fileName,
        CancellationToken cancellationToken)
    {
        var escapedName = fileName.Replace("'", "\\'", StringComparison.Ordinal);
        var query = $"name='{escapedName}' and '{folderId}' in parents and trashed=false";
        var listRequest = service.Files.List();
        listRequest.Q = query;
        listRequest.Fields = "files(id,name,modifiedTime)";
        listRequest.Spaces = "drive";
        var list = await listRequest.ExecuteAsync(cancellationToken);
        return list.Files?.FirstOrDefault();
    }

    private static async Task DownloadAsync(
        DriveService service,
        string fileId,
        string localPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await using var stream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
        await service.Files.Get(fileId).DownloadAsync(stream, cancellationToken);
    }

    private static async Task UploadAsync(
        DriveService service,
        string folderId,
        string fileName,
        string localPath,
        string? existingFileId,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
        if (!string.IsNullOrWhiteSpace(existingFileId))
        {
            var update = service.Files.Update(new DriveFile(), existingFileId, stream, "application/json");
            update.SupportsAllDrives = true;
            await update.UploadAsync(cancellationToken);
            return;
        }

        var metadata = new DriveFile
        {
            Name = fileName,
            Parents = [folderId],
        };
        var create = service.Files.Create(metadata, stream, "application/json");
        create.SupportsAllDrives = true;
        await create.UploadAsync(cancellationToken);
    }
}
