﻿using System;
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

namespace Fidels
{
    /// <summary>
    /// Interaction logic for PrintOrders.xaml
    /// </summary>
    public partial class PrintOrders : Window
    {
        public PrintOrders()
        {
            InitializeComponent();
        }

        public void FillTxtblck(string str)
        {
            ordTxtBlck.Text = str;
        }
    }
}