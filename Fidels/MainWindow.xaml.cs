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
using System.Globalization;
using System.Collections.Specialized;
using System.Collections;

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
        private bool stocksNeedUpdate = false;
        private Dictionary<int, int> bottlesSoldDct = new Dictionary<int, int>();

        public MainWindow()
        {
            InitializeComponent();
            DateTime now = DateTime.Now;
            for (int i = 0; i < cmbYear.Items.Count; i++)
            {
                if (Int32.Parse(((ComboBoxItem)cmbYear.Items[i]).Content.ToString()) == now.Year)
                {
                    cmbYear.SelectedIndex = i;
                }
            }
            for (int i = 0; i < cmbMonth.Items.Count; i++)
            {
                if (Int32.Parse(((ComboBoxItem)cmbMonth.Items[i]).Tag.ToString()) == now.Month)
                {
                    cmbMonth.SelectedIndex = i;
                }
            }
            updateCmbWeeks(); //updates weeks list and that in turn will also syncTables 

            for (int i = 0; i < cmbWeek.Items.Count; i++)
            {
                if (Int32.Parse(cmbWeek.Items[i].ToString()) == service.getWeek(now))
                {
                    cmbWeek.SelectedIndex = i;
                }
            }
            syncStocks();
        }

        private struct StockValues
        {
            public int totalStock, amountToBuy;
            public decimal stockValue;
        }

        //retrieves stock values from stocks dataTable of given row index
        private StockValues getStockValues(int rowIndex)
        {
            decimal price = stocks.Rows[rowIndex].Field<decimal>("unit_price");
            int officeStock = stocks.Rows[rowIndex].Field<int>("office_stock");
            int display = stocks.Rows[rowIndex].Field<int>("display");
            int speedRail = stocks.Rows[rowIndex].Field<int>("speed_rail");
            int barStock = stocks.Rows[rowIndex].Field<int>("stock_bar");
            int minimumStock = stocks.Rows[rowIndex].Field<int>("min_stock");
            int delivery = stocks.Rows[rowIndex].Field<int>("delivery");
            int totalStock = officeStock + display + speedRail + barStock + delivery;
            decimal stockValue = totalStock * price;
            int ammountToBuy = minimumStock - totalStock;
            StockValues sValues = new StockValues();
            sValues.totalStock = totalStock;
            sValues.stockValue = stockValue;
            sValues.amountToBuy = ammountToBuy;
            return sValues;
        }

        private void updateModelIndexes()
        {
            for (int i = 0; i < stocks.Rows.Count; i++)
            {
                stocks.Rows[i].SetField<int>(stocks.Columns["modelIndex"], i);
            }
        }

        private void getBottlesSold(int year, int weekNo)
        {
            DataTable dtPrev = service.bottlesSold(year, weekNo);
            bottlesSoldDct.Clear();
            int product_id, office_stock = 0, speed_rail = 0, stock_bar = 0, display = 0, delivery = 0;
            for (int i = 0; i < dtPrev.Rows.Count; i++)
            {
                product_id = dtPrev.Rows[i].Field<int>("product_id");
                office_stock = dtPrev.Rows[i].Field<int>("office_stock");
                speed_rail = dtPrev.Rows[i].Field<int>("speed_rail");
                stock_bar = dtPrev.Rows[i].Field<int>("stock_bar");
                display = dtPrev.Rows[i].Field<int>("display");
                delivery = dtPrev.Rows[i].Field<int>("delivery");
                int sum = office_stock + speed_rail + stock_bar + display + delivery;
                //add and also check if by some mistake same product was already there and update
                if (bottlesSoldDct.ContainsKey(product_id))
                {
                    bottlesSoldDct[product_id] = sum;
                }
                else
                {
                    bottlesSoldDct.Add(product_id, sum);
                }
            }
        }

        private void syncStocks()
        {
            try
            {
                int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
                int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
                int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());

                stocks = service.getStocks(year, month, weekNo);
                stocks.Columns.Add("rowBackground", typeof(Brush));
                stocks.Columns.Add("modelIndex", typeof(int));
                stocks.Columns.Add("totalPrevious", typeof(int));
                stocks.AcceptChanges();
                updateModelIndexes();
                dataGrid2.ItemsSource = stocks.AsDataView();
                dataGrid2.SelectedValuePath = "stock_id";
                ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid2.ItemsSource);
                view.GroupDescriptions.Add(new PropertyGroupDescription("product_name"));

                getBottlesSold(year, weekNo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while geting stocks table:\n\n" + ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        private void syncFakturas()
        {
            Debug.WriteLine("Syncinc fakturas");
            try
            {
                int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
                int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
                int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());
                ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid3.ItemsSource);
                view.GroupDescriptions.Add(new PropertyGroupDescription("name"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while geting stocks table:\n\n" + ex.Message + "\n\n" + ex.StackTrace);
            }
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

        private void cmbWeek_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded && allowSync)
            {
                dataGrid2.SelectedIndex = -1;
                syncStocks();
            }
        }

        private void cmbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                dataGrid2.SelectedIndex = -1;
                allowSync = false; //to avoid double sync when cmbWeekSelected event is called

                updateCmbWeeks();
                syncStocks();
                allowSync = true;
            }
        }

        private void cmbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                dataGrid2.SelectedIndex = -1;
                syncStocks();
            }
        }

        private void dataGrid2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid2.SelectedItem != null)
            {
                lblStatus.Content = "";
                DataRowView dataRowView = ((DataRowView)dataGrid2.SelectedItem);
                int selectedIndex = (int)(dataRowView["modelIndex"]);
                StockValues sValues = getStockValues(selectedIndex);
                lblTotalStock.Content = sValues.totalStock;
                lblStockValue.Content = sValues.stockValue;
                if (sValues.amountToBuy <= 0)
                {
                    lblAmountTobuy.Content = "none";
                    lblAmountTobuy.Foreground = Brushes.Green;
                }
                else
                {
                    lblAmountTobuy.Content = sValues.amountToBuy;
                    lblAmountTobuy.Foreground = Brushes.Red;
                }
                int product_id = stocks.Rows[selectedIndex].Field<int>("product_id");
                int bttlSold = 0;
                try
                {
                    bttlSold = bottlesSoldDct[product_id] - sValues.totalStock;
                }
                catch (KeyNotFoundException)
                {
                    lblBottlesSold.Content = "No previous week data found...";
                    lblBottlesSold.Foreground = Brushes.Red;
                    return;
                }
                lblBottlesSold.Content = bttlSold;
                lblBottlesSold.Foreground = Brushes.Black;
            }
        }

        private void dataGrid2_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            lblStatus.Content = "";
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(service.orderPrint(stocks));
            PrintOrders window2 = new PrintOrders();
            window2.FillTxtblck(service.orderPrint(stocks));
            window2.Show();
        }

        private void dataGrid2_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            stocksNeedUpdate = true;
        }

        private void dataGrid2_CurrentCellChanged(object sender, EventArgs e)
        {
            if (stocksNeedUpdate)
            {
                try
                {
                    service.updateStocks(stocks);
                    syncStocks();
                    lblStatus.Content = "Updated";
                    lblStatus.Foreground = Brushes.Green;
                }
                catch (Exception ex)
                {
                    lblStatus.Content = "Failed";
                    lblStatus.Foreground = Brushes.Red;
                    MessageBox.Show("Failed to update stocks\n\n" + ex.Message + "\n\n" + ex.StackTrace);
                }
                stocksNeedUpdate = false;
            }
        }

        private void dataGrid2_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGridRow row = e.Row;
            DataRowView dataRowView = ((DataRowView)row.Item);

            int selectedIndex = (int)(dataRowView["modelIndex"]);
            StockValues sValues = getStockValues(selectedIndex);

            if (sValues.amountToBuy > 0)
            {
                row.Background = Brushes.Red;
            }
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            CreateProduct window = new CreateProduct();
            window.ShowDialog();
            if (window.DialogResult.HasValue && window.DialogResult.Value)
            {
                syncStocks();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            object selItem = dataGrid2.SelectedItem;
            if (selItem == null)
            {
                MessageBox.Show("Select a product first.");
                return;
            }
            if (MessageBox.Show("Are you sure you want to delete this product?", "Question", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            DataRowView dataRowView = ((DataRowView)selItem);
            int selectedIndex = (int)(dataRowView["modelIndex"]);
            int product_id = stocks.Rows[selectedIndex].Field<int>("product_id");

            bool deleted = service.deleteProduct(product_id);
            if (!deleted)
            {
                lblStatus.Content = "Failed to delete";
                lblStatus.Foreground = Brushes.Red;
                return;
            }
            lblStatus.Content = "Deleted";
            lblStatus.Foreground = Brushes.Red;
            syncStocks();
        }
    }
}
