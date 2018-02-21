﻿using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace BriefFiniteElementNet.Controls
{
    /// <summary>
    /// Interaction logic for MatrixVisualizerControl.xaml
    /// </summary>
    public partial class MatrixVisualizerControl : UserControl
    {
        public MatrixVisualizerControl()
        {
            InitializeComponent();
        }

        private void DataGrid_OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex()).ToString();
        }

        private void DataGridCell_GotFocus(object sender, RoutedEventArgs e)
        {
            var src = e.Source as DataGridCell;

            //DataGrid.cells src.Column
            //e.Row.Header = (e.Row.GetIndex()).ToString();
        }


        public static void VisualizeInNewWindow(Matrix matrix, string title = "")
        {
            var wnd = new Window() { Title = title };
            var mtxCtrl = new MatrixVisualizerControl();
            mtxCtrl.VisualizeMatrix(matrix);

            wnd.Content = mtxCtrl;

            wnd.Show();
        }

        public static void VisualizeInNewWindow(Matrix matrix, string title ,bool showDialog)
        {
            var wnd = new Window() { Title = title };
            var mtxCtrl = new MatrixVisualizerControl();
            mtxCtrl.VisualizeMatrix(matrix);

            wnd.Content = mtxCtrl;

            if (showDialog)
                wnd.ShowDialog();
            else
                wnd.Show();
        }

        public void VisualizeMatrix(BriefFiniteElementNet.Matrix mtx)
        {
            var tbl = new DataTable();
            target = mtx;

            for (var j = 0; j < mtx.ColumnCount; j++)
            {
                tbl.Columns.Add(j.ToString(), typeof(double));
            }

            for (var i = 0; i < mtx.RowCount; i++)
            {
                tbl.Rows.Add(mtx.ExtractRow(i).CoreArray.Cast<object>().ToArray());
            }

            DataGrid.ItemsSource = tbl.DefaultView;
        }

        private BriefFiniteElementNet.Matrix target;

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();

                for (int i = 0; i < target.RowCount; i++)
                {
                    for (int j = 0; j < target.ColumnCount; j++)
                    {
                        sb.Append(target[i, j]);
                        sb.Append(",");
                    }

                    sb.Append(";");
                }


                Clipboard.SetText(sb.ToString().Replace(",;", ";"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
