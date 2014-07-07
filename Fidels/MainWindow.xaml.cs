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
        private DataTable staffs;
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
            combobox1.ItemsSource = service.getCompanyNames();

            updateFakturaGrid();
            syncStocks();
            syncStaff();
            int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
            int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
            int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());
            updateBudget(year, month, weekNo);
        }

        public void updateBudget(int year, int month, int week) {
            decimal totalTurnOver = 0;
            decimal totalStockValue = 0;
            service.getStockTotals(year, month, week, out totalTurnOver, out totalStockValue);
            lblTotalValueStock.Content = totalStockValue;
            lblTurnOver.Content = totalTurnOver.ToString();
            lblTotalWages.Content=service.getTotalWagesCost(year, week);
            lblTotalFaktura.Content = service.getTotalWeeklyFakturaAmount(year, month, week);
            lblSupposedPercentWage.Content = service.getSupposedWagePercent().ToString()+"%";
            lblSupposedPercentFaktura.Content = service.getSupposedFakturaPercent().ToString()+"%";
            decimal fakturaPercent = service.getPercentage(totalTurnOver, service.getTotalWeeklyFakturaAmount(year, month, week));
            decimal wagePercent  = service.getPercentage(totalTurnOver, service.getTotalWagesCost(year, week));
            if (fakturaPercent != -1)
                lblPercentFaktura.Content = fakturaPercent.ToString()+"%";
            else lblPercentFaktura.Content = "N/A";
            if (wagePercent != -1)
                lblPercentWage.Content = wagePercent + "%";
            else lblPercentWage.Content = "N/A";
            if (fakturaPercent != -1)
                lbldscrFakt.Content = service.getSupposedFakturaPercent() - Convert.ToInt32(fakturaPercent);
            else lbldscrFakt.Content = "N/A";
            if (wagePercent != -1)
                lbldscrWage.Content = service.getSupposedWagePercent() - wagePercent;
            else lbldscrWage.Content = "N/A";
            if ((service.getSupposedWagePercent() - wagePercent)<0)
                lbldscrWage.Foreground = Brushes.Red;
            else lbldscrWage.Foreground = Brushes.Black;
            if ((service.getSupposedFakturaPercent() - Convert.ToInt32(fakturaPercent) < 0))
                lbldscrWage.Foreground = Brushes.Red;
            else lbldscrWage.Foreground = Brushes.Black;
        }

        public void updateFakturaGrid()
        {
            int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
            int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
            int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());
            
            dataGrid3.ItemsSource = service.getFakturas(year, month, weekNo).AsDataView();
            dataGrid3.SelectedValuePath = "faktura_id";
            ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid3.ItemsSource);
            view.GroupDescriptions.Add(new PropertyGroupDescription("name"));
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

        private void syncStaff()
        {
            try
            {
                int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
                int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());

                staffs = service.getEmployeesHours(year, weekNo);
                staffs.AcceptChanges();

                dataGridStaff.ItemsSource = staffs.AsDataView();
                dataGridStaff.SelectedValuePath = "employee_hours_id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while geting staff table:\n\n" + ex.Message + "\n\n" + ex.StackTrace);
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
                updateFakturaGrid();
                int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
                int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
                int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());
                updateBudget(year, month, weekNo);
                syncStaff();
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
                syncStaff();
                allowSync = true;
                updateFakturaGrid();
                int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
                int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
                int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());
                updateBudget(year, month, weekNo);
            }
        }

        private void cmbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                dataGrid2.SelectedIndex = -1;
                syncStocks();
                updateFakturaGrid();
                int year = Int32.Parse(((ComboBoxItem)cmbYear.SelectedItem).Content.ToString());
                int month = Int32.Parse(((ComboBoxItem)cmbMonth.SelectedItem).Tag.ToString());
                int weekNo = Int32.Parse(cmbWeek.SelectedValue.ToString());
                updateBudget(year, month, weekNo);
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
                decimal supposeTurnover = 0;
                try
                {
                    bttlSold = bottlesSoldDct[product_id] - sValues.totalStock;
                    supposeTurnover = ((decimal.Divide(bttlSold, 5) * 4 * 16) + (decimal.Divide(bttlSold, 5) * 8)) * stocks.Rows[selectedIndex].Field<decimal>("unit_price");
                }
                catch (KeyNotFoundException)
                {
                    lblBottlesSold.Content = "No previous week data found...";
                    lblSupposeTurnover.Content = "";
                    lblBottlesSold.Foreground = Brushes.Red;
                    return;
                }
                lblBottlesSold.Content = bttlSold;
                lblBottlesSold.Foreground = Brushes.Black;
                lblSupposeTurnover.Content = supposeTurnover;
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

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid3.SelectedIndex != -1)
                service.deleteFaktura(Convert.ToInt32(dataGrid3.SelectedValue.ToString()));
            updateFakturaGrid();
        }

        private void addBtn_Click(object sender, RoutedEventArgs e)
        {
            if (combobox1.SelectedIndex != -1)
                service.AddFaktura(combobox1.SelectedIndex + 1, txtbx_serial.Text, Convert.ToDecimal(txtbx_amount.Text));
            updateFakturaGrid();
        }

        private void dataGridStaff_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridStaff.SelectedItem != null)
            {
                lblStatusStaff.Content = "";
                DataRowView dataRowView = ((DataRowView)dataGridStaff.SelectedItem);
                int selectedIndex = dataGridStaff.SelectedIndex;
                string name = staffs.Rows[dataGridStaff.SelectedIndex].Field<string>("name");
                TimeSpan workedHours = staffs.Rows[dataGridStaff.SelectedIndex].Field<TimeSpan>("worked_hours");
                decimal hourlyWage = staffs.Rows[dataGridStaff.SelectedIndex].Field<decimal>("hourly_wage");
                txbName.Text = name;
                txbHours.Text = workedHours.ToString("c");
                txbHourlyWage.Text = hourlyWage.ToString("0.##");
                lblTotalCost.Content = (Decimal.Divide(hourlyWage, 3600) * (decimal)workedHours.TotalSeconds).ToString("0.##") + " kr";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txbName.Text.Trim().Length == 0)
                MessageBox.Show("Please enter name.");
            else
            {
                if (txbHours.Text.Trim().Length == 0)
                    txbHours.Text = "0";
                if (txbHourlyWage.Text.Trim().Length == 0)
                    txbHourlyWage.Text = "0";
                string name = txbName.Text;
                TimeSpan hours = TimeSpan.Parse(txbHours.Text);
                decimal hourlyWage = Decimal.Parse(txbHourlyWage.Text);
                bool created = service.createEmployee(name, hours, hourlyWage);
                if (!created)
                {
                    lblStatusStaff.Content = "Failed to cceate";
                    lblStatusStaff.Foreground = Brushes.Green;
                    return;
                }
                lblStatusStaff.Content = "Created";
                lblStatusStaff.Foreground = Brushes.Green;
                syncStaff();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            int selectedIndex = dataGridStaff.SelectedIndex;
            if (selectedIndex == -1)
            {
                MessageBox.Show("Select an employee first.");
                return;
            }

            if (txbName.Text.Trim().Length == 0)
                MessageBox.Show("Please enter name.");
            else
            {
                if (txbHours.Text.Trim().Length == 0)
                    txbHours.Text = "0";
                if (txbHourlyWage.Text.Trim().Length == 0)
                    txbHourlyWage.Text = "0";
                string name = txbName.Text;
                TimeSpan hours = TimeSpan.Parse(txbHours.Text);
                decimal hourlyWage = Decimal.Parse(txbHourlyWage.Text);
                bool updated = service.updateEmployee(name, hourlyWage, hours, staffs.Rows[dataGridStaff.SelectedIndex].Field<int>("employee_id"), (int)dataGridStaff.SelectedValue);
                if (!updated)
                {
                    lblStatusStaff.Content = "Failed to update";
                    lblStatusStaff.Foreground = Brushes.Green;
                    return;
                }
                lblStatusStaff.Content = "Updated";
                lblStatusStaff.Foreground = Brushes.Green;

                syncStaff();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int selectedIndex = dataGridStaff.SelectedIndex;
            if (selectedIndex == -1)
            {
                MessageBox.Show("Select an employee first.");
                return;
            }
            if (MessageBox.Show("Are you sure you want to delete this employee?", "Question", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            int employee_id = staffs.Rows[selectedIndex].Field<int>("employee_id");

            bool deleted = service.deleteEmployee(employee_id);
            if (!deleted)
            {
                lblStatusStaff.Content = "Failed to delete";
                lblStatusStaff.Foreground = Brushes.Red;
                return;
            }
            lblStatusStaff.Content = "Deleted";
            lblStatusStaff.Foreground = Brushes.Red;
            syncStaff();
        }



        private void validTextBox(TextBox textBox)
        {
            string tString = textBox.Text;
            if (tString.Trim() == "") return;
            for (int i = 0; i < tString.Length; i++)
            {
                if (!char.IsNumber(tString[i]))
                {
                    MessageBox.Show("Please enter a valid number");
                    textBox.Text = "";
                    return;
                }
            }
        }

        private void txtbx_amount_TextChanged(object sender, TextChangedEventArgs e)
        {
            validTextBox((TextBox)sender);
        }

        private void newcompBtn_Click(object sender, RoutedEventArgs e)
        {
            Window1 window = new Window1();
            window.ShowDialog();
            combobox1.ItemsSource = service.getCompanyNames();
        }


    }
}
