using System;
using System.Reflection;
using System.Windows.Forms;
using static System.Environment;
using static System.IntPtr;
using static TEKLauncher.Data.LocalizationManager;

namespace TEKLauncher.UI
{
    internal class FolderSelectDialog
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public string Path;
        private static readonly Assembly WindowsForms = typeof(FileDialog).Assembly;
        private static readonly uint PickFolders = (uint)WindowsForms.GetType("System.Windows.Forms.FileDialogNative+FOS").GetField("FOS_PICKFOLDERS").GetValue(null);
        private static readonly ConstructorInfo VistaDialogEventsConstructor = WindowsForms.GetType("System.Windows.Forms.FileDialog+VistaDialogEvents").GetConstructor(Flags, null, new[] { typeof(FileDialog) }, null);
        private static readonly Type IFileDialog = WindowsForms.GetType("System.Windows.Forms.FileDialogNative+IFileDialog");
        private static readonly MethodInfo GetOptions = typeof(FileDialog).GetMethod("GetOptions", Flags), SetOptions = IFileDialog.GetMethod("SetOptions", Flags);
        internal bool Show()
        {
            OpenFileDialog Dialog = new OpenFileDialog
            {
                AddExtension = false,
                CheckFileExists = false,
                DereferenceLinks = true,
                Filter = "Folders|\n",
                InitialDirectory = CurrentDirectory,
                Multiselect = false,
                Title = LocString(LocCode.SelectFolder)
            };
            object VistaDialog = typeof(OpenFileDialog).GetMethod("CreateVistaDialog", Flags).Invoke(Dialog, new object[0]);
            typeof(OpenFileDialog).GetMethod("OnBeforeVistaDialog", Flags).Invoke(Dialog, new[] { VistaDialog });
            SetOptions.Invoke(VistaDialog, new object[] { (uint)GetOptions.Invoke(Dialog, new object[0]) | PickFolders });
            object[] AdviseParameters = new[] { VistaDialogEventsConstructor.Invoke(new[] { Dialog }), 0U };
            IFileDialog.GetMethod("Advise").Invoke(VistaDialog, AdviseParameters);
            try
            {
                bool Result = (int)IFileDialog.GetMethod("Show").Invoke(VistaDialog, new object[] { Zero }) == 0;
                if (Result)
                    Path = Dialog.FileName;
                return Result;
            }
            finally { IFileDialog.GetMethod("Unadvise").Invoke(VistaDialog, new[] { AdviseParameters[1] }); }
        }
    }
}