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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Data;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Fidels
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Service service = Service.getInstance();
        private DataTable stocks;
        private bool allowSync = false;
        private bool stocksNeedUpdate = false;

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
            //service.ensureWeek();
            syncStocks();
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

        private void cmbWeek_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("cmbWeek_SelectionChanged");
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
            if (dataGrid2.SelectedIndex != -1)
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

        private void dataGrid2_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (stocksNeedUpdate)
            {
                try
                {
                    service.updateStocks(stocks);
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

        private void Grid_LostFocus(object sender, RoutedEventArgs e)
        {
            //may or may not be useful
        }

        private void dataGrid2_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            lblStatus.Content = "";
        }


        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(Service.orderPrint(stocks));
            PrintOrders window2 = new PrintOrders();
            window2.FillTxtblck(Service.orderPrint(stocks));
            window2.Show();
        }

        private void dataGrid2_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            stocksNeedUpdate = true;
        }

    }
}
