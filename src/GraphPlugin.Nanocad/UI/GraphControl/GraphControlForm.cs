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
    private readonly Action<EdgeStyle> _applyEdgeStyle;

    public GraphControlAction SelectedAction { get; private set; } = GraphControlAction.None;

    public GraphControlForm(
        int vertexCount,
        int edgeCount,
        EdgeStyle currentStyle,
        Action<EdgeStyle> applyEdgeStyle
    )
    {
        ArgumentNullException.ThrowIfNull(currentStyle);
        ArgumentNullException.ThrowIfNull(applyEdgeStyle);

        _applyEdgeStyle = applyEdgeStyle;

        Text = "GraphPlugin — Control Center";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(540, 590);
        MinimumSize = Size;
        MaximumSize = Size;
        Font = new Font("Segoe UI", 9F);
        Padding = new Padding(12);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "Управление текущим DWG-графом",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 10),
        };

        var graphGroup = CreateGraphGroup(vertexCount, edgeCount);
        var styleGroup = CreateStyleGroup(currentStyle);

        _statusLabel = new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = " ",
            Margin = new Padding(3, 8, 3, 8),
        };

        var closeButton = new Button
        {
            Text = "Закрыть",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            DialogResult = DialogResult.Cancel,
            Padding = new Padding(12, 4, 12, 4),
        };

        CancelButton = closeButton;

        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(graphGroup, 0, 1);
        root.Controls.Add(styleGroup, 0, 2);
        root.Controls.Add(_statusLabel, 0, 3);
        root.Controls.Add(closeButton, 0, 4);

        Controls.Add(root);

        _colorComboBox = FindRequiredControl<ComboBox>(styleGroup, "ColorComboBox");
        _lineTypeComboBox = FindRequiredControl<ComboBox>(styleGroup, "LineTypeComboBox");
        _lineWeightInput = FindRequiredControl<NumericUpDown>(styleGroup, "LineWeightInput");

        _colorComboBox.DataSource = Enum.GetValues<GraphColor>();
        _colorComboBox.SelectedItem = currentStyle.Color;

        _lineTypeComboBox.DataSource = Enum.GetValues<EdgeLineType>();
        _lineTypeComboBox.SelectedItem = currentStyle.LineType;

        var clampedWeight = Math.Clamp(currentStyle.LineWeightMm, 0.01, 10.0);
        _lineWeightInput.Value = (decimal)clampedWeight;
    }

    private GroupBox CreateGraphGroup(int vertexCount, int edgeCount)
    {
        var group = new GroupBox
        {
            Text = "Граф",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

        var statsLabel = new Label
        {
            AutoSize = true,
            Text = $"Вершин: {vertexCount}     Рёбер: {edgeCount}",
            Margin = new Padding(3, 0, 3, 10),
        };
        layout.SetColumnSpan(statsLabel, 2);
        layout.Controls.Add(statsLabel, 0, 0);

        layout.Controls.Add(CreateActionButton("Создать вершину", GraphControlAction.CreateVertex), 0, 1);
        layout.Controls.Add(CreateActionButton("Создать ребро", GraphControlAction.CreateEdge), 1, 1);
        layout.Controls.Add(CreateActionButton("Построить граф", GraphControlAction.BuildGraph), 0, 2);
        layout.Controls.Add(CreateActionButton("Разбить ребро", GraphControlAction.SplitEdge), 1, 2);
        layout.Controls.Add(CreateActionButton("Добавить изгиб", GraphControlAction.AddBend), 0, 3);
        layout.Controls.Add(CreateActionButton("Кратчайший путь", GraphControlAction.ShortestPath), 1, 3);

        var clearPathButton = CreateActionButton("Очистить кратчайший путь", GraphControlAction.ClearShortestPath);
        layout.SetColumnSpan(clearPathButton, 2);
        layout.Controls.Add(clearPathButton, 0, 4);

        group.Controls.Add(layout);

        return group;
    }

    private GroupBox CreateStyleGroup(EdgeStyle currentStyle)
    {
        var group = new GroupBox
        {
            Text = "Стиль рёбер",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(0),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        var colorComboBox = new ComboBox
        {
            Name = "ColorComboBox",
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };

        var lineTypeComboBox = new ComboBox
        {
            Name = "LineTypeComboBox",
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
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
            Margin = new Padding(3, 6, 3, 0),
            TextAlign = ContentAlignment.MiddleCenter,
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
            Margin = new Padding(6, 4, 6, 4),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = true,
        };

        button.Click += (_, _) => SelectAction(action);

        return button;
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Text = text,
            Anchor = AnchorStyles.Left,
        };
    }

    private void SelectAction(GraphControlAction action)
    {
        SelectedAction = action;
        DialogResult = DialogResult.OK;
        Close();
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

    private static T FindRequiredControl<T>(Control root, string name)
        where T : Control
    {
        var matches = root.Controls.Find(name, true);

        if (matches.Length == 0 || matches[0] is not T control)
            throw new InvalidOperationException($"Control '{name}' was not found.");

        return control;
    }
}
