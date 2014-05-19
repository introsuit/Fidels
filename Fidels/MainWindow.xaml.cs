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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Data;
using System.ComponentModel;
using System.Diagnostics;

namespace Fidels
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Service service = Service.getInstance();
        private DataTable stocks;
        private bool allowSync = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updateCmbWeeks(); //updates weeks list and that in turn will also syncTables
            //test
        }    

        private void syncStocks()
        {
            Debug.WriteLine("Syncinc stocks");
            try
            {
                int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
                int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
                int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());
                
                stocks = service.getStocks(year, month, weekNo);              
                dataGrid2.ItemsSource = stocks.AsDataView();              
                dataGrid2.SelectedValuePath = "stock_id";

                ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid2.ItemsSource);
                view.GroupDescriptions.Add(new PropertyGroupDescription("product_name"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while geting stocks table:\n\n" + ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        private void cmbWeek_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("cmbWeek_SelectionChanged");
            if (this.IsLoaded && allowSync)
            {
                syncStocks();
            }
        }

        private void cmbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                allowSync = false; //to avoid double sync when cmbWeekSelected event is called

                updateCmbWeeks();
                syncStocks();

                allowSync = true;        
            }
        }

        private void cmbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
                syncStocks();
        }

        private void updateCmbWeeks()
        {
            cmbWeek.Items.Clear();         

            int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
            int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());

            WeeksRange weekR = service.getWeeksRange(year, month);
            for (int i = weekR.from; i <= weekR.to; i++)
            {
                cmbWeek.Items.Add(i);
            }
            cmbWeek.SelectedIndex = 0;
        }

        private void dataGrid2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            decimal price = stocks.Rows[dataGrid2.SelectedIndex].Field<decimal>("unit_price");
            int officeStock = stocks.Rows[dataGrid2.SelectedIndex].Field<int>("office_stock");
            int display = stocks.Rows[dataGrid2.SelectedIndex].Field<int>("display");
            int speedRail = stocks.Rows[dataGrid2.SelectedIndex].Field<int>("speed_rail");
            int barStock = stocks.Rows[dataGrid2.SelectedIndex].Field<int>("stock_bar");
            int minimumStock = stocks.Rows[dataGrid2.SelectedIndex].Field<int>("min_stock");
            int totalStock = officeStock + display + speedRail + barStock;
            decimal stockValue = totalStock * price;
            int ammountToBuy = minimumStock - totalStock;
            lblTotalStock.Content = totalStock;
            lblStockValue.Content = stockValue;
            if (ammountToBuy <= 0)
            {
                lblAmountTobuy.Content = "none";
                lblAmountTobuy.Foreground = Brushes.Green;
            }
            else
            {
                lblAmountTobuy.Content = ammountToBuy;
                lblAmountTobuy.Foreground = Brushes.Red;
            }
        }
    }
}
