// -----------------------------------------------------------------------
// <copyright file="AIAssistantView.xaml.cs" company="SysAdminX">
//     Copyright (c) SysAdminX. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Windows.Controls;
using SysAdminX.AIAssistant.ViewModels;

namespace SysAdminX.AIAssistant.Views;

/// <summary>
/// Interaction logic for AIAssistantView.xaml
/// </summary>
public partial class AIAssistantView : Page
{
    public AIAssistantViewModel ViewModel { get; }

    public AIAssistantView(AIAssistantViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }
}
