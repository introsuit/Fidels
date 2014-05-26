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

namespace Fidels
{
    public partial class CreateProduct : Window
    {
        private Service service = Service.getInstance();

        public CreateProduct()
        {
            InitializeComponent();
            cmbProductGroup.ItemsSource = service.updateComboBox();
            cmbProductGroup.SelectedIndex = 0;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (txbName.Text.Trim().Length == 0)
                MessageBox.Show("Please enter name.");
            else
            {
                if (txbPrice.Text.Trim().Length == 0)
                    txbPrice.Text = "0";
                if (txbOfficeStock.Text.Trim().Length == 0)
                    txbOfficeStock.Text = "0";
                if (txbDisplay.Text.Trim().Length == 0)
                    txbDisplay.Text = "0";
                if (txbSpeedRail.Text.Trim().Length == 0)
                    txbSpeedRail.Text = "0";
                if (txbBarStock.Text.Trim().Length == 0)
                    txbBarStock.Text = "0";
                if (txbMinimumStock.Text.Trim().Length == 0)
                    txbMinimumStock.Text = "0";
                DialogResult = true;
                service.createProduct(txbName.Text, cmbProductGroup.SelectedIndex + 1);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            validTextBox((TextBox)sender);
        }
    }
}
