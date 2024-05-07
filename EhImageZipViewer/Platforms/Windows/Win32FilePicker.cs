using System.Runtime.InteropServices;
using Microsoft.Maui.Platform;
using Windows.Storage;
using Windows.ApplicationModel;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.Foundation;
using Windows.Storage.AccessCache;

namespace EhImageZipViewer;

public static class Win32FilePicker
{
    public async static Task<FileResult?> PickAsync()
    {
        var filePath = PickInternal();
        if (filePath == null) return null;

        var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
        if(Package.Current != null) // AppInfoUtils.AppInfoUtils
        {
            StorageApplicationPermissions.FutureAccessList.Add(storageFile);
        }

        return new FileResult(storageFile.Path, storageFile.ContentType);
    }

    public unsafe static string? PickInternal()
    {
        var currUI = System.Globalization.CultureInfo.CurrentUICulture;
        Windows.Win32.PInvoke.SetThreadUILanguage((ushort)currUI.LCID);

        var dialog = (IFileOpenDialog)new FileOpenDialog();

        var pszName = Marshal.StringToHGlobalAuto("Zip Files");
        var pszSpec = Marshal.StringToHGlobalAuto("*.zip");
        var fileTypes = new COMDLG_FILTERSPEC[]
        {
                new() {
                    pszName = new PCWSTR((char*)pszName),
                    pszSpec = new PCWSTR((char*)pszSpec)
                }
        };

        var fileTypesHandler = GCHandle.Alloc(fileTypes, GCHandleType.Pinned);
        dialog.SetFileTypes((uint)fileTypes.Length, (COMDLG_FILTERSPEC*)fileTypesHandler.AddrOfPinnedObject());

        var options = (FILEOPENDIALOGOPTIONS)0;
        dialog.GetOptions(&options);
        options |= FILEOPENDIALOGOPTIONS.FOS_DONTADDTORECENT;
        options |= FILEOPENDIALOGOPTIONS.FOS_SUPPORTSTREAMABLEITEMS;
        dialog.SetOptions(options);

        var hwnd = WindowStateManager.Default.GetActiveWindow()!.GetWindowHandle();
        try
        {
            dialog.Show(new HWND(hwnd));
            dialog.GetResult(out var item);

            var pszFilePath = new PWSTR();
            item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &pszFilePath);

            var filePath = pszFilePath.ToString();
            return filePath;
        }
        catch (COMException e) when (e.ErrorCode == -2147023673) // Canceled
        {
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(pszName);
            Marshal.FreeHGlobal(pszSpec);
            fileTypesHandler.Free();
        }
    }
}

public static class FileResultExtensions
{
    public static FileStream OpenRead(this FileResult fileResult) => new FileStream(fileResult.FullPath, FileMode.Open, FileAccess.Read);
}

