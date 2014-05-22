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
using System.Collections.Specialized;

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

            //TODO highlighting doesn't work when app is started
            highlightRowsToBuy(); 
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

            int totalStock = officeStock + display + speedRail + barStock;
            decimal stockValue = totalStock * price;
            int ammountToBuy = minimumStock - totalStock;

            StockValues sValues = new StockValues();
            sValues.totalStock = totalStock;
            sValues.stockValue = stockValue;
            sValues.amountToBuy = ammountToBuy;
            return sValues;
        }

        private void highlightRowsToBuy()
        {
            Debug.WriteLine("Highlighting");
            dataGrid2.UpdateLayout();
            for (int i = 0; i < stocks.Rows.Count; i++)
            {
                StockValues sValues = getStockValues(i);
                if (sValues.amountToBuy > 0)
                {
                    
                    
                        //DataRow dr = stocks.Rows[i];
                        //DataRow newRow = stocks.NewRow();
                        //// We "clone" the row
                        //newRow.ItemArray = dr.ItemArray;
                        //// We remove the old and insert the new
                        //stocks.Rows.Remove(dr);
                        //stocks.Rows.InsertAt(newRow, 0);

                        //dataGrid2.UpdateLayout();

                        DataGridRow dataGridRow = dataGrid2.ItemContainerGenerator.ContainerFromIndex(0) as DataGridRow;
                        Debug.WriteLine(dataGridRow);
                        if (dataGridRow != null)
                        {
                            dataGridRow.Background = Brushes.Red;
                        }

                }              
            }
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
                highlightRowsToBuy();
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
                highlightRowsToBuy();
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
            int selectedIndex = dataGrid2.SelectedIndex;
            if (selectedIndex != -1)
            {
                lblStatus.Content = "";
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
            }
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

        private void dataGrid2_CurrentCellChanged(object sender, EventArgs e)
        {
            if (stocksNeedUpdate)
            {
                try
                {
                    service.updateStocks(stocks);
                    lblStatus.Content = "Updated";
                    lblStatus.Foreground = Brushes.Green;

                    highlightRowsToBuy();
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

    }
}
