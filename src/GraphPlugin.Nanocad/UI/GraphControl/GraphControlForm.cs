using System.Drawing;
using System.Windows.Forms;
using GraphPlugin.Domain.Models;

namespace GraphPlugin.Nanocad.UI.GraphControl;

public sealed class GraphControlForm : Form
{
    private readonly ComboBox _colorComboBox;
    private readonly ComboBox _lineTypeComboBox;
    private readonly NumericUpDown _lineWeightInput;
    private readonly Label _statusLabel;
    private readonly Label _statisticsLabel;
    private readonly Action<EdgeStyle> _applyEdgeStyle;
    private readonly Action<GraphControlAction> _executeAction;
    private readonly Func<(int VertexCount, int EdgeCount)> _getStatistics;

    public GraphControlForm(
        EdgeStyle currentStyle,
        Action<EdgeStyle> applyEdgeStyle,
        Action<GraphControlAction> executeAction,
        Func<(int VertexCount, int EdgeCount)> getStatistics
    )
    {
        ArgumentNullException.ThrowIfNull(currentStyle);
        ArgumentNullException.ThrowIfNull(applyEdgeStyle);
        ArgumentNullException.ThrowIfNull(executeAction);
        ArgumentNullException.ThrowIfNull(getStatistics);

        _applyEdgeStyle = applyEdgeStyle;
        _executeAction = executeAction;
        _getStatistics = getStatistics;

        Text = "GraphPlugin — Control Center";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        ClientSize = new Size(620, 760);
        MinimumSize = new Size(620, 680);
        Font = new Font("Segoe UI", 9F);
        Padding = new Padding(12);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var i = 0; i < root.RowCount; i++)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "Управление текущим DWG-графом",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 12),
        };

        var graphGroup = CreateGraphGroup();
        var styleGroup = CreateStyleGroup(currentStyle);

        _statusLabel = new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = " ",
            Margin = new Padding(3, 10, 3, 8),
        };

        var closePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0),
        };

        var closeButton = new Button
        {
            Text = "Закрыть",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 7, 14, 7),
            Margin = new Padding(0),
        };
        closeButton.Click += (_, _) => Close();
        closePanel.Controls.Add(closeButton);

        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(graphGroup, 0, 1);
        root.Controls.Add(styleGroup, 0, 2);
        root.Controls.Add(_statusLabel, 0, 3);
        root.Controls.Add(closePanel, 0, 4);

        Controls.Add(root);

        _statisticsLabel = FindRequiredControl<Label>(graphGroup, "StatisticsLabel");
        _colorComboBox = FindRequiredControl<ComboBox>(styleGroup, "ColorComboBox");
        _lineTypeComboBox = FindRequiredControl<ComboBox>(styleGroup, "LineTypeComboBox");
        _lineWeightInput = FindRequiredControl<NumericUpDown>(styleGroup, "LineWeightInput");

        _colorComboBox.DataSource = Enum.GetValues<GraphColor>();
        _colorComboBox.SelectedItem = currentStyle.Color;

        _lineTypeComboBox.DataSource = Enum.GetValues<EdgeLineType>();
        _lineTypeComboBox.SelectedItem = currentStyle.LineType;

        var clampedWeight = Math.Clamp(currentStyle.LineWeightMm, 0.01, 10.0);
        _lineWeightInput.Value = (decimal)clampedWeight;

        Shown += (_, _) => RefreshStatistics();
        Activated += (_, _) => RefreshStatistics();
    }

    private GroupBox CreateGraphGroup()
    {
        var group = new GroupBox
        {
            Text = "Граф",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 12),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 7,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        for (var i = 0; i < layout.RowCount; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var statisticsLabel = new Label
        {
            Name = "StatisticsLabel",
            AutoSize = true,
            Text = "Вершин: —     Рёбер: —",
            Margin = new Padding(5, 2, 5, 10),
        };
        layout.SetColumnSpan(statisticsLabel, 2);
        layout.Controls.Add(statisticsLabel, 0, 0);

        layout.Controls.Add(CreateActionButton("Создать вершину", GraphControlAction.CreateVertex), 0, 1);
        layout.Controls.Add(CreateActionButton("Создать ребро", GraphControlAction.CreateEdge), 1, 1);
        layout.Controls.Add(CreateActionButton("Построить граф", GraphControlAction.BuildGraph), 0, 2);
        layout.Controls.Add(CreateActionButton("Разбить ребро", GraphControlAction.SplitEdge), 1, 2);
        layout.Controls.Add(CreateActionButton("Добавить изгиб", GraphControlAction.AddBend), 0, 3);
        layout.Controls.Add(CreateActionButton("Кратчайший путь", GraphControlAction.ShortestPath), 1, 3);
        layout.Controls.Add(CreateActionButton("Прикрепить файл", GraphControlAction.AttachFile), 0, 4);
        layout.Controls.Add(CreateActionButton("Открепить файл", GraphControlAction.DetachFile), 1, 4);
        layout.Controls.Add(CreateActionButton("Открыть файл", GraphControlAction.OpenFile), 0, 5);
        layout.Controls.Add(CreateActionButton("Список файлов", GraphControlAction.ListFiles), 1, 5);

        var clearPathButton = CreateActionButton("Очистить кратчайший путь", GraphControlAction.ClearShortestPath);
        layout.SetColumnSpan(clearPathButton, 2);
        layout.Controls.Add(clearPathButton, 0, 6);

        group.Controls.Add(layout);

        return group;
    }

    private GroupBox CreateStyleGroup(EdgeStyle currentStyle)
    {
        var group = new GroupBox
        {
            Text = "Стиль рёбер",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 4,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var i = 0; i < layout.RowCount; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var colorComboBox = new ComboBox
        {
            Name = "ColorComboBox",
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(5),
        };

        var lineTypeComboBox = new ComboBox
        {
            Name = "LineTypeComboBox",
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(5),
        };

        var lineWeightInput = new NumericUpDown
        {
            Name = "LineWeightInput",
            Dock = DockStyle.Fill,
            DecimalPlaces = 2,
            Increment = 0.05M,
            Minimum = 0.01M,
            Maximum = 10M,
            Value = (decimal)Math.Clamp(currentStyle.LineWeightMm, 0.01, 10.0),
            Margin = new Padding(5),
        };

        layout.Controls.Add(CreateFieldLabel("Цвет"), 0, 0);
        layout.Controls.Add(colorComboBox, 1, 0);
        layout.Controls.Add(CreateFieldLabel("Тип линии"), 0, 1);
        layout.Controls.Add(lineTypeComboBox, 1, 1);
        layout.Controls.Add(CreateFieldLabel("Толщина, мм"), 0, 2);
        layout.Controls.Add(lineWeightInput, 1, 2);

        var applyButton = new Button
        {
            Text = "Применить стиль",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 44),
            Padding = new Padding(8, 7, 8, 7),
            Margin = new Padding(5, 8, 5, 4),
        };
        applyButton.Click += ApplyStyle;

        layout.SetColumnSpan(applyButton, 2);
        layout.Controls.Add(applyButton, 0, 3);

        group.Controls.Add(layout);

        return group;
    }

    private Button CreateActionButton(string text, GraphControlAction action)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 44),
            Padding = new Padding(8, 7, 8, 7),
            Margin = new Padding(5),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        button.Click += (_, _) => ExecuteAction(action);

        return button;
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(5),
        };
    }

    private void ExecuteAction(GraphControlAction action)
    {
        try
        {
            _executeAction(action);
            _statusLabel.ForeColor = SystemColors.GrayText;
            _statusLabel.Text = "Команда передана в nanoCAD.";
        }
        catch (Exception exception)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = "Не удалось запустить команду: " + exception.Message;
        }
    }

    private void ApplyStyle(object? sender, EventArgs eventArgs)
    {
        if (_colorComboBox.SelectedItem is not GraphColor color)
            return;

        if (_lineTypeComboBox.SelectedItem is not EdgeLineType lineType)
            return;

        try
        {
            var style = new EdgeStyle(color, lineType, (double)_lineWeightInput.Value);

            _applyEdgeStyle(style);

            _statusLabel.ForeColor = SystemColors.GrayText;
            _statusLabel.Text = "Стиль рёбер применён.";
        }
        catch (Exception exception)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = "Не удалось применить стиль: " + exception.Message;
        }
    }

    private void RefreshStatistics()
    {
        try
        {
            var statistics = _getStatistics();
            _statisticsLabel.Text = $"Вершин: {statistics.VertexCount}     Рёбер: {statistics.EdgeCount}";
        }
        catch
        {
            _statisticsLabel.Text = "Вершин: —     Рёбер: —";
        }
    }

    private static T FindRequiredControl<T>(Control root, string name)
        where T : Control
    {
        var matches = root.Controls.Find(name, true);

        if (matches.Length == 0 || matches[0] is not T control)
            throw new InvalidOperationException($"Control '{name}' was not found.");

        return control;
    }
}
