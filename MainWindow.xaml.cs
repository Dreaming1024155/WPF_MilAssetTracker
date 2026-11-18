using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace WPF_프로젝트;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Closing += (_, _) => _viewModel.Save();
    }
}
