using System;
using System.Drawing;
using System.Windows.Forms;
using SportClubApp.Models;
using SportClubApp.Services;

namespace SportClubApp.Forms
{
    public sealed class ProductEditForm : Form
    {
        private readonly CatalogService _catalogService = new CatalogService();
        private readonly TextBox _name = new TextBox();
        private readonly TextBox _cat = new TextBox();
        private readonly NumericUpDown _price = new NumericUpDown();
        private readonly NumericUpDown _oldPrice = new NumericUpDown();
        private readonly NumericUpDown _disc = new NumericUpDown();
        private readonly NumericUpDown _stock = new NumericUpDown();
        private readonly TextBox _image = new TextBox();

        public ProductItem Product { get; }

        public ProductEditForm(ProductItem item = null)
        {
            Product = item ?? new ProductItem();
            Text = "Товар / услуга";
            Width = 620;
            Height = 460;
            StartPosition = FormStartPosition.CenterParent;
            Icon = SystemIcons.Information;
            BackColor = Color.White;

            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8, Padding = new Padding(16) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _name.MaxLength = 150;
            _cat.MaxLength = 80;

            AddRow(grid, "Название", _name, 0);
            AddRow(grid, "Категория", _cat, 1);
            _price.Maximum = 1000000; _price.DecimalPlaces = 2; AddRow(grid, "Цена", _price, 2);
            _oldPrice.Maximum = 1000000; _oldPrice.DecimalPlaces = 2; AddRow(grid, "Старая цена", _oldPrice, 3);
            _disc.Maximum = 100; AddRow(grid, "% скидки", _disc, 4);
            _stock.Maximum = 100000; AddRow(grid, "Остаток", _stock, 5);

            var imgPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            _image.Width = 260; _image.MaxLength = 260;
            var browse = new Button { Text = "Выбрать...", Height = 30 };
            browse.Click += (_, __) => SelectImage();
            imgPanel.Controls.Add(_image); imgPanel.Controls.Add(browse);
            AddRow(grid, "Изображение", imgPanel, 6);

            var save = new Button { Text = "Сохранить", Dock = DockStyle.Fill, Height = 36, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            save.Click += (_, __) => SaveAndClose();
            AddRow(grid, "", save, 7);

            Controls.Add(grid);
            Bind();
        }

        private static void AddRow(TableLayoutPanel grid, string label, Control control, int row)
        {
            grid.Controls.Add(new Label { Text = label, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            control.Dock = DockStyle.Fill;
            grid.Controls.Add(control, 1, row);
        }

        private void Bind()
        {
            _name.Text = Product.Name;
            _cat.Text = Product.Category;
            _price.Value = Product.Price;
            _oldPrice.Value = Product.OldPrice ?? 0;
            _disc.Value = Product.DiscountPercent ?? 0;
            _stock.Value = Product.StockQty;
            _image.Text = Product.ImagePath;
        }

        private void SelectImage()
        {
            using (var d = new OpenFileDialog())
            {
                d.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp";
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    _image.Text = _catalogService.CopyImageToProject(d.FileName);
                }
            }
        }

        private void SaveAndClose()
        {
            Product.Name = _name.Text.Trim();
            Product.Category = _cat.Text.Trim();
            Product.Price = _price.Value;
            Product.OldPrice = _oldPrice.Value > 0 ? _oldPrice.Value : (decimal?)null;
            Product.DiscountPercent = _disc.Value > 0 ? (int)_disc.Value : (int?)null;
            Product.StockQty = (int)_stock.Value;
            Product.ImagePath = string.IsNullOrWhiteSpace(_image.Text) ? null : _image.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
