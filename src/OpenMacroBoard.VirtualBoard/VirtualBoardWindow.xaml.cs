using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenMacroBoard.VirtualBoard
{
    /// <summary>
    /// Interaction logic for VirtualBoardWindow.xaml
    /// </summary>
    public partial class VirtualBoardWindow : Window
    {
        /// <summary>
        /// Creates an instance of <see cref="VirtualBoardWindow"/>.
        /// </summary>
        /// <param name="viewModel"></param>
        internal VirtualBoardWindow(VirtualBoardViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        /// <summary>
        /// Is used to hide title bar icon
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// Thanks to Sheridan (https://stackoverflow.com/questions/18580430/hide-the-icon-from-a-wpf-window)
        /// </remarks>
        protected override void OnSourceInitialized(EventArgs e)
        {
            IconHelper.RemoveIcon(this);
        }
    }
}
