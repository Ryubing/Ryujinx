using Avalonia.Platform.Storage;
using Gommon;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Utilities
{
    public static class StorageProviderExtensions
    {
        public static async Task<Optional<IStorageFolder>> OpenSingleFolderPickerAsync(this IStorageProvider storageProvider, FolderPickerOpenOptions openOptions = null) =>
            await storageProvider.OpenFolderPickerAsync(FixOpenOptions(openOptions, false))
                .Then(folders => folders.FindFirst());

        public static async Task<Optional<IStorageFile>> OpenSingleFilePickerAsync(this IStorageProvider storageProvider, FilePickerOpenOptions openOptions = null) =>
            await storageProvider.OpenFilePickerAsync(FixOpenOptions(openOptions, false))
                .Then(files => files.FindFirst());
        
        public static async Task<Optional<IReadOnlyList<IStorageFolder>>> OpenMultiFolderPickerAsync(this IStorageProvider storageProvider, FolderPickerOpenOptions openOptions = null) =>
            await storageProvider.OpenFolderPickerAsync(FixOpenOptions(openOptions, true))
                .Then(folders => folders.Count > 0 ? Optional.Of(folders) : default);

        public static async Task<Optional<IReadOnlyList<IStorageFile>>> OpenMultiFilePickerAsync(this IStorageProvider storageProvider, FilePickerOpenOptions openOptions = null) =>
            await storageProvider.OpenFilePickerAsync(FixOpenOptions(openOptions, true))
                .Then(files => files.Count > 0 ? Optional.Of(files) : default);
        
        private static FilePickerOpenOptions FixOpenOptions(this FilePickerOpenOptions openOptions, bool allowMultiple)
        {
            if (openOptions is null)
                return new FilePickerOpenOptions { AllowMultiple = allowMultiple };

            openOptions.AllowMultiple = allowMultiple;
            
            return openOptions;
        }
        
        private static FolderPickerOpenOptions FixOpenOptions(this FolderPickerOpenOptions openOptions, bool allowMultiple)
        {
            if (openOptions is null)
                return new FolderPickerOpenOptions { AllowMultiple = allowMultiple };

            openOptions.AllowMultiple = allowMultiple;
            
            return openOptions;
        }
    }
}
