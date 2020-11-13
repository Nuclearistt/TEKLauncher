using System.Windows.Controls;
using TEKLauncher.ARK;
using TEKLauncher.Controls;
using static TEKLauncher.ARK.DLCManager;

namespace TEKLauncher.Pages
{
    public partial class DLCsPage : Page
    {
        public DLCsPage()
        {
            InitializeComponent();
            for (int Iterator = 0; Iterator < DLCs.Length; Iterator++)
            {
                if (Iterator / 3 == DLCs.Length / 3)
                    switch (DLCs.Length % 3)
                    {
                        case 1: AddItem(1, Iterator); break;
                        case 2: AddItem(Iterator % 3 == 0 ? 0 : 2, Iterator); break;
                    }
                else
                    AddItem(Iterator % 3, Iterator);
            }
        }
        private void AddItem(int StackIndex, int DLCIndex) => ((Panel)RootGrid.Children[StackIndex]).Children.Add(new DLCItem(DLCs[DLCIndex]));
        internal DLCItem GetItem(MapCode Code)
        {
            foreach (Panel Stack in RootGrid.Children)
                foreach (DLCItem Item in Stack.Children)
                    if (Item.DLC.Code == Code)
                        return Item;
            return null;
        }
    }
}