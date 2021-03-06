﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Markup;
using System.Windows.Data;
using System.ComponentModel;

namespace CaseControl
{
    public class BillingReportPrinter : DocumentPaginator
    {
        #region Private Members

        private DataGrid _documentSource;
        private Collection<ColumnDefinition> _tableColumnDefinitions;
        private double _avgRowHeight;
        private double _availableHeight;
        private int _rowsPerPage;
        private int _pageCount;

        private string FileID;
        private string FirstName;
        private string LastName;
        private string AssignedAttorney;

        #endregion

        #region Constructor

        public BillingReportPrinter(DataGrid documentSource, string documentTitle, Size pageSize, Thickness pageMargin, string fileID, string firstName, string lastName, string assignedAttorney)
        {
            _tableColumnDefinitions = new Collection<ColumnDefinition>();
            _documentSource = documentSource;

            _tableColumnDefinitions = new Collection<ColumnDefinition>();
            _documentSource = documentSource;

            this.DocumentTitle = documentTitle;
            this.PageSize = pageSize;
            this.PageMargin = pageMargin;
            this.FileID = fileID;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.AssignedAttorney = assignedAttorney;


            if (_documentSource != null)
                MeasureElements();
        }

        #endregion

        #region Public Properties

        #region Styling

        public Style AlternatingRowBorderStyle { get; set; }

        public Style DocumentHeaderTextStyle { get; set; }

        public Style DocumentFooterTextStyle { get; set; }

        public Style TableCellTextStyle { get; set; }

        public Style TableHeaderTextStyle { get; set; }

        public Style TableHeaderBorderStyle { get; set; }

        public Style GridContainerStyle { get; set; }

        #endregion

        public string DocumentTitle { get; set; }

        public Thickness PageMargin { get; set; }

        public override Size PageSize { get; set; }

        public override bool IsPageCountValid
        {
            get { return true; }
        }

        public override int PageCount
        {
            get { return _pageCount; }
        }

        public override IDocumentPaginatorSource Source
        {
            get { return null; }
        }

        #endregion

        #region Public Methods

        public override DocumentPage GetPage(int pageNumber)
        {
            DocumentPage page = null;
            List<object> itemsSource = new List<object>();

            ICollectionView viewSource = CollectionViewSource.GetDefaultView(_documentSource.ItemsSource);

            if (viewSource != null)
            {
                BillingReportViewModel temp = new BillingReportViewModel();
                temp.ItemNo = "=====";
                temp.Date = "====";
                temp.Description = "===========";
                temp.BillingType = "===========";
                temp.GeneralAccountFunds = "===========";
                temp.TrustAccountFunds = "===============";
                temp.TrustAccountFunds = "=========";
                temp.CheckNo = "=========";
                itemsSource.Add(temp);

                foreach (object item in viewSource)
                    itemsSource.Add(item);
            }

            if (itemsSource != null)
            {
                int rowIndex = 1;
                int startPos = pageNumber * _rowsPerPage;
                int endPos = startPos + _rowsPerPage;

                //Create a new grid
                Grid tableGrid = CreateTable(true) as Grid;

                for (int index = startPos; index < endPos && index < itemsSource.Count; index++)
                {
                    Console.WriteLine("Adding: " + index);

                    if (rowIndex > 0)
                    {
                        object item = itemsSource[index];
                        int columnIndex = 0;

                        if (_documentSource.Columns != null)
                        {
                            foreach (DataGridColumn column in _documentSource.Columns)
                            {
                                if (column.Visibility == Visibility.Visible)
                                {
                                    AddTableCell(tableGrid, column, item, columnIndex, rowIndex);
                                    columnIndex++;
                                }
                            }
                        }

                        if (this.AlternatingRowBorderStyle != null && rowIndex % 2 == 0)
                        {
                            Border alernatingRowBorder = new Border();

                            alernatingRowBorder.Style = this.AlternatingRowBorderStyle;
                            alernatingRowBorder.SetValue(Grid.RowProperty, rowIndex);
                            alernatingRowBorder.SetValue(Grid.ColumnSpanProperty, columnIndex);
                            alernatingRowBorder.SetValue(Grid.ZIndexProperty, -1);
                            tableGrid.Children.Add(alernatingRowBorder);
                        }
                    }

                    rowIndex++;
                }

                page = ConstructPage(tableGrid, pageNumber);
            }

            return page;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This function measures the heights of the page header, page footer and grid header and the first row in the grid
        /// in order to work out how manage pages might be required.
        /// </summary>
        private void MeasureElements()
        {
            double allocatedSpace = 0;

            //Measure the page header
            ContentControl pageHeader = new ContentControl();
            pageHeader.Content = CreateDocumentHeader();
            allocatedSpace = MeasureHeight(pageHeader);

            //Measure the page footer
            ContentControl pageFooter = new ContentControl();
            pageFooter.Content = CreateDocumentFooter(0);
            allocatedSpace += MeasureHeight(pageFooter);

            //Measure the table header
            ContentControl tableHeader = new ContentControl();
            tableHeader.Content = CreateTable(false);
            allocatedSpace += MeasureHeight(tableHeader);

            //Include any margins
            allocatedSpace += this.PageMargin.Bottom + this.PageMargin.Top;

            //Work out how much space we need to display the grid
            _availableHeight = this.PageSize.Height - allocatedSpace;

            //Calculate the height of the first row
            _avgRowHeight = MeasureHeight(CreateTempRow());

            //Calculate how many rows we can fit on each page
            double rowsPerPage = Math.Floor(_availableHeight / _avgRowHeight);

            if (!double.IsInfinity(rowsPerPage))
                _rowsPerPage = Convert.ToInt32(rowsPerPage);

            //Count the rows in the document source
            double rowCount = CountRows(_documentSource.ItemsSource);

            //Calculate the nuber of pages that we will need
            if (rowCount > 0)
                _pageCount = Convert.ToInt32(Math.Ceiling(rowCount / rowsPerPage));
        }

        /// <summary>
        /// This method constructs the document page (visual) to print
        /// </summary>
        private DocumentPage ConstructPage(Grid content, int pageNumber)
        {
            if (content == null)
                return null;

            //Build the page inc header and footer
            Grid pageGrid = new Grid();

            //Header row
            AddGridRow(pageGrid, GridLength.Auto);

            //Content row
            //AddGridRow(pageGrid, new GridLength(1.0d, GridUnitType.Auto));
            AddGridRow(pageGrid, new GridLength(1.0d, GridUnitType.Star));

            //Footer row
            AddGridRow(pageGrid, GridLength.Auto);

            ContentControl pageHeader = new ContentControl();
            pageHeader.Content = this.CreateDocumentHeader();
            pageGrid.Children.Add(pageHeader);

            if (content != null)
            {
                content.SetValue(Grid.RowProperty, 1);
                pageGrid.Children.Add(content);
            }

            ContentControl pageFooter = new ContentControl();
            pageFooter.Content = CreateDocumentFooter(pageNumber + 1);
            pageFooter.SetValue(Grid.RowProperty, 2);

            pageGrid.Children.Add(pageFooter);

            double width = this.PageSize.Width - (this.PageMargin.Left + this.PageMargin.Right);
            double height = this.PageSize.Height - (this.PageMargin.Top + this.PageMargin.Bottom);

            pageGrid.Measure(new Size(width, height));
            pageGrid.Arrange(new Rect(this.PageMargin.Left, this.PageMargin.Top, width, height));

            return new DocumentPage(pageGrid);
        }

        /// <summary>
        /// Creates a default header for the document; containing the doc title
        /// </summary>
        private object CreateDocumentHeader()
        {
            Border headerBorder = new Border();
            TextBlock titleText = new TextBlock();
            titleText.Style = this.DocumentHeaderTextStyle;
            titleText.TextTrimming = TextTrimming.CharacterEllipsis;
            titleText.Text = this.DocumentTitle;
            titleText.HorizontalAlignment = HorizontalAlignment.Center;
            titleText.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            titleText.FontSize = 15.0;

            headerBorder.Margin = new Thickness(0, 0, 0, 0);
            headerBorder.Child = titleText;

            Grid grid = new Grid();

            grid.Children.Add(headerBorder);

            Label lblFirstName = new Label();
            lblFirstName.Style = this.DocumentHeaderTextStyle;
            lblFirstName.Content = "First Name:";
            lblFirstName.HorizontalAlignment = HorizontalAlignment.Left;
            lblFirstName.VerticalAlignment = VerticalAlignment.Center;

            lblFirstName.Margin = new Thickness(20, 0, 0, 0);

            grid.Children.Add(lblFirstName);

            TextBlock firstName = new TextBlock();
            firstName.Style = this.DocumentHeaderTextStyle;
            firstName.TextTrimming = TextTrimming.CharacterEllipsis;
            //firstName.Text = this.FirstName;
            firstName.Text = this.FirstName;
            firstName.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            firstName.HorizontalAlignment = HorizontalAlignment.Left;
            firstName.VerticalAlignment = VerticalAlignment.Center;

            firstName.Margin = new Thickness(90, 0, 0, 0);
            grid.Children.Add(firstName);

            Label lblLastName = new Label();
            lblLastName.Style = this.DocumentHeaderTextStyle;
            lblLastName.Content = "Last Name:";
            lblLastName.HorizontalAlignment = HorizontalAlignment.Left;
            lblLastName.VerticalAlignment = VerticalAlignment.Center;

            lblLastName.Margin = new Thickness(400, 0, 0, 0);

            grid.Children.Add(lblLastName);

            TextBlock lastName = new TextBlock();
            lastName.Style = this.DocumentHeaderTextStyle;
            lastName.TextTrimming = TextTrimming.CharacterEllipsis;
            //lastName.Text = this.LastName;
            lastName.Text = this.LastName;
            lastName.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            lastName.HorizontalAlignment = HorizontalAlignment.Left;
            lastName.VerticalAlignment = VerticalAlignment.Center;

            lastName.Margin = new Thickness(480, 0, 80, 0);
            grid.Children.Add(lastName);

            Label lblFileID = new Label();
            lblFileID.Style = this.DocumentHeaderTextStyle;
            lblFileID.Content = "File No.:";
            lblFileID.HorizontalAlignment = HorizontalAlignment.Left;
            lblFileID.VerticalAlignment = VerticalAlignment.Center;

            lblFileID.Margin = new Thickness(20, 30, 0, 0);

            grid.Children.Add(lblFileID);

            TextBlock fileID = new TextBlock();
            fileID.Style = this.DocumentHeaderTextStyle;
            fileID.TextTrimming = TextTrimming.CharacterEllipsis;
            //fileIDme.Text = this.FirstName;
            fileID.Text = this.FileID;
            fileID.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            fileID.HorizontalAlignment = HorizontalAlignment.Left;
            fileID.VerticalAlignment = VerticalAlignment.Center;

            fileID.Margin = new Thickness(90, 30, 0, 0);
            grid.Children.Add(fileID);

            Label lblAssignedAttorney = new Label();
            lblAssignedAttorney.Style = this.DocumentHeaderTextStyle;
            lblAssignedAttorney.Content = "Assigned Attorney:";
            lblAssignedAttorney.HorizontalAlignment = HorizontalAlignment.Left;
            lblAssignedAttorney.VerticalAlignment = VerticalAlignment.Center;

            lblAssignedAttorney.Margin = new Thickness(380, 30, 0, 0);

            grid.Children.Add(lblAssignedAttorney);

            TextBlock assignedAttorney = new TextBlock();
            assignedAttorney.Style = this.DocumentHeaderTextStyle;
            assignedAttorney.TextTrimming = TextTrimming.CharacterEllipsis;
            //assignedAttorney.Text = this.LastName;
            assignedAttorney.Text = this.AssignedAttorney;
            assignedAttorney.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            assignedAttorney.HorizontalAlignment = HorizontalAlignment.Left;
            assignedAttorney.VerticalAlignment = VerticalAlignment.Center;

            assignedAttorney.Margin = new Thickness(500, 30, 0, 0);
            grid.Children.Add(assignedAttorney);

            grid.Height = 100;
            return grid;
        }

        /// <summary>
        /// Creates a default page footer consisting of datetime and page number
        /// </summary>
        private object CreateDocumentFooter(int pageNumber)
        {
            Grid footerGrid = new Grid();
            footerGrid.Margin = new Thickness(0, 10, 0, 0);

            ColumnDefinition colDefinition = new ColumnDefinition();
            colDefinition.Width = new GridLength(0.5d, GridUnitType.Star);

            TextBlock dateTimeText = new TextBlock();
            dateTimeText.Style = this.DocumentFooterTextStyle;
            dateTimeText.Text = DateTime.Now.ToString("dd-MMM-yyy HH:mm");

            footerGrid.Children.Add(dateTimeText);

            TextBlock pageNumberText = new TextBlock();
            pageNumberText.Style = this.DocumentFooterTextStyle;
            pageNumberText.Text = "Page " + pageNumber.ToString() + " of " + this.PageCount.ToString();
            pageNumberText.SetValue(Grid.ColumnProperty, 1);
            pageNumberText.HorizontalAlignment = HorizontalAlignment.Right;

            footerGrid.Children.Add(pageNumberText);

            return footerGrid;
        }

        /// <summary>
        /// Counts the number of rows in the document source
        /// </summary>
        /// <param name="itemsSource"></param>
        /// <returns></returns>
        private double CountRows(IEnumerable itemsSource)
        {
            int count = 0;

            if (itemsSource != null)
            {
                foreach (object item in itemsSource)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// The following function creates a temp table with a single row so that it can be measured and used to 
        /// calculate the totla number of req'd pages
        /// </summary>
        /// <returns></returns>
        private Grid CreateTempRow()
        {
            Grid tableRow = new Grid();

            if (_documentSource != null)
            {
                foreach (ColumnDefinition colDefinition in _tableColumnDefinitions)
                {
                    ColumnDefinition copy = XamlReader.Parse(XamlWriter.Save(colDefinition)) as ColumnDefinition;
                    tableRow.ColumnDefinitions.Add(copy);
                }

                foreach (object item in _documentSource.ItemsSource)
                {
                    int columnIndex = 0;
                    if (_documentSource.Columns != null)
                    {
                        foreach (DataGridColumn column in _documentSource.Columns)
                        {
                            if (column.Visibility == Visibility.Visible)
                            {
                                AddTableCell(tableRow, column, item, columnIndex, 0);
                                columnIndex++;
                            }
                        }
                    }

                    //We only want to measure teh first row
                    break;
                }
            }

            return tableRow;
        }

        /// <summary>
        /// This function counts the number of rows in the document
        /// </summary>
        private object CreateTable(bool createRowDefinitions)
        {
            if (_documentSource == null)
                return null;

            Grid table = new Grid();
            table.Style = this.GridContainerStyle;

            int columnIndex = 0;


            if (_documentSource.Columns != null)
            {
                double totalColumnWidth = _documentSource.Columns.Sum(column => column.Visibility == Visibility.Visible ? column.Width.Value : 0);

                foreach (DataGridColumn column in _documentSource.Columns)
                {
                    if (column.Visibility == Visibility.Visible)
                    {
                        AddTableColumn(table, totalColumnWidth, columnIndex, column);
                        columnIndex++;
                    }
                }
            }

            if (this.TableHeaderBorderStyle != null)
            {
                Border headerBackground = new Border();
                headerBackground.Style = this.TableHeaderBorderStyle;
                headerBackground.SetValue(Grid.ColumnSpanProperty, columnIndex);
                headerBackground.SetValue(Grid.ZIndexProperty, -1);

                table.Children.Add(headerBackground);
            }

            if (createRowDefinitions)
            {
                for (int i = 0; i <= _rowsPerPage; i++)
                    //table.RowDefinitions.Add(new RowDefinition());
                    table.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            }

            return table;

        }

        /// <summary>
        /// Measures the height of an element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private double MeasureHeight(FrameworkElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            element.Measure(this.PageSize);
            return element.DesiredSize.Height;
        }

        /// <summary>
        /// Adds a column to a grid
        /// </summary>
        /// <param name="grid">Grid to add the column to</param>
        /// <param name="columnIndex">Index of the column</param>
        /// <param name="column">Source column defintition which will be used to calculate the width of the column</param>
        private void AddTableColumn(Grid grid, double totalColumnWidth, int columnIndex, DataGridColumn column)
        {
            double proportion = column.Width.Value / (this.PageSize.Width - (this.PageMargin.Left + this.PageMargin.Right));

            ColumnDefinition colDefinition = new ColumnDefinition();
            TextBlock text = new TextBlock();
            text.Padding = new Thickness(5);

            if (columnIndex != 2)
            {
                colDefinition.Width = new GridLength(proportion, GridUnitType.Auto);
            }
            else
            {
                text.TextWrapping = TextWrapping.Wrap;
                colDefinition.Width = new GridLength(proportion, GridUnitType.Star);
            }
            grid.ColumnDefinitions.Add(colDefinition);

            //text.Style = this.TableHeaderTextStyle;
            text.TextTrimming = TextTrimming.CharacterEllipsis;
            text.Text = column.Header.ToString(); ;
            text.SetValue(Grid.ColumnProperty, columnIndex);

            grid.Children.Add(text);
            _tableColumnDefinitions.Add(colDefinition);
        }

        /// <summary>
        /// Adds a cell to a grid
        /// </summary>
        /// <param name="grid">Grid to add teh cell to</param>
        /// <param name="column">Source column definition which contains binding info</param>
        /// <param name="item">The binding source</param>
        /// <param name="columnIndex">Column index</param>
        /// <param name="rowIndex">Row index</param>
        private void AddTableCell(Grid grid, DataGridColumn column, object item, int columnIndex, int rowIndex)
        {
            if (column is DataGridTemplateColumn)
            {
                DataGridTemplateColumn templateColumn = column as DataGridTemplateColumn;
                ContentControl contentControl = new ContentControl();

                contentControl.Focusable = true;
                contentControl.ContentTemplate = templateColumn.CellTemplate;
                contentControl.Content = item;

                contentControl.SetValue(Grid.ColumnProperty, columnIndex);
                contentControl.SetValue(Grid.RowProperty, rowIndex);

                grid.Children.Add(contentControl);
            }
            else if (column is DataGridTextColumn)
            {
                DataGridTextColumn textColumn = column as DataGridTextColumn;
                TextBlock text = new TextBlock { Text = "Text" };
                text.Padding = new Thickness(5);

                //text.Style = this.TableCellTextStyle;

                text.TextTrimming = TextTrimming.CharacterEllipsis;
                text.DataContext = item;

                Binding binding = textColumn.Binding as Binding;

  
                //if (!string.IsNullOrEmpty(column.DisplayFormat))
                //binding.StringFormat = column.DisplayFormat;

                text.SetBinding(TextBlock.TextProperty, binding);

                //If General Account Fund or Trust Account Fund make right

                if (columnIndex == 4)
                {
                    text.TextAlignment = TextAlignment.Right;
                    text.Text += "     ";
                }
                if (columnIndex == 5)
                {
                    text.TextAlignment = TextAlignment.Right;
                    text.Text += "      ";
                }

                // If General Account Fund or Trust Account Fund make Center
                if (rowIndex == 0)
                {
                    text.TextAlignment = TextAlignment.Center;
                    text.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
                }
                if (columnIndex == 2 && rowIndex > 1)
                {
                    //text.Text = string.Empty;
                    //for (int i = 0; i < 300; i++)
                    //{
                    //    text.Text += i.ToString();
                    //}
                    //string[] splitter = new string[] { "\r\n" };
                    //string[] splitMessage = text.Text.Split(splitter, StringSplitOptions.None);
                    //text.Text = string.Empty;

                    //for (int index = 0; index < splitMessage.Length; index++)
                    //{
                    //    if (index > 0)
                    //    {
                    //        text.Text += Constants.MULTI_LINE_SEPARATOR;
                    //    }
                    //        text.Text += splitMessage[index];
                    //}
                }

                text.SetValue(Grid.ColumnProperty, columnIndex);
                text.SetValue(Grid.RowProperty, rowIndex);
                grid.Children.Add(text);
            }
        }

        /// <summary>
        /// Adds a row to a grid
        /// </summary>
        private void AddGridRow(Grid grid, GridLength rowHeight)
        {
            if (grid == null)
                return;

            RowDefinition rowDef = new RowDefinition();

            if (rowHeight != null)
                rowDef.Height = rowHeight;

            grid.RowDefinitions.Add(rowDef);
        }

        #endregion

    }
}
